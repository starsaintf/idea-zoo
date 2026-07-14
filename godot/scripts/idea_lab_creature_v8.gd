extends "res://scripts/idea_lab_creature.gd"

func configure(data: Dictionary):
	super(data)
	var metrics: Dictionary = data.get("metrics", {})
	set_stage(
		float(metrics.get("evidence", 0.2)),
		1.0 - float(metrics.get("safety", 0.8)),
		min(1.0, float(data.get("guardrails", []).size()) / 6.0)
	)

func molt(updated_profile: Dictionary, guardrails: Array):
	profile = updated_profile.duplicate(true)
	profile["guardrails"] = guardrails.duplicate()
	configure(profile)

func _process(delta):
	super(delta)
	if follow_target == null:
		return
	var yaw = follow_target.rotation.y
	if follow_target.has_method("get_facing_yaw"):
		yaw = float(follow_target.get_facing_yaw())
	var offset = Vector3(1.35, 0.0, 1.75).rotated(Vector3.UP, yaw)
	var desired = follow_target.global_position + offset
	global_position = global_position.lerp(desired, min(1.0, delta * 3.0))
