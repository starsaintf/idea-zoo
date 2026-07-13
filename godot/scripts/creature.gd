extends CharacterBody3D

signal agitation_changed(value)

var active = false
var tethered = false
var settled = false
var agitation = 34.0
var correct_seals = 0
var target_position = Vector3.ZERO
var tether_target: Node3D
var segments = []
var segment_materials = []
var time = 0.0
var roam_clock = 0.0
var body_root: Node3D
var name_label: Label3D

func _ready():
	name = "QueueEater"
	_build_creature()
	visible = false

func _build_creature():
	var collision = CollisionShape3D.new()
	var sphere = SphereShape3D.new()
	sphere.radius = 1.25
	collision.shape = sphere
	collision.position.y = 1.0
	add_child(collision)

	body_root = Node3D.new()
	add_child(body_root)
	for i in range(12):
		var segment = MeshInstance3D.new()
		var mesh = BoxMesh.new()
		mesh.size = Vector3(1.05 - i * 0.025, 0.55, 0.85)
		segment.mesh = mesh
		var mat = StandardMaterial3D.new()
		mat.albedo_color = Color("#e0d4bc") if i % 2 == 0 else Color("#c89a57")
		mat.roughness = 0.62
		mat.metallic = 0.18 if i % 2 else 0.0
		segment.material_override = mat
		segment_materials.append(mat)
		body_root.add_child(segment)
		segments.append(segment)
	var head = MeshInstance3D.new()
	var head_mesh = PrismMesh.new()
	head_mesh.size = Vector3(1.5, 1.2, 1.8)
	head.mesh = head_mesh
	head.position = Vector3(0, 1.15, -0.65)
	head.material_override = _glow_material(Color("#b85a50"), 0.65)
	body_root.add_child(head)
	segments.push_front(head)

	for side in [-1.0, 1.0]:
		var eye = MeshInstance3D.new()
		var eye_mesh = SphereMesh.new()
		eye_mesh.radius = 0.13
		eye_mesh.height = 0.26
		eye.mesh = eye_mesh
		eye.position = Vector3(side * 0.34, 1.28, -1.4)
		eye.material_override = _glow_material(Color("#7de0d3"), 2.1)
		body_root.add_child(eye)

	name_label = Label3D.new()
	name_label.text = "THE QUEUE-EATER"
	name_label.font_size = 44
	name_label.outline_size = 9
	name_label.modulate = Color("#d2a45f")
	name_label.position = Vector3(0, 3.05, 0)
	name_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	name_label.visible = false
	add_child(name_label)

func _glow_material(color: Color, strength: float):
	var mat = StandardMaterial3D.new()
	mat.albedo_color = color
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = strength
	mat.roughness = 0.62
	return mat

func activate():
	active = true
	visible = true
	name_label.visible = true
	target_position = global_position + Vector3(5, 0, 2)

func apply_seals(seal_names: Array):
	correct_seals = 0
	for seal_name in seal_names:
		if seal_name in ["EXIT", "KEEPER", "BOUNDARY"]:
			correct_seals += 1
	agitation = clamp(76.0 - correct_seals * 20.0, 8.0, 88.0)
	agitation_changed.emit(agitation)
	_update_colors()

func begin_tether(target: Node3D):
	tethered = true
	tether_target = target
	agitation = min(agitation, 18.0)
	agitation_changed.emit(agitation)

func settle_at_zoo():
	tethered = false
	settled = true
	velocity = Vector3.ZERO
	name_label.text = "SPECIMEN A-17 · APPETITE: UNSEEN TIME"
	_update_colors()

func _physics_process(delta):
	time += delta
	_animate_body()
	if not active or settled:
		return
	if tethered and tether_target != null:
		var offset = tether_target.global_transform.basis.z.normalized() * 2.8
		var desired = tether_target.global_position + offset
		var direction = (desired - global_position)
		direction.y = 0
		velocity = direction.normalized() * min(5.4, direction.length() * 2.4)
		move_and_slide()
		look_at(global_position + velocity, Vector3.UP)
		return
	roam_clock -= delta
	if roam_clock <= 0.0 or global_position.distance_to(target_position) < 1.3:
		roam_clock = 2.2 + fmod(time, 2.8)
		var angle = time * 1.7 + roam_clock
		target_position = Vector3(cos(angle) * 7.0, global_position.y, 8.0 + sin(angle) * 6.0)
	var direction = target_position - global_position
	direction.y = 0
	var pace = 1.8 + agitation * 0.025
	velocity = direction.normalized() * pace
	move_and_slide()
	if velocity.length() > 0.1:
		look_at(global_position + velocity, Vector3.UP)

func _animate_body():
	for i in range(segments.size()):
		var segment = segments[i] as MeshInstance3D
		var depth = float(i) * 0.66
		var energy = 0.35 + agitation / 100.0
		segment.position = Vector3(
			sin(time * 3.2 - i * 0.55) * energy,
			0.85 + sin(time * 4.0 - i * 0.34) * 0.14,
			depth
		)
		segment.rotation.y = sin(time * 2.4 - i * 0.4) * 0.22
		segment.rotation.z = sin(time * 3.1 - i * 0.3) * 0.08

func _update_colors():
	var calm = agitation < 30.0
	for i in range(segment_materials.size()):
		var mat = segment_materials[i] as StandardMaterial3D
		if calm:
			mat.albedo_color = Color("#87b3a1") if i % 2 == 0 else Color("#d2a45f")
		else:
			mat.albedo_color = Color("#d8cdb7") if i % 2 == 0 else Color("#a6574e")

func set_classification(keeper_class: String, board_class: String):
	name_label.text = "KEEPER: %s · BOARD: %s" % [keeper_class, board_class]
	var palette = {
		"FLECK": Color("#d7c780"),
		"HAND": Color("#7f9f79"),
		"MIRROR": Color("#79b0b4"),
		"TEETH": Color("#a6574e"),
		"SWARM": Color("#aa87b2"),
		"WEATHER": Color("#8e7ba0"),
		"BURROWER": Color("#7f8883")
	}
	var chosen = palette.get(keeper_class, Color("#d2a45f"))
	for i in range(segment_materials.size()):
		var mat = segment_materials[i] as StandardMaterial3D
		mat.albedo_color = chosen.lightened(0.12) if i % 2 == 0 else chosen.darkened(0.12)
	if keeper_class != board_class:
		name_label.modulate = Color("#d46458")
	else:
		name_label.modulate = Color("#d2a45f")
