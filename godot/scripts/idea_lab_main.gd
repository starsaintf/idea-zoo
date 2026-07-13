extends Node3D

const Analysis = preload("res://scripts/idea_lab_analysis.gd")
const ZooWorld = preload("res://scripts/idea_lab_world.gd")
const Keeper = preload("res://scripts/idea_lab_keeper.gd")
const Specimen = preload("res://scripts/idea_lab_creature.gd")
const LabHUD = preload("res://scripts/idea_lab_hud.gd")

enum Phase { INTAKE, HATCHING, TESTING, MOLT, DECISION, END }

var phase = Phase.INTAKE
var world
var keeper
var creature
var hud
var profile = {}
var keeper_profile = {}
var completed_tests = 0
var board_exposed = false
var pending_continue = ""

func _ready():
	_ensure_input_actions()
	world = ZooWorld.new()
	add_child(world)
	keeper = Keeper.new()
	keeper.position = world.spawn_point
	add_child(keeper)
	keeper.set_controls_locked(true)
	creature = Specimen.new()
	creature.position = world.hatch_point
	creature.visible = false
	add_child(creature)
	hud = LabHUD.new()
	add_child(hud)
	hud.intake_submitted.connect(_on_intake_submitted)
	hud.test_submitted.connect(_on_test_submitted)
	hud.molt_submitted.connect(_on_molt_submitted)
	hud.overlay_continued.connect(_on_overlay_continued)
	hud.interact_pressed.connect(_on_interact)
	hud.joystick_changed.connect(keeper.set_mobile_vector)
	hud.lens_changed.connect(keeper.set_lens)
	keeper.interact_requested.connect(_on_interact)
	keeper.lens_changed.connect(_on_lens_changed)
	Engine.max_fps = 30 if DisplayServer.is_touchscreen_available() else 60
	if DisplayServer.is_touchscreen_available():
		get_viewport().scaling_3d_scale = 0.68

func _process(_delta):
	if keeper == null or world == null or hud == null or profile.is_empty():
		return
	_update_prompt()
	if phase == Phase.DECISION:
		var decision = world.nearest_decision(keeper.global_position)
		if not decision.is_empty():
			hud.set_prompt("TOUCH · ISSUE %s RULING" % decision)

func _on_intake_submitted(intake: Dictionary, incoming_keeper: Dictionary):
	_reset_case()
	keeper_profile = incoming_keeper.duplicate(true)
	profile = Analysis.analyze(intake)
	keeper.configure(keeper_profile)
	keeper.position = world.spawn_point
	keeper.set_controls_locked(true)
	creature.visible = true
	creature.position = world.hatch_point
	creature.configure(profile)
	creature.scale = Vector3.ONE * 0.08
	creature.set_follow_target(keeper)
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_BACK)
	tween.set_ease(Tween.EASE_OUT)
	tween.tween_property(creature, "scale", Vector3.ONE, 1.8)
	phase = Phase.HATCHING
	hud.set_specimen(profile)
	hud.set_progress(0, 4)
	hud.set_objective("TWENTY-FOUR HOURS", "Your real idea has taken a body. Learn what it wants before deciding what it deserves.")
	var assumptions: Array = profile.get("assumptions", [])
	var assumption_text = ""
	for item in assumptions:
		assumption_text += "\n• " + String(item)
	pending_continue = "begin_tests"
	hud.show_message(
		"%s HAS HATCHED" % String(profile.get("creature_name", "THE SPECIMEN")).to_upper(),
		"The Zoo reads it as %s-class. It feeds on %s. The Board has already pencilled in %s before seeing evidence.\n\nCurrent assumptions:%s" % [String(profile.get("class", "")), String(profile.get("appetite", "")), String(profile.get("board_class", "")), assumption_text],
		"ENTER THE LIVING ZOO"
	)

func _on_overlay_continued():
	if pending_continue == "begin_tests":
		pending_continue = ""
		phase = Phase.TESTING
		keeper.set_controls_locked(false)
		hud.set_objective("MAKE OR BREAK THE IDEA", "Visit all four evidence habitats. The creature changes when reality contradicts you.")
	elif pending_continue == "board":
		pending_continue = ""
		keeper.set_controls_locked(false)
	elif pending_continue == "decision":
		pending_continue = ""
		keeper.set_controls_locked(false)

func _on_interact():
	if keeper.controls_locked or profile.is_empty():
		return
	if phase == Phase.DECISION:
		var decision = world.nearest_decision(keeper.global_position)
		if not decision.is_empty():
			_choose_decision(decision)
		return
	var station = world.nearest_station(keeper.global_position)
	if station.is_empty():
		return
	var station_id = String(station["id"])
	match station_id:
		"desire", "commitment", "burden", "refusal":
			_open_test(station_id)
		"board":
			_open_board_record()
		"molt":
			if completed_tests >= 4:
				phase = Phase.MOLT
				keeper.set_controls_locked(true)
				hud.show_molt(profile)

func _open_test(test_id: String):
	for test in profile.get("tests", []):
		if String(test.get("id", "")) == test_id:
			keeper.set_controls_locked(true)
			hud.show_test(test)
			return

func _on_test_submitted(test_id: String, strength: int, note: String):
	profile = Analysis.apply_test(profile, test_id, strength, note)
	world.set_station_complete(test_id)
	completed_tests += 1
	var metrics: Dictionary = profile.get("metrics", {})
	creature.set_stage(float(metrics.get("evidence", 0.2)), 1.0 - float(metrics.get("safety", 0.5)), float(profile.get("guardrails", []).size()) / 6.0)
	hud.set_specimen(profile)
	hud.set_progress(completed_tests, 4)
	hud.close_overlay()
	keeper.set_controls_locked(false)
	if completed_tests == 2 and not board_exposed:
		world.reveal_board_record()
		hud.set_objective("THE OFFICIAL STORY MOVED FIRST", "A sealed Board record has opened. You can inspect it, or keep testing the idea.")
	elif completed_tests >= 4:
		world.enable_molt_and_decision()
		hud.set_objective("THE IDEA HAS EVIDENCE NOW", "Take it to the Molt House. Change the real idea before judging it.")
	else:
		hud.set_objective("EVIDENCE CHANGES THE BODY", "%d tests remain. Look for the next lit habitat." % (4 - completed_tests))

func _open_board_record():
	board_exposed = true
	profile["board_exposed"] = true
	world.set_station_complete("board")
	keeper.set_controls_locked(true)
	pending_continue = "board"
	hud.show_message(
		"FAST CITY MANDATE",
		"The Board marked this specimen %s before it hatched because that classification makes it easier to fund, sell, and deploy. Your observed class is %s. The institution is now part of the evidence." % [String(profile.get("board_class", "HAND")), String(profile.get("class", ""))],
		"RETURN TO THE CASE"
	)

func _on_molt_submitted(revised_promise: String, revised_audience: String, guardrails: Array):
	profile = Analysis.molt(profile, revised_promise, revised_audience, guardrails)
	creature.molt(profile, guardrails)
	world.set_station_complete("molt")
	phase = Phase.DECISION
	hud.set_specimen(profile)
	hud.close_overlay()
	keeper.set_controls_locked(false)
	hud.set_objective("THE IDEA CAN LEAVE IN FIVE WAYS", "Walk into a ruling gate: build, molt again, hibernate, sanctuary, or break.")

func _choose_decision(decision: String):
	phase = Phase.END
	keeper.set_controls_locked(true)
	profile = Analysis.decision_record(profile, decision)
	_save_record(profile)
	hud.show_result(profile)

func _on_lens_changed(active: bool):
	if creature != null and creature.visible:
		creature.set_revealed(active)

func _update_prompt():
	if phase == Phase.TESTING or phase == Phase.MOLT:
		var station = world.nearest_station(keeper.global_position)
		if station.is_empty():
			hud.set_prompt("HOLD LENS · SEE WHAT THE IDEA HIDES")
		else:
			var id = String(station["id"])
			if id == "board":
				hud.set_prompt("TOUCH · OPEN THE SEALED CLASSIFICATION")
			elif id == "molt":
				hud.set_prompt("TOUCH · EDIT THE REAL IDEA")
			else:
				hud.set_prompt("TOUCH · RUN %s" % String(station["title"]))

func _save_record(record: Dictionary):
	var path = "user://idea_zoo_real_ideas.json"
	var archive = []
	if FileAccess.file_exists(path):
		var existing = FileAccess.get_file_as_string(path)
		var parsed = JSON.parse_string(existing)
		if parsed is Array:
			archive = parsed
	archive.append(record)
	var file = FileAccess.open(path, FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(archive, "\t"))

func _reset_case():
	completed_tests = 0
	board_exposed = false
	pending_continue = ""
	phase = Phase.INTAKE
	if creature != null:
		creature.visible = false
	for station in world.stations:
		var id = String(station["id"])
		station["completed"] = false
		station["available"] = id in ["desire", "commitment", "burden", "refusal"]
		station["root"].visible = bool(station["available"])

func _ensure_input_actions():
	var bindings = {
		"move_forward": [KEY_W, KEY_UP],
		"move_back": [KEY_S, KEY_DOWN],
		"move_left": [KEY_A, KEY_LEFT],
		"move_right": [KEY_D, KEY_RIGHT],
		"interact": [KEY_E, KEY_ENTER],
		"lens": [KEY_SPACE]
	}
	for action in bindings.keys():
		if not InputMap.has_action(action):
			InputMap.add_action(action, 0.2)
		for keycode in bindings[action]:
			var event = InputEventKey.new()
			event.physical_keycode = keycode
			InputMap.action_add_event(action, event)
