extends Node3D

const AmbientOrganism = preload("res://scripts/ambient_organism.gd")
const StaffMember = preload("res://scripts/staff_member.gd")

var clues = []
var anchors = []
var verdict_gates = []
var staff_members = []
var ecology = []
var departments = {}
var classification_bars = {}
var classification_labels = {}
var staff_stamps = []
var verdict_root: Node3D
var department_root: Node3D
var classification_root: Node3D
var processing_dais: Node3D
var board_record: Node3D
var board_crystal: MeshInstance3D
var board_label: Label3D
var creature_spawn = Vector3(0, 0.65, 8)
var zoo_spawn = Vector3(0, 1.0, -24)
var zoo_return = Vector3(0, 0.6, -27)
var color_ink = Color("#071015")
var color_paper = Color("#e8dfcf")
var color_brass = Color("#d2a45f")
var color_glass = Color("#5ca6a6")
var color_rust = Color("#a6574e")
var color_moss = Color("#7f9f79")
var board_exposed = false

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
	_build_departments()
	_build_classification_board()
	_build_board_record()
	_build_staff()
	_build_ecology()
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
	var body = StaticBody3D.new()
	body.name = "CivicTerrarium"
	var mesh = MeshInstance3D.new()
	var cylinder = CylinderMesh.new()
	cylinder.top_radius = 39.0
	cylinder.bottom_radius = 39.0
	cylinder.height = 1.2
	cylinder.radial_segments = 64
	mesh.mesh = cylinder
	mesh.material_override = material(Color("#102126"))
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
			if p.z < -12.0 or abs(p.x) < 4.0 or abs(p.z - 8.0) < 4.5:
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
	var data = [
		[Vector3(-9, 0.55, 7), "THE EMPTY CUP", "A courier crossed the square without waiting. Behind this wall, a porter lost forty minutes he cannot explain."],
		[Vector3(8, 0.55, 14), "THE AGREEMENT OF CLOCKS", "Every clock reports a faster city. None records whose day became longer."],
		[Vector3(4, 0.55, 1), "THE UNNAMED KEEPER", "The creature avoids permanent staff. It circles temporary workers and unowned maintenance hatches."]
	]
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
	for i in range(3):
		var a = -PI * 0.5 + TAU * float(i) / 3.0
		var p = creature_spawn + Vector3(cos(a) * 5.0, -0.5, sin(a) * 5.0)
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

func _build_departments():
	department_root = Node3D.new()
	department_root.name = "ZooDepartments"
	add_child(department_root)
	var data = [
		["hatchery", "HATCHERY", Vector3(-14, 0, -20.5), Color("#67c8b8")],
		["behaviour", "BEHAVIOUR YARD", Vector3(14, 0, -20.5), color_rust],
		["counterfactual", "CIVIC ARENA", Vector3(-17, 0, -13.5), Color("#7896c8")],
		["jury", "CHILDREN'S JURY", Vector3(17, 0, -13.5), Color("#d6c56f")],
		["weather", "WEATHER DOME", Vector3(-23, 0, -6.5), Color("#9078a3")],
		["working", "WORKING PADDOCK", Vector3(23, 0, -6.5), color_moss],
		["molt", "MOLT HOUSE", Vector3(-8.5, 0, -24), color_glass],
		["white", "WHITE ROOM", Vector3(8.5, 0, -24), Color("#ddd9cf")],
		["archive", "SEALED ARCHIVE", Vector3(0, 0, -21.5), Color("#9b7746")],
		["sanctuary", "QUIET SANCTUARY", Vector3(-20, 0, -25.5), Color("#8e7ba0")],
		["vault", "PREDATOR VAULT", Vector3(20, 0, -25.5), color_rust]
	]
	for item in data:
		var root = Node3D.new()
		root.position = item[2]
		root.name = String(item[0]).capitalize()
		department_root.add_child(root)
		_make_box(root, Vector3(0, 0.16, 0), Vector3(5.2, 0.32, 4.2), material(Color("#13242a"), 0.0, 0.12), true)
		_make_arch(root, Vector3(0, 0.16, -1.8), 3.7, 3.0, item[3])
		_make_label(root, item[1], Vector3(0, 3.65, -1.8), 0.22, item[3])
		var lamp = MeshInstance3D.new()
		var orb = SphereMesh.new()
		orb.radius = 0.16
		orb.height = 0.32
		lamp.mesh = orb
		lamp.position = Vector3(0, 3.15, -1.8)
		lamp.material_override = material(Color("#465559"), 0.08)
		root.add_child(lamp)
		departments[item[0]] = {"node": root, "lamp": lamp, "color": item[3]}
	processing_dais = Node3D.new()
	processing_dais.name = "ClassificationDais"
	processing_dais.position = Vector3(0, 0, -16.2)
	department_root.add_child(processing_dais)
	var dais = MeshInstance3D.new()
	var cyl = CylinderMesh.new()
	cyl.top_radius = 3.5
	cyl.bottom_radius = 3.8
	cyl.height = 0.42
	cyl.radial_segments = 48
	dais.mesh = cyl
	dais.position.y = 0.2
	dais.material_override = material(Color("#1b3034"), 0.15, 0.2)
	processing_dais.add_child(dais)
	_make_label(processing_dais, "CLASSIFICATION IS A VERDICT BEFORE THE VERDICT", Vector3(0, 1.0, 0), 0.2, color_paper)
	department_root.visible = true

func _build_classification_board():
	classification_root = Node3D.new()
	classification_root.name = "ClassificationBoard"
	classification_root.position = Vector3(0, 0, -18.3)
	classification_root.visible = false
	add_child(classification_root)
	_make_box(classification_root, Vector3(0, 2.4, 0), Vector3(7.8, 4.8, 0.35), material(Color("#0b171b"), 0.0, 0.15), false)
	_make_label(classification_root, "OFFICIAL TAXONOMY", Vector3(0, 4.45, -0.25), 0.23, color_brass)
	var classes = ["FLECK", "HAND", "MIRROR", "TEETH", "SWARM", "WEATHER", "BURROWER"]
	for i in range(classes.size()):
		var y = 3.65 - float(i) * 0.52
		var label = _make_label(classification_root, classes[i], Vector3(-2.7, y, -0.25), 0.13, color_paper)
		label.billboard = BaseMaterial3D.BILLBOARD_DISABLED
		var bar = _make_box(classification_root, Vector3(-0.4, y, -0.22), Vector3(0.15, 0.22, 0.16), material(color_glass, 0.55), false)
		classification_bars[classes[i]] = bar
		classification_labels[classes[i]] = label
	var official = _make_label(classification_root, "BOARD CLASSIFICATION: HAND", Vector3(0, 0.05, -0.25), 0.18, color_brass)
	official.name = "OfficialLabel"
	official.billboard = BaseMaterial3D.BILLBOARD_DISABLED

func _build_board_record():
	board_record = Node3D.new()
	board_record.name = "BoardMandate"
	board_record.position = Vector3(0, 0.45, -21.5)
	board_record.visible = false
	add_child(board_record)
	board_crystal = MeshInstance3D.new()
	var prism = PrismMesh.new()
	prism.size = Vector3(0.9, 1.7, 0.9)
	board_crystal.mesh = prism
	board_crystal.position.y = 0.8
	board_crystal.material_override = material(Color(0.74, 0.45, 0.22, 0.5), 1.2)
	board_record.add_child(board_crystal)
	board_label = _make_label(board_record, "FAST CITY MANDATE · SEALED", Vector3(0, 2.1, 0), 0.2, color_brass)
	board_label.visible = false

func _build_staff():
	var data = [
		{"id":"hatchkeeper","name":"Iri Vale","role":"HATCHKEEPER","department":"hatchery","position":Vector3(-14,0,-19.6),"color":Color("#67c8b8"),"report_title":"FIRST ACTION: WORK","report_body":"It clears a path before it looks for food. Its body is useful.","votes":{"HAND":3},"integrity":1,"trust":1},
		{"id":"appetite_reader","name":"Sena Or","role":"APPETITE READER","department":"behaviour","position":Vector3(14,0,-19.6),"color":color_rust,"report_title":"APPETITE: UNCOUNTED TIME","report_body":"The queue disappears only because someone else is made to wait invisibly.","votes":{"TEETH":3,"HAND":1},"integrity":5,"trust":2},
		{"id":"counterfactual_vet","name":"Dr. Morrow","role":"COUNTERFACTUAL VET","department":"counterfactual","position":Vector3(-17,0,-12.6),"color":Color("#7896c8"),"report_title":"AT SCALE: WEATHER","report_body":"One specimen shortens a line. A million specimens make delay into climate.","votes":{"WEATHER":3,"SWARM":1},"integrity":4,"leakage":1},
		{"id":"children_jury","name":"The Four Small Chairs","role":"CHILDREN'S JURY","department":"jury","position":Vector3(17,0,-12.6),"color":Color("#d6c56f"),"report_title":"REFUSAL TEST: FAILED","report_body":"The people feeding it cannot leave the line. That is not consent.","votes":{"TEETH":2,"MIRROR":1},"integrity":6,"trust":5},
		{"id":"weather_warden","name":"Olan Ash","role":"WEATHER WARDEN","department":"weather","position":Vector3(-23,0,-5.6),"color":Color("#9078a3"),"report_title":"SPREAD: BACKGROUND ASSUMPTION","report_body":"The slogan has already entered the weather: faster is being mistaken for fairer.","votes":{"WEATHER":2,"SWARM":2},"integrity":3,"leakage":-2},
		{"id":"release_shepherd","name":"Mara Coil","role":"RELEASE SHEPHERD","department":"working","position":Vector3(23,0,-5.6),"color":color_moss,"report_title":"UTILITY: HIGH UNDER HARNESS","report_body":"It can work if the exit, keeper and boundary remain visible in public.","votes":{"HAND":2},"integrity":-2,"trust":1},
		{"id":"molt_surgeon","name":"Tov Glass","role":"MOLT SURGEON","department":"molt","position":Vector3(-8.5,0,-23.1),"color":color_glass,"report_title":"MOLT: POSSIBLE","report_body":"Remove its hunger for scale and it becomes a smaller hospital animal.","votes":{"HAND":1,"FLECK":1},"integrity":2,"trust":2},
		{"id":"mercy_butcher","name":"Eda White","role":"MERCY BUTCHER","department":"white","position":Vector3(8.5,0,-23.1),"color":Color("#ddd9cf"),"report_title":"CORE: SEPARABLE","report_body":"The harmful appetite can be cut away. Destruction is not the only clean act.","votes":{"TEETH":1},"integrity":4,"leakage":-1}
	]
	for item in data:
		var member = StaffMember.new()
		member.configure(item)
		member.visible = false
		add_child(member)
		staff_members.append(member)

func _build_ecology():
	var data = [
		{"kind":"FLECK","name":"Hospital Lantern","position":Vector3(-26,0.2,-4),"note":"It cannot scale. It keeps one frightened room from becoming unbearable."},
		{"kind":"HAND","name":"Copper Hound","position":Vector3(-18,0,3),"note":"It pulls what the city names and stops when the keeper stops."},
		{"kind":"MIRROR","name":"Glass Stag","position":Vector3(20,0,3),"note":"It changes its posture to match whoever is watching."},
		{"kind":"TEETH","name":"Crown Jackal","position":Vector3(20,0,-25),"note":"It becomes useful by deciding who may be prey."},
		{"kind":"SWARM","name":"Choir Flies","position":Vector3(-22,0,14),"note":"Each body is trivial. Together they make repetition feel like evidence."},
		{"kind":"WEATHER","name":"Consensus Rain","position":Vector3(23,1.3,14),"note":"It no longer argues. It changes what the city considers normal."},
		{"kind":"BURROWER","name":"Ledger Mole","position":Vector3(25,0,22),"note":"It numbers the valves nobody notices until the city floods."}
	]
	for item in data:
		var organism = AmbientOrganism.new()
		organism.configure(item)
		add_child(organism)
		ecology.append(organism)

func _build_verdict_garden():
	verdict_root = Node3D.new()
	verdict_root.name = "VerdictGarden"
	verdict_root.position = Vector3(0, 0, -10.2)
	verdict_root.visible = false
	add_child(verdict_root)
	var data = [
		["OPEN GATE", "release", Color("#7f9f79")],
		["BRASS HARNESS", "restricted", color_brass],
		["MOLT POOL", "molt", Color("#6bb0ad")],
		["QUIET SANCTUARY", "sanctuary", Color("#8e7ba0")],
		["WHITE ROOM", "destroy", color_rust]
	]
	for i in range(data.size()):
		var x = (float(i) - 2.0) * 4.2
		var gate = Node3D.new()
		gate.position = Vector3(x, 0, 0)
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

func reveal_departments(active: bool):
	classification_root.visible = active
	board_record.visible = active
	for key in departments.keys():
		var item = departments[key]
		var lamp = item["lamp"] as MeshInstance3D
		lamp.material_override = material(item["color"] if active else Color("#465559"), 0.9 if active else 0.08)
	for member in staff_members:
		member.set_available(active)

func reveal_verdict_garden():
	verdict_root.visible = true

func reveal_board_trace(active: bool):
	if board_record == null or board_exposed:
		return
	board_crystal.visible = active
	board_label.visible = active

func expose_board_record():
	board_exposed = true
	board_crystal.material_override = material(Color("#a6574e"), 1.8)
	board_label.text = "CONFLICT RECORDED · BOARD FUNDED FAST CITY"
	board_label.visible = true
	var archive = departments.get("archive", null)
	if archive != null:
		var lamp = archive["lamp"] as MeshInstance3D
		lamp.material_override = material(color_rust, 1.4)

func place_seal(anchor_index: int, seal_name: String, seal_color: Color):
	var anchor = anchors[anchor_index]
	anchor["seal"] = seal_name
	if anchor["label"] != null:
		anchor["label"].queue_free()
	anchor["label"] = _make_label(anchor["node"], seal_name.to_upper(), Vector3(0, 0.7, 0), 0.22, seal_color)
	var disc = anchor["node"].get_child(0) as MeshInstance3D
	disc.material_override = material(seal_color, 1.15, 0.25)

func consult_staff(index: int):
	if index < 0 or index >= staff_members.size():
		return null
	var member = staff_members[index]
	if member.visited or not member.available:
		return null
	member.mark_visited()
	_add_staff_stamp(member)
	return {
		"id": member.staff_id,
		"name": member.display_name,
		"role": member.role,
		"title": member.report_title,
		"body": member.report_body,
		"votes": member.class_votes,
		"integrity": member.integrity_delta,
		"trust": member.trust_delta,
		"leakage": member.leakage_delta
	}

func _add_staff_stamp(member):
	var index = staff_stamps.size()
	var stamp = _make_label(classification_root, member.role, Vector3(-2.8 + float(index % 4) * 1.9, -0.45 - float(index / 4) * 0.42, -0.25), 0.11, color_brass)
	stamp.billboard = BaseMaterial3D.BILLBOARD_DISABLED
	staff_stamps.append(stamp)

func close_staff_round():
	for member in staff_members:
		if not member.visited:
			member.close_shift()

func update_classification_board(scores: Dictionary, official_class: String, corruption_exposed: bool):
	if classification_root == null:
		return
	var highest = 1.0
	for value in scores.values():
		highest = max(highest, float(value))
	for class_key in classification_bars.keys():
		var holder = classification_bars[class_key] as Node3D
		var score = float(scores.get(class_key, 0.0))
		holder.scale.x = max(0.06, score / highest * 22.0)
		holder.position.x = -0.4 + holder.scale.x * 0.075
		var mesh = holder.get_child(0) as MeshInstance3D
		mesh.material_override = material(color_rust if class_key == official_class and not corruption_exposed else color_glass, 0.7)
	var official = classification_root.get_node_or_null("OfficialLabel") as Label3D
	if official != null:
		official.text = ("CONFLICT DISCLOSED · OBSERVED: " if corruption_exposed else "BOARD CLASSIFICATION: ") + official_class
		official.modulate = color_rust if not corruption_exposed else color_moss

func set_player(player: Node3D):
	for organism in ecology:
		organism.set_player(player)

func set_city_state(leakage: float, stability: float):
	for organism in ecology:
		organism.set_city_state(leakage, stability)

func observe_ecology(index: int):
	if index < 0 or index >= ecology.size():
		return null
	var organism = ecology[index]
	if organism.observed:
		return null
	organism.mark_observed()
	return {"kind": organism.kind, "name": organism.display_name, "note": organism.note}

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

func nearest_staff(position: Vector3, max_distance := 2.5):
	var best = null
	var distance = max_distance
	for i in range(staff_members.size()):
		var member = staff_members[i]
		if member.visited or not member.available:
			continue
		var d = position.distance_to(member.global_position)
		if d < distance:
			distance = d
			best = i
	return best

func nearest_ecology(position: Vector3, max_distance := 2.8):
	var best = null
	var distance = max_distance
	for i in range(ecology.size()):
		var organism = ecology[i]
		if organism.observed or not organism.visible:
			continue
		var d = position.distance_to(organism.global_position)
		if d < distance:
			distance = d
			best = i
	return best

func board_record_near(position: Vector3, max_distance := 3.0) -> bool:
	return board_record != null and board_record.visible and not board_exposed and position.distance_to(board_record.global_position) < max_distance

func processing_dais_near(position: Vector3, max_distance := 3.8) -> bool:
	return processing_dais != null and position.distance_to(processing_dais.global_position) < max_distance

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
