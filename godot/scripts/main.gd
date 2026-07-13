extends Node3D

const CivicWorld = preload("res://scripts/civic_world.gd")
const KeeperPlayer = preload("res://scripts/player.gd")
const QueueEater = preload("res://scripts/creature.gd")
const GameHUD = preload("res://scripts/hud.gd")

enum Phase { TRACE, SEAL, TETHER, RETURN, VERDICT, END }

var world
var player
var creature
var hud
var phase = Phase.TRACE
var scan_progress = 0.0
var scanning_clue = -1
var seal_index = 0
var seals = ["EXIT", "KEEPER", "BOUNDARY", "AMPLIFY"]
var seal_colors = [Color("#67c8b8"), Color("#d2a45f"), Color("#7896c8"), Color("#a6574e")]
var stability = 72.0
var trust = 61.0
var leakage = 8.0
var agitation = 34.0
var found_lantern = false
var found_mole = false
var ending_chosen = false

func _ready():
	_ensure_input_actions()
	Engine.max_fps = 60
	if OS.has_feature("mobile"):
		get_viewport().scaling_3d_scale = 0.72
	world = CivicWorld.new()
	add_child(world)
	creature = QueueEater.new()
	creature.position = world.creature_spawn
	creature.agitation_changed.connect(_on_agitation_changed)
	add_child(creature)
	player = KeeperPlayer.new()
	player.position = world.zoo_spawn
	add_child(player)
	hud = GameHUD.new()
	add_child(hud)
	player.interact_requested.connect(_on_interact)
	player.cycle_seal_requested.connect(_cycle_seal)
	player.lens_changed.connect(_on_lens_changed)
	hud.interact_pressed.connect(_on_interact)
	hud.cycle_pressed.connect(_cycle_seal)
	hud.lens_changed.connect(player.set_lens)
	hud.joystick_changed.connect(player.set_mobile_vector)
	hud.set_seal(seals[seal_index], false)
	hud.set_metrics(stability, trust, leakage, agitation)
	hud.set_objective("FIELD ORDER 24-A", "Follow the missing minutes into Open Market. Hold the Resonance Lens near anything the city is pretending not to see.")
	hud.show_message("KEEPER'S OATH", "Glassmarket does not need another hero with a weapon. It needs someone who can tell the difference between a creature, a tool and an appetite.\n\nTonight's report: the city has begun moving faster, but the people who maintain it are losing hours. Find where those hours went.")

func _ensure_input_actions():
	var bindings = {"move_forward": [KEY_W, KEY_UP], "move_back": [KEY_S, KEY_DOWN], "move_left": [KEY_A, KEY_LEFT], "move_right": [KEY_D, KEY_RIGHT], "interact": [KEY_E, KEY_ENTER], "lens": [KEY_SPACE], "cycle_seal": [KEY_Q, KEY_TAB]}
	for action in bindings.keys():
		if not InputMap.has_action(action):
			InputMap.add_action(action, 0.2)
		for keycode in bindings[action]:
			var event = InputEventKey.new()
			event.physical_keycode = keycode
			InputMap.action_add_event(action, event)

func _process(delta):
	if world == null or player == null:
		return
	player.on_threadway = world.is_on_threadway(player.global_position)
	world.reveal_clues(player.lens_active and phase == Phase.TRACE)
	_handle_scanning(delta)
	_update_context_prompt()
	if phase == Phase.RETURN and creature.tethered:
		if player.global_position.distance_to(world.zoo_return) < 4.0:
			_arrive_at_zoo()
	_update_metrics(delta)

func _handle_scanning(delta):
	if phase != Phase.TRACE or not player.lens_active:
		scan_progress = 0.0
		scanning_clue = -1
		hud.set_scan(0.0, false)
		return
	var nearby = world.nearest_clue(player.global_position, 4.0)
	if nearby == null:
		scan_progress = max(0.0, scan_progress - delta * 1.8)
		scanning_clue = -1
		hud.set_scan(scan_progress, scan_progress > 0.01)
		return
	if scanning_clue != nearby:
		scanning_clue = nearby
		scan_progress = 0.0
	scan_progress += delta / 1.35
	hud.set_scan(scan_progress, true)
	if scan_progress >= 1.0:
		_complete_clue(nearby)
		scan_progress = 0.0
		scanning_clue = -1
		hud.set_scan(0.0, false)

func _complete_clue(index: int):
	var clue = world.clues[index]
	clue["scanned"] = true
	clue["label"].visible = true
	clue["crystal"].visible = true
	trust = min(100.0, trust + 3.0)
	hud.show_message(clue["title"], clue["text"])
	var count = _scanned_count()
	if count == 1:
		hud.set_objective("THE CITY IS BORROWING", "Two traces remain. Look for clocks and people who do not appear in the success story.")
	elif count == 2:
		hud.set_objective("AN APPETITE WITHOUT A NAME", "One trace remains. Search where temporary workers and maintenance routes meet.")
	elif count >= 3:
		_begin_manifestation()

func _scanned_count() -> int:
	var count = 0
	for clue in world.clues:
		if clue["scanned"]:
			count += 1
	return count

func _begin_manifestation():
	phase = Phase.SEAL
	creature.activate()
	world.reveal_anchors(true)
	hud.set_seal(seals[seal_index], true)
	hud.set_objective("THE QUEUE-EATER HAS A BODY", "Place three civic seals around it. The clues point to the rules it can survive without becoming a predator.")
	hud.show_message("MANIFESTATION: QUEUE-EATER", "It did not eliminate waiting. It moved waiting into people the city had stopped counting.\n\nChoose rules, not attacks: EXIT lets affected citizens refuse. KEEPER names who maintains the system. BOUNDARY limits its territory. AMPLIFY makes it faster and hungrier.")

func _on_interact():
	if phase == Phase.END:
		return
	var optional = world.optional_near(player.global_position)
	if optional != null:
		_discover_optional(optional)
		return
	match phase:
		Phase.SEAL:
			var anchor = world.nearest_anchor(player.global_position)
			if anchor != null:
				_place_seal(anchor)
		Phase.TETHER:
			if player.global_position.distance_to(creature.global_position) < 3.3:
				_begin_tether()
		Phase.RETURN:
			if player.global_position.distance_to(world.zoo_return) < 4.0:
				_arrive_at_zoo()
		Phase.VERDICT:
			var gate = world.nearest_verdict(player.global_position)
			if gate != null:
				_choose_verdict(world.verdict_gates[gate]["id"])

func _place_seal(anchor_index: int):
	var seal_name = seals[seal_index]
	world.place_seal(anchor_index, seal_name, seal_colors[seal_index])
	var chosen = []
	var filled = 0
	for anchor in world.anchors:
		if not anchor["seal"].is_empty():
			filled += 1
			chosen.append(anchor["seal"])
	if filled < 3:
		hud.set_objective("RULES TAKE SHAPE", "%d of 3 seals placed. You can replace any seal by touching its plate again." % filled)
		return
	creature.apply_seals(chosen)
	if creature.correct_seals == 3:
		phase = Phase.TETHER
		hud.set_objective("THE CREATURE CAN NOW REFUSE YOU", "That is a good sign. Approach it and touch to attach the civic tether.")
		hud.show_message("THE APPETITE HAS EDGES", "The Queue-Eater slows. It still wants to work, but it can no longer pretend that maintenance is free or that every citizen consented.")
	else:
		leakage = min(100.0, leakage + 6.0)
		trust = max(0.0, trust - 4.0)
		hud.set_objective("THE RULES FEED IT", "One or more seals reward the appetite instead of containing it. Replace the wrong plate.")
		hud.show_message("WRONG KIND OF ORDER", "The creature becomes more efficient and less governable. Glassmarket applauds the speed before noticing who paid for it.")

func _cycle_seal():
	if phase != Phase.SEAL:
		return
	seal_index = (seal_index + 1) % seals.size()
	hud.set_seal(seals[seal_index], true)
	if OS.has_feature("mobile"):
		Input.vibrate_handheld(22)

func _begin_tether():
	creature.begin_tether(player)
	player.tethering = true
	phase = Phase.RETURN
	hud.set_seal("", false)
	hud.set_objective("A PROCESSION, NOT A PRIZE", "Walk the Queue-Eater back to the Zoo. Stay on the turquoise Threadways; they were built for moving consequences without hiding them.")
	if OS.has_feature("mobile"):
		Input.vibrate_handheld(55)

func _arrive_at_zoo():
	if phase != Phase.RETURN:
		return
	phase = Phase.VERDICT
	player.tethering = false
	creature.settle_at_zoo()
	creature.global_position = Vector3(0, 0.7, -21.5)
	world.reveal_verdict_garden()
	hud.set_objective("THE FIVE GATES", "Walk to a verdict and touch it. The safest choice is not always the kindest. The useful choice is not always the one Glassmarket deserves.")
	hud.show_message("THE VERDICT GARDEN", "OPEN GATE releases it. BRASS HARNESS permits work under visible limits. MOLT POOL changes its nature. QUIET SANCTUARY keeps it alive but inactive. WHITE ROOM dissolves the original—while risking that its story breeds copies.")

func _choose_verdict(verdict: String):
	if ending_chosen:
		return
	ending_chosen = true
	phase = Phase.END
	player.can_move = false
	var title = ""
	var body = ""
	match verdict:
		"restricted":
			title = "THE CITY KEEPS ITS TIME"
			body = "The Queue-Eater enters the Working Paddock under three visible rules. Journeys shorten, but no citizen can be forced to feed it. A maintenance guild receives both authority and blame.\n\nGlassmarket remains imperfect. It becomes legible."
			stability += 18
			trust += 17
		"release":
			title = "THE FASTEST CITY IN THE WORLD"
			body = "The gates open. Glassmarket celebrates a week without queues. By the second week, night workers begin arriving home tomorrow. Nobody can find the delay because the city has made it invisible."
			stability -= 24
			trust -= 21
			leakage += 14
		"molt":
			title = "A SMALLER ANIMAL"
			body = "The Molt Pool removes the creature's hunger for scale. It now clears one hospital queue at a time, then sleeps. Investors lose interest. Patients do not."
			stability += 9
			trust += 15
		"sanctuary":
			title = "SAFE, ALIVE, UNUSED"
			body = "The Queue-Eater remains in the Quiet Sanctuary. Glassmarket returns to waiting, but the porters recover their stolen hours. Citizens argue for years about whether caution was wisdom or fear."
			stability += 4
			trust += 5
		"destroy":
			title = "THE STORY ESCAPES"
			body = "The White Room dissolves the creature. The Zoo publishes a warning explaining exactly how it fed. By morning, six smaller Queue-Eaters hatch from jokes, songs and efficiency manuals. The cage held. The story did not."
			stability -= 12
			leakage += 35
	if found_mole:
		body += "\n\nThe Ledger Mole had already numbered the hidden maintenance routes. When the city's clocks buckled at midnight, engineers knew what to disconnect."
		stability += 9
	if found_lantern:
		body += "\n\nAt the hospital, the little Lantern Moth continues doing something too small for the city metrics: it keeps frightened people company."
		trust += 6
	stability = clamp(stability, 0, 100)
	trust = clamp(trust, 0, 100)
	leakage = clamp(leakage, 0, 100)
	hud.set_metrics(stability, trust, leakage, agitation)
	hud.set_objective("CASE CLOSED · GLASSMARKET REMEMBERS", title)
	hud.show_message(title, body + "\n\nFinal record — Stability %d · Trust %d · Story leakage %d" % [stability, trust, leakage])

func _discover_optional(node: Node3D):
	var kind = String(node.get_meta("optional_kind"))
	if kind == "lantern" and not found_lantern:
		found_lantern = true
		trust = min(100.0, trust + 6.0)
		hud.show_message("HOSPITAL LANTERN", "It cannot scale. It cannot monetize grief. It merely stays beside people during bad news. The Zoo's efficiency auditors marked it nonessential. The patients did not.")
	elif kind == "mole" and not found_mole:
		found_mole = true
		stability = min(100.0, stability + 8.0)
		hud.show_message("LEDGER MOLE", "It has spent years numbering valves, cables and service tunnels. Nobody noticed because its work only becomes visible when something else fails.")
	node.visible = false
	_update_metrics(0.0)

func _on_lens_changed(active: bool):
	world.reveal_clues(active and phase == Phase.TRACE)
	if active and OS.has_feature("mobile"):
		Input.vibrate_handheld(12)

func _on_agitation_changed(value: float):
	agitation = value
	hud.set_metrics(stability, trust, leakage, agitation)

func _update_context_prompt():
	var prompt = ""
	if phase == Phase.TRACE:
		var clue = world.nearest_clue(player.global_position, 4.0)
		if clue != null:
			prompt = "HOLD LENS · TUNE THE TRACE" if not player.lens_active else "KEEP THE LENS STEADY"
	elif phase == Phase.SEAL:
		var anchor = world.nearest_anchor(player.global_position)
		if anchor != null:
			prompt = "TOUCH · PLACE %s" % seals[seal_index]
	elif phase == Phase.TETHER:
		if player.global_position.distance_to(creature.global_position) < 3.3:
			prompt = "TOUCH · ATTACH CIVIC TETHER"
	elif phase == Phase.RETURN:
		if player.global_position.distance_to(world.zoo_return) < 4.0:
			prompt = "TOUCH · ENTER THE VERDICT GARDEN"
	elif phase == Phase.VERDICT:
		var gate = world.nearest_verdict(player.global_position)
		if gate != null:
			prompt = "TOUCH · CHOOSE %s" % world.verdict_gates[gate]["title"]
	if prompt.is_empty():
		var optional = world.optional_near(player.global_position)
		if optional != null:
			prompt = "TOUCH · RECORD UNLICENSED ORGANISM"
	hud.set_prompt(prompt)

func _update_metrics(delta):
	if phase == Phase.SEAL and creature.active and creature.agitation > 55.0:
		stability = max(0.0, stability - delta * 0.35)
		leakage = min(100.0, leakage + delta * 0.22)
	hud.set_metrics(stability, trust, leakage, agitation)
