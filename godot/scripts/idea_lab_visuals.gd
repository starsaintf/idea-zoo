extends RefCounted

static func material(color: Color, emission := 0.0, metallic := 0.0) -> StandardMaterial3D:
	var mat = StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = 0.78
	mat.metallic = metallic
	if emission > 0.0:
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = emission
	return mat

static func box(size: Vector3, color: Color, emission := 0.0) -> MeshInstance3D:
	var node = MeshInstance3D.new()
	var mesh = BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = material(color, emission)
	return node

static func sphere(radius: float, color: Color, emission := 0.0) -> MeshInstance3D:
	var node = MeshInstance3D.new()
	var mesh = SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius * 2.0
	node.mesh = mesh
	node.material_override = material(color, emission)
	return node

static func capsule(radius: float, height: float, color: Color) -> MeshInstance3D:
	var node = MeshInstance3D.new()
	var mesh = CapsuleMesh.new()
	mesh.radius = radius
	mesh.height = height
	node.mesh = mesh
	node.material_override = material(color)
	return node

static func label(text: String, font_size: int, color: Color) -> Label3D:
	var node = Label3D.new()
	node.text = text
	node.font_size = font_size
	node.outline_size = 7
	node.modulate = color
	node.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	node.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	return node

static func arch(text: String, color: Color, paper: Color) -> Node3D:
	var root = Node3D.new()
	for side in [-1.0, 1.0]:
		var pillar = box(Vector3(0.45, 4.2, 0.45), color, 0.15)
		pillar.position = Vector3(2.2 * side, 2.1, 0)
		root.add_child(pillar)
	var lintel = box(Vector3(5.0, 0.45, 0.55), color, 0.18)
	lintel.position.y = 4.1
	root.add_child(lintel)
	var tag = label(text, 34, paper)
	tag.position = Vector3(0, 4.85, 0)
	root.add_child(tag)
	return root

static func staff_figure(staff_name: String, role: String, color: Color, paper: Color) -> Node3D:
	var root = Node3D.new()
	var body = capsule(0.38, 1.35, color.darkened(0.16))
	body.position.y = 1.0
	root.add_child(body)
	var head = sphere(0.28, Color("#8c604a"))
	head.position.y = 1.95
	root.add_child(head)
	var tag = label("%s\n%s" % [staff_name, role], 19, paper)
	tag.position = Vector3(0, 2.7, 0)
	root.add_child(tag)
	return root

static func station(title: String, color: Color, staff_name: String, role: String, paper: Color) -> Dictionary:
	var root = Node3D.new()
	var dais = MeshInstance3D.new()
	var dais_mesh = CylinderMesh.new()
	dais_mesh.top_radius = 3.2
	dais_mesh.bottom_radius = 3.5
	dais_mesh.height = 0.35
	dais_mesh.radial_segments = 28
	dais.mesh = dais_mesh
	dais.position.y = 0.17
	dais.material_override = material(Color(color, 0.3), 0.18, 0.2)
	root.add_child(dais)
	var marker = MeshInstance3D.new()
	var marker_mesh = TorusMesh.new()
	marker_mesh.inner_radius = 1.75
	marker_mesh.outer_radius = 1.86
	marker_mesh.rings = 36
	marker.mesh = marker_mesh
	marker.position.y = 0.52
	marker.rotation_degrees.x = 90
	marker.material_override = material(color, 0.65, 0.25)
	root.add_child(marker)
	var tag = label(title, 30, paper)
	tag.position = Vector3(0, 3.8, 0)
	root.add_child(tag)
	var figure = staff_figure(staff_name, role, color, paper)
	figure.position = Vector3(0, 0.4, 0)
	root.add_child(figure)
	return {"root": root, "marker": marker}
