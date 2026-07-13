extends RefCounted

const CLASSES = ["FLECK", "HAND", "MIRROR", "TEETH", "SWARM", "WEATHER", "BURROWER"]
const APPETITES = ["attention", "data", "money", "trust", "obedience", "labour", "care", "time"]

static func analyze(intake: Dictionary) -> Dictionary:
	var raw = _joined_text(intake)
	var specimen_class = _classify(raw, intake)
	var appetite = _appetite(raw)
	var promise = _clean(String(intake.get("promise", intake.get("idea", ""))))
	var audience = _clean(String(intake.get("audience", "people")))
	var payer = _clean(String(intake.get("payer", "not yet known")))
	var burden = _burden(intake, appetite)
	var risk = _risk_score(intake, raw)
	var evidence = _evidence_score(String(intake.get("evidence", "")))
	var feasibility = _feasibility_score(intake)
	var viability = _viability_score(intake)
	var desirability = clamp(0.32 + evidence * 0.55 + (0.1 if audience.length() > 5 else 0.0), 0.0, 1.0)
	var safety = clamp(1.0 - risk, 0.0, 1.0)
	var title = _clean(String(intake.get("title", "Untitled Idea")))
	return {
		"title": title,
		"idea": _clean(String(intake.get("idea", title))),
		"promise": promise,
		"audience": audience,
		"payer": payer,
		"problem": _clean(String(intake.get("problem", "an unverified problem"))),
		"evidence_note": _clean(String(intake.get("evidence", "no direct evidence yet"))),
		"dependency": _clean(String(intake.get("dependency", appetite))),
		"harm": _clean(String(intake.get("harm", "the hidden cost is still unknown"))),
		"maintenance": _clean(String(intake.get("maintenance", burden))),
		"class": specimen_class,
		"appetite": appetite,
		"burden": burden,
		"creature_name": _creature_name(title, specimen_class, appetite),
		"board_class": _board_class(intake, raw),
		"metrics": {
			"desirability": desirability,
			"feasibility": feasibility,
			"viability": viability,
			"safety": safety,
			"evidence": evidence
		},
		"tests": _tests(intake, appetite),
		"assumptions": _assumptions(intake),
		"guardrails": [],
		"revisions": [],
		"decision": "UNDECIDED"
	}

static func apply_test(profile: Dictionary, test_id: String, strength: int, note: String) -> Dictionary:
	var updated = profile.duplicate(true)
	var metrics: Dictionary = updated.get("metrics", {}).duplicate(true)
	var normalized = clamp(float(strength) / 3.0, 0.0, 1.0)
	match test_id:
		"desire":
			metrics["desirability"] = clamp(float(metrics.get("desirability", 0.3)) * 0.55 + normalized * 0.45, 0.0, 1.0)
		"commitment":
			metrics["viability"] = clamp(float(metrics.get("viability", 0.25)) * 0.45 + normalized * 0.55, 0.0, 1.0)
		"burden":
			metrics["feasibility"] = clamp(float(metrics.get("feasibility", 0.45)) + (normalized - 0.5) * 0.28, 0.0, 1.0)
			metrics["safety"] = clamp(float(metrics.get("safety", 0.55)) + (normalized - 0.5) * 0.18, 0.0, 1.0)
		"refusal":
			metrics["safety"] = clamp(float(metrics.get("safety", 0.55)) * 0.55 + normalized * 0.45, 0.0, 1.0)
	metrics["evidence"] = clamp(float(metrics.get("evidence", 0.1)) + 0.12 + normalized * 0.08, 0.0, 1.0)
	updated["metrics"] = metrics
	var completed: Dictionary = updated.get("completed_tests", {}).duplicate(true)
	completed[test_id] = {"strength": strength, "note": _clean(note)}
	updated["completed_tests"] = completed
	return updated

static func molt(profile: Dictionary, revised_promise: String, revised_audience: String, guardrails: Array) -> Dictionary:
	var updated = profile.duplicate(true)
	var revision = {
		"before": String(updated.get("promise", "")),
		"after": _clean(revised_promise),
		"audience_before": String(updated.get("audience", "")),
		"audience_after": _clean(revised_audience),
		"guardrails": guardrails.duplicate()
	}
	var revisions: Array = updated.get("revisions", []).duplicate(true)
	revisions.append(revision)
	updated["revisions"] = revisions
	updated["promise"] = revision["after"]
	updated["audience"] = revision["audience_after"]
	updated["guardrails"] = guardrails.duplicate()
	var metrics: Dictionary = updated.get("metrics", {}).duplicate(true)
	metrics["safety"] = clamp(float(metrics.get("safety", 0.5)) + guardrails.size() * 0.06, 0.0, 1.0)
	metrics["feasibility"] = clamp(float(metrics.get("feasibility", 0.5)) - guardrails.size() * 0.018, 0.0, 1.0)
	updated["metrics"] = metrics
	return updated

static func decision_record(profile: Dictionary, decision: String) -> Dictionary:
	var record = profile.duplicate(true)
	record["decision"] = decision
	record["next_actions"] = _next_actions(record, decision)
	record["verdict_reason"] = _verdict_reason(record, decision)
	return record

static func _classify(raw: String, intake: Dictionary) -> String:
	var scores = {"FLECK": 0, "HAND": 0, "MIRROR": 0, "TEETH": 0, "SWARM": 0, "WEATHER": 0, "BURROWER": 0}
	_score_words(scores, raw, "HAND", ["tool", "service", "help", "build", "translate", "work", "deliver", "device", "hardware"])
	_score_words(scores, raw, "MIRROR", ["rank", "reputation", "identity", "profile", "score", "recommend", "personalize", "status"])
	_score_words(scores, raw, "TEETH", ["control", "enforce", "monitor", "surveil", "ban", "mandatory", "weapon", "punish"])
	_score_words(scores, raw, "SWARM", ["social", "share", "viral", "community", "network", "marketplace", "creator", "invite"])
	_score_words(scores, raw, "WEATHER", ["culture", "movement", "public", "everyone", "society", "narrative", "campaign", "belief"])
	_score_words(scores, raw, "BURROWER", ["infrastructure", "records", "workflow", "compliance", "archive", "maintenance", "operations", "standard"])
	_score_words(scores, raw, "FLECK", ["comfort", "small", "personal", "gift", "journal", "poem", "delight", "memory"])
	if String(intake.get("payer", "")).is_empty():
		scores["FLECK"] += 1
	var best = "HAND"
	var best_score = -1
	for key in scores.keys():
		if int(scores[key]) > best_score:
			best = String(key)
			best_score = int(scores[key])
	return best

static func _appetite(raw: String) -> String:
	var map = {
		"attention": ["attention", "views", "audience", "engagement", "content"],
		"data": ["data", "track", "model", "ai", "personalize", "record"],
		"money": ["revenue", "pay", "price", "sale", "subscription", "profit"],
		"trust": ["trust", "private", "secure", "health", "finance", "legal"],
		"obedience": ["mandatory", "enforce", "control", "policy", "compliance"],
		"labour": ["worker", "manual", "operate", "moderate", "maintain", "support"],
		"care": ["care", "wellbeing", "comfort", "support", "friend"],
		"time": ["faster", "speed", "save time", "instant", "waiting"]
	}
	var best = "attention"
	var score = 0
	for key in map.keys():
		var local = 0
		for word in map[key]:
			if raw.contains(String(word)):
				local += 1
		if local > score:
			best = String(key)
			score = local
	return best

static func _tests(intake: Dictionary, appetite: String) -> Array:
	return [
		{"id": "desire", "title": "DESIRE YARD", "question": "Did real people describe this problem before hearing your solution?", "mission": "Speak to five likely users. Ask how they solve it today before describing the idea."},
		{"id": "commitment", "title": "COMMITMENT PADDOCK", "question": "What has anyone risked to get this outcome?", "mission": "Ask three people for money, a preorder, a signed pilot, data access, or one hour of their time."},
		{"id": "burden", "title": "BURROWER TUNNEL", "question": "Who performs the invisible work when the idea succeeds?", "mission": "Map every recurring human, technical, legal, and support task. Price the most expensive dependency."},
		{"id": "refusal", "title": "REFUSAL GATE", "question": "Can affected people leave without punishment?", "mission": "Write the easiest opt-out, deletion, appeal, or shutdown path. Test it against the cruelest plausible use."}
	]

static func _assumptions(intake: Dictionary) -> Array:
	var items = []
	if String(intake.get("audience", "")).length() < 8:
		items.append("The first user is not yet specific enough.")
	if String(intake.get("payer", "")).length() < 3:
		items.append("The payer and beneficiary may be different people.")
	if String(intake.get("evidence", "")).length() < 12:
		items.append("The problem is currently supported more by belief than evidence.")
	if String(intake.get("maintenance", "")).length() < 8:
		items.append("The recurring maintenance burden is unnamed.")
	if String(intake.get("harm", "")).length() < 8:
		items.append("The cruelest plausible use has not been described.")
	if items.is_empty():
		items.append("The strongest assumptions are now hidden in scale, ownership, and timing.")
	return items

static func _next_actions(profile: Dictionary, decision: String) -> Array:
	var title = String(profile.get("title", "the idea"))
	match decision:
		"BUILD":
			return ["Run the highest-risk test before adding features.", "Ask three qualified users for a concrete commitment.", "Build the smallest version that can disprove %s." % title]
		"MOLT":
			return ["Rewrite the promise in one measurable sentence.", "Retest the revised idea with five people from the narrower audience.", "Remove one dependency before expanding scope."]
		"HIBERNATE":
			return ["Write three conditions that would make the timing right.", "Choose a review date and stop spending on it until then.", "Preserve research, contacts, and prototypes in the specimen record."]
		"SANCTUARY":
			return ["Define what success means without forcing revenue or scale.", "Set a humane time and money boundary.", "Keep the idea alive for craft, learning, or community value."]
		"BREAK":
			return ["Write which assumption failed and what evidence changed your mind.", "Extract reusable assets, relationships, and technical lessons.", "Tell collaborators the project is closed instead of leaving it undead."]
	return ["Choose the next irreversible test."]

static func _verdict_reason(profile: Dictionary, decision: String) -> String:
	var metrics: Dictionary = profile.get("metrics", {})
	var evidence = float(metrics.get("evidence", 0.0))
	var safety = float(metrics.get("safety", 0.0))
	if decision == "BUILD":
		return "The specimen has enough evidence to earn another controlled investment."
	if decision == "MOLT":
		return "The useful core may survive, but its present form carries avoidable weakness."
	if decision == "HIBERNATE":
		return "The idea may be viable later, but current timing or dependencies are hostile."
	if decision == "SANCTUARY":
		return "The idea has value that should not be distorted by compulsory scale."
	if decision == "BREAK":
		return "Ending it now protects future time and preserves what was learned. Evidence %.0f%%, safety %.0f%%." % [evidence * 100.0, safety * 100.0]
	return "No ruling recorded."

static func _board_class(intake: Dictionary, raw: String) -> String:
	if not String(intake.get("payer", "")).is_empty() or raw.contains("revenue") or raw.contains("business"):
		return "HAND"
	if raw.contains("social") or raw.contains("platform"):
		return "SWARM"
	return "FLECK"

static func _risk_score(intake: Dictionary, raw: String) -> float:
	var score = 0.18
	if String(intake.get("harm", "")).length() > 18:
		score += 0.12
	for word in ["data", "children", "health", "money", "surveil", "mandatory", "control", "worker"]:
		if raw.contains(word):
			score += 0.07
	return clamp(score, 0.0, 0.92)

static func _evidence_score(text: String) -> float:
	var lower = text.to_lower()
	var score = 0.08
	for word in ["interview", "customer", "user", "paid", "pilot", "prototype", "tested", "revenue", "preorder"]:
		if lower.contains(word):
			score += 0.11
	return clamp(score, 0.0, 0.88)

static func _feasibility_score(intake: Dictionary) -> float:
	var score = 0.42
	if String(intake.get("dependency", "")).length() > 6:
		score += 0.08
	if String(intake.get("maintenance", "")).length() > 8:
		score += 0.08
	return clamp(score, 0.0, 0.8)

static func _viability_score(intake: Dictionary) -> float:
	var score = 0.22
	if String(intake.get("payer", "")).length() > 4:
		score += 0.18
	if String(intake.get("evidence", "")).to_lower().contains("paid"):
		score += 0.28
	return clamp(score, 0.0, 0.82)

static func _burden(intake: Dictionary, appetite: String) -> String:
	var named = _clean(String(intake.get("maintenance", "")))
	if not named.is_empty():
		return named
	return "recurring %s, support, and maintenance" % appetite

static func _creature_name(title: String, specimen_class: String, appetite: String) -> String:
	var clean_title = title.strip_edges()
	if clean_title.length() > 24:
		clean_title = clean_title.left(24)
	var suffix = {"FLECK": "Moth", "HAND": "Bearer", "MIRROR": "Glassling", "TEETH": "Crown", "SWARM": "Choir", "WEATHER": "Front", "BURROWER": "Numberer"}.get(specimen_class, "Specimen")
	if clean_title.is_empty():
		clean_title = appetite.capitalize()
	return "%s %s" % [clean_title, suffix]

static func _score_words(scores: Dictionary, raw: String, key: String, words: Array):
	for word in words:
		if raw.contains(String(word)):
			scores[key] = int(scores[key]) + 1

static func _joined_text(intake: Dictionary) -> String:
	var parts = []
	for value in intake.values():
		parts.append(String(value))
	return " ".join(parts).to_lower()

static func _clean(value: String) -> String:
	return value.strip_edges().replace("\n", " ").replace("  ", " ")
