extends "res://scripts/idea_lab_main.gd"

const AnalysisV8 = preload("res://scripts/idea_lab_analysis_v8.gd")
const ZooWorldV8 = preload("res://scripts/idea_lab_world.gd")
const KeeperV8 = preload("res://scripts/idea_lab_keeper_v8.gd")
const SpecimenV8 = preload("res://scripts/idea_lab_creature_v8.gd")
const LabHUDV8 = preload("res://scripts/idea_lab_hud.gd")

const VALID_TESTS = ["desire", "commitment", "burden", "refusal"]
const VALID_DECISIONS = ["BUILD", "MOLT", "HIBERNATE", "SANCTUARY", "BREAK"]

var pending_decision := ""
var decision_armed_until := 0
var overlay_was_open := false
var last_archive_backup_path := ""
var last_save_ok := true

func _ready():
	_ensure_input_actions()

	world = ZooWorldV8.new()
	add_child(world)

	keeper = KeeperV8.new()
	keeper.position = world.spawn_point
	add_child(keeper)
	keeper.set_controls_locked(true)

	creature = SpecimenV8.new()
	creature.position = world.hatch_point
	creature.visible = false
	add_child(creature)

	hud = LabHUDV8.new()
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
	overlay_was_open = hud.is_overlay_open()
	_reset_transient_input()

func _process(delta):
	var overlay_open = hud != null and hud.is_overlay_open()
	if overlay_open and not overlay_was_open:
		_reset_transient_input()
	overlay_was_open = overlay_open

	if not pending_decision.is_empty() and Time.get_ticks_msec() > decision_armed_until:
		pending_decision = ""
		if phase == Phase.DECISION and hud != null:
			hud.set_objective("THE IDEA CAN LEAVE IN FIVE WAYS", "Enter a gate, then confirm the same ruling before it becomes permanent.")

	super(delta)

	if phase == Phase.DECISION and not pending_decision.is_empty() and not overlay_open:
		hud.set_prompt("TOUCH AGAIN · CONFIRM %s" % pending_decision)

func _on_intake_submitted(intake: Dictionary, incoming_keeper: Dictionary):
	if transition_locked:
		return
	transition_locked = true
	_reset_case(false)

	keeper_profile = incoming_keeper.duplicate(true)
	profile = AnalysisV8.analyze(intake)
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

func _on_overlay_cancelled():
	transition_locked = false
	if phase == Phase.MOLT:
		phase = Phase.TESTING
	if phase in [Phase.TESTING, Phase.DECISION]:
		keeper.set_controls_locked(false)

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
	elif phase == Phase.MOLT:
		if station_id == "board":
			_open_board_record()
		elif station_id == "molt":
			keeper.set_controls_locked(true)
			hud.show_molt(profile)

func _on_test_submitted(test_id: String, strength: int, note: String):
	if transition_locked or phase != Phase.TESTING:
		return
	if not VALID_TESTS.has(test_id):
		hud.show_inline_error("The Zoo could not identify this evidence habitat.")
		return
	if strength < 0 or strength > 3 or note.strip_edges().length() < 3:
		hud.show_inline_error("Record a valid evidence strength and what actually happened.")
		return
	if completed_test_ids.has(test_id):
		hud.close_overlay()
		keeper.set_controls_locked(false)
		return

	transition_locked = true
	completed_test_ids[test_id] = true
	profile = AnalysisV8.apply_test(profile, test_id, strength, note)
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

func _on_molt_submitted(revised_promise: String, revised_audience: String, guardrails: Array):
	if transition_locked or phase != Phase.MOLT:
		return
	var promise = revised_promise.strip_edges()
	var audience = revised_audience.strip_edges()
	if promise.is_empty() or audience.is_empty():
		hud.show_inline_error("The Molt House needs a measurable promise and a specific first audience.")
		return
	var unchanged = promise == String(profile.get("promise", "")).strip_edges() and audience == String(profile.get("audience", "")).strip_edges()
	if unchanged and guardrails.is_empty():
		hud.show_inline_error("Nothing changed. Rewrite the idea or add at least one rule before calling it a molt.")
		return

	transition_locked = true
	profile = AnalysisV8.molt(profile, promise, audience, guardrails)
	creature.molt(profile, guardrails)
	world.set_station_complete("molt")
	world.enable_decision()
	phase = Phase.DECISION
	pending_decision = ""
	hud.set_specimen(profile)
	hud.close_overlay()
	keeper.set_controls_locked(false)
	hud.set_objective("THE IDEA CAN LEAVE IN FIVE WAYS", "Enter a ruling gate. The same gate must be confirmed before the ruling becomes permanent.")
	transition_locked = false

func _choose_decision(decision: String):
	if transition_locked or phase != Phase.DECISION or not VALID_DECISIONS.has(decision):
		return
	var now = Time.get_ticks_msec()
	if pending_decision != decision or now > decision_armed_until:
		pending_decision = decision
		decision_armed_until = now + 4000
		hud.set_objective("%s RULING ARMED" % decision, "Touch the same gate again within four seconds to make it permanent.")
		return

	transition_locked = true
	pending_decision = ""
	phase = Phase.END
	keeper.set_controls_locked(true)
	profile = AnalysisV8.decision_record(profile, decision)
	last_save_ok = _save_record(profile)
	profile["save_ok"] = last_save_ok
	hud.show_result(profile)
	if not last_save_ok:
		hud.show_inline_error("This browser blocked local storage. Copy the result before closing this tab.")
	transition_locked = false

func _save_record(record: Dictionary) -> bool:
	var path = "user://idea_zoo_real_ideas.json"
	var archive: Array = []
	last_archive_backup_path = ""
	if FileAccess.file_exists(path):
		var raw = FileAccess.get_file_as_string(path)
		var parsed = JSON.parse_string(raw)
		if parsed is Array:
			archive = parsed
		elif not raw.strip_edges().is_empty():
			var stamp = int(Time.get_unix_time_from_system() * 1000.0)
			last_archive_backup_path = "user://idea_zoo_real_ideas_corrupt_%d.json" % stamp
			var backup = FileAccess.open(last_archive_backup_path, FileAccess.WRITE)
			if backup != null:
				backup.store_string(raw)
				backup.close()

	var stored = record.duplicate(true)
	var now_ms = int(Time.get_unix_time_from_system() * 1000.0)
	var slug = String(stored.get("title", "idea")).to_lower().replace(" ", "-")
	stored["saved_at"] = Time.get_datetime_string_from_system()
	stored["record_id"] = "%s-%d-%d" % [slug, now_ms, randi_range(1000, 9999)]
	archive.append(stored)
	var serialized = JSON.stringify(archive, "\t")

	var temp_path = path + ".tmp"
	var temp = FileAccess.open(temp_path, FileAccess.WRITE)
	if temp == null:
		return false
	temp.store_string(serialized)
	temp.close()

	var path_absolute = ProjectSettings.globalize_path(path)
	var temp_absolute = ProjectSettings.globalize_path(temp_path)
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(path_absolute)
	var renamed = DirAccess.rename_absolute(temp_absolute, path_absolute)
	if renamed == OK:
		return true

	var fallback = FileAccess.open(path, FileAccess.WRITE)
	if fallback == null:
		return false
	fallback.store_string(serialized)
	fallback.close()
	return true

func _reset_case(reset_profile := true):
	super(reset_profile)
	pending_decision = ""
	decision_armed_until = 0
	overlay_was_open = hud != null and hud.is_overlay_open()
	_reset_transient_input()

func _reset_transient_input():
	if keeper != null:
		keeper.set_mobile_vector(Vector2.ZERO)
		keeper.set_controls_locked(true)
	if hud != null and hud.joystick != null and hud.joystick.has_method("reset_input"):
		hud.joystick.reset_input()

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
		if InputMap.has_action(action):
			continue
		InputMap.add_action(action, 0.2)
		for keycode in bindings[action]:
			var event = InputEventKey.new()
			event.physical_keycode = keycode
			InputMap.action_add_event(action, event)
