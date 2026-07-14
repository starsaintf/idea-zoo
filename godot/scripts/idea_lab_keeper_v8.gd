extends "res://scripts/idea_lab_keeper.gd"

func configure(data: Dictionary):
	if body_root != null:
		body_root.visible = false
	super(data)

func set_controls_locked(locked: bool):
	super(locked)
	if locked:
		look_touch_index = -1
		set_lens(false)

func get_facing_yaw() -> float:
	return body_root.rotation.y if body_root != null else rotation.y
