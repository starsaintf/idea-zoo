extends CanvasLayer

signal intake_submitted(intake, keeper_profile)
signal test_submitted(test_id, strength, note)
signal molt_submitted(promise, audience, guardrails)
signal overlay_continued
signal interact_pressed
signal joystick_changed(value)
signal lens_changed(active)

const Joystick = preload("res://scripts/virtual_joystick.gd")

var objective_label: Label
var specimen_label: Label
var metrics_label: Label
var prompt_label: Label
var progress_label: Label
var overlay: PanelContainer
var overlay_box: VBoxContainer
var touch_root: Control
var joystick
var interact_button: Button
var lens_button: Button
var fields = {}
var current_test_id = ""
var is_touch = false
var paper = Color("#e5dcc9")
var ink = Color("#071015")
var teal = Color("#56b7ad")
var brass = Color("#d2a45f")
var rust = Color("#aa5549")

func _ready():
	is_touch = DisplayServer.is_touchscreen_available() or OS.has_environment("IDEA_ZOO_MOBILE_TEST")
	_build_status()
	_build_overlay()
	_build_touch_controls()
	get_viewport().size_changed.connect(_layout_touch_controls)
	show_intake()

func _build_status():
	var top = PanelContainer.new()
	top.set_anchors_preset(Control.PRESET_TOP_WIDE)
	top.offset_left = 18
	top.offset_top = 14
	top.offset_right = -18
	top.offset_bottom = 102
	top.add_theme_stylebox_override("panel", _panel(Color(0.02, 0.055, 0.07, 0.92), teal))
	add_child(top)
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 20)
	top.add_child(row)
	objective_label = _label("WHISPER GATE", 17, paper)
	objective_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(objective_label)
	specimen_label = _label("NO SPECIMEN", 16, brass)
	specimen_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(specimen_label)
	metrics_label = _label("EVIDENCE 0 · SAFETY 0", 15, teal)
	metrics_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	metrics_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(metrics_label)
	progress_label = _label("0 / 4 TESTS", 15, paper)
	progress_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	row.add_child(progress_label)
	prompt_label = _label("", 17, paper)
	prompt_label.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	prompt_label.offset_left = 180
	prompt_label.offset_right = -180
	prompt_label.offset_top = -72
	prompt_label.offset_bottom = -24
	prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt_label.add_theme_stylebox_override("normal", _panel(Color(0.02, 0.055, 0.07, 0.9), brass))
	add_child(prompt_label)

func _build_overlay():
	overlay = PanelContainer.new()
	overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.add_theme_stylebox_override("panel", _panel(Color(0.015, 0.04, 0.052, 0.985), brass))
	add_child(overlay)
	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 34)
	margin.add_theme_constant_override("margin_right", 34)
	margin.add_theme_constant_override("margin_top", 24)
	margin.add_theme_constant_override("margin_bottom", 24)
	overlay.add_child(margin)
	var scroll = ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	margin.add_child(scroll)
	overlay_box = VBoxContainer.new()
	overlay_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	overlay_box.add_theme_constant_override("separation", 12)
	scroll.add_child(overlay_box)

func _build_touch_controls():
	touch_root = Control.new()
	touch_root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	touch_root.mouse_filter = Control.MOUSE_FILTER_PASS
	add_child(touch_root)
	joystick = Joystick.new()
	joystick.size = Vector2(154, 154)
	joystick.vector_changed.connect(func(value): joystick_changed.emit(value))
	touch_root.add_child(joystick)
	interact_button = _button("TOUCH", teal, 18)
	interact_button.size = Vector2(118, 118)
	interact_button.pressed.connect(func(): interact_pressed.emit())
	touch_root.add_child(interact_button)
	lens_button = _button("LENS", brass, 17)
	lens_button.size = Vector2(92, 92)
	lens_button.button_down.connect(func(): lens_changed.emit(true))
	lens_button.button_up.connect(func(): lens_changed.emit(false))
	touch_root.add_child(lens_button)
	touch_root.visible = is_touch
	_layout_touch_controls()

func _layout_touch_controls():
	if touch_root == null:
		return
	var viewport_size = get_viewport().get_visible_rect().size
	joystick.position = Vector2(76, viewport_size.y - 200)
	interact_button.position = Vector2(viewport_size.x - 194, viewport_size.y - 164)
	lens_button.position = Vector2(viewport_size.x - 302, viewport_size.y - 138)

func show_intake():
	_clear_overlay()
	overlay.visible = true
	touch_root.visible = false
	_add_title("THE WHISPER GATE")
	_add_body("Bring an idea you may actually spend years building. The Zoo will give it a body, test what it wants, and help you decide whether it deserves more of your life.")
	var keeper_row = HBoxContainer.new()
	keeper_row.add_theme_constant_override("separation", 10)
	overlay_box.add_child(keeper_row)
	fields["keeper_name"] = _line("Keeper name", "Jay")
	keeper_row.add_child(fields["keeper_name"])
	fields["method"] = _options(["Observer", "Molter", "Shepherd", "Dissenter"])
	keeper_row.add_child(fields["method"])
	fields["body"] = _options(["Balanced", "Broad", "Tall", "Compact"])
	keeper_row.add_child(fields["body"])
	fields["skin"] = _options(["Ebony", "Umber", "Copper", "Sienna", "Sand"])
	keeper_row.add_child(fields["skin"])
	fields["coat"] = _options(["Teal", "Burgundy", "Ochre", "Indigo", "Moss"])
	keeper_row.add_child(fields["coat"])
	_add_section("THE REAL IDEA")
	fields["title"] = _line("Name the idea", "")
	overlay_box.add_child(fields["title"])
	fields["idea"] = _text("Describe it plainly. No pitch language.", "")
	overlay_box.add_child(fields["idea"])
	fields["problem"] = _line("What painful problem exists without it?", "")
	overlay_box.add_child(fields["problem"])
	fields["promise"] = _line("What measurable outcome does it promise?", "")
	overlay_box.add_child(fields["promise"])
	var market_row = HBoxContainer.new()
	market_row.add_theme_constant_override("separation", 10)
	overlay_box.add_child(market_row)
	fields["audience"] = _line("First specific user", "")
	market_row.add_child(fields["audience"])
	fields["payer"] = _line("Who pays or commits resources?", "")
	market_row.add_child(fields["payer"])
	fields["evidence"] = _line("What have you already tested?", "")
	overlay_box.add_child(fields["evidence"])
	fields["dependency"] = _line("What must exist for it to work?", "")
	overlay_box.add_child(fields["dependency"])
	fields["maintenance"] = _line("Who keeps it alive after launch?", "")
	overlay_box.add_child(fields["maintenance"])
	fields["harm"] = _line("What is its cruelest plausible use?", "")
	overlay_box.add_child(fields["harm"])
	var submit = _button("HATCH THIS IDEA", brass, 20)
	submit.pressed.connect(_submit_intake)
	overlay_box.add_child(submit)

func show_message(title: String, body: String, button_text := "CONTINUE"):
	_clear_overlay()
	overlay.visible = true
	_add_title(title)
	_add_body(body)
	var button = _button(button_text, brass, 19)
	button.pressed.connect(_continue_overlay)
	overlay_box.add_child(button)

func show_test(test: Dictionary):
	_clear_overlay()
	overlay.visible = true
	current_test_id = String(test.get("id", ""))
	_add_title(String(test.get("title", "EVIDENCE TEST")))
	_add_body(String(test.get("question", "What did reality show?")))
	_add_section("REAL-WORLD MISSION")
	_add_body(String(test.get("mission", "Run a falsifiable test.")))
	var options = ["No evidence yet", "Anecdote or weak signal", "Repeated behaviour", "Money, signed pilot, or costly commitment"]
	for index in range(options.size()):
		var button = _button("%d · %s" % [index, options[index]], teal if index > 1 else brass, 16)
		button.pressed.connect(_choose_test_strength.bind(index))
		overlay_box.add_child(button)
	fields["test_note"] = _text("What happened? Record names, numbers, or the strongest contradiction.", "")
	overlay_box.add_child(fields["test_note"])

func show_molt(profile: Dictionary):
	_clear_overlay()
	overlay.visible = true
	_add_title("THE MOLT HOUSE")
	_add_body("Do not defend the original shape. Preserve the useful core and change what evidence has made indefensible.")
	fields["revised_promise"] = _text("Revised measurable promise", String(profile.get("promise", "")))
	overlay_box.add_child(fields["revised_promise"])
	fields["revised_audience"] = _line("Narrower first audience", String(profile.get("audience", "")))
	overlay_box.add_child(fields["revised_audience"])
	_add_section("RULES TO ADD TO THE CREATURE")
	fields["guardrails"] = []
	for text in ["People can refuse without penalty", "A named keeper owns maintenance", "The idea has a clear boundary", "Uncertainty remains visible", "Users can appeal, delete, or recall", "The idea expires unless renewed"]:
		var check = CheckButton.new()
		check.text = text
		check.add_theme_font_size_override("font_size", 16)
		check.add_theme_color_override("font_color", paper)
		overlay_box.add_child(check)
		fields["guardrails"].append(check)
	var button = _button("LET IT MOLT", Color("#8e7bb8"), 20)
	button.pressed.connect(_submit_molt)
	overlay_box.add_child(button)

func show_result(record: Dictionary):
	_clear_overlay()
	overlay.visible = true
	touch_root.visible = false
	_add_title("RULING · %s" % String(record.get("decision", "UNDECIDED")))
	_add_body(String(record.get("verdict_reason", "")))
	_add_section("THE IDEA THAT LEAVES THE ZOO")
	_add_body("%s\n\nPromise: %s\nFirst user: %s\nClass: %s · Appetite: %s" % [String(record.get("title", "")), String(record.get("promise", "")), String(record.get("audience", "")), String(record.get("class", "")), String(record.get("appetite", ""))])
	_add_section("NEXT REAL-WORLD ACTIONS")
	var actions: Array = record.get("next_actions", [])
	for index in range(actions.size()):
		_add_body("%d. %s" % [index + 1, String(actions[index])])
	_add_body("The complete specimen record has been saved inside this device. Your idea is not finished; it now has evidence, constraints, and a deliberate next state.")
	var again = _button("BRING ANOTHER IDEA", brass, 18)
	again.pressed.connect(show_intake)
	overlay_box.add_child(again)

func set_objective(title: String, detail: String):
	objective_label.text = "%s\n%s" % [title, detail]

func set_specimen(profile: Dictionary):
	specimen_label.text = "%s\n%s · feeds on %s" % [String(profile.get("creature_name", "SPECIMEN")), String(profile.get("class", "")), String(profile.get("appetite", ""))]
	set_metrics(profile)

func set_metrics(profile: Dictionary):
	var metrics: Dictionary = profile.get("metrics", {})
	metrics_label.text = "EVIDENCE %.0f%% · DESIRE %.0f%% · VIABILITY %.0f%% · SAFETY %.0f%%" % [float(metrics.get("evidence", 0.0)) * 100.0, float(metrics.get("desirability", 0.0)) * 100.0, float(metrics.get("viability", 0.0)) * 100.0, float(metrics.get("safety", 0.0)) * 100.0]

func set_progress(completed: int, total: int):
	progress_label.text = "%d / %d TESTS" % [completed, total]

func set_prompt(text: String):
	prompt_label.text = text
	prompt_label.visible = not text.is_empty()

func close_overlay():
	overlay.visible = false
	touch_root.visible = is_touch

func _submit_intake():
	var intake = {}
	for key in ["title", "idea", "problem", "promise", "audience", "payer", "evidence", "dependency", "maintenance", "harm"]:
		var control = fields[key]
		intake[key] = control.text.strip_edges()
	if String(intake["title"]).is_empty() or String(intake["idea"]).is_empty() or String(intake["promise"]).is_empty():
		_add_body("The Gate needs at least a name, a plain description, and a measurable promise.", rust)
		return
	var keeper = {
		"name": fields["keeper_name"].text.strip_edges(),
		"method": fields["method"].get_item_text(fields["method"].selected),
		"body": fields["body"].get_item_text(fields["body"].selected),
		"skin": fields["skin"].get_item_text(fields["skin"].selected),
		"coat": fields["coat"].get_item_text(fields["coat"].selected)
	}
	intake_submitted.emit(intake, keeper)

func _choose_test_strength(strength: int):
	var note = fields["test_note"].text.strip_edges()
	test_submitted.emit(current_test_id, strength, note)

func _submit_molt():
	var guardrails = []
	for check in fields["guardrails"]:
		if check.button_pressed:
			guardrails.append(check.text)
	molt_submitted.emit(fields["revised_promise"].text, fields["revised_audience"].text, guardrails)

func _continue_overlay():
	close_overlay()
	overlay_continued.emit()

func _clear_overlay():
	fields.clear()
	for child in overlay_box.get_children():
		child.queue_free()

func _add_title(text: String):
	var node = _label(text, 31, brass)
	node.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	overlay_box.add_child(node)

func _add_section(text: String):
	overlay_box.add_child(_label(text, 18, teal))

func _add_body(text: String, color := Color("#e5dcc9")):
	var node = _label(text, 17, color)
	node.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	overlay_box.add_child(node)

func _label(text: String, size: int, color: Color) -> Label:
	var node = Label.new()
	node.text = text
	node.add_theme_font_size_override("font_size", size)
	node.add_theme_color_override("font_color", color)
	return node

func _line(placeholder: String, value: String) -> LineEdit:
	var node = LineEdit.new()
	node.placeholder_text = placeholder
	node.text = value
	node.custom_minimum_size = Vector2(180, 48)
	node.add_theme_font_size_override("font_size", 16)
	return node

func _text(placeholder: String, value: String) -> TextEdit:
	var node = TextEdit.new()
	node.placeholder_text = placeholder
	node.text = value
	node.custom_minimum_size = Vector2(240, 88)
	node.add_theme_font_size_override("font_size", 16)
	return node

func _options(items: Array) -> OptionButton:
	var node = OptionButton.new()
	node.custom_minimum_size = Vector2(130, 48)
	for item in items:
		node.add_item(String(item))
	return node

func _button(text: String, color: Color, size: int) -> Button:
	var node = Button.new()
	node.text = text
	node.custom_minimum_size = Vector2(150, 52)
	node.add_theme_font_size_override("font_size", size)
	node.add_theme_stylebox_override("normal", _panel(Color(color, 0.86), color))
	node.add_theme_stylebox_override("pressed", _panel(color.lightened(0.12), paper))
	node.add_theme_color_override("font_color", ink)
	return node

func _panel(background: Color, border: Color) -> StyleBoxFlat:
	var style = StyleBoxFlat.new()
	style.bg_color = background
	style.border_color = border
	style.set_border_width_all(1)
	style.set_corner_radius_all(12)
	style.content_margin_left = 14
	style.content_margin_right = 14
	style.content_margin_top = 10
	style.content_margin_bottom = 10
	return style
