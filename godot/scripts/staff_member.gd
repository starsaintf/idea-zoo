extends Node3D

var staff_id = ""
var display_name = ""
var role = ""
var department = ""
var report_title = ""
var report_body = ""
var class_votes = {}
var integrity_delta = 0.0
var trust_delta = 0.0
var leakage_delta = 0.0
var visited = false
var available = false
var time = 0.0
var lamp: MeshInstance3D
var body_root: Node3D

func configure(data: Dictionary):
	staff_id = String(data.get("id", "staff"))
	display_name = String(data.get("name", "Keeper"))
	role = String(data.get("role", "STAFF"))
	department = String(data.get("department", "ZOO"))
	report_title = String(data.get("report_title", role))
	report_body = String(data.get("report_body", ""))
	class_votes = data.get("votes", {})
	integrity_delta = float(data.get("integrity", 0.0))
	trust_delta = float(data.get("trust", 0.0))
	leakage_delta = float(data.get("leakage", 0.0))
	position = data.get("position", Vector3.ZERO)
	name = display_name.replace(" ", "")
	set_meta("staff_id", staff_id)
	_build(data.get("color", Color("#d2a45f")))

func _material(color: Color, emission := 0.0):
	var mat = StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = 0.78
	if emission > 0.0:
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = emission
	return mat

func _build(accent: Color):
	body_root = Node3D.new()
	add_child(body_root)
	var body = MeshInstance3D.new()
	var capsule = CapsuleMesh.new()
	capsule.radius = 0.38
	capsule.height = 1.35
	body.mesh = capsule
	body.position.y = 0.82
	body.material_override = _material(Color("#d9d0bd"))
	body_root.add_child(body)
	var head = MeshInstance3D.new()
	var sphere = SphereMesh.new()
	sphere.radius = 0.28
	sphere.height = 0.56
	head.mesh = sphere
	head.position.y = 1.72
	head.material_override = _material(Color("#6b4f40"))
	body_root.add_child(head)
	var sash = MeshInstance3D.new()
	var sash_mesh = BoxMesh.new()
	sash_mesh.size = Vector3(0.12, 1.3, 0.56)
	sash.mesh = sash_mesh
	sash.position = Vector3(0.16, 0.94, -0.34)
	sash.rotation.z = -0.35
	sash.material_override = _material(accent, 0.38)
	body_root.add_child(sash)
	lamp = MeshInstance3D.new()
	var lamp_mesh = SphereMesh.new()
	lamp_mesh.radius = 0.13
	lamp_mesh.height = 0.26
	lamp.mesh = lamp_mesh
	lamp.position = Vector3(0, 2.35, 0)
	lamp.material_override = _material(Color("#445359"), 0.15)
	add_child(lamp)
	var label = Label3D.new()
	label.text = display_name.to_upper() + "\n" + role
	label.font_size = 34
	label.outline_size = 8
	label.modulate = accent
	label.position = Vector3(0, 2.75, 0)
	label.scale = Vector3.ONE * 0.17
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	add_child(label)

func set_available(value: bool):
	available = value and not visited
	visible = value
	_update_lamp()

func mark_visited():
	visited = true
	available = false
	_update_lamp()

func close_shift():
	available = false
	_update_lamp()

func _update_lamp():
	if lamp == null:
		return
	var color = Color("#5fc9b8") if available else Color("#4b5557")
	if visited:
		color = Color("#d2a45f")
	lamp.material_override = _material(color, 1.2 if available or visited else 0.1)

func _process(delta):
	time += delta
	if body_root != null:
		body_root.position.y = sin(time * 1.7 + float(staff_id.hash() % 7)) * 0.025
		if available:
			body_root.rotation.y = sin(time * 0.55) * 0.16
