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
var profile: Dictionary = {}
var keeper_profile: Dictionary = {}
var completed_tests := 0
var completed_test_ids: Dictionary = {}
var board_exposed := false
var pending_continue := ""
var transition_locked := false

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
	hud.overlay_cancelled.connect(_on_overlay_cancelled)
	hud.restart_requested.connect(_on_restart_requested)
	hud.interact_pressed.connect(_on_interact)
	hud.joystick_changed.connect(keeper.set_mobile_vector)
	hud.lens_changed.connect(keeper.set_lens)
	keeper.interact_requested.connect(_on_interact)
	keeper.lens_changed.connect(_on_lens_changed)

	var mobile = DisplayServer.is_touchscreen_available() or OS.has_environment("IDEA_ZOO_MOBILE_TEST")
	Engine.max_fps = 30 if mobile else 60
	if mobile:
		get_viewport().scaling_3d_scale = 0.68

func _process(_delta):
	if keeper == null or world == null or hud == null:
		return
	if hud.is_overlay_open():
		hud.set_prompt("")
		return
	if profile.is_empty():
		return
	_update_prompt()

func _on_intake_submitted(intake: Dictionary, incoming_keeper: Dictionary):
	if transition_locked:
		return
	transition_locked = true
	_reset_case(false)

	keeper_profile = incoming_keeper.duplicate(true)
	profile = Analysis.analyze(intake)
	keeper.configure(keeper_profile)
	keeper.position = world.spawn_point
	keeper.velocity = Vector3.ZERO
	keeper.set_controls_locked(true)

	creature.visible = true
	creature.position = world.hatch_point
	creature.configure(profile)
	creature.scale = Vector3.ONE * 0.08
	creature.set_follow_target(keeper)

	var tween = create_tween()
	tween.set_trans(Tween.TRANS_BACK)
	tween.set_ease(Tween.EASE_OUT)
	tween.tween_property(creature, "scale", Vector3.ONE, 1.25)

	phase = Phase.HATCHING
	hud.set_specimen(profile)
	hud.set_progress(0, 4)
	hud.set_objective("TWENTY-FOUR HOURS", "Your real idea has taken a body. Learn what it wants before deciding what it deserves.")

	var assumptions: Array = profile.get("assumptions", [])
	var assumption_text := ""
	for item in assumptions:
		assumption_text += "\n• " + String(item)

	pending_continue = "begin_tests"
	hud.show_message(
		"%s HAS HATCHED" % String(profile.get("creature_name", "THE SPECIMEN")).to_upper(),
		"The Zoo reads it as %s-class. It feeds on %s. The Board has already pencilled in %s before seeing evidence.\n\nCurrent assumptions:%s" % [
			String(profile.get("class", "")),
			String(profile.get("appetite", "")),
			String(profile.get("board_class", "")),
			assumption_text
		],
		"ENTER THE LIVING ZOO"
	)
	transition_locked = false

func _on_overlay_continued():
	match pending_continue:
		"begin_tests":
			phase = Phase.TESTING
			keeper.set_controls_locked(false)
			hud.set_objective("MAKE OR BREAK THE IDEA", "Visit all four evidence habitats. The creature changes when reality contradicts you.")
		"board":
			keeper.set_controls_locked(false)
	pending_continue = ""

func _on_overlay_cancelled():
	transition_locked = false
	if phase in [Phase.TESTING, Phase.MOLT, Phase.DECISION]:
		keeper.set_controls_locked(false)

func _on_restart_requested():
	_reset_case(true)
	hud.show_intake()

func _on_interact():
	if transition_locked or keeper.controls_locked or profile.is_empty() or hud.is_overlay_open():
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
	if phase == Phase.TESTING:
		match station_id:
			"desire", "commitment", "burden", "refusal":
				_open_test(station_id)
			"board":
				_open_board_record()
			"molt":
				if completed_tests == 4:
					phase = Phase.MOLT
					keeper.set_controls_locked(true)
					hud.show_molt(profile)
	elif phase == Phase.MOLT and station_id == "molt":
		keeper.set_controls_locked(true)
		hud.show_molt(profile)

func _open_test(test_id: String):
	if phase != Phase.TESTING or completed_test_ids.has(test_id) or transition_locked:
		return
	for test in profile.get("tests", []):
		if String(test.get("id", "")) == test_id:
			keeper.set_controls_locked(true)
			hud.show_test(test)
			return

func _on_test_submitted(test_id: String, strength: int, note: String):
	if transition_locked or phase != Phase.TESTING:
		return
	if completed_test_ids.has(test_id):
		hud.close_overlay()
		keeper.set_controls_locked(false)
		return

	transition_locked = true
	completed_test_ids[test_id] = true
	profile = Analysis.apply_test(profile, test_id, strength, note)
	world.set_station_complete(test_id)
	completed_tests = completed_test_ids.size()

	var metrics: Dictionary = profile.get("metrics", {})
	creature.set_stage(
		float(metrics.get("evidence", 0.2)),
		1.0 - float(metrics.get("safety", 0.5)),
		float(profile.get("guardrails", []).size()) / 6.0
	)
	hud.set_specimen(profile)
	hud.set_progress(completed_tests, 4)
	hud.close_overlay()
	keeper.set_controls_locked(false)

	if completed_tests == 2 and not board_exposed:
		world.reveal_board_record()
		hud.set_objective("THE OFFICIAL STORY MOVED FIRST", "A sealed Board record has opened. Inspect it, or keep testing the idea.")
	elif completed_tests == 4:
		world.enable_molt()
		hud.set_objective("THE IDEA HAS EVIDENCE NOW", "Take it to the Molt House. The Decision Garden remains sealed until the idea changes.")
	else:
		hud.set_objective("EVIDENCE CHANGES THE BODY", "%d tests remain. Look for the next lit habitat." % (4 - completed_tests))

	transition_locked = false

func _open_board_record():
	if board_exposed or transition_locked:
		return
	transition_locked = true
	board_exposed = true
	profile["board_exposed"] = true
	world.set_station_complete("board")
	keeper.set_controls_locked(true)
	pending_continue = "board"
	hud.show_message(
		"FAST CITY MANDATE",
		"The Board marked this specimen %s before it hatched because that classification makes it easier to fund, sell, and deploy. Your observed class is %s. The institution is now part of the evidence." % [
			String(profile.get("board_class", "HAND")),
			String(profile.get("class", ""))
		],
		"RETURN TO THE CASE"
	)
	transition_locked = false

func _on_molt_submitted(revised_promise: String, revised_audience: String, guardrails: Array):
	if transition_locked or phase != Phase.MOLT:
		return
	if revised_promise.strip_edges().is_empty() or revised_audience.strip_edges().is_empty():
		hud.show_inline_error("The Molt House needs a measurable promise and a specific first audience.")
		return

	transition_locked = true
	profile = Analysis.molt(profile, revised_promise, revised_audience, guardrails)
	creature.molt(profile, guardrails)
	world.set_station_complete("molt")
	world.enable_decision()
	phase = Phase.DECISION
	hud.set_specimen(profile)
	hud.close_overlay()
	keeper.set_controls_locked(false)
	hud.set_objective("THE IDEA CAN LEAVE IN FIVE WAYS", "Walk into a ruling gate: Build, Molt, Hibernate, Sanctuary, or Break.")
	transition_locked = false

func _choose_decision(decision: String):
	if transition_locked or phase != Phase.DECISION:
		return
	transition_locked = true
	phase = Phase.END
	keeper.set_controls_locked(true)
	profile = Analysis.decision_record(profile, decision)
	_save_record(profile)
	hud.show_result(profile)
	transition_locked = false

func _on_lens_changed(active: bool):
	if creature != null and creature.visible:
		creature.set_revealed(active)

func _update_prompt():
	if phase == Phase.DECISION:
		var decision = world.nearest_decision(keeper.global_position)
		if decision.is_empty():
			hud.set_prompt("ENTER A GATE · ISSUE A REAL-WORLD RULING")
		else:
			hud.set_prompt("TOUCH · ISSUE %s RULING" % decision)
		return

	if phase in [Phase.TESTING, Phase.MOLT]:
		var station = world.nearest_station(keeper.global_position)
		if station.is_empty():
			hud.set_prompt("HOLD LENS · SEE WHAT THE IDEA HIDES")
			return
		var station_id = String(station["id"])
		match station_id:
			"board":
				hud.set_prompt("TOUCH · OPEN THE SEALED CLASSIFICATION")
			"molt":
				hud.set_prompt("TOUCH · EDIT THE REAL IDEA")
			_:
				hud.set_prompt("TOUCH · RUN %s" % String(station["title"]))

func _save_record(record: Dictionary):
	var path = "user://idea_zoo_real_ideas.json"
	var archive: Array = []
	if FileAccess.file_exists(path):
		var parsed = JSON.parse_string(FileAccess.get_file_as_string(path))
		if parsed is Array:
			archive = parsed

	var stored = record.duplicate(true)
	stored["saved_at"] = Time.get_datetime_string_from_system()
	stored["record_id"] = "%s-%d" % [String(stored.get("title", "idea")).to_lower().replace(" ", "-"), int(Time.get_unix_time_from_system())]
	archive.append(stored)

	var file = FileAccess.open(path, FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(archive, "\t"))

func _reset_case(reset_profile := true):
	completed_tests = 0
	completed_test_ids.clear()
	board_exposed = false
	pending_continue = ""
	transition_locked = false
	phase = Phase.INTAKE

	if reset_profile:
		profile.clear()
		keeper_profile.clear()

	if keeper != null:
		keeper.set_controls_locked(true)
		keeper.set_mobile_vector(Vector2.ZERO)
		keeper.position = world.spawn_point
		keeper.velocity = Vector3.ZERO

	if creature != null:
		creature.visible = false
		creature.set_revealed(false)
		creature.set_follow_target(null)

	if world != null:
		world.reset_case()

	if hud != null:
		hud.reset_status()

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
