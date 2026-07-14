extends Control

signal vector_changed(value)

var touch_index = -1
var value = Vector2.ZERO
var radius = 62.0
var knob_radius = 24.0
var dead_zone = 0.14

func _ready():
	custom_minimum_size = Vector2(154, 154)
	mouse_filter = Control.MOUSE_FILTER_STOP
	queue_redraw()

func _gui_input(event):
	if event is InputEventScreenTouch:
		if event.pressed and touch_index == -1:
			touch_index = event.index
			_update_value(event.position)
		elif not event.pressed and event.index == touch_index:
			reset_input()
	elif event is InputEventScreenDrag and event.index == touch_index:
		_update_value(event.position)
	elif event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				touch_index = -2
				_update_value(event.position)
			else:
				reset_input()
	elif event is InputEventMouseMotion and touch_index == -2:
		_update_value(event.position)

func _update_value(position: Vector2):
	var center = size * 0.5
	var offset = position - center
	if offset.length() > radius:
		offset = offset.normalized() * radius
	var magnitude = offset.length() / radius
	if magnitude <= dead_zone:
		value = Vector2.ZERO
	else:
		var remapped = (magnitude - dead_zone) / (1.0 - dead_zone)
		value = offset.normalized() * clamp(remapped, 0.0, 1.0)
	vector_changed.emit(value)
	queue_redraw()

func reset_input():
	touch_index = -1
	value = Vector2.ZERO
	vector_changed.emit(value)
	queue_redraw()

func _draw():
	var center = size * 0.5
	draw_circle(center, radius + 12.0, Color(0.02, 0.05, 0.06, 0.48))
	draw_arc(center, radius, 0, TAU, 64, Color(0.82, 0.64, 0.37, 0.62), 2.0)
	draw_circle(center + value * radius, knob_radius, Color(0.27, 0.72, 0.68, 0.82))
	draw_arc(center + value * radius, knob_radius, 0, TAU, 32, Color(0.91, 0.87, 0.78, 0.92), 2.0)
