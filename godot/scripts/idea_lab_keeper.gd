extends CharacterBody3D

signal interact_requested
signal lens_changed(active)

var profile = {
	"name": "Keeper",
	"method": "Observer",
	"body": "Balanced",
	"skin": "Umber",
	"coat": "Teal"
}
var mobile_vector = Vector2.ZERO
var lens_active = false
var controls_locked = true
var camera_yaw = 0.0
var camera_pitch = -0.38
var camera_target_yaw = 0.0
var camera_target_pitch = -0.38
var move_speed = 6.0
var body_root: Node3D
var camera_pivot: Node3D
var spring_arm: SpringArm3D
var camera: Camera3D
var look_touch_index = -1
var walk_time = 0.0
var camera_zone_bottom_ratio = 0.68

var coat_colors = {
	"Teal": Color("#18545a"),
	"Burgundy": Color("#6f2f3c"),
	"Ochre": Color("#a87732"),
	"Indigo": Color("#35466f"),
	"Moss": Color("#536d4f")
}
var skin_colors = {
	"Ebony": Color("#3d291f"),
	"Umber": Color("#6d5141"),
	"Copper": Color("#9b6a4e"),
	"Sienna": Color("#b57a5d"),
	"Sand": Color("#d1a17f")
}

func _ready():
	name = "Keeper"
	_build_collision()
	_build_body()
	_build_camera()

func configure(data: Dictionary):
	for key in data.keys():
		profile[key] = data[key]
	if body_root != null:
		body_root.queue_free()
		body_root = null
	_build_body()

func _build_collision():
	var collision = CollisionShape3D.new()
	var capsule = CapsuleShape3D.new()
	capsule.radius = 0.42
	capsule.height = 1.7
	collision.shape = capsule
	collision.position.y = 0.88
	add_child(collision)

func _build_body():
	body_root = Node3D.new()
	body_root.name = "KeeperVisual"
	add_child(body_root)
	var coat_color = coat_colors.get(String(profile.get("coat", "Teal")), Color("#18545a"))
	var skin_color = skin_colors.get(String(profile.get("skin", "Umber")), Color("#6d5141"))
	var body_style = String(profile.get("body", "Balanced"))
	var method = String(profile.get("method", "Observer"))
	var width = 0.46
	var height = 1.35
	if body_style == "Broad":
		width = 0.55
	elif body_style == "Tall":
		height = 1.52
	elif body_style == "Compact":
		width = 0.40
		height = 1.18

	var torso = MeshInstance3D.new()
	var torso_mesh = CapsuleMesh.new()
	torso_mesh.radius = width
	torso_mesh.height = height
	torso.mesh = torso_mesh
	torso.position.y = 0.84
	torso.material_override = _material(Color("#d9d0bd"))
	body_root.add_child(torso)

	var coat = MeshInstance3D.new()
	var coat_mesh = PrismMesh.new()
	coat_mesh.size = Vector3(width * 2.25, height * 1.08, 0.58)
	coat.mesh = coat_mesh
	coat.position = Vector3(0, 0.88, 0.25)
	coat.rotation_degrees.x = -7
	coat.material_override = _material(coat_color, 0.12)
	body_root.add_child(coat)

	var head = MeshInstance3D.new()
	var head_mesh = SphereMesh.new()
	head_mesh.radius = 0.31
	head_mesh.height = 0.62
	head.mesh = head_mesh
	head.position.y = 1.72 + (height - 1.35) * 0.36
	head.material_override = _material(skin_color)
	body_root.add_child(head)

	var lens = MeshInstance3D.new()
	var lens_mesh = TorusMesh.new()
	lens_mesh.inner_radius = 0.12
	lens_mesh.outer_radius = 0.19
	lens.mesh = lens_mesh
	lens.position = Vector3(0.27, head.position.y + 0.02, -0.28)
	lens.rotation_degrees.x = 90
	lens.material_override = _material(Color("#63d3c7"), 1.25, 0.2)
	body_root.add_child(lens)

	_build_method_gear(method, coat_color)

	var name_plate = Label3D.new()
	name_plate.text = "%s · %s" % [String(profile.get("name", "Keeper")).to_upper(), method.to_upper()]
	name_plate.position = Vector3(0, 2.35 + (height - 1.35) * 0.36, 0)
	name_plate.font_size = 24
	name_plate.outline_size = 6
	name_plate.modulate = Color("#eadfca")
	name_plate.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	body_root.add_child(name_plate)

func _build_method_gear(method: String, coat_color: Color):
	match method:
		"Observer":
			var shoulder_lens = _sphere(0.18, Color("#69cfc3"), 0.85)
			shoulder_lens.position = Vector3(-0.42, 1.48, 0.12)
			body_root.add_child(shoulder_lens)
		"Molter":
			for side in [-1.0, 1.0]:
				var tool = _box(Vector3(0.12, 0.68, 0.12), Color("#d2a45f"), 0.35)
				tool.position = Vector3(0.45 * side, 0.95, 0.38)
				tool.rotation_degrees.z = 14 * side
				body_root.add_child(tool)
		"Shepherd":
			var spool = MeshInstance3D.new()
			var spool_mesh = TorusMesh.new()
			spool_mesh.inner_radius = 0.22
			spool_mesh.outer_radius = 0.34
			spool.mesh = spool_mesh
			spool.position = Vector3(-0.5, 0.62, 0.28)
			spool.rotation_degrees.x = 90
			spool.material_override = _material(Color("#d2a45f"), 0.3, 0.5)
			body_root.add_child(spool)
		"Dissenter":
			var sash = _box(Vector3(0.12, 1.55, 0.06), Color("#c85e52"), 0.15)
			sash.position = Vector3(0, 1.02, -0.46)
			sash.rotation_degrees.z = -28
			body_root.add_child(sash)
		_:
			var pin = _sphere(0.12, coat_color.lightened(0.3), 0.5)
			pin.position = Vector3(0.32, 1.35, -0.48)
			body_root.add_child(pin)

func _build_camera():
	camera_pivot = Node3D.new()
	camera_pivot.name = "CameraPivot"
	camera_pivot.position.y = 1.45
	add_child(camera_pivot)
	spring_arm = SpringArm3D.new()
	spring_arm.spring_length = 7.4
	spring_arm.margin = 0.28
	spring_arm.collision_mask = 1
	spring_arm.add_excluded_object(get_rid())
	camera_pivot.add_child(spring_arm)
	camera = Camera3D.new()
	camera.fov = 58.0
	camera.current = true
	spring_arm.add_child(camera)
	_apply_camera_rotation()

func _unhandled_input(event):
	if controls_locked:
		return
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		camera_target_yaw -= event.relative.x * 0.0032
		camera_target_pitch = clamp(camera_target_pitch - event.relative.y * 0.0025, -0.72, -0.16)
	elif event is InputEventScreenTouch:
		var viewport_size = get_viewport().get_visible_rect().size
		var in_camera_zone = event.position.x > viewport_size.x * 0.42 and event.position.y < viewport_size.y * camera_zone_bottom_ratio
		if event.pressed and in_camera_zone and look_touch_index == -1:
			look_touch_index = event.index
		elif not event.pressed and event.index == look_touch_index:
			look_touch_index = -1
	elif event is InputEventScreenDrag and event.index == look_touch_index:
		var relative = event.relative
		if relative.length() > 42.0:
			relative = relative.normalized() * 42.0
		camera_target_yaw -= relative.x * 0.0024
		camera_target_pitch = clamp(camera_target_pitch - relative.y * 0.0019, -0.72, -0.16)

func _process(delta):
	camera_yaw = lerp_angle(camera_yaw, camera_target_yaw, min(1.0, delta * 10.0))
	camera_pitch = lerp(camera_pitch, camera_target_pitch, min(1.0, delta * 10.0))
	_apply_camera_rotation()
	if not controls_locked and Input.is_action_just_pressed("interact"):
		interact_requested.emit()
	if not DisplayServer.is_touchscreen_available():
		set_lens(Input.is_action_pressed("lens"))

func _physics_process(delta):
	walk_time += delta
	var input_vector = Vector2.ZERO
	if not controls_locked:
		input_vector = Input.get_vector("move_left", "move_right", "move_forward", "move_back")
		if mobile_vector.length() > 0.05:
			input_vector = mobile_vector
	var local_direction = Vector3(input_vector.x, 0, input_vector.y)
	var direction = local_direction.rotated(Vector3.UP, camera_yaw).normalized()
	velocity.x = move_toward(velocity.x, direction.x * move_speed, 22.0 * delta)
	velocity.z = move_toward(velocity.z, direction.z * move_speed, 22.0 * delta)
	if direction.length() > 0.05 and body_root != null:
		body_root.rotation.y = lerp_angle(body_root.rotation.y, atan2(-direction.x, -direction.z), 10.0 * delta)
		body_root.position.y = sin(walk_time * 10.0) * 0.035
	elif body_root != null:
		body_root.position.y = lerp(body_root.position.y, 0.0, min(1.0, delta * 8.0))
	if not is_on_floor():
		velocity.y -= 18.0 * delta
	else:
		velocity.y = -0.1
	move_and_slide()
	global_position.x = clamp(global_position.x, -30.0, 30.0)
	global_position.z = clamp(global_position.z, -30.0, 30.0)

func _apply_camera_rotation():
	if camera_pivot != null:
		camera_pivot.rotation = Vector3(camera_pitch, camera_yaw, 0)

func set_mobile_vector(value: Vector2):
	mobile_vector = value

func set_lens(active: bool):
	if lens_active == active:
		return
	lens_active = active
	lens_changed.emit(active)

func request_interact():
	if not controls_locked:
		interact_requested.emit()

func set_controls_locked(locked: bool):
	controls_locked = locked
	if locked:
		mobile_vector = Vector2.ZERO

func _material(color: Color, emission := 0.0, metallic := 0.0):
	var mat = StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = 0.78
	mat.metallic = metallic
	if emission > 0.0:
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = emission
	return mat

func _box(size: Vector3, color: Color, emission := 0.0):
	var mesh = MeshInstance3D.new()
	var shape = BoxMesh.new()
	shape.size = size
	mesh.mesh = shape
	mesh.material_override = _material(color, emission)
	return mesh

func _sphere(radius: float, color: Color, emission := 0.0):
	var mesh = MeshInstance3D.new()
	var shape = SphereMesh.new()
	shape.radius = radius
	shape.height = radius * 2.0
	mesh.mesh = shape
	mesh.material_override = _material(color, emission)
	return mesh
