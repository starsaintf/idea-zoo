extends CanvasLayer

signal interact_pressed
signal cycle_pressed
signal lens_changed(active)
signal joystick_changed(value)

var objective_label: Label
var detail_label: Label
var score_label: Label
var chain_label: Label
var prompt_panel: PanelContainer
var prompt_label: Label
var seal_label: Label
var bells_label: Label
var scan_bar: ProgressBar
var stability_bar: ProgressBar
var trust_bar: ProgressBar
var leakage_bar: ProgressBar
var focus_bar: ProgressBar
var focus_label: Label
var message_panel: PanelContainer
var message_title: Label
var message_body: Label
var message_close: Button
var restart_on_close = false
var toast_panel: PanelContainer
var toast_title: Label
var toast_body: Label
var toast_time = 0.0
var reward_label: Label
var reward_time = 0.0
var touch_controls: Control
var lens_button: Button
var interact_button: Button
var cycle_button: Button
var is_touch = false

func _ready():
	is_touch = DisplayServer.is_touchscreen_available() or OS.has_feature("mobile")
	_build_top_ledger()
	_build_prompt()
	_build_message()
	_build_toast()
	_build_controls()

func _process(delta):
	if toast_time > 0.0:
		toast_time -= delta
		if toast_time <= 0.0:
			toast_panel.visible = false
	if reward_time > 0.0:
		reward_time -= delta
		reward_label.modulate.a = clamp(reward_time, 0.0, 1.0)
		if reward_time <= 0.0:
			reward_label.visible = false

func _build_top_ledger():
	var margin = MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_TOP_WIDE)
	margin.add_theme_constant_override("margin_left", 14 if is_touch else 22)
	margin.add_theme_constant_override("margin_top", 10 if is_touch else 18)
	margin.add_theme_constant_override("margin_right", 14 if is_touch else 22)
	add_child(margin)
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 8 if is_touch else 18)
	margin.add_child(row)

	var objective_panel = PanelContainer.new()
	objective_panel.custom_minimum_size = Vector2(300 if is_touch else 430, 0)
	objective_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.035, 0.08, 0.1, 0.91), Color("#d2a45f")))
	row.add_child(objective_panel)
	var objective_box = VBoxContainer.new()
	objective_box.add_theme_constant_override("separation", 2)
	objective_panel.add_child(objective_box)
	objective_label = Label.new()
	objective_label.text = "FIELD ORDER"
	objective_label.add_theme_color_override("font_color", Color("#d2a45f"))
	objective_label.add_theme_font_size_override("font_size", 11 if is_touch else 13)
	objective_box.add_child(objective_label)
	detail_label = Label.new()
	detail_label.text = "Follow the missing minutes."
	detail_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	detail_label.add_theme_font_size_override("font_size", 14 if is_touch else 18)
	detail_label.max_lines_visible = 2 if is_touch else 3
	objective_box.add_child(detail_label)
	var status = HBoxContainer.new()
	status.add_theme_constant_override("separation", 12)
	objective_box.add_child(status)
	score_label = Label.new()
	score_label.text = "SCORE 0000"
	score_label.add_theme_color_override("font_color", Color("#b8d7cf"))
	score_label.add_theme_font_size_override("font_size", 10 if is_touch else 12)
	status.add_child(score_label)
	chain_label = Label.new()
	chain_label.text = "CHAIN —"
	chain_label.add_theme_color_override("font_color", Color("#9d879f"))
	chain_label.add_theme_font_size_override("font_size", 10 if is_touch else 12)
	status.add_child(chain_label)

	var meters = HBoxContainer.new()
	meters.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	meters.add_theme_constant_override("separation", 4 if is_touch else 7)
	row.add_child(meters)
	stability_bar = _make_meter(meters, "CITY", Color("#7f9f79"))
	trust_bar = _make_meter(meters, "TRUST", Color("#d2a45f"))
	leakage_bar = _make_meter(meters, "LEAK", Color("#9a729f"))
	focus_bar = _make_meter(meters, "APPETITE", Color("#a6574e"), true)

func _make_meter(parent: Control, title: String, color: Color, focus := false):
	var panel = PanelContainer.new()
	panel.custom_minimum_size = Vector2(72 if is_touch else 108, 54 if is_touch else 64)
	panel.add_theme_stylebox_override("panel", _panel_style(Color(0.035, 0.08, 0.1, 0.82), Color(0.17, 0.25, 0.27, 1)))
	parent.add_child(panel)
	var box = VBoxContainer.new()
	panel.add_child(box)
	var label = Label.new()
	label.text = title
	label.add_theme_font_size_override("font_size", 8 if is_touch else 10)
	label.add_theme_color_override("font_color", Color("#aaa99f"))
	box.add_child(label)
	if focus:
		focus_label = label
	var bar = ProgressBar.new()
	bar.min_value = 0
	bar.max_value = 100
	bar.value = 50
	bar.show_percentage = false
	bar.custom_minimum_size = Vector2(58 if is_touch else 92, 8 if is_touch else 10)
	bar.add_theme_stylebox_override("background", _flat_style(Color("#1d2c31"), 5))
	bar.add_theme_stylebox_override("fill", _flat_style(color, 5))
	box.add_child(bar)
	return bar

func _build_prompt():
	prompt_panel = PanelContainer.new()
	prompt_panel.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	prompt_panel.position = Vector2(-190, -88 if is_touch else -102)
	prompt_panel.size = Vector2(380, 50)
	prompt_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.02, 0.05, 0.06, 0.92), Color("#d2a45f")))
	add_child(prompt_panel)
	prompt_label = Label.new()
	prompt_label.text = ""
	prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	prompt_label.add_theme_font_size_override("font_size", 13 if is_touch else 16)
	prompt_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	prompt_panel.add_child(prompt_label)
	prompt_panel.visible = false

	seal_label = Label.new()
	seal_label.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	seal_label.position = Vector2(-300, -118)
	seal_label.size = Vector2(270, 34)
	seal_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	seal_label.add_theme_color_override("font_color", Color("#d2a45f"))
	seal_label.add_theme_font_size_override("font_size", 13 if is_touch else 15)
	seal_label.text = "SEAL: EXIT"
	add_child(seal_label)

	bells_label = Label.new()
	bells_label.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	bells_label.position = Vector2(-300, -151)
	bells_label.size = Vector2(270, 30)
	bells_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	bells_label.add_theme_color_override("font_color", Color("#70c8bb"))
	bells_label.add_theme_font_size_override("font_size", 13 if is_touch else 15)
	bells_label.visible = false
	add_child(bells_label)

	scan_bar = ProgressBar.new()
	scan_bar.set_anchors_preset(Control.PRESET_CENTER)
	scan_bar.position = Vector2(-100, 74)
	scan_bar.size = Vector2(200, 10)
	scan_bar.min_value = 0
	scan_bar.max_value = 1
	scan_bar.show_percentage = false
	scan_bar.add_theme_stylebox_override("background", _flat_style(Color(0.05, 0.1, 0.11, 0.8), 5))
	scan_bar.add_theme_stylebox_override("fill", _flat_style(Color("#63d3c7"), 5))
	scan_bar.visible = false
	add_child(scan_bar)

	reward_label = Label.new()
	reward_label.set_anchors_preset(Control.PRESET_CENTER)
	reward_label.position = Vector2(-210, -106)
	reward_label.size = Vector2(420, 50)
	reward_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	reward_label.add_theme_color_override("font_color", Color("#efd093"))
	reward_label.add_theme_font_size_override("font_size", 22 if not is_touch else 18)
	reward_label.visible = false
	add_child(reward_label)

func _build_message():
	message_panel = PanelContainer.new()
	message_panel.set_anchors_preset(Control.PRESET_CENTER)
	var width = 620 if not is_touch else 560
	message_panel.position = Vector2(-width * 0.5, -155)
	message_panel.size = Vector2(width, 310)
	message_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.035, 0.075, 0.085, 0.97), Color("#d2a45f")))
	message_panel.visible = false
	add_child(message_panel)
	var box = VBoxContainer.new()
	box.add_theme_constant_override("separation", 12)
	message_panel.add_child(box)
	message_title = Label.new()
	message_title.add_theme_color_override("font_color", Color("#d2a45f"))
	message_title.add_theme_font_size_override("font_size", 24 if is_touch else 28)
	message_title.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(message_title)
	message_body = Label.new()
	message_body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	message_body.add_theme_font_size_override("font_size", 15 if is_touch else 18)
	message_body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	box.add_child(message_body)
	message_close = Button.new()
	message_close.text = "RETURN TO THE CITY"
	message_close.add_theme_stylebox_override("normal", _button_style(Color("#d2a45f"), Color("#171006")))
	message_close.add_theme_stylebox_override("hover", _button_style(Color("#efd093"), Color("#171006")))
	message_close.pressed.connect(_close_message)
	box.add_child(message_close)

func _build_toast():
	toast_panel = PanelContainer.new()
	toast_panel.set_anchors_preset(Control.PRESET_CENTER_RIGHT)
	toast_panel.position = Vector2(-354, -80)
	toast_panel.size = Vector2(330, 160)
	toast_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.025, 0.065, 0.072, 0.94), Color("#63d3c7")))
	toast_panel.visible = false
	add_child(toast_panel)
	var box = VBoxContainer.new()
	box.add_theme_constant_override("separation", 5)
	toast_panel.add_child(box)
	toast_title = Label.new()
	toast_title.add_theme_color_override("font_color", Color("#63d3c7"))
	toast_title.add_theme_font_size_override("font_size", 18)
	toast_title.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(toast_title)
	toast_body = Label.new()
	toast_body.add_theme_font_size_override("font_size", 14)
	toast_body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(toast_body)

func _build_controls():
	touch_controls = Control.new()
	touch_controls.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	touch_controls.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(touch_controls)
	var joystick_script = preload("res://scripts/virtual_joystick.gd")
	var joystick = joystick_script.new()
	joystick.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	joystick.position = Vector2(20, -198)
	joystick.size = Vector2(174, 174)
	joystick.vector_changed.connect(func(value): joystick_changed.emit(value))
	touch_controls.add_child(joystick)

	interact_button = _round_button("TOUCH", Vector2(142, 142), Color("#d2a45f"))
	interact_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	interact_button.position = Vector2(-164, -174)
	interact_button.pressed.connect(func(): interact_pressed.emit())
	touch_controls.add_child(interact_button)

	lens_button = _round_button("LENS", Vector2(106, 106), Color("#4bb5aa"))
	lens_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	lens_button.position = Vector2(-280, -124)
	lens_button.button_down.connect(func(): lens_changed.emit(true))
	lens_button.button_up.connect(func(): lens_changed.emit(false))
	touch_controls.add_child(lens_button)

	cycle_button = _round_button("SEAL", Vector2(88, 88), Color("#916e99"))
	cycle_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	cycle_button.position = Vector2(-274, -214)
	cycle_button.pressed.connect(func(): cycle_pressed.emit())
	touch_controls.add_child(cycle_button)

	if not is_touch:
		touch_controls.visible = false
		var help = Label.new()
		help.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
		help.position = Vector2(22, -64)
		help.text = "WASD move · right-drag look · hold SPACE for Lens · E touch · Q changes seal/class"
		help.add_theme_font_size_override("font_size", 13)
		help.add_theme_color_override("font_color", Color("#b8b2a7"))
		add_child(help)

func _round_button(text: String, button_size: Vector2, color: Color):
	var button = Button.new()
	button.text = text
	button.size = button_size
	button.custom_minimum_size = button_size
	button.add_theme_font_size_override("font_size", 16)
	button.add_theme_stylebox_override("normal", _button_style(Color(color, 0.72), Color("#071015"), 99))
	button.add_theme_stylebox_override("pressed", _button_style(Color(color, 0.96), Color("#071015"), 99))
	return button

func _panel_style(background: Color, border: Color):
	var style = StyleBoxFlat.new()
	style.bg_color = background
	style.border_color = border
	style.set_border_width_all(1)
	style.set_corner_radius_all(10)
	style.content_margin_left = 13
	style.content_margin_right = 13
	style.content_margin_top = 9
	style.content_margin_bottom = 9
	return style

func _flat_style(color: Color, radius: int):
	var style = StyleBoxFlat.new()
	style.bg_color = color
	style.set_corner_radius_all(radius)
	return style

func _button_style(background: Color, text_color: Color, radius := 12):
	var style = StyleBoxFlat.new()
	style.bg_color = background
	style.set_corner_radius_all(radius)
	style.content_margin_left = 14
	style.content_margin_right = 14
	style.content_margin_top = 11
	style.content_margin_bottom = 11
	return style

func set_objective(title: String, detail: String):
	objective_label.text = title
	detail_label.text = detail

func set_prompt(text: String):
	prompt_label.text = text
	prompt_panel.visible = not text.is_empty()

func set_seal(text: String, visible := true):
	seal_label.text = text if text.begins_with("CLASS") else "SEAL: " + text
	seal_label.visible = visible
	if cycle_button != null:
		cycle_button.text = "CLASS" if text.begins_with("CLASS") else "SEAL"

func set_bells(value: int, visible := true):
	bells_label.text = "BELL-HOURS · " + str(value)
	bells_label.visible = visible

func set_scan(value: float, visible: bool):
	scan_bar.value = value
	scan_bar.visible = visible

func set_metrics(stability: float, trust: float, leakage: float, focus: float):
	stability_bar.value = stability
	trust_bar.value = trust
	leakage_bar.value = leakage
	focus_bar.value = focus

func set_focus_metric(title: String, value: float):
	focus_label.text = title
	focus_bar.value = value
	var color = Color("#a6574e") if title == "APPETITE" else Color("#63d3c7")
	focus_bar.add_theme_stylebox_override("fill", _flat_style(color, 5))

func set_score(score: int, chain: int, best: int):
	score_label.text = "SCORE %04d · BEST %04d" % [score, best]
	chain_label.text = "CHAIN ×%d" % chain if chain > 1 else "CHAIN —"
	chain_label.add_theme_color_override("font_color", Color("#efd093") if chain >= 3 else Color("#9d879f"))

func show_message(title: String, body: String, close_text := "RETURN TO THE CITY", restart := false):
	message_title.text = title
	message_body.text = body
	message_close.text = close_text
	restart_on_close = restart
	message_panel.visible = true

func _close_message():
	if restart_on_close:
		get_tree().reload_current_scene()
	else:
		message_panel.visible = false

func show_brief(title: String, body: String, duration := 5.0):
	toast_title.text = title
	toast_body.text = body
	toast_panel.visible = true
	toast_time = duration

func flash_reward(label: String, points: int, chain: int):
	reward_label.text = "+%d · %s%s" % [points, label, " · CHAIN ×%d" % chain if chain >= 2 else ""]
	reward_label.modulate.a = 1.0
	reward_label.visible = true
	reward_time = 1.35
