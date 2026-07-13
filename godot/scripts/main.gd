extends Node3D

const CivicWorld = preload("res://scripts/civic_world.gd")
const KeeperPlayer = preload("res://scripts/player.gd")
const QueueEater = preload("res://scripts/creature.gd")
const GameHUD = preload("res://scripts/hud.gd")

enum Phase { TRACE, SEAL, TETHER, RETURN, PROCESSING, CLASSIFY, VERDICT, END }

var world
var player
var creature
var hud
var phase = Phase.TRACE

var scan_progress = 0.0
var scanning_clue = -1
var seal_index = 0
var seals = ["EXIT", "KEEPER", "BOUNDARY", "AMPLIFY"]
var seal_colors = [Color("#67c8b8"), Color("#d2a45f"), Color("#7896c8"), Color("#a6574e")]

var stability = 72.0
var trust = 61.0
var leakage = 8.0
var agitation = 34.0
var integrity = 58.0

var classes = ["FLECK", "HAND", "MIRROR", "TEETH", "SWARM", "WEATHER", "BURROWER"]
var classification_scores = {}
var selected_class_index = 1
var official_class = "HAND"
var staff_bells = 4
var reports_read = 0
var corruption_exposed = false
var ecology_found = {}
var bonus_bell_granted = false

var case_score = 0
var best_score = 0
var evidence_chain = 0
var chain_timer = 0.0
var ending_chosen = false

var mobile_runtime = false
var performance_profiles = [
	{"label": "ECO 30", "fps": 30, "start": 0.62, "min": 0.54, "max": 0.68},
	{"label": "BAL 45", "fps": 45, "start": 0.70, "min": 0.60, "max": 0.78},
	{"label": "QUALITY 60", "fps": 60, "start": 0.80, "min": 0.70, "max": 0.90}
]
var performance_profile_index = 2
var current_render_scale = 0.80
var performance_sample_time = 0.0
var low_fps_streak = 0
var high_fps_streak = 0
var diagnostics_active = false
var diagnostics_elapsed = 0.0
var diagnostics_sample_time = 0.0
var diagnostics_samples = []
var diagnostics_duration = 600.0

func _ready():
	_ensure_input_actions()
	_load_archive()
	for class_key in classes:
		classification_scores[class_key] = 0.0
	mobile_runtime = OS.has_feature("mobile") or DisplayServer.is_touchscreen_available() or OS.has_environment("IDEA_ZOO_MOBILE_TEST")

	world = CivicWorld.new()
	add_child(world)

	creature = QueueEater.new()
	creature.position = world.creature_spawn
	creature.agitation_changed.connect(_on_agitation_changed)
	add_child(creature)

	player = KeeperPlayer.new()
	player.position = world.zoo_spawn
	add_child(player)
	world.set_player(player)

	hud = GameHUD.new()
	add_child(hud)
	player.interact_requested.connect(_on_interact)
	player.cycle_seal_requested.connect(_cycle_context)
	player.lens_changed.connect(_on_lens_changed)
	hud.interact_pressed.connect(_on_interact)
	hud.cycle_pressed.connect(_cycle_context)
	hud.lens_changed.connect(player.set_lens)
	hud.joystick_changed.connect(player.set_mobile_vector)
	hud.performance_pressed.connect(_cycle_performance_profile)
	hud.diagnostics_pressed.connect(_toggle_diagnostics)
	performance_profile_index = 0 if mobile_runtime else 2
	_apply_performance_profile(performance_profile_index)

	hud.set_seal(seals[seal_index], false)
	hud.set_bells(0, false)
	hud.set_score(case_score, evidence_chain, best_score)
	_set_focus_metric("APPETITE", agitation)
	hud.set_objective("FIELD ORDER 24-A", "Follow the missing minutes into Open Market. Use the Resonance Lens near anything the city is pretending not to see.")
	hud.show_message(
		"KEEPER'S OATH",
		"Glassmarket is moving faster. The people maintaining it are losing hours. Find the missing time, give its appetite a body, and return with enough evidence to judge it."
	)

func _ensure_input_actions():
	var bindings = {
		"move_forward": [KEY_W, KEY_UP],
		"move_back": [KEY_S, KEY_DOWN],
		"move_left": [KEY_A, KEY_LEFT],
		"move_right": [KEY_D, KEY_RIGHT],
		"interact": [KEY_E, KEY_ENTER],
		"lens": [KEY_SPACE],
		"cycle_seal": [KEY_Q, KEY_TAB]
	}
	for action in bindings.keys():
		if not InputMap.has_action(action):
			InputMap.add_action(action, 0.2)
		for keycode in bindings[action]:
			var event = InputEventKey.new()
			event.physical_keycode = keycode
			InputMap.action_add_event(action, event)

func _process(delta):
	_update_performance(delta)
	if world == null or player == null:
		return
	player.on_threadway = world.is_on_threadway(player.global_position)
	world.reveal_clues(player.lens_active and phase == Phase.TRACE)
	world.reveal_board_trace(player.lens_active and phase == Phase.PROCESSING and world.board_record_near(player.global_position, 7.0))
	world.set_city_state(leakage, stability)
	_handle_scanning(delta)
	_update_context_prompt()
	if chain_timer > 0.0:
		chain_timer -= delta
		if chain_timer <= 0.0 and evidence_chain > 0:
			evidence_chain = 0
			hud.set_score(case_score, evidence_chain, best_score)
	if phase == Phase.RETURN and creature.tethered and player.global_position.distance_to(world.zoo_return) < 4.0:
		_arrive_at_zoo()
	_update_metrics(delta)

func _handle_scanning(delta):
	if phase != Phase.TRACE or not player.lens_active:
		scan_progress = 0.0
		scanning_clue = -1
		hud.set_scan(0.0, false)
		return
	var nearby = world.nearest_clue(player.global_position, 4.0)
	if nearby == null:
		scan_progress = max(0.0, scan_progress - delta * 1.8)
		scanning_clue = -1
		hud.set_scan(scan_progress, scan_progress > 0.01)
		return
	if scanning_clue != nearby:
		scanning_clue = nearby
		scan_progress = 0.0
	scan_progress += delta / 1.25
	hud.set_scan(scan_progress, true)
	if scan_progress >= 1.0:
		_complete_clue(nearby)
		scan_progress = 0.0
		scanning_clue = -1
		hud.set_scan(0.0, false)

func _complete_clue(index: int):
	var clue = world.clues[index]
	clue["scanned"] = true
	clue["label"].visible = true
	clue["crystal"].visible = true
	trust = min(100.0, trust + 3.0)
	_award_evidence(120, clue["title"])
	hud.show_brief(clue["title"], clue["text"])
	var count = _scanned_count()
	if count == 1:
		hud.set_objective("THE CITY IS BORROWING", "Two traces remain. Look for clocks and people missing from the success story.")
	elif count == 2:
		hud.set_objective("AN APPETITE WITHOUT A NAME", "One trace remains. Search where temporary workers and maintenance routes meet.")
	elif count >= 3:
		_begin_manifestation()

func _scanned_count() -> int:
	var count = 0
	for clue in world.clues:
		if clue["scanned"]:
			count += 1
	return count

func _begin_manifestation():
	phase = Phase.SEAL
	creature.activate()
	world.reveal_anchors(true)
	hud.set_seal(seals[seal_index], true)
	hud.set_objective("THE QUEUE-EATER HAS A BODY", "Place three civic seals. The right rules make it useful without making refusal impossible.")
	hud.show_message(
		"MANIFESTATION: QUEUE-EATER",
		"It did not remove waiting. It moved waiting into people the city stopped counting. EXIT permits refusal. KEEPER names responsibility. BOUNDARY limits territory. AMPLIFY rewards the appetite."
	)

func _on_interact():
	if phase == Phase.END:
		return

	var ecology_index = world.nearest_ecology(player.global_position)
	if ecology_index != null and (phase != Phase.TRACE or world.nearest_clue(player.global_position, 4.0) == null) and player.lens_active:
		_observe_ecology(ecology_index)
		return

	match phase:
		Phase.SEAL:
			var anchor = world.nearest_anchor(player.global_position)
			if anchor != null:
				_place_seal(anchor)
		Phase.TETHER:
			if player.global_position.distance_to(creature.global_position) < 3.3:
				_begin_tether()
		Phase.RETURN:
			if player.global_position.distance_to(world.zoo_return) < 4.0:
				_arrive_at_zoo()
		Phase.PROCESSING:
			if world.board_record_near(player.global_position) and player.lens_active:
				_expose_board_mandate()
				return
			var staff_index = world.nearest_staff(player.global_position)
			if staff_index != null:
				_consult_staff(staff_index)
				return
			if world.processing_dais_near(player.global_position) and (reports_read >= 2 or staff_bells <= 0):
				_begin_classification()
		Phase.CLASSIFY:
			if world.processing_dais_near(player.global_position):
				_submit_classification()
		Phase.VERDICT:
			var gate = world.nearest_verdict(player.global_position)
			if gate != null:
				_choose_verdict(world.verdict_gates[gate]["id"])

func _place_seal(anchor_index: int):
	var seal_name = seals[seal_index]
	world.place_seal(anchor_index, seal_name, seal_colors[seal_index])
	var chosen = []
	var filled = 0
	for anchor in world.anchors:
		if not anchor["seal"].is_empty():
			filled += 1
			chosen.append(anchor["seal"])
	if filled < 3:
		hud.set_objective("RULES TAKE SHAPE", "%d of 3 seals placed. Touch a plate again to replace its rule." % filled)
		_award_evidence(45, seal_name + " SEAL")
		return
	creature.apply_seals(chosen)
	if creature.correct_seals == 3:
		phase = Phase.TETHER
		_award_evidence(260, "CLEAN CONTAINMENT")
		hud.set_objective("THE CREATURE CAN NOW REFUSE YOU", "Approach it and attach the civic tether.")
		hud.show_brief("THE APPETITE HAS EDGES", "It still wants to work, but it can no longer hide maintenance or consent.")
	else:
		leakage = min(100.0, leakage + 6.0)
		trust = max(0.0, trust - 4.0)
		evidence_chain = 0
		hud.set_score(case_score, evidence_chain, best_score)
		hud.set_objective("THE RULES FEED IT", "One or more seals reward its appetite. Replace the wrong plate.")
		hud.show_brief("WRONG KIND OF ORDER", "The city applauds the speed before noticing who paid for it.")

func _cycle_context():
	if phase == Phase.SEAL:
		seal_index = (seal_index + 1) % seals.size()
		hud.set_seal(seals[seal_index], true)
	elif phase == Phase.CLASSIFY:
		selected_class_index = (selected_class_index + 1) % classes.size()
		var selected = classes[selected_class_index]
		hud.set_seal("CLASS · " + selected, true)
		world.update_classification_board(_display_scores(), selected, corruption_exposed)
		hud.show_brief("PROVISIONAL CLASS", selected + " · touch the dais to record it")
	if OS.has_feature("mobile"):
		Input.vibrate_handheld(22)

func _begin_tether():
	creature.begin_tether(player)
	player.tethering = true
	phase = Phase.RETURN
	hud.set_seal("", false)
	_award_evidence(180, "CIVIC TETHER")
	hud.set_objective("A PROCESSION, NOT A PRIZE", "Walk it back to the Zoo. Threadways move consequences without hiding them.")
	if OS.has_feature("mobile"):
		Input.vibrate_handheld(55)

func _arrive_at_zoo():
	if phase != Phase.RETURN:
		return
	phase = Phase.PROCESSING
	player.tethering = false
	creature.settle_at_zoo()
	creature.global_position = Vector3(0, 0.7, -16.2)
	world.reveal_departments(true)
	staff_bells = 4
	hud.set_bells(staff_bells, true)
	hud.set_seal("", false)
	_set_focus_metric("ZOO INTEGRITY", integrity)
	world.update_classification_board(_display_scores(), "HAND", false)
	_award_evidence(300, "SPECIMEN DELIVERED")
	hud.set_objective("FOUR BELLS BEFORE THE BOARD LOCKS", "Consult specialists. Each report costs one bell. Explore the living taxonomy for free evidence. Return to the central dais when ready.")
	hud.show_message(
		"THE INSTITUTION IS NOW PART OF THE CASE",
		"Eight specialists disagree. You have four bell-hours. The official board has already pencilled in HAND. Evidence can move the classification. Power can keep it still."
	)

func _observe_ecology(index: int):
	var record = world.observe_ecology(index)
	if record == null:
		return
	var kind = String(record["kind"])
	ecology_found[kind] = true
	integrity = min(100.0, integrity + 2.0)
	_award_evidence(95, kind + " RECORDED")
	hud.show_brief(record["name"], kind + " · " + record["note"])
	if ecology_found.size() >= 3 and not bonus_bell_granted and phase == Phase.PROCESSING:
		bonus_bell_granted = true
		staff_bells += 1
		hud.set_bells(staff_bells, true)
		_award_evidence(240, "TAXONOMY CHAIN")
		hud.show_brief("A FIFTH BELL", "Three living classes observed. The archive grants one extra consultation.")

func _consult_staff(index: int):
	if staff_bells <= 0:
		hud.show_brief("NO BELLS REMAIN", "Return to the classification dais.")
		return
	var report = world.consult_staff(index)
	if report == null:
		return
	staff_bells -= 1
	reports_read += 1
	hud.set_bells(staff_bells, true)
	for class_key in report["votes"].keys():
		classification_scores[class_key] = float(classification_scores.get(class_key, 0.0)) + float(report["votes"][class_key])
	integrity = clamp(integrity + float(report["integrity"]), 0.0, 100.0)
	trust = clamp(trust + float(report["trust"]), 0.0, 100.0)
	leakage = clamp(leakage + float(report["leakage"]), 0.0, 100.0)
	_award_evidence(160, report["role"])
	var leader = _evidence_leader()
	world.update_classification_board(_display_scores(), "HAND" if not corruption_exposed else leader, corruption_exposed)
	hud.show_brief(report["title"], report["body"])
	if staff_bells > 0:
		hud.set_objective("%d BELLS REMAIN" % staff_bells, "Choose another specialist, inspect the sealed archive, study the ecology, or return to the dais.")
	else:
		hud.set_objective("THE BOARD BELL HAS RUNG", "Return to the classification dais. What you did not investigate is now part of your decision.")
	if OS.has_feature("mobile"):
		Input.vibrate_handheld(30)

func _expose_board_mandate():
	if corruption_exposed:
		return
	if staff_bells <= 0:
		hud.show_brief("THE ARCHIVE HAS CLOSED", "The last bell has already rung.")
		return
	staff_bells -= 1
	corruption_exposed = true
	hud.set_bells(staff_bells, true)
	world.expose_board_record()
	integrity = min(100.0, integrity + 18.0)
	trust = min(100.0, trust + 8.0)
	stability = max(0.0, stability - 3.0)
	leakage = min(100.0, leakage + 5.0)
	_award_evidence(320, "CONFLICT DISCLOSED")
	world.update_classification_board(_display_scores(), _evidence_leader(), true)
	hud.show_message(
		"FAST CITY MANDATE",
		"The board funded the system before the specimen hatched. Its preferred classification was already written: HAND. Disclosure costs the Zoo calm, but restores the right to disagree."
	)

func _begin_classification():
	phase = Phase.CLASSIFY
	selected_class_index = classes.find(_evidence_leader())
	if selected_class_index < 0:
		selected_class_index = 1
	var selected = classes[selected_class_index]
	hud.set_bells(staff_bells, true)
	hud.set_seal("CLASS · " + selected, true)
	hud.set_objective("NAME THE BODY YOU ACTUALLY OBSERVED", "Cycle through the seven classes, then touch the dais. The board's label and your label may not match.")
	world.update_classification_board(_display_scores(), selected, corruption_exposed)
	hud.show_brief("CLASSIFICATION MODE", "Q or SEAL changes class · touch the dais to record")

func _submit_classification():
	var selected = classes[selected_class_index]
	var evidence_class = _evidence_leader()
	official_class = selected if corruption_exposed else "HAND"
	if selected == evidence_class:
		_award_evidence(420, "EVIDENCE MATCH")
		integrity = min(100.0, integrity + 7.0)
	else:
		trust = max(0.0, trust - 4.0)
		integrity = max(0.0, integrity - 5.0)
	if official_class != selected:
		leakage = min(100.0, leakage + 8.0)
		integrity = max(0.0, integrity - 10.0)
	world.close_staff_round()
	world.update_classification_board(_display_scores(), official_class, corruption_exposed)
	creature.set_classification(selected, official_class)
	phase = Phase.VERDICT
	hud.set_seal("", false)
	hud.set_bells(0, false)
	world.reveal_verdict_garden()
	var conflict_line = "The record now agrees with you." if official_class == selected else "The board records HAND over your " + selected + "."
	hud.set_objective("THE FIVE GATES", "Choose a verdict. Classification changes which harms the institution is willing to see.")
	hud.show_message(
		"KEEPER: " + selected + " · BOARD: " + official_class,
		conflict_line + " The creature remains the same body. Only the institution's permitted response has changed."
	)

func _display_scores() -> Dictionary:
	var result = classification_scores.duplicate()
	if not corruption_exposed:
		result["HAND"] = float(result.get("HAND", 0.0)) + 4.0
	return result

func _evidence_leader() -> String:
	var leader = "HAND"
	var highest = -999.0
	for class_key in classes:
		var value = float(classification_scores.get(class_key, 0.0))
		if value > highest:
			highest = value
			leader = class_key
	return leader

func _choose_verdict(verdict: String):
	if ending_chosen:
		return
	ending_chosen = true
	phase = Phase.END
	player.can_move = false
	var title = ""
	var body = ""
	var classification = classes[selected_class_index]
	match verdict:
		"restricted":
			title = "THE CITY KEEPS ITS TIME"
			body = "The Queue-Eater enters the Working Paddock under visible limits. Journeys shorten. Refusal remains possible. Maintenance receives both authority and blame."
			stability += 18
			trust += 17
			_award_verdict(520, classification in ["HAND", "TEETH", "WEATHER"])
		"release":
			title = "THE FASTEST CITY IN THE WORLD"
			body = "Queues vanish. Then night workers begin arriving home tomorrow. The delay survives by becoming somebody else's private problem."
			stability -= 24
			trust -= 21
			leakage += 14
			_award_verdict(180, classification == "HAND" and corruption_exposed)
		"molt":
			title = "A SMALLER ANIMAL"
			body = "The Molt House removes its hunger for scale. It clears one hospital queue at a time, then sleeps. Investors leave. Patients stay."
			stability += 9
			trust += 15
			_award_verdict(480, classification in ["HAND", "TEETH"])
		"sanctuary":
			title = "SAFE, ALIVE, UNUSED"
			body = "The creature remains in Quiet Sanctuary. The city waits again. The hidden workers recover their hours."
			stability += 4
			trust += 5
			_award_verdict(340, classification in ["WEATHER", "TEETH"])
		"destroy":
			title = "THE STORY ESCAPES"
			body = "The White Room dissolves the original. The warning teaches six imitators exactly how it fed. The cage holds. The explanation breeds."
			stability -= 12
			leakage += 35
			_award_verdict(120, false)

	if official_class != classification:
		body += "\n\nThe public record names it " + official_class + ". Future keepers inherit the board's mistake, not yours."
	if corruption_exposed:
		body += "\n\nThe funding conflict remains attached to the specimen record. The Zoo is less comfortable and more honest."
	if ecology_found.has("BURROWER"):
		body += "\n\nA Ledger Mole had already numbered the hidden maintenance routes. Engineers know what to disconnect when the clocks buckle."
		stability += 8
	if ecology_found.has("FLECK"):
		body += "\n\nAt the hospital, a Lantern Fleck continues doing something too small for the city metrics: staying beside frightened people."
		trust += 6

	stability = clamp(stability, 0.0, 100.0)
	trust = clamp(trust, 0.0, 100.0)
	leakage = clamp(leakage, 0.0, 100.0)
	integrity = clamp(integrity, 0.0, 100.0)
	best_score = max(best_score, case_score)
	_save_archive()
	_set_focus_metric("ZOO INTEGRITY", integrity)
	hud.set_score(case_score, evidence_chain, best_score)
	hud.set_objective("CASE CLOSED · GLASSMARKET REMEMBERS", title)
	var rank = _keeper_rank()
	hud.show_message(
		title,
		body + "\n\nKeeper rank: " + rank + "\nCase score: %d · Best: %d\nCity %d · Trust %d · Leakage %d · Zoo integrity %d" % [case_score, best_score, stability, trust, leakage, integrity],
		"RUN ANOTHER NIGHT",
		true
	)

func _award_verdict(points: int, coherent: bool):
	if coherent:
		_award_evidence(points, "COHERENT VERDICT")
	else:
		case_score += points
		evidence_chain = 0
		hud.set_score(case_score, evidence_chain, best_score)

func _award_evidence(base_points: int, label: String):
	if chain_timer > 0.0:
		evidence_chain = min(6, evidence_chain + 1)
	else:
		evidence_chain = 1
	chain_timer = 14.0
	var multiplier = 1.0 + float(evidence_chain - 1) * 0.18
	var gained = int(round(float(base_points) * multiplier))
	case_score += gained
	hud.set_score(case_score, evidence_chain, best_score)
	hud.flash_reward(label, gained, evidence_chain)
	if OS.has_feature("mobile") and evidence_chain >= 3:
		Input.vibrate_handheld(16 + evidence_chain * 4)

func _keeper_rank() -> String:
	if case_score >= 3600 and integrity >= 75.0:
		return "CIVIC NATURALIST"
	if case_score >= 2600:
		return "APPETITE READER"
	if case_score >= 1700:
		return "FIELD KEEPER"
	if corruption_exposed:
		return "DIFFICULT WITNESS"
	return "BOARD ASSISTANT"

func _on_lens_changed(active: bool):
	world.reveal_clues(active and phase == Phase.TRACE)
	world.reveal_board_trace(active and phase == Phase.PROCESSING and world.board_record_near(player.global_position, 7.0))
	if active and OS.has_feature("mobile"):
		Input.vibrate_handheld(12)

func _on_agitation_changed(value: float):
	agitation = value
	if phase < Phase.PROCESSING:
		_set_focus_metric("APPETITE", agitation)

func _update_context_prompt():
	var prompt = ""
	if phase == Phase.TRACE:
		var clue = world.nearest_clue(player.global_position, 4.0)
		if clue != null:
			prompt = "HOLD LENS · TUNE THE TRACE" if not player.lens_active else "KEEP THE LENS STEADY"
	elif phase == Phase.SEAL:
		var anchor = world.nearest_anchor(player.global_position)
		if anchor != null:
			prompt = "TOUCH · PLACE %s" % seals[seal_index]
	elif phase == Phase.TETHER:
		if player.global_position.distance_to(creature.global_position) < 3.3:
			prompt = "TOUCH · ATTACH CIVIC TETHER"
	elif phase == Phase.RETURN:
		if player.global_position.distance_to(world.zoo_return) < 4.0:
			prompt = "TOUCH · ENTER THE IDEA ZOO"
	elif phase == Phase.PROCESSING:
		if world.board_record_near(player.global_position):
			prompt = "HOLD LENS + TOUCH · OPEN SEALED MANDATE" if not player.lens_active else "TOUCH · DISCLOSE BOARD CONFLICT"
		else:
			var staff_index = world.nearest_staff(player.global_position)
			if staff_index != null:
				prompt = "TOUCH · SPEND 1 BELL ON %s" % world.staff_members[staff_index].role
			elif world.processing_dais_near(player.global_position) and (reports_read >= 2 or staff_bells <= 0):
				prompt = "TOUCH · BEGIN CLASSIFICATION"
	elif phase == Phase.CLASSIFY:
		if world.processing_dais_near(player.global_position):
			prompt = "Q/SEAL · CHANGE CLASS   TOUCH · RECORD %s" % classes[selected_class_index]
	elif phase == Phase.VERDICT:
		var gate = world.nearest_verdict(player.global_position)
		if gate != null:
			prompt = "TOUCH · CHOOSE %s" % world.verdict_gates[gate]["title"]

	if prompt.is_empty():
		var ecology_index = world.nearest_ecology(player.global_position)
		if ecology_index != null:
			prompt = "HOLD LENS + TOUCH · RECORD %s" % world.ecology[ecology_index].kind if not player.lens_active else "TOUCH · ADD TO LIVING TAXONOMY"
	hud.set_prompt(prompt)

func _update_metrics(delta):
	if phase == Phase.SEAL and creature.active and creature.agitation > 55.0:
		stability = max(0.0, stability - delta * 0.35)
		leakage = min(100.0, leakage + delta * 0.22)
	if phase < Phase.PROCESSING:
		hud.set_metrics(stability, trust, leakage, agitation)
	else:
		hud.set_metrics(stability, trust, leakage, integrity)

func _set_focus_metric(title: String, value: float):
	hud.set_focus_metric(title, value)
	if title == "APPETITE":
		hud.set_metrics(stability, trust, leakage, agitation)
	else:
		hud.set_metrics(stability, trust, leakage, integrity)

func _load_archive():
	var config = ConfigFile.new()
	if config.load("user://idea_zoo_archive.cfg") == OK:
		best_score = int(config.get_value("archive", "best_score", 0))

func _save_archive():
	var config = ConfigFile.new()
	config.set_value("archive", "best_score", best_score)
	config.save("user://idea_zoo_archive.cfg")

func _cycle_performance_profile():
	performance_profile_index = (performance_profile_index + 1) % performance_profiles.size()
	_apply_performance_profile(performance_profile_index)
	if mobile_runtime:
		Input.vibrate_handheld(18)

func _apply_performance_profile(index: int):
	var profile = performance_profiles[index]
	Engine.max_fps = int(profile["fps"])
	current_render_scale = float(profile["start"])
	get_viewport().scaling_3d_scale = current_render_scale
	low_fps_streak = 0
	high_fps_streak = 0
	if hud != null:
		hud.set_performance_mode(String(profile["label"]))

func _update_performance(delta: float):
	performance_sample_time += delta
	diagnostics_sample_time += delta
	if performance_sample_time >= 2.0:
		performance_sample_time = 0.0
		var profile = performance_profiles[performance_profile_index]
		var target_fps = float(profile["fps"])
		var measured_fps = float(Engine.get_frames_per_second())
		if measured_fps < target_fps * 0.82:
			low_fps_streak += 1
			high_fps_streak = 0
		elif measured_fps > target_fps * 0.96:
			high_fps_streak += 1
			low_fps_streak = 0
		else:
			low_fps_streak = max(0, low_fps_streak - 1)
			high_fps_streak = max(0, high_fps_streak - 1)
		if low_fps_streak >= 2 and current_render_scale > float(profile["min"]):
			current_render_scale = max(float(profile["min"]), current_render_scale - 0.04)
			get_viewport().scaling_3d_scale = current_render_scale
			low_fps_streak = 0
		elif high_fps_streak >= 5 and current_render_scale < float(profile["max"]):
			current_render_scale = min(float(profile["max"]), current_render_scale + 0.02)
			get_viewport().scaling_3d_scale = current_render_scale
			high_fps_streak = 0
	if diagnostics_active:
		diagnostics_elapsed += delta
		if diagnostics_sample_time >= 1.0:
			diagnostics_sample_time = 0.0
			diagnostics_samples.append(float(Engine.get_frames_per_second()))
			if diagnostics_samples.size() > int(diagnostics_duration):
				diagnostics_samples.pop_front()
			_update_diagnostics_hud()
		if diagnostics_elapsed >= diagnostics_duration:
			_finish_diagnostics()

func _toggle_diagnostics():
	if diagnostics_active:
		_finish_diagnostics()
	else:
		diagnostics_active = true
		diagnostics_elapsed = 0.0
		diagnostics_sample_time = 0.0
		diagnostics_samples.clear()
		hud.set_diagnostics("DEVICE TEST · 00:00 / 10:00\nPlay normally. FPS, scale and sustained drop are being measured.", true)

func _update_diagnostics_hud():
	if diagnostics_samples.is_empty():
		return
	var average = _average_slice(diagnostics_samples, 0, diagnostics_samples.size())
	var ordered = diagnostics_samples.duplicate()
	ordered.sort()
	var low_index = int(floor(float(ordered.size() - 1) * 0.05))
	var low_fps = float(ordered[low_index])
	var first_count = min(60, diagnostics_samples.size())
	var first_average = _average_slice(diagnostics_samples, 0, first_count)
	var last_start = max(0, diagnostics_samples.size() - min(60, diagnostics_samples.size()))
	var last_average = _average_slice(diagnostics_samples, last_start, diagnostics_samples.size())
	var trend = "WARM-UP"
	if diagnostics_samples.size() >= 120:
		trend = "STABLE" if last_average >= first_average * 0.85 else "SUSTAINED DROP"
	var minutes = int(floor(diagnostics_elapsed / 60.0))
	var seconds = int(diagnostics_elapsed) % 60
	var target = int(performance_profiles[performance_profile_index]["fps"])
	hud.set_diagnostics("DEVICE TEST · %02d:%02d / 10:00\nFPS %.0f avg · %.0f low · target %d\nScale %.2f · %s" % [minutes, seconds, average, low_fps, target, current_render_scale, trend], true)

func _finish_diagnostics():
	if not diagnostics_active:
		return
	diagnostics_active = false
	var target = float(performance_profiles[performance_profile_index]["fps"])
	var average = _average_slice(diagnostics_samples, 0, diagnostics_samples.size())
	var first_count = min(60, diagnostics_samples.size())
	var first_average = _average_slice(diagnostics_samples, 0, first_count)
	var last_start = max(0, diagnostics_samples.size() - min(60, diagnostics_samples.size()))
	var last_average = _average_slice(diagnostics_samples, last_start, diagnostics_samples.size())
	var sustained = diagnostics_samples.size() < 120 or last_average >= first_average * 0.85
	var passed = diagnostics_samples.size() >= 60 and average >= target * 0.85 and sustained
	var verdict = "PASS" if passed else "REVIEW"
	hud.set_diagnostics("DEVICE TEST · %s\nAverage %.0f FPS · final minute %.0f FPS\nScale %.2f · sustained %s" % [verdict, average, last_average, current_render_scale, "stable" if sustained else "drop detected"], true)
	if mobile_runtime:
		Input.vibrate_handheld(65 if passed else 28)

func _average_slice(values: Array, start_index: int, end_index: int) -> float:
	if values.is_empty() or end_index <= start_index:
		return 0.0
	var total = 0.0
	for index in range(start_index, end_index):
		total += float(values[index])
	return total / float(end_index - start_index)
