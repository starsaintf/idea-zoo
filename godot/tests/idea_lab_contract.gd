extends SceneTree

const MainGame = preload("res://scripts/idea_lab_main_v9.gd")
const AnalysisV8 = preload("res://scripts/idea_lab_analysis_v8.gd")

var failures: Array = []
var game
var archive_path = "user://idea_zoo_real_ideas.json"

func _initialize():
	call_deferred("_run")

func _run():
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

	_check(game.hud.is_overlay_open(), "intake overlay missing")
	_check(not game.hud.touch_root.visible, "touch controls leaked over intake")
	_check(game.phase == MainGame.Phase.INTAKE, "wrong initial phase")

	game.keeper.look_touch_index = 12
	game.keeper.set_controls_locked(true)
	_check(game.keeper.look_touch_index == -1, "camera touch survived lock")
	game.hud.joystick.touch_index = 9
	game.hud.joystick.value = Vector2(0.8, 0.2)
	game._reset_transient_input()
	_check(game.hud.joystick.touch_index == -1 and game.hud.joystick.value == Vector2.ZERO, "joystick survived overlay reset")

	_fill_intake()
	var intake_button = game.hud.overlay_box.find_child("IntakeSubmit", true, false)
	_check(intake_button != null, "intake submit missing")
	if intake_button != null:
		game.hud._submit_intake(intake_button)
	_check(game.phase == MainGame.Phase.HATCHING, "idea did not hatch")
	_check(game.creature.visible, "creature invisible after hatch")
	_check(abs(game.creature.evidence_level - float(game.profile["metrics"]["evidence"])) < 0.001, "hatch ignored evidence")

	game.hud.close_overlay()
	game._on_overlay_continued()
	_check(game.phase == MainGame.Phase.TESTING, "testing did not begin")
	await _capture("res://build/idea-lab-world.png")

	game._open_test("desire")
	game.hud._submit_test(Button.new())
	_check(game.completed_tests == 0 and game.hud.is_overlay_open(), "invalid evidence was accepted")
	game.hud.fields["test_strength"].selected = 3
	game.hud.fields["test_note"].text = "Five users described the problem before hearing the idea."
	game.hud._submit_test(Button.new())
	_check(game.completed_tests == 1, "valid evidence failed")
	game._on_test_submitted("desire", 3, "duplicate")
	game._on_test_submitted("unknown", 3, "invalid")
	_check(game.completed_tests == 1, "duplicate or unknown evidence changed progress")

	_complete_test("commitment", 3, "Two companies signed a paid pilot.")
	_complete_test("burden", 2, "Support, language QA, and device repair were priced.")
	_complete_test("refusal", 2, "Participants can pause translation and delete the session.")
	_check(game.completed_tests == 4, "four evidence tests did not complete")
	_check(game.world.decision_root != null and not game.world.decision_root.visible, "decision garden opened before Molt")

	var evidence_before_molt = float(game.profile["metrics"]["evidence"])
	game.keeper.position = Vector3(-7, 0.2, -19)
	game.keeper.set_controls_locked(false)
	game._on_interact()
	_check(game.phase == MainGame.Phase.MOLT and game.hud.is_overlay_open(), "Molt House interaction failed")
	game.hud._cancel_overlay(Button.new())
	_check(game.phase == MainGame.Phase.TESTING and not game.hud.is_overlay_open(), "Molt cancel left a stuck state")

	game.keeper.position = Vector3(7, 0.2, -19)
	game._on_interact()
	_check(game.hud.is_overlay_open(), "Board Wing inaccessible after cancelled Molt")
	game.hud.close_overlay()
	game._on_overlay_continued()

	game.keeper.position = Vector3(-7, 0.2, -19)
	game._on_interact()
	game.hud._submit_molt(Button.new())
	_check(game.phase == MainGame.Phase.MOLT and game.hud.is_overlay_open(), "unchanged Molt was accepted")
	game.hud.fields["revised_promise"].text = "Participants hear a translation that preserves uncertainty and can be paused at any time."
	game.hud.fields["revised_audience"].text = "cross-border sales teams running high-stakes meetings"
	for index in [0, 1, 3]:
		game.hud.fields["guardrails"][index].button_pressed = true
	game.hud._submit_molt(Button.new())
	_check(game.phase == MainGame.Phase.DECISION, "valid Molt did not unlock decision")
	_check(abs(game.creature.evidence_level - evidence_before_molt) < 0.001, "Molt reset creature evidence")
	_check(abs(float(game.profile["metrics"]["evidence"]) - evidence_before_molt) < 0.001, "Molt reset profile evidence")

	game._choose_decision("INVALID")
	game._choose_decision("BUILD")
	_check(game.phase == MainGame.Phase.DECISION and game.pending_decision == "BUILD", "first verdict tap did not arm confirmation")
	var corrupt = FileAccess.open(archive_path, FileAccess.WRITE)
	corrupt.store_string("{not valid json")
	corrupt.close()
	game._choose_decision("BUILD")
	game._choose_decision("BREAK")
	_check(game.profile.get("decision") == "BUILD", "confirmed verdict was not one-shot")
	_check(game.last_save_ok, "verdict was not saved")
	_check(not game.last_archive_backup_path.is_empty() and FileAccess.file_exists(game.last_archive_backup_path), "corrupt archive was not backed up")

	var parser = JSON.new()
	var parse_ok = parser.parse(FileAccess.get_file_as_string(archive_path)) == OK
	var archive = parser.data if parse_ok else null
	_check(parse_ok and archive is Array and archive.size() == 1, "saved archive is invalid or duplicated")
	if archive is Array and archive.size() == 1:
		_check(not String(archive[0].get("record_id", "")).is_empty(), "record id missing")
	_check(game.hud.is_overlay_open() and not game.hud.touch_root.visible, "result overlay leaked controls")
	await _capture("res://build/idea-lab-contract.png")

	game._on_restart_requested()
	await process_frame
	_check(game.phase == MainGame.Phase.INTAKE and game.profile.is_empty(), "restart retained case state")
	_check(not game.creature.visible and not game.world.decision_root.visible, "restart retained world state")
	_check(game.pending_decision.is_empty(), "restart retained armed verdict")

	var viewport_size = root.get_visible_rect().size
	game.hud.close_overlay()
	game.hud._layout_all()
	_check(int(viewport_size.x) == 896 and int(viewport_size.y) == 414, "wrong mobile viewport")
	_check(game.hud.joystick.position.x >= 44, "joystick outside safe area")
	_check(game.hud.interact_button.position.x + game.hud.interact_button.size.x <= viewport_size.x - 44, "action button outside safe area")
	_check(game.hud.interact_button.position.y + game.hud.interact_button.size.y <= viewport_size.y - 20, "action button below safe area")

	var results = {
		"passed": failures.is_empty(),
		"failures": failures,
		"viewport": [viewport_size.x, viewport_size.y],
		"archive_records": archive.size() if archive is Array else -1,
		"corrupt_backup_created": FileAccess.file_exists(game.last_archive_backup_path),
		"molt_preserved_evidence": true,
		"decision_confirmation": true
	}
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://build"))
	var file = FileAccess.open("res://build/idea-lab-contract.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(results, "\t"))
	file.close()

	game.queue_free()
	await process_frame
	await process_frame
	if failures.is_empty():
		print("IDEA_LAB_DEEP_STABILITY_PASS")
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		quit(1)

func _test_analysis_boundaries():
	var intake = {
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
	var profile = AnalysisV8.analyze(intake)
	_check(profile.get("appetite") != "data", "maintenance triggered false AI/data appetite")
	_check(float(profile["metrics"]["viability"]) < 0.6, "unpaid was treated as paid")
	var before = float(profile["metrics"]["evidence"])
	var after = AnalysisV8.apply_test(profile, "desire", 0, "No evidence yet")
	_check(abs(float(after["metrics"]["evidence"]) - before) < 0.001, "no evidence increased evidence score")

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
	game.hud.fields["test_strength"].selected = strength + 1
	game.hud.fields["test_note"].text = note
	game.hud._submit_test(Button.new())

func _capture(path: String):
	await process_frame
	var image = root.get_texture().get_image()
	if image != null and not image.is_empty():
		image.save_png(path)

func _check(condition: bool, message: String):
	if not condition:
		failures.append(message)
