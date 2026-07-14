extends RefCounted

const Base = preload("res://scripts/idea_lab_analysis.gd")
const VALID_TESTS = ["desire", "commitment", "burden", "refusal"]

static func analyze(intake: Dictionary) -> Dictionary:
	var profile: Dictionary = Base.analyze(intake)
	var raw = _normalize(_joined_text(intake))
	var specimen_class = _classify(raw, intake)
	var appetite = _appetite(raw)
	profile["class"] = specimen_class
	profile["appetite"] = appetite
	profile["burden"] = _burden(intake, appetite)
	profile["creature_name"] = _creature_name(String(profile.get("title", "Untitled Idea")), specimen_class, appetite)
	var metrics: Dictionary = profile.get("metrics", {}).duplicate(true)
	metrics["evidence"] = _evidence_score(_normalize(String(intake.get("evidence", ""))))
	metrics["viability"] = _viability_score(intake)
	metrics["safety"] = clamp(1.0 - _risk_score(intake, raw), 0.0, 1.0)
	profile["metrics"] = metrics
	return profile

static func apply_test(profile: Dictionary, test_id: String, strength: int, note: String) -> Dictionary:
	if not VALID_TESTS.has(test_id):
		return profile.duplicate(true)
	var bounded_strength = clampi(strength, 0, 3)
	var before = float(profile.get("metrics", {}).get("evidence", 0.08))
	var updated: Dictionary = Base.apply_test(profile, test_id, bounded_strength, note)
	var metrics: Dictionary = updated.get("metrics", {}).duplicate(true)
	var increments = [0.0, 0.06, 0.13, 0.20]
	metrics["evidence"] = clamp(before + float(increments[bounded_strength]), 0.0, 1.0)
	updated["metrics"] = metrics
	return updated

static func molt(profile: Dictionary, revised_promise: String, revised_audience: String, guardrails: Array) -> Dictionary:
	return Base.molt(profile, revised_promise, revised_audience, guardrails)

static func decision_record(profile: Dictionary, decision: String) -> Dictionary:
	return Base.decision_record(profile, decision)

static func _classify(raw: String, intake: Dictionary) -> String:
	var scores = {
		"FLECK": 0,
		"HAND": 0,
		"MIRROR": 0,
		"TEETH": 0,
		"SWARM": 0,
		"WEATHER": 0,
		"BURROWER": 0
	}
	_score(scores, raw, "HAND", ["tool", "service", "help", "build", "translate", "work", "deliver", "device", "hardware"])
	_score(scores, raw, "MIRROR", ["rank", "reputation", "identity", "profile", "score", "recommend", "personalize", "status"])
	_score(scores, raw, "TEETH", ["control", "enforce", "monitor", "surveil", "ban", "mandatory", "weapon", "punish"])
	_score(scores, raw, "SWARM", ["social", "share", "viral", "community", "network", "marketplace", "creator", "invite"])
	_score(scores, raw, "WEATHER", ["culture", "movement", "public", "everyone", "society", "narrative", "campaign", "belief"])
	_score(scores, raw, "BURROWER", ["infrastructure", "records", "workflow", "compliance", "archive", "maintenance", "operations", "standard"])
	_score(scores, raw, "FLECK", ["comfort", "small", "personal", "gift", "journal", "poem", "delight", "memory"])
	if String(intake.get("payer", "")).strip_edges().is_empty():
		scores["FLECK"] += 1
	var priority = ["TEETH", "WEATHER", "SWARM", "BURROWER", "MIRROR", "HAND", "FLECK"]
	var best = "HAND" if not String(intake.get("payer", "")).strip_edges().is_empty() else "FLECK"
	var best_score = int(scores[best])
	for key in priority:
		if int(scores[key]) > best_score:
			best = key
			best_score = int(scores[key])
	return best

static func _appetite(raw: String) -> String:
	var terms = {
		"attention": ["attention", "views", "audience", "engagement", "content"],
		"data": ["data", "track", "model", "ai", "personalize", "record"],
		"money": ["revenue", "pay", "price", "sale", "subscription", "profit"],
		"trust": ["trust", "private", "secure", "health", "finance", "legal"],
		"obedience": ["mandatory", "enforce", "control", "policy", "compliance"],
		"labour": ["worker", "manual", "operate", "moderate", "maintain", "support"],
		"care": ["care", "wellbeing", "comfort", "friend"],
		"time": ["faster", "speed", "save time", "instant", "waiting"]
	}
	var best = "attention"
	var best_score = 0
	for key in terms.keys():
		var score = 0
		for term in terms[key]:
			if _has(raw, String(term)):
				score += 1
		if score > best_score:
			best = String(key)
			best_score = score
	return best

static func _risk_score(intake: Dictionary, raw: String) -> float:
	var score = 0.18
	if String(intake.get("harm", "")).strip_edges().length() > 18:
		score += 0.12
	for term in ["data", "children", "health", "money", "surveil", "mandatory", "control", "worker"]:
		if _has(raw, term):
			score += 0.07
	return clamp(score, 0.0, 0.92)

static func _evidence_score(normalized: String) -> float:
	var score = 0.08
	for term in ["interview", "interviewed", "customer", "user", "users", "paid", "pilot", "prototype", "tested", "revenue", "preorder"]:
		if _has(normalized, term):
			score += 0.11
	return clamp(score, 0.0, 0.88)

static func _viability_score(intake: Dictionary) -> float:
	var score = 0.22
	if String(intake.get("payer", "")).strip_edges().length() > 4:
		score += 0.18
	var evidence = _normalize(String(intake.get("evidence", "")))
	if _has(evidence, "paid") or _has(evidence, "revenue") or _has(evidence, "preorder"):
		score += 0.28
	return clamp(score, 0.0, 0.82)

static func _burden(intake: Dictionary, appetite: String) -> String:
	var named = String(intake.get("maintenance", "")).strip_edges()
	return named if not named.is_empty() else "recurring %s, support, and maintenance" % appetite

static func _creature_name(title: String, specimen_class: String, appetite: String) -> String:
	var clean_title = title.strip_edges()
	if clean_title.length() > 24:
		clean_title = clean_title.left(24)
	if clean_title.is_empty():
		clean_title = appetite.capitalize()
	var suffixes = {
		"FLECK": "Moth",
		"HAND": "Bearer",
		"MIRROR": "Glassling",
		"TEETH": "Crown",
		"SWARM": "Choir",
		"WEATHER": "Front",
		"BURROWER": "Numberer"
	}
	return "%s %s" % [clean_title, String(suffixes.get(specimen_class, "Specimen"))]

static func _score(scores: Dictionary, raw: String, key: String, terms: Array):
	for term in terms:
		if _has(raw, String(term)):
			scores[key] = int(scores[key]) + 1

static func _joined_text(intake: Dictionary) -> String:
	var parts: Array = []
	for value in intake.values():
		parts.append(String(value))
	return " ".join(parts)

static func _normalize(value: String) -> String:
	var regex = RegEx.new()
	regex.compile("[^a-z0-9]+")
	var cleaned = regex.sub(value.to_lower(), " ", true).strip_edges()
	return " " + cleaned + " "

static func _has(normalized: String, term: String) -> bool:
	var wanted = _normalize(term).strip_edges()
	return normalized.contains(" " + wanted + " ")
