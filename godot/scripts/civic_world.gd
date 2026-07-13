extends Node3D

var clues = []
var anchors = []
var verdict_gates = []
var verdict_root: Node3D
var creature_spawn = Vector3(0, 0.65, 8)
var zoo_spawn = Vector3(0, 1.0, -24)
var zoo_return = Vector3(0, 0.6, -27)
var color_ink = Color("#071015")
var color_paper = Color("#e8dfcf")
var color_brass = Color("#d2a45f")
var color_glass = Color("#5ca6a6")
var color_rust = Color("#a6574e")
var color_moss = Color("#7f9f79")

func _ready():
	build_world()

func material(color: Color, emission_strength := 0.0, metallic := 0.0) -> StandardMaterial3D:
	var mat = StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = 0.82
	mat.metallic = metallic
	if emission_strength > 0.0:
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = emission_strength
	return mat

func build_world():
	_build_environment()
	_build_island()
	_build_zoo()
	_build_districts()
	_build_threadways()
	_build_clues()
	_build_seal_anchors()
	_build_optional_organisms()
	_build_verdict_garden()

func _build_environment():
	var env_node = WorldEnvironment.new()
	var env = Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("#0a1b22")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("#adc7bd")
	env.ambient_light_energy = 0.72
	env.fog_enabled = true
	env.fog_light_color = Color("#17333b")
	env.fog_density = 0.012
	env_node.environment = env
	add_child(env_node)
	var moon = DirectionalLight3D.new()
	moon.rotation_degrees = Vector3(-52, -32, 0)
	moon.light_color = Color("#d9cfb5")
	moon.light_energy = 1.05
	moon.shadow_enabled = false
	add_child(moon)

func _build_island():
	var ground_mat = material(Color("#102126"))
	var body = StaticBody3D.new()
	body.name = "CivicTerrarium"
	var mesh = MeshInstance3D.new()
	var cylinder = CylinderMesh.new()
	cylinder.top_radius = 39.0
	cylinder.bottom_radius = 39.0
	cylinder.height = 1.2
	cylinder.radial_segments = 64
	mesh.mesh = cylinder
	mesh.material_override = ground_mat
	body.add_child(mesh)
	var shape = CollisionShape3D.new()
	var collision = CylinderShape3D.new()
	collision.radius = 39.0
	collision.height = 1.2
	shape.shape = collision
	body.add_child(shape)
	body.position.y = -0.6
	add_child(body)
	for radius in [10.0, 20.0, 31.5]:
		var ring = MeshInstance3D.new()
		var torus = TorusMesh.new()
		torus.inner_radius = radius - 0.08
		torus.outer_radius = radius + 0.08
		torus.rings = 64
		torus.ring_segments = 8
		ring.mesh = torus
		ring.position.y = 0.03
		ring.material_override = material(color_brass, 0.55, 0.55)
		ring.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		add_child(ring)

func _build_zoo():
	var zoo = Node3D.new()
	zoo.name = "IdeaZoo"
	zoo.position = Vector3(0, 0, -30)
	add_child(zoo)
	_make_box(zoo, Vector3(0, 3.0, 0), Vector3(18, 6, 5), material(Color("#17272b"), 0.0, 0.1), true)
	_make_box(zoo, Vector3(0, 6.4, 0), Vector3(20, 0.32, 5.5), material(color_brass, 0.5, 0.65), false)
	for x in [-7.5, -4.5, 4.5, 7.5]:
		_make_box(zoo, Vector3(x, 2.6, 2.7), Vector3(0.35, 5.2, 0.35), material(color_brass, 0.35, 0.55), false)
	var gate = _make_arch(zoo, Vector3(0, 0, 2.8), 5.8, 5.2, color_brass)
	gate.name = "WhisperGate"
	_make_label(zoo, "THE IDEA ZOO", Vector3(0, 6.15, 2.8), 0.72, color_paper)
	_make_label(zoo, "EVERY CREATURE IS ALSO A CONSEQUENCE", Vector3(0, 5.35, 2.8), 0.22, color_brass)

func _build_districts():
	var building_colors = [Color("#17343a"), Color("#203036"), Color("#1d2c32"), Color("#273438")]
	var positions = []
	for ring_radius in [15.0, 26.0, 34.0]:
		var count = 14 if ring_radius < 20.0 else 18
		for i in range(count):
			var a = TAU * float(i) / float(count) + ring_radius * 0.013
			var p = Vector3(cos(a) * ring_radius, 0, sin(a) * ring_radius)
			if p.z < -23.0 or abs(p.x) < 4.0 or abs(p.z - 8.0) < 4.5:
				continue
			positions.append(p)
	var index = 0
	for p in positions:
		var height = 2.8 + float((index * 17) % 8) * 0.72
		var width = 2.6 + float((index * 11) % 4) * 0.35
		var depth = 2.4 + float((index * 7) % 5) * 0.28
		var root = Node3D.new()
		root.position = p
		root.rotation.y = atan2(p.x, p.z)
		add_child(root)
		_make_box(root, Vector3(0, height * 0.5, 0), Vector3(width, height, depth), material(building_colors[index % building_colors.size()]), true)
		_make_box(root, Vector3(0, height + 0.1, 0), Vector3(width + 0.2, 0.18, depth + 0.2), material(color_brass, 0.28, 0.5), false)
		if index % 3 == 0:
			_make_label(root, ["WAIT HERE", "NO IDEA UNTAGGED", "PUBLIC MEMORY", "MOLT PERMIT"][index % 4], Vector3(0, height * 0.55, depth * 0.52), 0.18, color_paper)
		index += 1
	_make_label(self, "OPEN MARKET", Vector3(-15, 1.2, 5), 0.42, color_brass)
	_make_label(self, "LOW HARBOUR", Vector3(18, 1.2, 18), 0.42, color_brass)
	_make_label(self, "SCHOOL QUARTER", Vector3(-20, 1.2, 22), 0.42, color_brass)

func _build_threadways():
	var mat = material(Color("#3aa7a0"), 1.2, 0.1)
	for axis in [0, 1]:
		var strip = MeshInstance3D.new()
		var box = BoxMesh.new()
		box.size = Vector3(3.2, 0.035, 66.0) if axis == 0 else Vector3(66.0, 0.035, 3.2)
		strip.mesh = box
		strip.position.y = 0.04
		strip.material_override = mat
		strip.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		add_child(strip)
	for item in [[Vector3(-1.2, 0.1, -16), "TO THE ZOO"], [Vector3(8, 0.1, 1.2), "FOLLOW THE MISSING MINUTES"], [Vector3(-12, 0.1, 1.2), "ALL SHORTCUTS LEAVE A SHADOW"]]:
		_make_label(self, item[1], item[0] + Vector3(0, 0.06, 0), 0.18, Color("#7ad1c7"), true)

func _build_clues():
	var data = [[Vector3(-9, 0.55, 7), "THE EMPTY CUP", "A courier crossed the square without waiting. Behind this wall, a porter lost forty minutes he cannot explain."], [Vector3(8, 0.55, 14), "THE AGREEMENT OF CLOCKS", "Every clock reports a faster city. None records whose day became longer."], [Vector3(4, 0.55, 1), "THE UNNAMED KEEPER", "The creature avoids permanent staff. It circles temporary workers and unowned maintenance hatches."]]
	for i in range(data.size()):
		var root = Node3D.new()
		root.name = "Clue_%d" % i
		root.position = data[i][0]
		add_child(root)
		var crystal = MeshInstance3D.new()
		var prism = PrismMesh.new()
		prism.size = Vector3(0.6, 1.5, 0.6)
		crystal.mesh = prism
		crystal.material_override = material(Color(0.15, 0.75, 0.72, 0.42), 1.3)
		crystal.position.y = 0.75
		root.add_child(crystal)
		var label = _make_label(root, data[i][1], Vector3(0, 1.85, 0), 0.24, color_brass)
		label.visible = false
		clues.append({"node": root, "title": data[i][1], "text": data[i][2], "scanned": false, "label": label, "crystal": crystal})

func _build_seal_anchors():
	var center = creature_spawn
	for i in range(3):
		var a = -PI * 0.5 + TAU * float(i) / 3.0
		var p = center + Vector3(cos(a) * 5.0, -0.5, sin(a) * 5.0)
		var root = Node3D.new()
		root.position = p
		root.visible = false
		add_child(root)
		var disc = MeshInstance3D.new()
		var cyl = CylinderMesh.new()
		cyl.top_radius = 1.05
		cyl.bottom_radius = 1.05
		cyl.height = 0.12
		cyl.radial_segments = 32
		disc.mesh = cyl
		disc.material_override = material(Color("#39545a"), 0.32)
		root.add_child(disc)
		anchors.append({"node": root, "seal": "", "label": null})

func _build_optional_organisms():
	var lantern = Node3D.new()
	lantern.name = "HospitalLantern"
	lantern.position = Vector3(-26, 1.4, -4)
	add_child(lantern)
	var wing_mat = material(Color("#79c6bf"), 1.6)
	for side in [-1.0, 1.0]:
		var wing = MeshInstance3D.new()
		var sphere = SphereMesh.new()
		sphere.radius = 0.7
		sphere.height = 1.0
		wing.mesh = sphere
		wing.scale = Vector3(1.3, 0.18, 0.8)
		wing.position.x = side * 0.65
		wing.rotation.z = side * 0.35
		wing.material_override = wing_mat
		lantern.add_child(wing)
	_make_label(lantern, "A SMALL THING CAN STILL HOLD A ROOM TOGETHER", Vector3(0, 1.55, 0), 0.2, color_paper)
	lantern.set_meta("optional_kind", "lantern")
	var mole = Node3D.new()
	mole.name = "LedgerMole"
	mole.position = Vector3(25, 0.6, 22)
	add_child(mole)
	var body = MeshInstance3D.new()
	var body_mesh = SphereMesh.new()
	body_mesh.radius = 0.9
	body_mesh.height = 1.45
	body.mesh = body_mesh
	body.scale = Vector3(1.25, 0.72, 1.0)
	body.material_override = material(Color("#89918c"))
	mole.add_child(body)
	for j in range(5):
		_make_box(mole, Vector3(-1.0 + j * 0.5, 0.9, 0), Vector3(0.12, 0.8, 0.7), material(color_brass, 0.22, 0.45), false)
	_make_label(mole, "IT HAS NUMBERED WHAT THE CITY FORGOT", Vector3(0, 1.8, 0), 0.2, color_paper)
	mole.set_meta("optional_kind", "mole")

func _build_verdict_garden():
	verdict_root = Node3D.new()
	verdict_root.name = "VerdictGarden"
	verdict_root.position = Vector3(0, 0, -26)
	verdict_root.visible = false
	add_child(verdict_root)
	var data = [["OPEN GATE", "release", Color("#7f9f79")], ["BRASS HARNESS", "restricted", color_brass], ["MOLT POOL", "molt", Color("#6bb0ad")], ["QUIET SANCTUARY", "sanctuary", Color("#8e7ba0")], ["WHITE ROOM", "destroy", color_rust]]
	for i in range(data.size()):
		var x = (float(i) - 2.0) * 4.2
		var gate = Node3D.new()
		gate.position = Vector3(x, 0, 3.5)
		verdict_root.add_child(gate)
		_make_arch(gate, Vector3.ZERO, 3.2, 3.4, data[i][2])
		_make_label(gate, data[i][0], Vector3(0, 3.9, 0), 0.22, data[i][2])
		verdict_gates.append({"node": gate, "id": data[i][1], "title": data[i][0]})

func reveal_clues(active: bool):
	for clue in clues:
		if not clue["scanned"]:
			clue["crystal"].visible = active
			clue["label"].visible = active

func reveal_anchors(active: bool):
	for anchor in anchors:
		anchor["node"].visible = active

func reveal_verdict_garden():
	verdict_root.visible = true

func place_seal(anchor_index: int, seal_name: String, seal_color: Color):
	var anchor = anchors[anchor_index]
	anchor["seal"] = seal_name
	if anchor["label"] != null:
		anchor["label"].queue_free()
	anchor["label"] = _make_label(anchor["node"], seal_name.to_upper(), Vector3(0, 0.7, 0), 0.22, seal_color)
	var disc = anchor["node"].get_child(0) as MeshInstance3D
	disc.material_override = material(seal_color, 1.15, 0.25)

func nearest_clue(position: Vector3, max_distance := 3.2):
	var best = null
	var distance = max_distance
	for i in range(clues.size()):
		if clues[i]["scanned"]:
			continue
		var d = position.distance_to(clues[i]["node"].global_position)
		if d < distance:
			distance = d
			best = i
	return best

func nearest_anchor(position: Vector3, max_distance := 2.6):
	var best = null
	var distance = max_distance
	for i in range(anchors.size()):
		var d = position.distance_to(anchors[i]["node"].global_position)
		if d < distance:
			distance = d
			best = i
	return best

func nearest_verdict(position: Vector3, max_distance := 2.3):
	if verdict_root == null or not verdict_root.visible:
		return null
	var best = null
	var distance = max_distance
	for i in range(verdict_gates.size()):
		var d = position.distance_to(verdict_gates[i]["node"].global_position)
		if d < distance:
			distance = d
			best = i
	return best

func optional_near(position: Vector3, max_distance := 2.8):
	for child in get_children():
		if child is Node3D and child.has_meta("optional_kind") and child.visible:
			if position.distance_to(child.global_position) < max_distance:
				return child
	return null

func is_on_threadway(position: Vector3) -> bool:
	return abs(position.x) < 1.8 or abs(position.z) < 1.8

func _make_box(parent: Node, position: Vector3, size: Vector3, mat: Material, collision: bool):
	var holder: Node3D
	if collision:
		holder = StaticBody3D.new()
	else:
		holder = Node3D.new()
	holder.position = position
	parent.add_child(holder)
	var mesh_instance = MeshInstance3D.new()
	var mesh = BoxMesh.new()
	mesh.size = size
	mesh_instance.mesh = mesh
	mesh_instance.material_override = mat
	holder.add_child(mesh_instance)
	if collision:
		var collision_shape = CollisionShape3D.new()
		var shape = BoxShape3D.new()
		shape.size = size
		collision_shape.shape = shape
		holder.add_child(collision_shape)
	return holder

func _make_arch(parent: Node, position: Vector3, width: float, height: float, color: Color):
	var root = Node3D.new()
	root.position = position
	parent.add_child(root)
	var mat = material(color, 0.72, 0.55)
	_make_box(root, Vector3(-width * 0.5, height * 0.5, 0), Vector3(0.35, height, 0.45), mat, false)
	_make_box(root, Vector3(width * 0.5, height * 0.5, 0), Vector3(0.35, height, 0.45), mat, false)
	_make_box(root, Vector3(0, height, 0), Vector3(width + 0.35, 0.35, 0.45), mat, false)
	return root

func _make_label(parent: Node, text: String, position: Vector3, scale_value: float, color: Color, horizontal := false):
	var label = Label3D.new()
	label.text = text
	label.font_size = 42
	label.modulate = color
	label.outline_size = 8
	label.outline_modulate = Color(0.01, 0.03, 0.04, 0.88)
	label.position = position
	label.scale = Vector3.ONE * scale_value
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	if horizontal:
		label.rotation_degrees.x = -90
		label.billboard = BaseMaterial3D.BILLBOARD_DISABLED
	parent.add_child(label)
	return label
