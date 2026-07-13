extends CharacterBody3D

signal interact_requested
signal cycle_seal_requested
signal lens_changed(active)

var move_vector = Vector2.ZERO
var mobile_vector = Vector2.ZERO
var camera_yaw = 0.0
var camera_pitch = -0.42
var move_speed = 6.2
var threadway_speed = 9.2
var lens_active = false
var can_move = true
var tethering = false
var on_threadway = false
var camera_pivot: Node3D
var spring_arm: SpringArm3D
var camera: Camera3D
var body_visual: Node3D
var look_touch_index = -1
var visual_time = 0.0

func _ready():
	name = "Keeper"
	_build_body()
	_build_camera()

func _build_body():
	var collision = CollisionShape3D.new()
	var capsule = CapsuleShape3D.new()
	capsule.radius = 0.42
	capsule.height = 1.65
	collision.shape = capsule
	collision.position.y = 0.86
	add_child(collision)
	body_visual = Node3D.new()
	body_visual.name = "KeeperVisual"
	add_child(body_visual)
	var coat = MeshInstance3D.new()
	var coat_mesh = CapsuleMesh.new()
	coat_mesh.radius = 0.43
	coat_mesh.height = 1.3
	coat.mesh = coat_mesh
	coat.position.y = 0.83
	coat.material_override = _material(Color("#d9d0bd"))
	body_visual.add_child(coat)
	var head = MeshInstance3D.new()
	var head_mesh = SphereMesh.new()
	head_mesh.radius = 0.31
	head_mesh.height = 0.62
	head.mesh = head_mesh
	head.position.y = 1.74
	head.material_override = _material(Color("#6d5141"))
	body_visual.add_child(head)
	var cloak = MeshInstance3D.new()
	var cloak_mesh = PrismMesh.new()
	cloak_mesh.size = Vector3(0.9, 1.4, 0.55)
	cloak.mesh = cloak_mesh
	cloak.position = Vector3(0, 0.95, 0.33)
	cloak.rotation_degrees.x = -9
	cloak.material_override = _material(Color("#18545a"), 0.28)
	body_visual.add_child(cloak)
	var lens = MeshInstance3D.new()
	var lens_mesh = SphereMesh.new()
	lens_mesh.radius = 0.13
	lens_mesh.height = 0.2
	lens.mesh = lens_mesh
	lens.position = Vector3(0.25, 1.75, -0.23)
	lens.material_override = _material(Color("#63d3c7"), 1.5)
	body_visual.add_child(lens)

func _build_camera():
	camera_pivot = Node3D.new()
	camera_pivot.name = "CameraPivot"
	camera_pivot.position.y = 1.45
	add_child(camera_pivot)
	spring_arm = SpringArm3D.new()
	spring_arm.spring_length = 7.8
	spring_arm.margin = 0.25
	spring_arm.collision_mask = 1
	camera_pivot.add_child(spring_arm)
	camera = Camera3D.new()
	camera.fov = 58.0
	camera.current = true
	spring_arm.add_child(camera)
	_apply_camera_rotation()

func _material(color: Color, emission := 0.0):
	var mat = StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = 0.78
	if emission > 0.0:
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = emission
	return mat

func _unhandled_input(event):
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		camera_yaw -= event.relative.x * 0.004
		camera_pitch = clamp(camera_pitch - event.relative.y * 0.003, -0.78, -0.18)
		_apply_camera_rotation()
	elif event is InputEventScreenTouch:
		if event.pressed and event.position.x > get_viewport().get_visible_rect().size.x * 0.42 and look_touch_index == -1:
			look_touch_index = event.index
		elif not event.pressed and event.index == look_touch_index:
			look_touch_index = -1
	elif event is InputEventScreenDrag and event.index == look_touch_index:
		camera_yaw -= event.relative.x * 0.007
		camera_pitch = clamp(camera_pitch - event.relative.y * 0.005, -0.78, -0.18)
		_apply_camera_rotation()

func _process(_delta):
	var keyboard_lens = Input.is_action_pressed("lens")
	if keyboard_lens != lens_active and not DisplayServer.is_touchscreen_available():
		set_lens(keyboard_lens)
	if Input.is_action_just_pressed("interact"):
		interact_requested.emit()
	if Input.is_action_just_pressed("cycle_seal"):
		cycle_seal_requested.emit()

func _physics_process(delta):
	visual_time += delta
	if can_move:
		move_vector = Input.get_vector("move_left", "move_right", "move_forward", "move_back")
		if mobile_vector.length() > 0.05:
			move_vector = mobile_vector
		var local_direction = Vector3(move_vector.x, 0, move_vector.y)
		var direction = local_direction.rotated(Vector3.UP, camera_yaw).normalized()
		var current_speed = threadway_speed if on_threadway else move_speed
		if tethering:
			current_speed *= 0.78
		velocity.x = move_toward(velocity.x, direction.x * current_speed, 22.0 * delta)
		velocity.z = move_toward(velocity.z, direction.z * current_speed, 22.0 * delta)
		if direction.length() > 0.05:
			body_visual.rotation.y = lerp_angle(body_visual.rotation.y, atan2(-direction.x, -direction.z), 10.0 * delta)
			body_visual.position.y = sin(visual_time * 10.0) * 0.035
		else:
			body_visual.position.y = lerp(body_visual.position.y, 0.0, 8.0 * delta)
	else:
		velocity.x = move_toward(velocity.x, 0.0, 25.0 * delta)
		velocity.z = move_toward(velocity.z, 0.0, 25.0 * delta)
	if not is_on_floor():
		velocity.y -= 18.0 * delta
	else:
		velocity.y = -0.1
	move_and_slide()
	global_position.x = clamp(global_position.x, -37.0, 37.0)
	global_position.z = clamp(global_position.z, -37.0, 37.0)

func _apply_camera_rotation():
	if camera_pivot == null:
		return
	camera_pivot.rotation = Vector3(camera_pitch, camera_yaw, 0)

func set_mobile_vector(value: Vector2):
	mobile_vector = value

func set_lens(active: bool):
	if lens_active == active:
		return
	lens_active = active
	lens_changed.emit(active)

func request_interact():
	interact_requested.emit()

func request_cycle_seal():
	cycle_seal_requested.emit()
