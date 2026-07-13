extends CanvasLayer

signal interact_pressed
signal cycle_pressed
signal lens_changed(active)
signal joystick_changed(value)

var objective_label: Label
var detail_label: Label
var prompt_panel: PanelContainer
var prompt_label: Label
var seal_label: Label
var scan_bar: ProgressBar
var stability_bar: ProgressBar
var trust_bar: ProgressBar
var leakage_bar: ProgressBar
var agitation_bar: ProgressBar
var message_panel: PanelContainer
var message_title: Label
var message_body: Label
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
	_build_controls()

func _build_top_ledger():
	var margin = MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_TOP_WIDE)
	margin.add_theme_constant_override("margin_left", 22)
	margin.add_theme_constant_override("margin_top", 18)
	margin.add_theme_constant_override("margin_right", 22)
	add_child(margin)
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 18)
	margin.add_child(row)
	var objective_panel = PanelContainer.new()
	objective_panel.custom_minimum_size = Vector2(440, 0)
	objective_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.035, 0.08, 0.1, 0.91), Color("#d2a45f")))
	row.add_child(objective_panel)
	var objective_box = VBoxContainer.new()
	objective_box.add_theme_constant_override("separation", 3)
	objective_panel.add_child(objective_box)
	objective_label = Label.new()
	objective_label.text = "FIELD ORDER"
	objective_label.add_theme_color_override("font_color", Color("#d2a45f"))
	objective_label.add_theme_font_size_override("font_size", 13)
	objective_box.add_child(objective_label)
	detail_label = Label.new()
	detail_label.text = "Follow the missing minutes."
	detail_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	detail_label.add_theme_font_size_override("font_size", 18)
	objective_box.add_child(detail_label)
	var meters = HBoxContainer.new()
	meters.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	meters.add_theme_constant_override("separation", 7)
	row.add_child(meters)
	stability_bar = _make_meter(meters, "CITY", Color("#7f9f79"))
	trust_bar = _make_meter(meters, "TRUST", Color("#d2a45f"))
	leakage_bar = _make_meter(meters, "LEAKAGE", Color("#9a729f"))
	agitation_bar = _make_meter(meters, "APPETITE", Color("#a6574e"))

func _make_meter(parent: Control, title: String, color: Color):
	var panel = PanelContainer.new()
	panel.custom_minimum_size = Vector2(112, 64)
	panel.add_theme_stylebox_override("panel", _panel_style(Color(0.035, 0.08, 0.1, 0.82), Color(0.17, 0.25, 0.27, 1)))
	parent.add_child(panel)
	var box = VBoxContainer.new()
	panel.add_child(box)
	var label = Label.new()
	label.text = title
	label.add_theme_font_size_override("font_size", 10)
	label.add_theme_color_override("font_color", Color("#aaa99f"))
	box.add_child(label)
	var bar = ProgressBar.new()
	bar.min_value = 0
	bar.max_value = 100
	bar.value = 50
	bar.show_percentage = false
	bar.custom_minimum_size = Vector2(96, 10)
	bar.add_theme_stylebox_override("background", _flat_style(Color("#1d2c31"), 5))
	bar.add_theme_stylebox_override("fill", _flat_style(color, 5))
	box.add_child(bar)
	return bar

func _build_prompt():
	prompt_panel = PanelContainer.new()
	prompt_panel.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	prompt_panel.position = Vector2(-185, -102)
	prompt_panel.size = Vector2(370, 54)
	prompt_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.02, 0.05, 0.06, 0.92), Color("#d2a45f")))
	add_child(prompt_panel)
	prompt_label = Label.new()
	prompt_label.text = ""
	prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	prompt_label.add_theme_font_size_override("font_size", 16)
	prompt_panel.add_child(prompt_label)
	prompt_panel.visible = false
	seal_label = Label.new()
	seal_label.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	seal_label.position = Vector2(-295, -126)
	seal_label.size = Vector2(260, 36)
	seal_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	seal_label.add_theme_color_override("font_color", Color("#d2a45f"))
	seal_label.add_theme_font_size_override("font_size", 15)
	seal_label.text = "SEAL: EXIT"
	add_child(seal_label)
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

func _build_message():
	message_panel = PanelContainer.new()
	message_panel.set_anchors_preset(Control.PRESET_CENTER)
	message_panel.position = Vector2(-280, -145)
	message_panel.size = Vector2(560, 290)
	message_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.035, 0.075, 0.085, 0.97), Color("#d2a45f")))
	message_panel.visible = false
	add_child(message_panel)
	var box = VBoxContainer.new()
	box.add_theme_constant_override("separation", 14)
	message_panel.add_child(box)
	message_title = Label.new()
	message_title.add_theme_color_override("font_color", Color("#d2a45f"))
	message_title.add_theme_font_size_override("font_size", 28)
	box.add_child(message_title)
	message_body = Label.new()
	message_body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	message_body.add_theme_font_size_override("font_size", 18)
	message_body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	box.add_child(message_body)
	var close = Button.new()
	close.text = "RETURN TO THE CITY"
	close.add_theme_stylebox_override("normal", _button_style(Color("#d2a45f"), Color("#171006")))
	close.add_theme_stylebox_override("hover", _button_style(Color("#efd093"), Color("#171006")))
	close.pressed.connect(func(): message_panel.visible = false)
	box.add_child(close)

func _build_controls():
	touch_controls = Control.new()
	touch_controls.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	touch_controls.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(touch_controls)
	var joystick_script = preload("res://scripts/virtual_joystick.gd")
	var joystick = joystick_script.new()
	joystick.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	joystick.position = Vector2(24, -204)
	joystick.size = Vector2(180, 180)
	joystick.vector_changed.connect(func(value): joystick_changed.emit(value))
	touch_controls.add_child(joystick)
	interact_button = _round_button("TOUCH", Vector2(150, 150), Color("#d2a45f"))
	interact_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	interact_button.position = Vector2(-174, -184)
	interact_button.pressed.connect(func(): interact_pressed.emit())
	touch_controls.add_child(interact_button)
	lens_button = _round_button("LENS", Vector2(112, 112), Color("#4bb5aa"))
	lens_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	lens_button.position = Vector2(-294, -130)
	lens_button.button_down.connect(func(): lens_changed.emit(true))
	lens_button.button_up.connect(func(): lens_changed.emit(false))
	touch_controls.add_child(lens_button)
	cycle_button = _round_button("SEAL", Vector2(92, 92), Color("#916e99"))
	cycle_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	cycle_button.position = Vector2(-286, -225)
	cycle_button.pressed.connect(func(): cycle_pressed.emit())
	touch_controls.add_child(cycle_button)
	if not is_touch:
		touch_controls.visible = false
		var help = Label.new()
		help.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
		help.position = Vector2(22, -64)
		help.text = "WASD move · right-drag look · hold SPACE for the Lens · E touch · Q change seal"
		help.add_theme_font_size_override("font_size", 13)
		help.add_theme_color_override("font_color", Color("#b8b2a7"))
		add_child(help)

func _round_button(text: String, button_size: Vector2, color: Color):
	var button = Button.new()
	button.text = text
	button.size = button_size
	button.custom_minimum_size = button_size
	button.add_theme_font_size_override("font_size", 17)
	button.add_theme_stylebox_override("normal", _button_style(Color(color, 0.72), Color("#071015"), 99))
	button.add_theme_stylebox_override("pressed", _button_style(Color(color, 0.96), Color("#071015"), 99))
	return button

func _panel_style(background: Color, border: Color):
	var style = StyleBoxFlat.new()
	style.bg_color = background
	style.border_color = border
	style.set_border_width_all(1)
	style.set_corner_radius_all(10)
	style.content_margin_left = 15
	style.content_margin_right = 15
	style.content_margin_top = 11
	style.content_margin_bottom = 11
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
	seal_label.text = "SEAL: " + text
	seal_label.visible = visible

func set_scan(value: float, visible: bool):
	scan_bar.value = value
	scan_bar.visible = visible

func set_metrics(stability: float, trust: float, leakage: float, agitation: float):
	stability_bar.value = stability
	trust_bar.value = trust
	leakage_bar.value = leakage
	agitation_bar.value = agitation

func show_message(title: String, body: String):
	message_title.text = title
	message_body.text = body
	message_panel.visible = true
