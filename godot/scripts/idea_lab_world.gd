extends Node3D

const V = preload("res://scripts/idea_lab_visuals.gd")

var stations: Array = []
var decision_gates: Array = []
var decision_root: Node3D
var spawn_point = Vector3(0, 0.2, 12)
var hatch_point = Vector3(0, 0.6, 7.5)

var ink = Color("#071015")
var paper = Color("#d8d1c2")
var brass = Color("#d2a45f")
var glass = Color("#56b7ad")
var rust = Color("#aa5549")

func _ready():
	_build_environment()
	_build_floor()
	_build_paths()
	_add_arch(Vector3(0, 0, 13), "WHISPER GATE", brass)
	_add_arch(Vector3(0, 0, 7.5), "HATCHERY", glass)
	_add_station("desire", "DESIRE YARD", Vector3(-13, 0, 3), glass, "MARA ROOK", "HATCHKEEPER", true)
	_add_station("commitment", "COMMITMENT PADDOCK", Vector3(13, 0, 3), brass, "TOMA REED", "RELEASE SHEPHERD", true)
	_add_station("burden", "BURROWER TUNNEL", Vector3(-13, 0, -9), Color("#879a6a"), "SEFU ANIK", "APPETITE READER", true)
	_add_station("refusal", "REFUSAL GATE", Vector3(13, 0, -9), rust, "NARA VOSS", "MERCY BUTCHER", true)
	_add_station("molt", "MOLT HOUSE", Vector3(-7, 0, -19), Color("#8e7bb8"), "ELIAN THREAD", "MOLT SURGEON", false)
	_add_station("board", "SEALED BOARD WING", Vector3(7, 0, -19), rust, "SEN OSEI", "COUNTERFACTUAL VET", false)
	_add_decision_garden(Vector3(0, 0, -29))

func _build_environment():
	var world_env = WorldEnvironment.new()
	var env = Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("#07141a")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("#89a5a1")
	env.ambient_light_energy = 0.72
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	world_env.environment = env
	add_child(world_env)

	var light = DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-58, -28, 0)
	light.light_color = Color("#f0d9b5")
	light.light_energy = 1.15
	add_child(light)

func _build_floor():
	var body = StaticBody3D.new()
	var visual = MeshInstance3D.new()
	var plane = PlaneMesh.new()
	plane.size = Vector2(72, 72)
	visual.mesh = plane
	visual.material_override = V.material(Color("#10252a"))
	body.add_child(visual)

	var collision = CollisionShape3D.new()
	var shape = BoxShape3D.new()
	shape.size = Vector3(72, 0.2, 72)
	collision.shape = shape
	collision.position.y = -0.12
	body.add_child(collision)
	add_child(body)

func _build_paths():
	var paths = [
		[Vector3(0, 0.04, 12), Vector3(0, 0.04, 7.5)],
		[Vector3(0, 0.04, 7.5), Vector3(-13, 0.04, 3)],
		[Vector3(0, 0.04, 7.5), Vector3(13, 0.04, 3)],
		[Vector3(-13, 0.04, 3), Vector3(-13, 0.04, -9)],
		[Vector3(13, 0.04, 3), Vector3(13, 0.04, -9)],
		[Vector3(-13, 0.04, -9), Vector3(-7, 0.04, -19)],
		[Vector3(13, 0.04, -9), Vector3(7, 0.04, -19)],
		[Vector3(-7, 0.04, -19), Vector3(0, 0.04, -29)],
		[Vector3(7, 0.04, -19), Vector3(0, 0.04, -29)]
	]
	var unique_points: Dictionary = {}
	for pair in paths:
		_add_path_segment(pair[0], pair[1])
		unique_points[str(pair[0])] = pair[0]
		unique_points[str(pair[1])] = pair[1]
	for point in unique_points.values():
		var disc = MeshInstance3D.new()
		var mesh = CylinderMesh.new()
		mesh.top_radius = 1.4
		mesh.bottom_radius = 1.4
		mesh.height = 0.05
		mesh.radial_segments = 24
		disc.mesh = mesh
		disc.position = point
		disc.material_override = V.material(Color("#2f6967"), 0.18, 0.1)
		add_child(disc)

func _add_path_segment(a: Vector3, b: Vector3):
	var distance = a.distance_to(b)
	var path = V.box(Vector3(1.1, 0.04, distance), Color("#234d4e"), 0.08)
	path.position = (a + b) * 0.5
	path.rotation.y = atan2(b.x - a.x, b.z - a.z)
	add_child(path)

func _add_arch(position: Vector3, title: String, color: Color):
	var node = V.arch(title, color, paper)
	node.position = position
	add_child(node)

func _add_station(id: String, title: String, position: Vector3, color: Color, staff_name: String, role: String, available: bool):
	var built = V.station(title, color, staff_name, role, paper)
	var root: Node3D = built["root"]
	root.name = id.capitalize()
	root.position = position
	root.visible = available
	add_child(root)
	stations.append({
		"id": id,
		"title": title,
		"root": root,
		"position": position,
		"marker": built["marker"],
		"available": available,
		"completed": false,
		"initial_available": available,
		"color": color
	})

func _add_decision_garden(position: Vector3):
	decision_root = Node3D.new()
	decision_root.position = position
	decision_root.visible = false
	add_child(decision_root)

	var title = V.label("THE DECISION GARDEN", 40, paper)
	title.position = Vector3(0, 4.2, 0)
	decision_root.add_child(title)

	var ids = ["BUILD", "MOLT", "HIBERNATE", "SANCTUARY", "BREAK"]
	var colors = [glass, Color("#8e7bb8"), Color("#7896c8"), Color("#91a875"), rust]
	for index in range(ids.size()):
		var angle = -1.2 + float(index) * 0.6
		var gate = V.box(Vector3(2.6, 3.6, 0.5), colors[index], 0.25)
		gate.position = Vector3(sin(angle) * 9.0, 1.8, cos(angle) * 4.0)
		decision_root.add_child(gate)

		var tag = V.label(ids[index], 26, ink)
		tag.position = gate.position + Vector3(0, 0.2, -0.3)
		decision_root.add_child(tag)

		decision_gates.append({
			"id": ids[index],
			"position": position + Vector3(gate.position.x, 0, gate.position.z)
		})

	stations.append({
		"id": "decision",
		"title": "DECISION GARDEN",
		"root": decision_root,
		"position": position,
		"available": false,
		"completed": false,
		"initial_available": false
	})

func nearest_station(player_position: Vector3, max_distance := 3.8) -> Dictionary:
	var found: Dictionary = {}
	var best = max_distance
	for station in stations:
		if station["id"] == "decision":
			continue
		if not bool(station.get("available", false)) or bool(station.get("completed", false)):
			continue
		var distance = player_position.distance_to(station["position"])
		if distance < best:
			best = distance
			found = station
	return found

func nearest_decision(player_position: Vector3, max_distance := 2.7) -> String:
	if decision_root == null or not decision_root.visible:
		return ""
	for gate in decision_gates:
		if player_position.distance_to(gate["position"]) < max_distance:
			return String(gate["id"])
	return ""

func set_station_complete(id: String):
	for station in stations:
		if station["id"] != id:
			continue
		station["completed"] = true
		station["available"] = false
		if station.has("marker"):
			station["marker"].material_override = V.material(Color("#4c6b61"), 0.08)
		return

func set_station_available(id: String, available: bool):
	for station in stations:
		if station["id"] != id:
			continue
		station["available"] = available
		station["root"].visible = available or bool(station.get("completed", false))
		return

func reveal_board_record():
	set_station_available("board", true)

func enable_molt():
	set_station_available("molt", true)

func enable_decision():
	set_station_available("decision", true)
	if decision_root != null:
		decision_root.visible = true

func reset_case():
	for station in stations:
		station["completed"] = false
		station["available"] = bool(station.get("initial_available", false))
		station["root"].visible = bool(station["available"])
		if station.has("marker"):
			station["marker"].material_override = V.material(station["color"], 0.65, 0.25)
	if decision_root != null:
		decision_root.visible = false
