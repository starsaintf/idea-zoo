extends Node3D

var kind = "FLECK"
var display_name = ""
var note = ""
var home = Vector3.ZERO
var player: Node3D
var leakage = 0.0
var stability = 70.0
var time = 0.0
var parts = []
var glow_parts = []
var observed = false

func configure(data: Dictionary):
	kind = String(data.get("kind", "FLECK"))
	display_name = String(data.get("name", kind))
	note = String(data.get("note", ""))
	position = data.get("position", Vector3.ZERO)
	home = position
	name = display_name.replace(" ", "")
	set_meta("ecology_kind", kind)
	set_meta("ecology_name", display_name)
	set_meta("ecology_note", note)
	_build()

func set_player(value: Node3D):
	player = value

func set_city_state(new_leakage: float, new_stability: float):
	leakage = new_leakage
	stability = new_stability

func mark_observed():
	observed = true
	for part in glow_parts:
		if part is MeshInstance3D:
			var mat = part.material_override as StandardMaterial3D
			if mat != null:
				mat.emission_energy_multiplier = 1.8

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

func _mesh(mesh: PrimitiveMesh, at: Vector3, color: Color, emission := 0.0, scale_value := Vector3.ONE):
	var node = MeshInstance3D.new()
	node.mesh = mesh
	node.position = at
	node.scale = scale_value
	node.material_override = _material(color, emission)
	add_child(node)
	parts.append(node)
	if emission > 0.0:
		glow_parts.append(node)
	return node

func _box(size: Vector3, at: Vector3, color: Color, emission := 0.0):
	var mesh = BoxMesh.new()
	mesh.size = size
	return _mesh(mesh, at, color, emission)

func _sphere(radius: float, at: Vector3, color: Color, emission := 0.0, scale_value := Vector3.ONE):
	var mesh = SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius * 2.0
	return _mesh(mesh, at, color, emission, scale_value)

func _build():
	match kind:
		"FLECK":
			_build_fleck()
		"HAND":
			_build_hand()
		"MIRROR":
			_build_mirror()
		"TEETH":
			_build_teeth()
		"SWARM":
			_build_swarm()
		"WEATHER":
			_build_weather()
		"BURROWER":
			_build_burrower()
	var label = Label3D.new()
	label.text = kind + " · " + display_name
	label.font_size = 34
	label.outline_size = 8
	label.modulate = Color("#d2a45f")
	label.position = Vector3(0, 2.7, 0)
	label.scale = Vector3.ONE * 0.18
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	add_child(label)

func _build_fleck():
	var body = _sphere(0.26, Vector3(0, 0.9, 0), Color("#d7c780"), 1.2)
	for side in [-1.0, 1.0]:
		var wing = _sphere(0.52, Vector3(side * 0.42, 0.92, 0), Color("#6fc7bd"), 1.1, Vector3(1.2, 0.12, 0.72))
		wing.rotation.z = side * 0.35
	parts.append(body)

func _build_hand():
	_sphere(0.72, Vector3(0, 0.78, 0), Color("#8ea589"), 0.15, Vector3(1.25, 0.72, 0.82))
	_sphere(0.4, Vector3(0, 1.05, -0.72), Color("#8ea589"), 0.15)
	for x in [-0.45, 0.45]:
		for z in [-0.42, 0.42]:
			_box(Vector3(0.18, 0.75, 0.18), Vector3(x, 0.38, z), Color("#596f5d"))
	_box(Vector3(1.5, 0.75, 1.3), Vector3(0, 0.8, 1.7), Color("#27383c"))
	for i in range(3):
		_box(Vector3(0.08, 0.58, 1.3), Vector3(-0.45 + i * 0.45, 1.0, 1.7), Color("#d2a45f"), 0.25)

func _build_mirror():
	_sphere(0.7, Vector3(0, 1.0, 0), Color("#7da9ae"), 0.45, Vector3(0.85, 1.35, 0.45))
	_sphere(0.38, Vector3(0, 1.85, -0.18), Color("#c2dbd8"), 0.75)
	for side in [-1.0, 1.0]:
		var antler = _box(Vector3(0.09, 0.9, 0.09), Vector3(side * 0.28, 2.42, -0.1), Color("#d7e6df"), 0.45)
		antler.rotation.z = side * 0.34

func _build_teeth():
	_sphere(0.82, Vector3(0, 0.9, 0), Color("#864943"), 0.38, Vector3(1.2, 0.78, 0.9))
	_sphere(0.54, Vector3(0, 1.3, -0.75), Color("#a6574e"), 0.7)
	for side in [-1.0, 1.0]:
		var fang = PrismMesh.new()
		fang.size = Vector3(0.18, 0.55, 0.18)
		_mesh(fang, Vector3(side * 0.22, 1.08, -1.2), Color("#eee4d2"), 0.25)
	for i in range(7):
		var bar = _box(Vector3(0.1, 2.6, 0.1), Vector3(-1.8 + i * 0.6, 1.3, -1.8), Color("#d2a45f"), 0.18)
		bar.rotation.y = 0.0

func _build_swarm():
	for i in range(18):
		var angle = TAU * float(i) / 18.0
		var radius = 0.55 + float(i % 5) * 0.18
		_sphere(0.1, Vector3(cos(angle) * radius, 0.7 + float(i % 4) * 0.32, sin(angle) * radius), Color("#c8a8d2"), 1.1)

func _build_weather():
	for i in range(5):
		var torus = TorusMesh.new()
		torus.inner_radius = 0.9 + i * 0.28
		torus.outer_radius = 1.03 + i * 0.28
		torus.rings = 32
		torus.ring_segments = 6
		var ring = _mesh(torus, Vector3(0, 0.8 + i * 0.32, 0), Color("#8a75a0"), 0.75)
		ring.rotation.x = 0.35 + i * 0.12

func _build_burrower():
	_sphere(0.72, Vector3(0, 0.65, 0), Color("#7f8883"), 0.1, Vector3(1.25, 0.65, 0.95))
	_sphere(0.35, Vector3(0, 0.9, -0.65), Color("#909993"), 0.1)
	for i in range(6):
		_box(Vector3(0.08, 0.7, 0.75), Vector3(-0.8 + i * 0.32, 1.15, 0.05), Color("#d2a45f"), 0.18)

func _process(delta):
	time += delta
	match kind:
		"FLECK":
			position = home + Vector3(cos(time * 0.8) * 1.8, 1.2 + sin(time * 2.2) * 0.45, sin(time * 0.8) * 1.8)
			rotation.y = -time * 0.8
		"HAND":
			position.x = home.x + sin(time * 0.34) * 5.0
			rotation.y = PI * 0.5 if cos(time * 0.34) > 0.0 else -PI * 0.5
		"MIRROR":
			if player != null:
				look_at(Vector3(player.global_position.x, global_position.y, player.global_position.z), Vector3.UP)
				rotation.y += sin(time * 1.4) * 0.08
		"TEETH":
			position.x = home.x + sin(time * (0.75 + leakage * 0.006)) * 1.15
			rotation.y = sin(time * 0.75) * 0.65
		"SWARM":
			rotation.y += delta * (0.55 + leakage * 0.018)
			for i in range(parts.size()):
				var part = parts[i] as MeshInstance3D
				part.position.y += sin(time * 3.0 + i) * 0.002
				part.visible = i < int(8 + leakage * 0.1)
		"WEATHER":
			rotation.y += delta * (0.2 + leakage * 0.012)
			position.y = home.y + sin(time * 0.7) * 0.35
		"BURROWER":
			position.z = home.z + sin(time * 0.22) * 2.0
			visible = not (fmod(time, 9.0) > 7.2 and stability > 35.0)
