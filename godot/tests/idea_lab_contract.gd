extends SceneTree

const MainGame = preload("res://scripts/idea_lab_main.gd")

var failures = []
var results = {}
var game

func _initialize():
	call_deferred("_run")

func _run():
	root.size = Vector2i(896, 414)
	game = MainGame.new()
	root.add_child(game)
	await process_frame
	await process_frame
	var intake = {
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
	var keeper = {"name": "Jay", "method": "Observer", "body": "Balanced", "skin": "Umber", "coat": "Burgundy"}
	game._on_intake_submitted(intake, keeper)
	_check(game.profile.get("title") == "Meeting Bridge", "intake title was not preserved")
	_check(game.creature.visible, "specimen did not hatch")
	_check(String(game.profile.get("class", "")).length() > 0, "specimen class missing")
	game._on_overlay_continued()
	for test_id in ["desire", "commitment", "burden", "refusal"]:
		game._on_test_submitted(test_id, 2 if test_id != "commitment" else 3, "verified evidence for " + test_id)
	_check(game.completed_tests == 4, "four evidence tests did not complete")
	_check(float(game.profile["metrics"]["evidence"]) > 0.55, "evidence score did not grow")
	game._on_molt_submitted(
		"Participants hear a translation that preserves uncertainty and can be paused at any time.",
		"cross-border sales teams running high-stakes meetings",
		["People can refuse without penalty", "Uncertainty remains visible", "A named keeper owns maintenance"]
	)
	_check(game.phase == MainGame.Phase.DECISION, "molt did not unlock decision phase")
	_check(game.profile.get("guardrails", []).size() == 3, "guardrails were not attached")
	game._choose_decision("BUILD")
	_check(game.profile.get("decision") == "BUILD", "ruling was not recorded")
	_check(game.profile.get("next_actions", []).size() == 3, "real-world action plan missing")
	_check(FileAccess.file_exists("user://idea_zoo_real_ideas.json"), "specimen archive was not saved")
	var viewport_size = root.get_visible_rect().size
	_check(game.hud.joystick.position.x >= 44, "joystick violates left safe area")
	_check(game.hud.interact_button.position.x + game.hud.interact_button.size.x <= viewport_size.x - 44, "action button violates right safe area")
	_check(game.hud.interact_button.position.y + game.hud.interact_button.size.y <= viewport_size.y - 20, "action button violates bottom safe area")
	results = {
		"passed": failures.is_empty(),
		"failures": failures,
		"class": game.profile.get("class"),
		"appetite": game.profile.get("appetite"),
		"decision": game.profile.get("decision"),
		"evidence": game.profile["metrics"]["evidence"],
		"guardrails": game.profile.get("guardrails", []).size(),
		"next_actions": game.profile.get("next_actions", []),
		"viewport": [viewport_size.x, viewport_size.y]
	}
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://build"))
	var file = FileAccess.open("res://build/idea-lab-contract.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(results, "\t"))
	await process_frame
	var image = root.get_texture().get_image()
	if image != null and not image.is_empty():
		image.save_png("res://build/idea-lab-contract.png")
	if failures.is_empty():
		print("IDEA_LAB_CONTRACT_PASS")
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		quit(1)

func _check(condition: bool, message: String):
	if not condition:
		failures.append(message)
