extends SceneTree

const MainGame = preload("res://scripts/main.gd")

var failures: Array[String] = []
var results = {}
var game

func _initialize():
	root.size = Vector2i(896, 414)
	game = MainGame.new()
	root.add_child(game)
	call_deferred("_run_tests")

func _check(condition: bool, label: String):
	results[label] = condition
	if not condition:
		failures.append(label)

func _near(value: float, expected: float, tolerance := 0.5) -> bool:
	return abs(value - expected) <= tolerance

func _run_tests():
	for frame in range(12):
		await process_frame

	game.hud.message_panel.visible = false
	game.hud.is_touch = true
	game.hud.touch_controls.visible = true
	game.hud._layout_touch_controls()
	await process_frame

	_check(_near(game.hud.joystick.position.x, 76.0), "joystick_safe_left")
	_check(_near(game.hud.interact_button.position.x, -194.0), "action_safe_right")
	_check(_near(game.hud.interact_button.position.y, -164.0), "action_safe_bottom")
	_check(_near(game.hud.performance_button.position.x, -198.0), "performance_safe_right")
	_check(game.hud.joystick.size.x >= 154.0, "joystick_touch_target")
	_check(game.hud.interact_button.size.x >= 118.0, "action_touch_target")

	game._apply_performance_profile(0)
	_check(Engine.max_fps == 30, "eco_30_fps_cap")
	_check(_near(game.current_render_scale, 0.62, 0.001), "eco_render_scale")
	_check(game.hud.performance_button.text == "ECO 30", "eco_mode_visible")

	var joystick = game.hud.joystick
	joystick._update_value(joystick.size * 0.5 + Vector2(4, 0))
	_check(joystick.value == Vector2.ZERO, "joystick_dead_zone")
	joystick._update_value(joystick.size * 0.5 + Vector2(joystick.radius, 0))
	_check(joystick.value.length() > 0.98, "joystick_full_range")
	joystick._update_value(joystick.size * 0.5)

	var player = game.player
	var lower_touch = InputEventScreenTouch.new()
	lower_touch.index = 7
	lower_touch.pressed = true
	lower_touch.position = Vector2(520, 340)
	player._unhandled_input(lower_touch)
	_check(player.look_touch_index == -1, "bottom_controls_excluded_from_camera")

	var look_touch = InputEventScreenTouch.new()
	look_touch.index = 8
	look_touch.pressed = true
	look_touch.position = Vector2(520, 175)
	player._unhandled_input(look_touch)
	_check(player.look_touch_index == 8, "camera_zone_claims_touch")
	var yaw_before = player.camera_target_yaw
	var look_drag = InputEventScreenDrag.new()
	look_drag.index = 8
	look_drag.relative = Vector2(120, 45)
	player._unhandled_input(look_drag)
	var yaw_delta = abs(player.camera_target_yaw - yaw_before)
	_check(yaw_delta <= 0.061, "camera_swipe_bounded")
	_check(player.camera_target_pitch >= -0.64 and player.camera_target_pitch <= -0.27, "camera_pitch_clamped")
	var look_release = InputEventScreenTouch.new()
	look_release.index = 8
	look_release.pressed = false
	look_release.position = Vector2(575, 187)
	player._unhandled_input(look_release)
	_check(player.look_touch_index == -1, "camera_touch_released")

	for frame in range(30):
		await process_frame
	var camera_distance = player.camera.global_position.distance_to(player.global_position)
	_check(camera_distance > 1.0 and camera_distance < 8.0, "camera_distance_recovered")
	_check(_near(player.spring_arm.spring_length, 6.6, 0.01), "spring_arm_length")
	_check(_near(player.spring_arm.margin, 0.48, 0.01), "spring_arm_margin")

	game._toggle_diagnostics()
	_check(game.diagnostics_active, "diagnostics_started")
	game.diagnostics_samples.clear()
	for sample in range(120):
		game.diagnostics_samples.append(30.0)
	game.diagnostics_elapsed = 120.0
	game._finish_diagnostics()
	_check(not game.diagnostics_active, "diagnostics_stopped")
	_check("PASS" in game.hud.diagnostics_label.text, "diagnostics_pass_logic")

	var image = root.get_texture().get_image()
	image.save_png("res://build/mobile-test-camera.png")
	results["camera_distance"] = camera_distance
	results["bounded_yaw_delta"] = yaw_delta
	results["render_scale"] = game.current_render_scale
	results["viewport"] = [root.size.x, root.size.y]
	results["failures"] = failures
	var file = FileAccess.open("res://build/mobile-test-results.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(results, "  "))
	file.close()

	if failures.is_empty():
		print("MOBILE_CONTRACT_PASS")
		quit(0)
	else:
		push_error("MOBILE_CONTRACT_FAIL: " + ", ".join(failures))
		quit(1)
