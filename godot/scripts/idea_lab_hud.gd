extends CanvasLayer

signal intake_submitted(intake, keeper_profile)
signal test_submitted(test_id, strength, note)
signal molt_submitted(promise, audience, guardrails)
signal overlay_continued
signal overlay_cancelled
signal restart_requested
signal interact_pressed
signal joystick_changed(value)
signal lens_changed(active)

const Joystick = preload("res://scripts/virtual_joystick.gd")

var status_panel: PanelContainer
var status_row: HBoxContainer
var objective_label: Label
var specimen_label: Label
var metrics_label: Label
var prompt_label: Label
var progress_label: Label

var overlay: PanelContainer
var overlay_scroll: ScrollContainer
var overlay_box: VBoxContainer
var error_label: Label

var touch_root: Control
var joystick
var interact_button: Button
var lens_button: Button

var fields: Dictionary = {}
var current_test_id := ""
var selected_strength := -1
var submit_locked := false
var is_touch := false
var is_compact := false

var paper = Color("#e5dcc9")
var ink = Color("#071015")
var teal = Color("#56b7ad")
var brass = Color("#d2a45f")
var rust = Color("#aa5549")
var violet = Color("#8e7bb8")

func _ready():
	is_touch = DisplayServer.is_touchscreen_available() or OS.has_environment("IDEA_ZOO_MOBILE_TEST")
	_build_status()
	_build_touch_controls()
	_build_overlay()
	get_viewport().size_changed.connect(_layout_all)
	_layout_all()
	show_intake()

func _build_status():
	status_panel = PanelContainer.new()
	status_panel.set_anchors_preset(Control.PRESET_TOP_WIDE)
	status_panel.add_theme_stylebox_override("panel", _panel(Color(0.02, 0.055, 0.07, 0.94), teal))
	add_child(status_panel)

	status_row = HBoxContainer.new()
	status_row.add_theme_constant_override("separation", 16)
	status_panel.add_child(status_row)

	objective_label = _label("WHISPER GATE", 16, paper)
	objective_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	objective_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status_row.add_child(objective_label)

	specimen_label = _label("NO SPECIMEN", 15, brass)
	specimen_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	specimen_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status_row.add_child(specimen_label)

	metrics_label = _label("EVIDENCE 0 · SAFETY 0", 14, teal)
	metrics_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	metrics_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status_row.add_child(metrics_label)

	progress_label = _label("0 / 4", 14, paper)
	progress_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	status_row.add_child(progress_label)

	prompt_label = _label("", 16, paper)
	prompt_label.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt_label.add_theme_stylebox_override("normal", _panel(Color(0.02, 0.055, 0.07, 0.92), brass))
	prompt_label.visible = false
	add_child(prompt_label)

func _build_touch_controls():
	touch_root = Control.new()
	touch_root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	touch_root.mouse_filter = Control.MOUSE_FILTER_PASS
	touch_root.z_index = 10
	add_child(touch_root)

	joystick = Joystick.new()
	joystick.custom_minimum_size = Vector2(154, 154)
	joystick.size = Vector2(154, 154)
	joystick.vector_changed.connect(func(value): joystick_changed.emit(value))
	touch_root.add_child(joystick)

	interact_button = _button("TOUCH", teal, 18)
	interact_button.custom_minimum_size = Vector2(118, 118)
	interact_button.size = Vector2(118, 118)
	interact_button.pressed.connect(func(): interact_pressed.emit())
	touch_root.add_child(interact_button)

	lens_button = _button("LENS", brass, 17)
	lens_button.custom_minimum_size = Vector2(92, 92)
	lens_button.size = Vector2(92, 92)
	lens_button.button_down.connect(func(): lens_changed.emit(true))
	lens_button.button_up.connect(func(): lens_changed.emit(false))
	touch_root.add_child(lens_button)

	touch_root.visible = is_touch

func _build_overlay():
	overlay = PanelContainer.new()
	overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.add_theme_stylebox_override("panel", _panel(Color(0.015, 0.04, 0.052, 0.992), brass))
	overlay.z_index = 100
	overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(overlay)

	var margin = MarginContainer.new()
	margin.name = "OverlayMargin"
	overlay.add_child(margin)

	overlay_scroll = ScrollContainer.new()
	overlay_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	overlay_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	overlay_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	margin.add_child(overlay_scroll)

	overlay_box = VBoxContainer.new()
	overlay_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	overlay_box.add_theme_constant_override("separation", 12)
	overlay_scroll.add_child(overlay_box)

func _layout_all():
	var viewport_size = get_viewport().get_visible_rect().size
	is_compact = viewport_size.x < 760.0 or viewport_size.y < 520.0

	status_panel.offset_left = 12 if is_compact else 18
	status_panel.offset_top = 10 if is_compact else 14
	status_panel.offset_right = -12 if is_compact else -18
	status_panel.offset_bottom = 82 if is_compact else 104

	objective_label.add_theme_font_size_override("font_size", 13 if is_compact else 16)
	specimen_label.add_theme_font_size_override("font_size", 13 if is_compact else 15)
	metrics_label.add_theme_font_size_override("font_size", 12 if is_compact else 14)
	progress_label.add_theme_font_size_override("font_size", 12 if is_compact else 14)
	specimen_label.visible = not is_compact
	metrics_label.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS

	prompt_label.offset_left = 210 if is_compact else 160
	prompt_label.offset_right = -210 if is_compact else -160
	prompt_label.offset_top = -58 if is_compact else -72
	prompt_label.offset_bottom = -14 if is_compact else -24
	prompt_label.add_theme_font_size_override("font_size", 14 if is_compact else 16)

	var margin = overlay.get_node_or_null("OverlayMargin")
	if margin != null:
		var side = 18 if is_compact else 34
		var vertical = 14 if is_compact else 24
		margin.add_theme_constant_override("margin_left", side)
		margin.add_theme_constant_override("margin_right", side)
		margin.add_theme_constant_override("margin_top", vertical)
		margin.add_theme_constant_override("margin_bottom", vertical)
	overlay_box.custom_minimum_size.x = max(300.0, viewport_size.x - (36.0 if is_compact else 68.0))

	_layout_touch_controls()

func _layout_touch_controls():
	if touch_root == null:
		return
	var viewport_size = get_viewport().get_visible_rect().size
	var side_safe = 44.0
	var bottom_safe = 20.0
	var joystick_size = 132.0 if is_compact else 154.0
	var action_size = 104.0 if is_compact else 118.0
	var lens_size = 78.0 if is_compact else 92.0

	joystick.custom_minimum_size = Vector2(joystick_size, joystick_size)
	joystick.size = Vector2(joystick_size, joystick_size)
	joystick.position = Vector2(side_safe, viewport_size.y - joystick_size - bottom_safe)

	interact_button.custom_minimum_size = Vector2(action_size, action_size)
	interact_button.size = Vector2(action_size, action_size)
	interact_button.position = Vector2(viewport_size.x - action_size - side_safe, viewport_size.y - action_size - bottom_safe)

	lens_button.custom_minimum_size = Vector2(lens_size, lens_size)
	lens_button.size = Vector2(lens_size, lens_size)
	lens_button.position = Vector2(
		viewport_size.x - action_size - side_safe - lens_size - 14.0,
		viewport_size.y - lens_size - bottom_safe
	)

func show_intake():
	_open_overlay()
	_clear_overlay()
	reset_status()
	_add_title("THE WHISPER GATE")
	_add_body("Bring an idea you may actually spend years building. The Zoo will give it a body, test what it wants, and help you decide whether it deserves more of your life.")

	_add_section("YOUR KEEPER")
	var keeper_container = VBoxContainer.new() if is_compact else HBoxContainer.new()
	keeper_container.add_theme_constant_override("separation", 10)
	overlay_box.add_child(keeper_container)

	fields["keeper_name"] = _line("Keeper name", "Jay")
	keeper_container.add_child(fields["keeper_name"])
	fields["method"] = _options(["Observer", "Molter", "Shepherd", "Dissenter"])
	keeper_container.add_child(fields["method"])
	fields["body"] = _options(["Balanced", "Broad", "Tall", "Compact"])
	keeper_container.add_child(fields["body"])
	fields["skin"] = _options(["Ebony", "Umber", "Copper", "Sienna", "Sand"])
	keeper_container.add_child(fields["skin"])
	fields["coat"] = _options(["Teal", "Burgundy", "Ochre", "Indigo", "Moss"])
	keeper_container.add_child(fields["coat"])

	_add_section("THE REAL IDEA")
	fields["title"] = _line("Name the idea", "")
	overlay_box.add_child(fields["title"])
	fields["idea"] = _text("Describe it plainly. No pitch language.", "")
	overlay_box.add_child(fields["idea"])
	fields["problem"] = _line("What painful problem exists without it?", "")
	overlay_box.add_child(fields["problem"])
	fields["promise"] = _line("What measurable outcome does it promise?", "")
	overlay_box.add_child(fields["promise"])

	var market_container = VBoxContainer.new() if is_compact else HBoxContainer.new()
	market_container.add_theme_constant_override("separation", 10)
	overlay_box.add_child(market_container)
	fields["audience"] = _line("First specific user", "")
	market_container.add_child(fields["audience"])
	fields["payer"] = _line("Who pays or commits resources?", "")
	market_container.add_child(fields["payer"])

	fields["evidence"] = _line("What have you already tested?", "")
	overlay_box.add_child(fields["evidence"])
	fields["dependency"] = _line("What must exist for it to work?", "")
	overlay_box.add_child(fields["dependency"])
	fields["maintenance"] = _line("Who keeps it alive after launch?", "")
	overlay_box.add_child(fields["maintenance"])
	fields["harm"] = _line("What is its cruelest plausible use?", "")
	overlay_box.add_child(fields["harm"])

	_add_error_label()
	var submit = _button("HATCH THIS IDEA", brass, 20)
	submit.name = "IntakeSubmit"
	submit.pressed.connect(_submit_intake.bind(submit))
	overlay_box.add_child(submit)

func show_message(title: String, body: String, button_text := "CONTINUE"):
	_open_overlay()
	_clear_overlay()
	_add_title(title)
	_add_body(body)
	var button = _button(button_text, brass, 19)
	button.pressed.connect(_continue_overlay.bind(button))
	overlay_box.add_child(button)

func show_test(test: Dictionary):
	_open_overlay()
	_clear_overlay()
	current_test_id = String(test.get("id", ""))
	selected_strength = -1

	_add_title(String(test.get("title", "EVIDENCE TEST")))
	_add_body(String(test.get("question", "What did reality show?")))
	_add_section("REAL-WORLD MISSION")
	_add_body(String(test.get("mission", "Run a falsifiable test.")))

	fields["test_strength"] = _options([
		"Choose evidence strength",
		"0 · No evidence yet",
		"1 · Anecdote or weak signal",
		"2 · Repeated behaviour",
		"3 · Money, signed pilot, or costly commitment"
	])
	overlay_box.add_child(fields["test_strength"])

	fields["test_note"] = _text("What happened? Record names, numbers, or the strongest contradiction.", "")
	overlay_box.add_child(fields["test_note"])

	_add_error_label()
	var submit = _button("RECORD THIS EVIDENCE", teal, 19)
	submit.pressed.connect(_submit_test.bind(submit))
	overlay_box.add_child(submit)

	var cancel = _button("RETURN WITHOUT RECORDING", Color("#47585d"), 15)
	cancel.pressed.connect(_cancel_overlay.bind(cancel))
	overlay_box.add_child(cancel)

func show_molt(profile: Dictionary):
	_open_overlay()
	_clear_overlay()
	_add_title("THE MOLT HOUSE")
	_add_body("Do not defend the original shape. Preserve the useful core and change what evidence has made indefensible.")

	fields["revised_promise"] = _text("Revised measurable promise", String(profile.get("promise", "")))
	overlay_box.add_child(fields["revised_promise"])
	fields["revised_audience"] = _line("Narrower first audience", String(profile.get("audience", "")))
	overlay_box.add_child(fields["revised_audience"])

	_add_section("RULES TO ADD TO THE CREATURE")
	fields["guardrails"] = []
	for text in [
		"People can refuse without penalty",
		"A named keeper owns maintenance",
		"The idea has a clear boundary",
		"Uncertainty remains visible",
		"Users can appeal, delete, or recall",
		"The idea expires unless renewed"
	]:
		var check = CheckButton.new()
		check.text = text
		check.add_theme_font_size_override("font_size", 16)
		check.add_theme_color_override("font_color", paper)
		overlay_box.add_child(check)
		fields["guardrails"].append(check)

	_add_error_label()
	var submit = _button("LET IT MOLT", violet, 20)
	submit.pressed.connect(_submit_molt.bind(submit))
	overlay_box.add_child(submit)

	var cancel = _button("RETURN TO THE ZOO", Color("#47585d"), 15)
	cancel.pressed.connect(_cancel_overlay.bind(cancel))
	overlay_box.add_child(cancel)

func show_result(record: Dictionary):
	_open_overlay()
	_clear_overlay()
	_add_title("RULING · %s" % String(record.get("decision", "UNDECIDED")))
	_add_body(String(record.get("verdict_reason", "")))
	_add_section("THE IDEA THAT LEAVES THE ZOO")
	_add_body("%s\n\nPromise: %s\nFirst user: %s\nClass: %s · Appetite: %s" % [
		String(record.get("title", "")),
		String(record.get("promise", "")),
		String(record.get("audience", "")),
		String(record.get("class", "")),
		String(record.get("appetite", ""))
	])

	_add_section("NEXT REAL-WORLD ACTIONS")
	var actions: Array = record.get("next_actions", [])
	for index in range(actions.size()):
		_add_body("%d. %s" % [index + 1, String(actions[index])])

	_add_body("The complete specimen record has been saved on this device. The idea is no longer a loose thought; it has evidence, constraints, and a deliberate next state.")

	var again = _button("BRING ANOTHER IDEA", brass, 18)
	again.pressed.connect(_restart.bind(again))
	overlay_box.add_child(again)

func set_objective(title: String, detail: String):
	objective_label.text = "%s\n%s" % [title, detail]

func set_specimen(profile: Dictionary):
	specimen_label.text = "%s\n%s · feeds on %s" % [
		String(profile.get("creature_name", "SPECIMEN")),
		String(profile.get("class", "")),
		String(profile.get("appetite", ""))
	]
	set_metrics(profile)

func set_metrics(profile: Dictionary):
	var metrics: Dictionary = profile.get("metrics", {})
	if is_compact:
		metrics_label.text = "E %.0f · D %.0f · V %.0f · S %.0f" % [
			float(metrics.get("evidence", 0.0)) * 100.0,
			float(metrics.get("desirability", 0.0)) * 100.0,
			float(metrics.get("viability", 0.0)) * 100.0,
			float(metrics.get("safety", 0.0)) * 100.0
		]
	else:
		metrics_label.text = "EVIDENCE %.0f%% · DESIRE %.0f%% · VIABILITY %.0f%% · SAFETY %.0f%%" % [
			float(metrics.get("evidence", 0.0)) * 100.0,
			float(metrics.get("desirability", 0.0)) * 100.0,
			float(metrics.get("viability", 0.0)) * 100.0,
			float(metrics.get("safety", 0.0)) * 100.0
		]

func set_progress(completed: int, total: int):
	progress_label.text = "%d / %d" % [completed, total]

func set_prompt(text: String):
	prompt_label.text = text
	prompt_label.visible = not text.is_empty() and not is_overlay_open()

func reset_status():
	objective_label.text = "WHISPER GATE"
	specimen_label.text = "NO SPECIMEN"
	metrics_label.text = "EVIDENCE 0 · SAFETY 0"
	progress_label.text = "0 / 4"
	set_prompt("")

func close_overlay():
	overlay.visible = false
	touch_root.visible = is_touch
	submit_locked = false
	lens_changed.emit(false)

func is_overlay_open() -> bool:
	return overlay != null and overlay.visible

func show_inline_error(message: String):
	if error_label == null or not is_instance_valid(error_label):
		_add_error_label()
	error_label.text = message
	error_label.visible = true
	submit_locked = false

func _open_overlay():
	overlay.visible = true
	touch_root.visible = false
	set_prompt("")
	joystick_changed.emit(Vector2.ZERO)
	lens_changed.emit(false)
	submit_locked = false

func _submit_intake(button: Button):
	if submit_locked:
		return

	var intake: Dictionary = {}
	for key in ["title", "idea", "problem", "promise", "audience", "payer", "evidence", "dependency", "maintenance", "harm"]:
		var control = fields.get(key)
		intake[key] = control.text.strip_edges() if control != null else ""

	if String(intake["title"]).is_empty() or String(intake["idea"]).is_empty() or String(intake["promise"]).is_empty():
		show_inline_error("The Gate needs a name, a plain description, and a measurable promise.")
		return

	submit_locked = true
	button.disabled = true
	var keeper = {
		"name": fields["keeper_name"].text.strip_edges() if not fields["keeper_name"].text.strip_edges().is_empty() else "Keeper",
		"method": fields["method"].get_item_text(fields["method"].selected),
		"body": fields["body"].get_item_text(fields["body"].selected),
		"skin": fields["skin"].get_item_text(fields["skin"].selected),
		"coat": fields["coat"].get_item_text(fields["coat"].selected)
	}
	intake_submitted.emit(intake, keeper)

func _submit_test(button: Button):
	if submit_locked:
		return
	var option: OptionButton = fields.get("test_strength")
	var note_control = fields.get("test_note")
	var note = note_control.text.strip_edges() if note_control != null else ""

	if option == null or option.selected <= 0:
		show_inline_error("Choose the strength of the evidence before recording it.")
		return
	if note.length() < 3:
		show_inline_error("Record what actually happened, even when the result was 'nothing happened'.")
		return

	submit_locked = true
	button.disabled = true
	selected_strength = option.selected - 1
	test_submitted.emit(current_test_id, selected_strength, note)

func _submit_molt(button: Button):
	if submit_locked:
		return
	var promise = fields["revised_promise"].text.strip_edges()
	var audience = fields["revised_audience"].text.strip_edges()

	if promise.is_empty() or audience.is_empty():
		show_inline_error("The Molt House needs a measurable promise and a specific first audience.")
		return

	var guardrails: Array = []
	for check in fields["guardrails"]:
		if check.button_pressed:
			guardrails.append(check.text)

	submit_locked = true
	button.disabled = true
	molt_submitted.emit(promise, audience, guardrails)

func _continue_overlay(button: Button):
	if submit_locked:
		return
	submit_locked = true
	button.disabled = true
	close_overlay()
	overlay_continued.emit()

func _cancel_overlay(button: Button):
	if submit_locked:
		return
	submit_locked = true
	button.disabled = true
	close_overlay()
	overlay_cancelled.emit()

func _restart(button: Button):
	if submit_locked:
		return
	submit_locked = true
	button.disabled = true
	restart_requested.emit()

func _clear_overlay():
	fields.clear()
	current_test_id = ""
	selected_strength = -1
	error_label = null
	submit_locked = false
	for child in overlay_box.get_children():
		overlay_box.remove_child(child)
		child.queue_free()
	overlay_scroll.scroll_vertical = 0

func _add_error_label():
	error_label = _label("", 15, rust.lightened(0.2))
	error_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	error_label.visible = false
	overlay_box.add_child(error_label)

func _add_title(text: String):
	var node = _label(text, 30 if is_compact else 38, brass)
	node.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	node.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	overlay_box.add_child(node)

func _add_section(text: String):
	var node = _label(text, 17, teal)
	node.add_theme_stylebox_override("normal", _panel(Color(0.02, 0.07, 0.08, 0.88), teal))
	overlay_box.add_child(node)

func _add_body(text: String, color := Color("#e5dcc9")):
	var node = _label(text, 15 if is_compact else 16, color)
	node.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	overlay_box.add_child(node)

func _line(placeholder: String, value: String) -> LineEdit:
	var node = LineEdit.new()
	node.placeholder_text = placeholder
	node.text = value
	node.custom_minimum_size = Vector2(180, 46)
	node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	node.virtual_keyboard_enabled = true
	node.add_theme_font_size_override("font_size", 16)
	node.add_theme_color_override("font_color", paper)
	node.add_theme_color_override("font_placeholder_color", Color("#8d9796"))
	node.add_theme_stylebox_override("normal", _panel(Color(0.02, 0.06, 0.075, 0.94), Color("#355b5d")))
	node.add_theme_stylebox_override("focus", _panel(Color(0.025, 0.075, 0.085, 0.98), brass))
	return node

func _text(placeholder: String, value: String) -> TextEdit:
	var node = TextEdit.new()
	node.placeholder_text = placeholder
	node.text = value
	node.custom_minimum_size = Vector2(260, 82 if is_compact else 96)
	node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	node.wrap_mode = TextEdit.LINE_WRAPPING_BOUNDARY
	node.virtual_keyboard_enabled = true
	node.add_theme_font_size_override("font_size", 16)
	node.add_theme_color_override("font_color", paper)
	node.add_theme_color_override("font_placeholder_color", Color("#8d9796"))
	node.add_theme_stylebox_override("normal", _panel(Color(0.02, 0.06, 0.075, 0.94), Color("#355b5d")))
	node.add_theme_stylebox_override("focus", _panel(Color(0.025, 0.075, 0.085, 0.98), brass))
	return node

func _options(items: Array) -> OptionButton:
	var node = OptionButton.new()
	node.custom_minimum_size = Vector2(150, 46)
	node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	node.add_theme_font_size_override("font_size", 15)
	for item in items:
		node.add_item(String(item))
	return node

func _button(text: String, color: Color, font_size: int) -> Button:
	var node = Button.new()
	node.text = text
	node.custom_minimum_size = Vector2(180, 50)
	node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	node.focus_mode = Control.FOCUS_NONE
	node.add_theme_font_size_override("font_size", font_size)
	node.add_theme_color_override("font_color", ink)
	node.add_theme_stylebox_override("normal", _button_style(Color(color, 0.92), 13))
	node.add_theme_stylebox_override("hover", _button_style(color.lightened(0.08), 13))
	node.add_theme_stylebox_override("pressed", _button_style(color.darkened(0.08), 13))
	node.add_theme_stylebox_override("disabled", _button_style(Color(color, 0.35), 13))
	return node

func _label(text: String, font_size: int, color: Color) -> Label:
	var node = Label.new()
	node.text = text
	node.add_theme_font_size_override("font_size", font_size)
	node.add_theme_color_override("font_color", color)
	return node

func _panel(background: Color, border: Color) -> StyleBoxFlat:
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

func _button_style(background: Color, radius: int) -> StyleBoxFlat:
	var style = StyleBoxFlat.new()
	style.bg_color = background
	style.set_corner_radius_all(radius)
	style.content_margin_left = 14
	style.content_margin_right = 14
	style.content_margin_top = 11
	style.content_margin_bottom = 11
	return style
