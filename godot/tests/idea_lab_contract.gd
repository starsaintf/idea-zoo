extends SceneTree

const MainGame = preload("res://scripts/idea_lab_main.gd")

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

	game = MainGame.new()
	root.add_child(game)
	await process_frame
	await process_frame

	_check(game.hud.is_overlay_open(), "intake overlay did not open")
	_check(not game.hud.touch_root.visible, "touch controls were visible above intake")
	_check(game.hud.overlay.z_index > game.hud.touch_root.z_index, "modal overlay is not above touch controls")
	_check(game.phase == MainGame.Phase.INTAKE, "game did not begin in intake phase")

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

	game.hud.close_overlay()
	game._on_overlay_continued()
	await process_frame
	_check(game.phase == MainGame.Phase.TESTING, "testing phase did not begin")
	_check(game.hud.touch_root.visible, "mobile controls did not return after story overlay")

	await _capture("res://build/idea-lab-world.png")

	game._open_test("desire")
	_check(game.hud.is_overlay_open(), "evidence overlay did not open")
	_check(not game.hud.touch_root.visible, "touch controls leaked over evidence overlay")
	var dummy = Button.new()
	game.hud._submit_test(dummy)
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

	_complete_test("commitment", 3, "Two companies signed a paid pilot.")
	_check(game.world.stations.any(func(item): return item["id"] == "board" and item["available"]), "Board record did not unlock after two tests")
	_complete_test("burden", 2, "Support, language QA, and device repair were priced.")
	_complete_test("refusal", 2, "Participants can pause translation and delete the session.")

	_check(game.completed_tests == 4, "four distinct evidence tests did not complete")
	_check(game.world.decision_root != null and not game.world.decision_root.visible, "Decision Garden appeared before the Molt")
	_check(_station_available("molt"), "Molt House did not unlock")

	game.phase = MainGame.Phase.MOLT
	game.hud.show_molt(game.profile)
	game.hud.fields["revised_promise"].text = ""
	game.hud._submit_molt(Button.new())
	_check(game.phase == MainGame.Phase.MOLT, "blank molt changed phase")
	_check(game.hud.is_overlay_open(), "blank molt closed the form")

	game.hud.fields["revised_promise"].text = "Participants hear a translation that preserves uncertainty and can be paused at any time."
	game.hud.fields["revised_audience"].text = "cross-border sales teams running high-stakes meetings"
	for index in [0, 1, 3]:
		game.hud.fields["guardrails"][index].button_pressed = true
	game.hud._submit_molt(Button.new())

	_check(game.phase == MainGame.Phase.DECISION, "valid molt did not unlock decision phase")
	_check(game.profile.get("guardrails", []).size() == 3, "guardrails were not attached")
	_check(game.world.decision_root.visible, "Decision Garden did not appear after molt")

	game._choose_decision("BUILD")
	game._choose_decision("BREAK")
	_check(game.profile.get("decision") == "BUILD", "ruling was not one-shot")
	_check(game.profile.get("next_actions", []).size() == 3, "real-world action plan missing")
	_check(FileAccess.file_exists(archive_path), "specimen archive was not saved")
	var archive = JSON.parse_string(FileAccess.get_file_as_string(archive_path))
	_check(archive is Array and archive.size() == 1, "duplicate verdict created duplicate archive records")
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
		"overlay_above_touch": game.hud.overlay.z_index > game.hud.touch_root.z_index,
		"decision_after_molt": true
	}

	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://build"))
	var file = FileAccess.open("res://build/idea-lab-contract.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(results, "\t"))

	if failures.is_empty():
		print("IDEA_LAB_STABILITY_PASS")
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		quit(1)

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
