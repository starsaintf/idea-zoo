extends SceneTree

const MainGame = preload("res://scripts/idea_lab_main_v8.gd")
const AnalysisV8 = preload("res://scripts/idea_lab_analysis_v8.gd")

var failures: Array = []
var results: Dictionary = {}
var game

func _initialize():
	call_deferred("_run")

func _run():
	var archive_path = "user://idea_zoo_real_ideas.json"
	if FileAccess.file_exists(archive_path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(archive_path))

	DisplayServer.window_set_size(Vector2i(896, 414))
	root.size = Vector2i(896, 414)
	root.content_scale_size = Vector2i(896, 414)

	_test_analysis_boundaries()

	game = MainGame.new()
	root.add_child(game)
	await process_frame
	await process_frame

	_check(game.hud.is_overlay_open(), "intake overlay did not open")
	_check(not game.hud.touch_root.visible, "touch controls were visible above intake")
	_check(game.hud.overlay.z_index > game.hud.touch_root.z_index, "modal overlay is not above touch controls")
	_check(game.phase == MainGame.Phase.INTAKE, "game did not begin in intake phase")

	game.keeper.look_touch_index = 12
	game.keeper.set_controls_locked(true)
	_check(game.keeper.look_touch_index == -1, "camera touch survived a control lock")
	game.hud.joystick.touch_index = 9
	game.hud.joystick.value = Vector2(0.8, 0.2)
	game._reset_transient_input()
	_check(game.hud.joystick.touch_index == -1 and game.hud.joystick.value == Vector2.ZERO, "joystick survived an interrupted overlay transition")

	_fill_intake()
	var intake_button = game.hud.overlay_box.find_child("IntakeSubmit", true, false)
	_check(intake_button != null, "intake submit button missing")
	if intake_button != null:
		game.hud._submit_intake(intake_button)

	_check(game.profile.get("title") == "Meeting Bridge", "intake title was not preserved")
	_check(game.creature.visible, "specimen did not hatch")
	_check(game.phase == MainGame.Phase.HATCHING, "hatching phase did not begin")
	_check(game.hud.is_overlay_open(), "hatching story overlay missing")
	_check(not game.hud.touch_root.visible, "touch controls leaked over hatching story")
	_check(abs(game.creature.evidence_level - float(game.profile["metrics"]["evidence"])) < 0.001, "hatch ignored the idea's actual evidence")

	game.hud.close_overlay()
	game._on_overlay_continued()
	await process_frame
	_check(game.phase == MainGame.Phase.TESTING, "testing phase did not begin")
	_check(game.hud.touch_root.visible, "mobile controls did not return after story overlay")

	await _capture("res://build/idea-lab-world.png")

	game._open_test("desire")
	_check(game.hud.is_overlay_open(), "evidence overlay did not open")
	_check(not game.hud.touch_root.visible, "touch controls leaked over evidence overlay")
	game.hud._submit_test(Button.new())
	_check(game.completed_tests == 0, "test submitted without evidence strength")
	_check(game.hud.is_overlay_open(), "invalid evidence closed the form")

	game.hud.fields["test_strength"].selected = 3
	game.hud.fields["test_note"].text = "Five users described the problem before hearing the idea."
	game.hud._submit_test(Button.new())
	_check(game.completed_tests == 1, "valid evidence did not complete")
	_check(game.completed_test_ids.has("desire"), "completed evidence id was not recorded")
	_check(not game.hud.is_overlay_open(), "evidence overlay did not close after valid submission")

	game._on_test_submitted("desire", 3, "duplicate")
	_check(game.completed_tests == 1, "duplicate evidence counted twice")
	game._on_test_submitted("unknown", 3, "invalid station")
	_check(game.completed_tests == 1, "unknown evidence habitat changed progress")

	_complete_test("commitment", 3, "Two companies signed a paid pilot.")
	_check(_station_available("board"), "Board record did not unlock after two tests")
	_complete_test("burden", 2, "Support, language QA, and device repair were priced.")
	_complete_test("refusal", 2, "Participants can pause translation and delete the session.")

	_check(game.completed_tests == 4, "four distinct evidence tests did not complete")
	_check(game.world.decision_root != null and not game.world.decision_root.visible, "Decision Garden appeared before the Molt")
	_check(_station_available("molt"), "Molt House did not unlock")

	var evidence_before_molt = float(game.profile["metrics"]["evidence"])
	game.keeper.position = Vector3(-7, 0.2, -19)
	game.keeper.set_controls_locked(false)
	game._on_interact()
	_check(game.phase == MainGame.Phase.MOLT and game.hud.is_overlay_open(), "walking into the Molt House did not open it")
	game.hud._cancel_overlay(Button.new())
	_check(game.phase == MainGame.Phase.TESTING, "cancelling the Molt House left the state machine stuck in Molt")
	_check(not game.hud.is_overlay_open(), "cancelling the Molt House left its overlay open")

	game.keeper.position = Vector3(7, 0.2, -19)
	game._on_interact()
	_check(game.hud.is_overlay_open(), "Board Wing became inaccessible after cancelling a Molt")
	game.hud.close_overlay()
	game._on_overlay_continued()
	_check(game.board_exposed, "Board record was not recorded as exposed")

	game.keeper.position = Vector3(-7, 0.2, -19)
	game._on_interact()
	_check(game.phase == MainGame.Phase.MOLT, "Molt House could not be re-entered")
	game.hud._submit_molt(Button.new())
	_check(game.phase == MainGame.Phase.MOLT, "unchanged idea with no rules was accepted as a Molt")
	_check(game.hud.is_overlay_open(), "invalid Molt closed the form")

	game.hud.fields["revised_promise"].text = "Participants hear a translation that preserves uncertainty and can be paused at any time."
	game.hud.fields["revised_audience"].text = "cross-border sales teams running high-stakes meetings"
	for index in [0, 1, 3]:
		game.hud.fields["guardrails"][index].button_pressed = true
	game.hud._submit_molt(Button.new())

	_check(game.phase == MainGame.Phase.DECISION, "valid Molt did not unlock decision phase")
	_check(game.profile.get("guardrails", []).size() == 3, "guardrails were not attached")
	_check(game.world.decision_root.visible, "Decision Garden did not appear after Molt")
	_check(abs(game.creature.evidence_level - evidence_before_molt) < 0.001, "Molt reset accumulated evidence")
	_check(abs(float(game.profile["metrics"]["evidence"]) - evidence_before_molt) < 0.001, "profile evidence changed during Molt")

	game._choose_decision("INVALID")
	_check(game.phase == MainGame.Phase.DECISION, "invalid ruling changed phase")
	game._choose_decision("BUILD")
	_check(game.phase == MainGame.Phase.DECISION and game.pending_decision == "BUILD", "first ruling tap did not arm confirmation")

	var corrupt = FileAccess.open(archive_path, FileAccess.WRITE)
	corrupt.store_string("{not valid json")
	corrupt.close()
	game._choose_decision("BUILD")
	game._choose_decision("BREAK")

	_check(game.profile.get("decision") == "BUILD", "confirmed ruling was not one-shot")
	_check(game.profile.get("next_actions", []).size() == 3, "real-world action plan missing")
	_check(game.last_save_ok, "valid ruling was not saved")
	_check(not game.last_archive_backup_path.is_empty(), "corrupt archive was overwritten without a backup")
	_check(FileAccess.file_exists(game.last_archive_backup_path), "corrupt archive backup file is missing")
	_check(FileAccess.file_exists(archive_path), "specimen archive was not saved")

	var archive = JSON.parse_string(FileAccess.get_file_as_string(archive_path))
	_check(archive is Array and archive.size() == 1, "duplicate verdict created duplicate archive records")
	if archive is Array and archive.size() == 1:
		_check(not String(archive[0].get("record_id", "")).is_empty(), "saved specimen has no unique record id")
	_check(game.hud.is_overlay_open(), "result overlay did not open")
	_check(not game.hud.touch_root.visible, "touch controls leaked over result overlay")

	await _capture("res://build/idea-lab-contract.png")

	game._on_restart_requested()
	await process_frame
	_check(game.phase == MainGame.Phase.INTAKE, "restart did not return to intake")
	_check(game.profile.is_empty(), "restart retained the old profile")
	_check(not game.creature.visible, "restart retained the old creature")
	_check(game.completed_tests == 0 and game.completed_test_ids.is_empty(), "restart retained test progress")
	_check(not game.world.decision_root.visible, "restart retained Decision Garden")
	_check(game.pending_decision.is_empty(), "restart retained an armed decision")
	_check(game.hud.is_overlay_open() and not game.hud.touch_root.visible, "restart did not restore a clean intake overlay")
	_check(_station_available("desire") and _station_available("commitment"), "restart did not restore evidence habitats")
	_check(not _station_available("molt") and not _station_available("board"), "restart retained locked institutions")

	var viewport_size = root.get_visible_rect().size
	_check(int(viewport_size.x) == 896 and int(viewport_size.y) == 414, "contract did not render at 896x414")
	game.hud.close_overlay()
	game.hud._layout_all()
	_check(game.hud.joystick.position.x >= 44, "joystick violates left safe area")
	_check(game.hud.interact_button.position.x + game.hud.interact_button.size.x <= viewport_size.x - 44, "action button violates right safe area")
	_check(game.hud.interact_button.position.y + game.hud.interact_button.size.y <= viewport_size.y - 20, "action button violates bottom safe area")

	results = {
		"passed": failures.is_empty(),
		"failures": failures,
		"viewport": [viewport_size.x, viewport_size.y],
		"archive_records": archive.size() if archive is Array else -1,
		"corrupt_backup_created": FileAccess.file_exists(game.last_archive_backup_path),
		"molt_preserved_evidence": abs(game.creature.evidence_level - evidence_before_molt) < 0.001 if game.creature.visible else true,
		"decision_confirmation": true,
		"overlay_above_touch": game.hud.overlay.z_index > game.hud.touch_root.z_index
	}

	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://build"))
	var file = FileAccess.open("res://build/idea-lab-contract.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(results, "\t"))
	file.close()

	if failures.is_empty():
		print("IDEA_LAB_DEEP_STABILITY_PASS")
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		quit(1)

func _test_analysis_boundaries():
	var maintenance_only = {
		"title": "Maintenance Ledger",
		"idea": "A maintenance workflow for repair crews.",
		"problem": "Repairs are lost between shifts.",
		"promise": "Crews see every unresolved repair.",
		"audience": "municipal repair crews",
		"payer": "city operations office",
		"evidence": "unpaid conversations with repair crews",
		"dependency": "existing work orders",
		"maintenance": "a maintenance coordinator",
		"harm": "managers could use it to blame workers"
	}
	var strict_profile = AnalysisV8.analyze(maintenance_only)
	_check(strict_profile.get("appetite") != "data", "the letters 'ai' inside maintenance triggered a data appetite")
	_check(float(strict_profile["metrics"]["viability"]) < 0.6, "the word unpaid was treated as paid evidence")
	var before = float(strict_profile["metrics"]["evidence"])
	var after = AnalysisV8.apply_test(strict_profile, "desire", 0, "No evidence yet")
	_check(abs(float(after["metrics"]["evidence"]) - before) < 0.001, "recording no evidence increased the evidence score")

func _fill_intake():
	var values = {
		"title": "Meeting Bridge",
		"idea": "A headset that translates business meetings in real time.",
		"problem": "People lose trust and detail when meetings cross languages.",
		"promise": "Two people can hold a ten minute meeting without stopping to manage a phone.",
		"audience": "cross-border sales teams",
		"payer": "export companies",
		"evidence": "interviewed six founders and built a prototype",
		"dependency": "speech models, microphones, and trusted translation",
		"maintenance": "a language quality and hardware support team",
		"harm": "the translation could hide uncertainty or change the force of consent"
	}
	for key in values.keys():
		game.hud.fields[key].text = values[key]
	game.hud.fields["keeper_name"].text = "Jay"
	game.hud.fields["coat"].selected = 1

func _complete_test(test_id: String, strength: int, note: String):
	game._open_test(test_id)
	_check(game.hud.is_overlay_open(), "%s overlay did not open" % test_id)
	game.hud.fields["test_strength"].selected = strength + 1
	game.hud.fields["test_note"].text = note
	game.hud._submit_test(Button.new())

func _station_available(station_id: String) -> bool:
	for station in game.world.stations:
		if station["id"] == station_id:
			return bool(station["available"])
	return false

func _capture(path: String):
	await process_frame
	var image = root.get_texture().get_image()
	if image != null and not image.is_empty():
		image.save_png(path)

func _check(condition: bool, message: String):
	if not condition:
		failures.append(message)
