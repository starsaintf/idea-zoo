extends Node3D

var profile = {}
var follow_target: Node3D
var body_root: Node3D
var hidden_burden: Node3D
var name_label: Label3D
var time = 0.0
var target_scale = 1.0
var mood = "curious"
var revealed = false
var evidence_level = 0.2
var risk_level = 0.2
var guardrail_level = 0.0

var palette = {
	"FLECK": Color("#f3c76f"),
	"HAND": Color("#67c8b8"),
	"MIRROR": Color("#8eb7d6"),
	"TEETH": Color("#c86b5b"),
	"SWARM": Color("#d6a55e"),
	"WEATHER": Color("#9a8fd5"),
	"BURROWER": Color("#91a875")
}

func _ready():
	body_root = Node3D.new()
	body_root.name = "SpecimenBody"
	add_child(body_root)

func configure(data: Dictionary):
	profile = data.duplicate(true)
	if body_root == null:
		_ready()
	_clear_body()
	_build_body()
	set_stage(0.2, 0.2, 0.0)

func _clear_body():
	if body_root == null:
		return
	for child in body_root.get_children():
		child.queue_free()
	name_label = null
	hidden_burden = null

func _build_body():
	var specimen_class = String(profile.get("class", "FLECK"))
	var base_color = palette.get(specimen_class, Color("#67c8b8"))
	var appetite = String(profile.get("appetite", "attention"))

	var core = MeshInstance3D.new()
	core.name = "Core"
	var core_mesh
	match specimen_class:
		"HAND":
			var capsule = CapsuleMesh.new()
			capsule.radius = 0.72
			capsule.height = 2.15
			core_mesh = capsule
		"MIRROR":
			var prism = PrismMesh.new()
			prism.size = Vector3(1.35, 2.15, 0.75)
			core_mesh = prism
		"TEETH":
			var cone = CylinderMesh.new()
			cone.top_radius = 0.22
			cone.bottom_radius = 0.95
			cone.height = 1.9
			cone.radial_segments = 7
			core_mesh = cone
		"WEATHER":
			var sphere = SphereMesh.new()
			sphere.radius = 1.05
			sphere.height = 1.45
			core_mesh = sphere
		"BURROWER":
			var low = CapsuleMesh.new()
			low.radius = 0.72
			low.height = 1.55
			core_mesh = low
		_:
			var sphere = SphereMesh.new()
			sphere.radius = 0.72 if specimen_class == "FLECK" else 0.88
			sphere.height = 1.45
			core_mesh = sphere
	core.mesh = core_mesh
	core.position.y = 1.15
	core.material_override = _material(base_color, 0.22)
	body_root.add_child(core)

	_build_face(base_color, specimen_class)
	_build_class_parts(base_color, specimen_class)
	_build_appetite_mark(appetite, base_color)
	_build_hidden_burden()

	name_label = Label3D.new()
	name_label.text = String(profile.get("creature_name", "UNNAMED SPECIMEN"))
	name_label.position = Vector3(0, 3.25, 0)
	name_label.font_size = 38
	name_label.outline_size = 8
	name_label.modulate = Color("#eadfca")
	name_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	body_root.add_child(name_label)

func _build_face(color: Color, specimen_class: String):
	for side in [-1.0, 1.0]:
		var eye = MeshInstance3D.new()
		var eye_mesh = SphereMesh.new()
		eye_mesh.radius = 0.12
		eye_mesh.height = 0.18
		eye.mesh = eye_mesh
		eye.position = Vector3(0.2 * side, 1.55, -0.62)
		eye.material_override = _material(Color("#071015"), 0.0)
		body_root.add_child(eye)
		if specimen_class == "MIRROR" or String(profile.get("appetite", "")) == "attention":
			var halo = MeshInstance3D.new()
			var halo_mesh = TorusMesh.new()
			halo_mesh.inner_radius = 0.14
			halo_mesh.outer_radius = 0.2
			halo.mesh = halo_mesh
			halo.position = eye.position + Vector3(0, 0, 0.04)
			halo.rotation_degrees.x = 90
			halo.material_override = _material(color.lightened(0.2), 1.1)
			body_root.add_child(halo)

func _build_class_parts(color: Color, specimen_class: String):
	match specimen_class:
		"HAND":
			for side in [-1.0, 1.0]:
				var limb = _capsule(Vector3(0.18, 1.55, 0.18), color.darkened(0.08))
				limb.position = Vector3(0.88 * side, 1.15, 0)
				limb.rotation_degrees.z = 20.0 * side
				body_root.add_child(limb)
			var harness = _box(Vector3(1.8, 0.18, 0.72), Color("#d2a45f"), 0.4)
			harness.position = Vector3(0, 1.2, 0.42)
			body_root.add_child(harness)
		"MIRROR":
			for i in range(6):
				var shard = MeshInstance3D.new()
				var shard_mesh = PrismMesh.new()
				shard_mesh.size = Vector3(0.28, 0.9, 0.08)
				shard.mesh = shard_mesh
				var angle = TAU * float(i) / 6.0
				shard.position = Vector3(cos(angle) * 1.0, 1.35 + sin(angle * 2.0) * 0.2, sin(angle) * 1.0)
				shard.rotation.y = -angle
				shard.material_override = _material(color.lightened(0.2), 0.6, 0.35)
				body_root.add_child(shard)
		"TEETH":
			for i in range(8):
				var tooth = MeshInstance3D.new()
				var tooth_mesh = CylinderMesh.new()
				tooth_mesh.top_radius = 0.0
				tooth_mesh.bottom_radius = 0.13
				tooth_mesh.height = 0.48
				tooth_mesh.radial_segments = 5
				tooth.mesh = tooth_mesh
				var angle = TAU * float(i) / 8.0
				tooth.position = Vector3(cos(angle) * 0.83, 0.55, sin(angle) * 0.83)
				tooth.rotation.x = PI
				tooth.material_override = _material(Color("#e6d6b9"), 0.15)
				body_root.add_child(tooth)
		"SWARM":
			for i in range(9):
				var mote = MeshInstance3D.new()
				var mote_mesh = SphereMesh.new()
				mote_mesh.radius = 0.13 + float(i % 3) * 0.025
				mote_mesh.height = 0.25
				mote.mesh = mote_mesh
				var angle = TAU * float(i) / 9.0
				mote.position = Vector3(cos(angle) * (1.0 + float(i % 2) * 0.25), 1.2 + sin(angle * 2.0) * 0.45, sin(angle) * (1.0 + float(i % 2) * 0.25))
				mote.material_override = _material(color, 0.8)
				mote.set_meta("orbit", angle)
				body_root.add_child(mote)
		"WEATHER":
			for i in range(5):
				var ribbon = MeshInstance3D.new()
				var ribbon_mesh = TorusMesh.new()
				ribbon_mesh.inner_radius = 0.85 + float(i) * 0.12
				ribbon_mesh.outer_radius = 0.91 + float(i) * 0.12
				ribbon_mesh.rings = 28
				ribbon.mesh = ribbon_mesh
				ribbon.position.y = 0.7 + float(i) * 0.3
				ribbon.rotation_degrees.x = 90 + float(i) * 7.0
				ribbon.material_override = _material(color.lightened(float(i) * 0.04), 0.35)
				body_root.add_child(ribbon)
		"BURROWER":
			for side in [-1.0, 1.0]:
				var claw = _box(Vector3(0.52, 0.18, 0.85), color.darkened(0.18), 0.0)
				claw.position = Vector3(0.55 * side, 0.45, -0.55)
				claw.rotation_degrees.y = 12.0 * side
				body_root.add_child(claw)
		"FLECK":
			for side in [-1.0, 1.0]:
				var wing = MeshInstance3D.new()
				var wing_mesh = PrismMesh.new()
				wing_mesh.size = Vector3(0.72, 0.08, 0.56)
				wing.mesh = wing_mesh
				wing.position = Vector3(0.52 * side, 1.25, 0)
				wing.rotation_degrees.z = 22.0 * side
				wing.material_override = _material(color.lightened(0.18), 0.55)
				body_root.add_child(wing)

func _build_appetite_mark(appetite: String, color: Color):
	var mark = Label3D.new()
	mark.text = appetite.to_upper()
	mark.position = Vector3(0, 0.25, -0.75)
	mark.font_size = 25
	mark.outline_size = 6
	mark.modulate = color.lightened(0.22)
	mark.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	body_root.add_child(mark)

func _build_hidden_burden():
	hidden_burden = Node3D.new()
	hidden_burden.name = "HiddenBurden"
	hidden_burden.visible = false
	body_root.add_child(hidden_burden)
	for i in range(4):
		var weight = _box(Vector3(0.38, 0.38, 0.38), Color("#9a4e44"), 0.35)
		weight.position = Vector3(-0.55 + float(i) * 0.36, 0.1 + float(i % 2) * 0.25, 0.65)
		hidden_burden.add_child(weight)
	var text = Label3D.new()
	text.text = String(profile.get("burden", "UNNAMED MAINTENANCE")).to_upper()
	text.position = Vector3(0, 0.95, 0.6)
	text.font_size = 20
	text.outline_size = 6
	text.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	text.modulate = Color("#e38a79")
	hidden_burden.add_child(text)

func _process(delta):
	time += delta
	if body_root == null:
		return
	body_root.position.y = sin(time * 2.2) * 0.08
	body_root.rotation.y += delta * (0.08 if mood != "agitated" else 0.28)
	var scale_speed = 2.2 * delta
	scale = scale.lerp(Vector3.ONE * target_scale, scale_speed)
	for child in body_root.get_children():
		if child.has_meta("orbit"):
			var angle = float(child.get_meta("orbit")) + time * (0.55 + risk_level * 0.6)
			var radius = 1.0 + evidence_level * 0.35
			child.position.x = cos(angle) * radius
			child.position.z = sin(angle) * radius
			child.position.y = 1.2 + sin(angle * 2.0 + time) * 0.45
	if follow_target != null:
		var desired = follow_target.global_position + Vector3(1.8, 0, 1.8).rotated(Vector3.UP, follow_target.rotation.y)
		global_position = global_position.lerp(desired, min(1.0, delta * 2.2))

func set_follow_target(target: Node3D):
	follow_target = target

func set_revealed(active: bool):
	revealed = active
	if hidden_burden != null:
		hidden_burden.visible = active

func set_stage(evidence: float, risk: float, guardrails: float):
	evidence_level = clamp(evidence, 0.0, 1.0)
	risk_level = clamp(risk, 0.0, 1.0)
	guardrail_level = clamp(guardrails, 0.0, 1.0)
	target_scale = 0.78 + evidence_level * 0.5
	mood = "agitated" if risk_level > 0.62 else "steady" if evidence_level > 0.55 else "curious"
	if name_label != null:
		name_label.modulate = Color("#e57f6c") if risk_level > 0.7 else Color("#eadfca")

func molt(updated_profile: Dictionary, guardrails: Array):
	profile = updated_profile.duplicate(true)
	profile["guardrails"] = guardrails.duplicate()
	configure(profile)
	set_stage(evidence_level, max(0.0, risk_level - float(guardrails.size()) * 0.08), min(1.0, float(guardrails.size()) / 6.0))

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

func _capsule(size: Vector3, color: Color):
	var mesh = MeshInstance3D.new()
	var shape = CapsuleMesh.new()
	shape.radius = size.x
	shape.height = size.y
	mesh.mesh = shape
	mesh.material_override = _material(color)
	return mesh
