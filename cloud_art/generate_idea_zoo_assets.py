#!/usr/bin/env python3
"""Generate the full low-poly Idea Zoo character and creature art package in Blender.

The script is designed for headless execution in GitHub Actions. It creates:
- three modular Keeper body frames;
- six named specialists;
- three Children's Jury members;
- five reusable creature-family rigs;
- FBX and GLB runtime exports;
- editable .blend sources;
- PNG review renders;
- a machine-readable manifest.

The generated models intentionally use rigid, bone-parented pieces. That keeps the
silhouette strong, makes the rigs deterministic, and stays inside the mobile budget.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

import bpy
from mathutils import Vector

Color = Tuple[float, float, float, float]

PALETTE: Dict[str, Color] = {
    "paper": (0.76, 0.70, 0.58, 1.0),
    "paper_light": (0.88, 0.84, 0.72, 1.0),
    "brass": (0.58, 0.39, 0.12, 1.0),
    "clay": (0.48, 0.29, 0.20, 1.0),
    "glass": (0.18, 0.48, 0.50, 0.46),
    "ink": (0.045, 0.065, 0.075, 1.0),
    "rust": (0.48, 0.13, 0.10, 1.0),
    "moss": (0.20, 0.31, 0.16, 1.0),
    "teal": (0.12, 0.84, 0.76, 1.0),
    "burgundy": (0.27, 0.09, 0.12, 1.0),
    "violet": (0.18, 0.15, 0.28, 1.0),
    "olive": (0.23, 0.24, 0.16, 1.0),
    "skin_0": (0.22, 0.12, 0.08, 1.0),
    "skin_1": (0.31, 0.17, 0.11, 1.0),
    "skin_2": (0.42, 0.25, 0.17, 1.0),
    "skin_3": (0.56, 0.36, 0.25, 1.0),
    "skin_4": (0.70, 0.49, 0.34, 1.0),
    "skin_5": (0.82, 0.63, 0.47, 1.0),
}

HUMAN_BONES = [
    "Hips", "Spine", "Chest", "Neck", "Head",
    "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
    "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
    "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
    "RightUpperLeg", "RightLowerLeg", "RightFoot",
]


def parse_args() -> argparse.Namespace:
    argv = sys.argv
    argv = argv[argv.index("--") + 1 :] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True, help="Repository-relative output root")
    parser.add_argument("--source-output", required=True, help="Editable .blend source root")
    parser.add_argument("--preview-output", required=True, help="PNG preview root")
    parser.add_argument("--manifest", required=True, help="Manifest JSON path")
    return parser.parse_args(argv)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.cameras, bpy.data.lights):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def material(name: str, color: Color, metallic: float = 0.0, roughness: float = 0.62) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if color[3] < 1.0:
            bsdf.inputs["Alpha"].default_value = color[3]
            mat.blend_method = "BLEND"
            mat.show_transparent_back = True
    return mat


def palette_material(key: str) -> bpy.types.Material:
    metallic = 0.72 if key == "brass" else 0.06
    rough = 0.28 if key in ("brass", "glass") else 0.68
    return material(f"IZ_{key}", PALETTE[key], metallic, rough)


def apply_material(obj: bpy.types.Object, mat_key: str) -> None:
    if obj.type == "MESH":
        obj.data.materials.append(palette_material(mat_key))


def bevel(obj: bpy.types.Object, amount: float = 0.08, segments: int = 2) -> None:
    modifier = obj.modifiers.new("CivicSoftEdge", "BEVEL")
    modifier.width = amount
    modifier.segments = segments
    modifier.limit_method = "ANGLE"


def add_cube(name: str, location: Sequence[float], scale: Sequence[float], mat_key: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bevel(obj, min(scale) * 0.16 if min(scale) > 0.02 else 0.01)
    apply_material(obj, mat_key)
    return obj


def add_sphere(name: str, location: Sequence[float], scale: Sequence[float], mat_key: str, segments: int = 16) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=max(8, segments // 2), location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_material(obj, mat_key)
    return obj


def add_cylinder(name: str, location: Sequence[float], radius: float, depth: float, mat_key: str, vertices: int = 12) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    apply_material(obj, mat_key)
    bevel(obj, min(radius * 0.22, 0.04))
    return obj


def add_cone(name: str, location: Sequence[float], radius1: float, radius2: float, depth: float, mat_key: str, vertices: int = 12) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    apply_material(obj, mat_key)
    bevel(obj, min(radius1 * 0.18, 0.035))
    return obj


def cylinder_between(name: str, a: Sequence[float], b: Sequence[float], radius: float, mat_key: str, vertices: int = 12) -> bpy.types.Object:
    p1, p2 = Vector(a), Vector(b)
    direction = p2 - p1
    obj = add_cylinder(name, (p1 + p2) * 0.5, radius, direction.length, mat_key, vertices)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    return obj


def parent_to_bone(obj: bpy.types.Object, armature: bpy.types.Object, bone_name: str) -> None:
    world = obj.matrix_world.copy()
    obj.parent = armature
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    obj.matrix_world = world


def add_empty(name: str, location: Sequence[float], armature: bpy.types.Object, bone_name: str) -> bpy.types.Object:
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    empty.empty_display_type = "SPHERE"
    empty.empty_display_size = 0.09
    empty.location = location
    parent_to_bone(empty, armature, bone_name)
    return empty


def add_bone(edit_bones, name: str, head: Sequence[float], tail: Sequence[float], parent=None, connected: bool = False):
    bone = edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    if parent:
        bone.parent = parent
        bone.use_connect = connected
    return bone


def humanoid_armature(name: str, height: float = 2.35, width: float = 1.0) -> bpy.types.Object:
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm = bpy.context.object
    arm.name = f"{name}_Rig"
    arm.data.name = f"{name}_Skeleton"
    eb = arm.data.edit_bones
    eb.remove(eb[0])
    scale = height / 2.35
    hip_z = 0.98 * scale
    hips = add_bone(eb, "Hips", (0, 0, hip_z), (0, 0, 1.16 * scale))
    spine = add_bone(eb, "Spine", (0, 0, 1.16 * scale), (0, 0, 1.56 * scale), hips, True)
    chest = add_bone(eb, "Chest", (0, 0, 1.56 * scale), (0, 0, 1.86 * scale), spine, True)
    neck = add_bone(eb, "Neck", (0, 0, 1.86 * scale), (0, 0, 2.00 * scale), chest, True)
    add_bone(eb, "Head", (0, 0, 2.00 * scale), (0, 0, 2.32 * scale), neck, True)
    shoulder_span = 0.48 * width * scale
    for side, sign in (("Left", 1), ("Right", -1)):
        shoulder = add_bone(eb, f"{side}Shoulder", (0, 0, 1.80 * scale), (sign * shoulder_span * 0.34, 0, 1.80 * scale), chest)
        upper = add_bone(eb, f"{side}UpperArm", (sign * shoulder_span * 0.34, 0, 1.80 * scale), (sign * shoulder_span, 0, 1.54 * scale), shoulder, True)
        lower = add_bone(eb, f"{side}LowerArm", (sign * shoulder_span, 0, 1.54 * scale), (sign * shoulder_span * 1.18, 0, 1.20 * scale), upper, True)
        add_bone(eb, f"{side}Hand", (sign * shoulder_span * 1.18, 0, 1.20 * scale), (sign * shoulder_span * 1.25, -0.02, 1.05 * scale), lower, True)
    leg_x = 0.20 * width * scale
    for side, sign in (("Left", 1), ("Right", -1)):
        upper = add_bone(eb, f"{side}UpperLeg", (sign * leg_x, 0, hip_z), (sign * leg_x, 0, 0.58 * scale), hips)
        lower = add_bone(eb, f"{side}LowerLeg", (sign * leg_x, 0, 0.58 * scale), (sign * leg_x, 0, 0.18 * scale), upper, True)
        add_bone(eb, f"{side}Foot", (sign * leg_x, 0, 0.18 * scale), (sign * leg_x, -0.22 * scale, 0.08 * scale), lower, True)
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.show_in_front = True
    arm.data.display_type = "STICK"
    return arm


def build_hair(arm: bpy.types.Object, head_z: float, width: float) -> None:
    styles = [
        ((0.00, 0.00, head_z + 0.20), (0.34 * width, 0.30, 0.16)),
        ((0.00, 0.02, head_z + 0.23), (0.38 * width, 0.31, 0.20)),
        ((0.00, 0.04, head_z + 0.27), (0.40 * width, 0.34, 0.26)),
        ((0.00, 0.05, head_z + 0.31), (0.42 * width, 0.36, 0.30)),
        ((0.00, 0.06, head_z + 0.35), (0.44 * width, 0.38, 0.34)),
    ]
    for index, (loc, scale) in enumerate(styles):
        hair = add_sphere(f"Hair_{index}", loc, scale, "ink", 12)
        parent_to_bone(hair, arm, "Head")


def build_lenses(arm: bpy.types.Object, chest_z: float, width: float) -> None:
    for index in range(3):
        ring = add_cylinder(f"Lens_{index}_Frame", (0.46 * width, -0.28, chest_z), 0.15 + index * 0.025, 0.045, "brass", 16)
        ring.rotation_euler = (math.radians(90), 0, 0)
        parent_to_bone(ring, arm, "Chest")
        glass = add_sphere(f"Lens_{index}_Glass", (0.46 * width, -0.31, chest_z), (0.11 + index * 0.02,) * 3, "glass", 12)
        parent_to_bone(glass, arm, "Chest")


def build_humanoid(name: str, role: str, body_width: float, height: float, coat: str, skin: str) -> bpy.types.Object:
    arm = humanoid_armature(name, height, body_width)
    s = height / 2.35
    head_z = 2.15 * s
    chest_z = 1.55 * s
    torso = add_sphere("FieldCoat" if role == "keeper" else "Coat", (0, 0, 1.30 * s), (0.45 * body_width * s, 0.32 * s, 0.60 * s), coat, 16)
    parent_to_bone(torso, arm, "Spine")
    apron = add_cube("CivicApron", (0, -0.31 * s, 1.30 * s), (0.34 * body_width * s, 0.045 * s, 0.46 * s), "paper")
    parent_to_bone(apron, arm, "Spine")
    head = add_sphere("HeadMesh", (0, 0, head_z), (0.31 * s, 0.29 * s, 0.34 * s), skin, 16)
    parent_to_bone(head, arm, "Head")
    eye = add_sphere("IntentEye", (0.11 * s, -0.275 * s, head_z + 0.02 * s), (0.055 * s,) * 3, "teal", 10)
    parent_to_bone(eye, arm, "Head")
    for side, sign in (("Left", 1), ("Right", -1)):
        upper = cylinder_between(f"{side}Arm", (sign * 0.20 * body_width * s, 0, 1.73 * s), (sign * 0.47 * body_width * s, 0, 1.43 * s), 0.115 * s, coat)
        parent_to_bone(upper, arm, f"{side}UpperArm")
        lower = cylinder_between(f"{side}Forearm", (sign * 0.47 * body_width * s, 0, 1.43 * s), (sign * 0.57 * body_width * s, -0.02 * s, 1.12 * s), 0.10 * s, coat)
        parent_to_bone(lower, arm, f"{side}LowerArm")
        hand = add_sphere(f"{side}HandMesh", (sign * 0.58 * body_width * s, -0.02 * s, 1.04 * s), (0.12 * s, 0.10 * s, 0.14 * s), skin, 12)
        parent_to_bone(hand, arm, f"{side}Hand")
        upper_leg = cylinder_between(f"{side}Thigh", (sign * 0.18 * body_width * s, 0, 0.95 * s), (sign * 0.18 * body_width * s, 0, 0.54 * s), 0.15 * s, "ink")
        parent_to_bone(upper_leg, arm, f"{side}UpperLeg")
        lower_leg = cylinder_between(f"{side}Shin", (sign * 0.18 * body_width * s, 0, 0.54 * s), (sign * 0.18 * body_width * s, 0, 0.17 * s), 0.13 * s, "ink")
        parent_to_bone(lower_leg, arm, f"{side}LowerLeg")
        foot = add_cube(f"{side}FootMesh", (sign * 0.18 * body_width * s, -0.10 * s, 0.08 * s), (0.15 * s, 0.24 * s, 0.09 * s), "ink")
        parent_to_bone(foot, arm, f"{side}Foot")
    build_hair(arm, head_z, body_width)
    if role == "keeper":
        build_lenses(arm, chest_z, body_width)
        for i, key in enumerate(("teal", "rust", "brass", "paper", "moss")):
            plate = add_cube(f"RulingPlate_{i}", (-0.22 * s + i * 0.11 * s, -0.36 * s, 1.48 * s), (0.045 * s, 0.025 * s, 0.12 * s), key)
            parent_to_bone(plate, arm, "Chest")
        thread = cylinder_between("ContainmentThread", (0.42 * body_width * s, -0.31 * s, 1.35 * s), (0.58 * body_width * s, -0.34 * s, 0.94 * s), 0.018 * s, "teal", 8)
        parent_to_bone(thread, arm, "Chest")
    add_empty("HeadSocket", (0, 0, head_z), arm, "Head")
    add_empty("RightHandSocket", (-0.60 * body_width * s, -0.02 * s, 1.02 * s), arm, "RightHand")
    add_empty("LeftHandSocket", (0.60 * body_width * s, -0.02 * s, 1.02 * s), arm, "LeftHand")
    return arm


def specialist_accessories(arm: bpy.types.Object, role: str, height: float, width: float) -> None:
    s = height / 2.35
    right = (-0.62 * width * s, -0.04 * s, 0.98 * s)
    if role == "mara":
        cape = add_cube("RookCape", (0, 0.26 * s, 1.48 * s), (0.58 * width * s, 0.06 * s, 0.72 * s), "paper")
        parent_to_bone(cape, arm, "Chest")
        stem = add_cylinder("HatchFork_Stem", right, 0.045 * s, 0.95 * s, "brass", 10)
        stem.rotation_euler = (0, 0, math.radians(-8))
        parent_to_bone(stem, arm, "RightHand")
        fork = add_cube("HatchFork_Tines", (right[0], right[1], right[2] - 0.48 * s), (0.24 * s, 0.04 * s, 0.05 * s), "teal")
        parent_to_bone(fork, arm, "RightHand")
    elif role == "toma":
        sash = add_cube("RecallSash", (0.12 * s, -0.35 * s, 1.42 * s), (0.11 * s, 0.03 * s, 0.72 * s), "teal")
        sash.rotation_euler.y = math.radians(-15)
        parent_to_bone(sash, arm, "Chest")
        staff = add_cylinder("ReleaseStaff", right, 0.05 * s, 1.35 * s, "brass", 10)
        parent_to_bone(staff, arm, "RightHand")
        light = add_sphere("RecallLight", (right[0], right[1], right[2] - 0.72 * s), (0.13 * s,) * 3, "teal", 12)
        parent_to_bone(light, arm, "RightHand")
    elif role == "sefu":
        for i in range(4):
            vial = add_sphere(f"AppetiteVial_{i}", ((i - 1.5) * 0.14 * s, -0.38 * s, 1.26 * s), (0.07 * s, 0.055 * s, 0.11 * s), "teal" if i % 2 else "rust", 10)
            parent_to_bone(vial, arm, "Spine")
        lens = add_cylinder("AppetiteLens_Frame", right, 0.18 * s, 0.04 * s, "brass", 16)
        lens.rotation_euler = (math.radians(90), 0, 0)
        parent_to_bone(lens, arm, "RightHand")
    elif role == "elian":
        spool = add_cylinder("MoltSpool", right, 0.22 * s, 0.16 * s, "brass", 14)
        spool.rotation_euler = (math.radians(90), 0, 0)
        parent_to_bone(spool, arm, "RightHand")
        for sign in (-1, 1):
            rail = cylinder_between(f"SurgicalFrame_{sign}", (sign * 0.52 * width * s, 0.28 * s, 0.54 * s), (sign * 0.52 * width * s, 0.28 * s, 1.62 * s), 0.025 * s, "paper", 8)
            parent_to_bone(rail, arm, "Spine")
    elif role == "sen":
        coat = add_cube("ForecastCoat", (0, 0.30 * s, 1.40 * s), (0.62 * width * s, 0.07 * s, 0.78 * s), "ink")
        parent_to_bone(coat, arm, "Chest")
        for i in range(3):
            frame = add_cube(f"CounterfactualFrame_{i}", (right[0] + (i - 1) * 0.10 * s, right[1], right[2] - i * 0.10 * s), (0.09 * s, 0.025 * s, 0.14 * s), "teal" if i == 1 else "paper")
            parent_to_bone(frame, arm, "RightHand")
    elif role == "nara":
        plate = add_cube("WhiteRoomPlate", (0, -0.37 * s, 1.34 * s), (0.35 * width * s, 0.035 * s, 0.43 * s), "paper_light")
        parent_to_bone(plate, arm, "Chest")
        handle = add_cylinder("MercyBell_Handle", right, 0.04 * s, 0.36 * s, "brass", 10)
        parent_to_bone(handle, arm, "RightHand")
        bell = add_cone("MercyBell", (right[0], right[1], right[2] - 0.26 * s), 0.18 * s, 0.08 * s, 0.24 * s, "rust", 16)
        parent_to_bone(bell, arm, "RightHand")


def jury_accessories(arm: bpy.types.Object, height: float, index: int) -> None:
    s = height / 2.35
    plate = add_cube("QuestionPlate", (0, -0.29 * s, 1.22 * s), (0.31 * s, 0.035 * s, 0.28 * s), ("rust", "teal", "paper")[index])
    parent_to_bone(plate, arm, "Spine")


def generic_armature(name: str, bones) -> bpy.types.Object:
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm = bpy.context.object
    arm.name = f"{name}_Rig"
    arm.data.name = f"{name}_Skeleton"
    eb = arm.data.edit_bones
    eb.remove(eb[0])
    created = {}
    for bone_name, head, tail, parent_name in bones:
        created[bone_name] = add_bone(eb, bone_name, head, tail, created.get(parent_name))
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.show_in_front = True
    return arm


def creature_sockets(arm: bpy.types.Object, head_loc: Sequence[float], body_bone: str = "Body", head_bone: str = "Head") -> None:
    add_empty("HeadSocket", head_loc, arm, head_bone)
    add_empty("AppetiteSocket", (0, -0.46, 1.02), arm, body_bone)
    add_empty("BurdenSocket", (0, 0.42, 1.32), arm, body_bone)
    add_empty("GuardrailRoot", (0, 0, 0.74), arm, body_bone)
    add_empty("TailSocket", (0, 0.62, 0.72), arm, body_bone)
    add_empty("EffectRoot", (0, 0, 1.10), arm, body_bone)


def build_avian() -> bpy.types.Object:
    bones = [
        ("Root", (0, 0, 0), (0, 0, 0.2), None), ("Body", (0, 0, 0.2), (0, 0, 1.25), "Root"),
        ("Head", (0, -0.18, 1.25), (0, -0.40, 1.72), "Body"),
        ("LeftWing", (0, 0, 1.10), (0.95, 0, 1.10), "Body"), ("RightWing", (0, 0, 1.10), (-0.95, 0, 1.10), "Body"),
        ("LeftLeg", (0.22, 0, 0.52), (0.26, -0.02, 0.08), "Body"), ("RightLeg", (-0.22, 0, 0.52), (-0.26, -0.02, 0.08), "Body"),
        ("Tail", (0, 0.28, 0.74), (0, 0.92, 0.52), "Body"),
    ]
    arm = generic_armature("Creature_Avian", bones)
    body = add_sphere("AvianBody", (0, 0, 0.92), (0.62, 0.47, 0.82), "clay", 16); parent_to_bone(body, arm, "Body")
    head = add_sphere("Head", (0, -0.34, 1.56), (0.38, 0.34, 0.34), "paper", 14); parent_to_bone(head, arm, "Head")
    for side, sign in (("Left", 1), ("Right", -1)):
        wing = add_cube(f"{side}Wing", (sign * 0.67, 0.02, 1.10), (0.58, 0.09, 0.38), "paper"); wing.rotation_euler.y = math.radians(sign * 8); parent_to_bone(wing, arm, f"{side}Wing")
        leg = cylinder_between(f"{side}LegMesh", (sign * 0.22, 0, 0.54), (sign * 0.26, 0, 0.10), 0.08, "brass", 10); parent_to_bone(leg, arm, f"{side}Leg")
    tail = add_cone("MemoryTail", (0, 0.63, 0.62), 0.18, 0.05, 0.78, "teal", 10); tail.rotation_euler.x = math.radians(72); parent_to_bone(tail, arm, "Tail")
    creature_sockets(arm, (0, -0.34, 1.56))
    return arm


def build_burden_beast() -> bpy.types.Object:
    bones = [
        ("Root", (0, 0, 0), (0, 0, 0.2), None), ("Body", (0, 0, 0.42), (0, 0, 1.18), "Root"),
        ("Head", (0, -0.52, 0.94), (0, -1.05, 1.08), "Body"),
        ("LeftFrontLeg", (0.48, -0.38, 0.72), (0.48, -0.40, 0.08), "Body"), ("RightFrontLeg", (-0.48, -0.38, 0.72), (-0.48, -0.40, 0.08), "Body"),
        ("LeftBackLeg", (0.48, 0.38, 0.72), (0.48, 0.40, 0.08), "Body"), ("RightBackLeg", (-0.48, 0.38, 0.72), (-0.48, 0.40, 0.08), "Body"),
        ("Tail", (0, 0.56, 0.78), (0, 1.12, 0.62), "Body"),
    ]
    arm = generic_armature("Creature_BurdenBeast", bones)
    body = add_sphere("BurdenBody", (0, 0, 0.82), (0.78, 1.02, 0.62), "clay", 16); parent_to_bone(body, arm, "Body")
    load = add_cube("LoadBack", (0, 0.16, 1.38), (0.70, 0.62, 0.18), "brass"); parent_to_bone(load, arm, "Body")
    head = add_sphere("Head", (0, -0.92, 0.98), (0.46, 0.42, 0.38), "paper", 14); parent_to_bone(head, arm, "Head")
    for name, x, y in (("LeftFront", 0.48, -0.38), ("RightFront", -0.48, -0.38), ("LeftBack", 0.48, 0.38), ("RightBack", -0.48, 0.38)):
        leg = cylinder_between(f"{name}LegMesh", (x, y, 0.70), (x, y, 0.10), 0.12, "ink", 10); parent_to_bone(leg, arm, f"{name}Leg")
    tail = cylinder_between("MemoryTail", (0, 0.58, 0.78), (0, 1.18, 0.58), 0.09, "teal", 10); parent_to_bone(tail, arm, "Tail")
    creature_sockets(arm, (0, -0.92, 0.98))
    return arm


def build_lantern() -> bpy.types.Object:
    bones = [
        ("Root", (0, 0, 0), (0, 0, 0.2), None), ("Body", (0, 0, 0.2), (0, 0, 1.34), "Root"),
        ("Head", (0, -0.10, 1.20), (0, -0.30, 1.62), "Body"),
        ("LeftAppendage", (0.38, 0, 0.96), (0.82, 0, 0.56), "Body"), ("RightAppendage", (-0.38, 0, 0.96), (-0.82, 0, 0.56), "Body"),
        ("Tail", (0, 0.28, 0.78), (0, 0.64, 0.38), "Body"),
    ]
    arm = generic_armature("Creature_Lantern", bones)
    shell = add_sphere("LanternShell", (0, 0, 0.98), (0.78, 0.70, 0.92), "glass", 18); parent_to_bone(shell, arm, "Body")
    core = add_sphere("InnerIdea", (0, 0, 0.98), (0.36, 0.34, 0.42), "teal", 14); parent_to_bone(core, arm, "Body")
    head = add_sphere("Head", (0, -0.32, 1.52), (0.34, 0.30, 0.31), "paper", 14); parent_to_bone(head, arm, "Head")
    for side, sign in (("Left", 1), ("Right", -1)):
        limb = cylinder_between(f"{side}AppendageMesh", (sign * 0.38, 0, 0.96), (sign * 0.80, 0, 0.58), 0.08, "brass", 10); parent_to_bone(limb, arm, f"{side}Appendage")
    for i in range(3):
        ring = add_cylinder(f"CivicRing_{i}", (0, 0, 0.74 + i * 0.22), 0.86 + i * 0.08, 0.035, "brass", 24); parent_to_bone(ring, arm, "Body")
    creature_sockets(arm, (0, -0.32, 1.52))
    return arm


def build_serpentine() -> bpy.types.Object:
    bones = [("Root", (0, 0, 0), (0, 0, 0.2), None)]
    parent = "Root"
    for i in range(7):
        name = f"Spine_{i}"
        z0 = 0.20 + i * 0.22
        bones.append((name, (0, i * 0.18 - 0.54, z0), (0, i * 0.18 - 0.36, z0 + 0.22), parent))
        parent = name
    bones.append(("Head", (0, 0.72, 1.54), (0, 0.92, 1.88), parent))
    arm = generic_armature("Creature_Serpentine", bones)
    for i in range(7):
        seg = add_sphere(f"CoilSegment_{i}", (0, i * 0.18 - 0.54, 0.40 + i * 0.22), (0.50 - i * 0.025,) * 3, "rust" if i % 3 == 0 else "clay", 14)
        parent_to_bone(seg, arm, f"Spine_{i}")
    head = add_sphere("Head", (0, 0.88, 1.84), (0.42, 0.40, 0.36), "paper", 14); parent_to_bone(head, arm, "Head")
    creature_sockets(arm, (0, 0.88, 1.84), "Spine_3", "Head")
    return arm


def build_choir() -> bpy.types.Object:
    bones = [("Root", (0, 0, 0), (0, 0, 0.25), None), ("Body", (0, 0, 0.25), (0, 0, 1.10), "Root")]
    for i in range(5):
        angle = i * math.tau / 5
        x, y = math.cos(angle) * 0.74, math.sin(angle) * 0.74
        bones.append((f"Voice_{i}", (0, 0, 0.90), (x, y, 1.10 + math.sin(angle * 2) * 0.12), "Body"))
    bones.append(("Head", (0, 0, 1.10), (0, -0.18, 1.52), "Body"))
    arm = generic_armature("Creature_Choir", bones)
    for i in range(5):
        angle = i * math.tau / 5
        loc = (math.cos(angle) * 0.74, math.sin(angle) * 0.74, 1.10 + math.sin(angle * 2) * 0.12)
        voice = add_sphere(f"ChoirBody_{i}", loc, (0.28, 0.26, 0.34), "paper" if i % 2 == 0 else "glass", 12)
        parent_to_bone(voice, arm, f"Voice_{i}")
        eye = add_sphere(f"IntentEye_{i}", (loc[0], loc[1] - 0.24, loc[2] + 0.02), (0.055,) * 3, "teal", 8)
        parent_to_bone(eye, arm, f"Voice_{i}")
    head = add_sphere("Head", (0, -0.18, 1.46), (0.32, 0.28, 0.30), "paper", 12); parent_to_bone(head, arm, "Head")
    creature_sockets(arm, (0, -0.18, 1.46))
    return arm


def add_guardrail_and_burden_demo(arm: bpy.types.Object) -> None:
    body_bone = "Body" if arm.pose.bones.get("Body") else "Spine_3"
    for i in range(2):
        ring = add_cylinder(f"GuardrailRing_{i}", (0, 0, 0.72 + i * 0.22), 0.92 + i * 0.10, 0.028, "teal", 24)
        ring.rotation_euler.x = math.radians(90)
        parent_to_bone(ring, arm, body_bone)
    for i in range(2):
        crate = add_cube(f"BurdenCrate_{i}", ((i - 0.5) * 0.28, 0.38, 1.30 + i * 0.10), (0.18, 0.18, 0.18), "rust" if i == 0 else "brass")
        parent_to_bone(crate, arm, body_bone)


def add_preview_stage() -> None:
    add_cylinder("PreviewPlinth", (0, 0, -0.08), 1.35, 0.16, "ink", 48)
    bpy.ops.object.light_add(type="AREA", location=(4.0, -4.0, 6.0))
    key = bpy.context.object
    key.name = "PreviewKey"
    key.data.energy = 850
    key.data.shape = "DISK"
    key.data.size = 4.0
    bpy.ops.object.light_add(type="AREA", location=(-3.0, 1.0, 3.0))
    fill = bpy.context.object
    fill.name = "PreviewFill"
    fill.data.energy = 500
    fill.data.size = 3.0
    bpy.ops.object.camera_add(location=(4.6, -7.2, 3.2))
    cam = bpy.context.object
    cam.name = "PreviewCamera"
    target = Vector((0, 0, 1.10))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam


def set_render(preview_path: Path) -> None:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(preview_path)
    scene.world.color = (0.025, 0.07, 0.085)
    bpy.ops.render.render(write_still=True)


def mesh_stats() -> Dict[str, int]:
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and not obj.name.startswith("Preview")]
    return {
        "mesh_objects": len(meshes),
        "vertices": sum(len(obj.data.vertices) for obj in meshes),
        "triangles": sum(sum(len(poly.vertices) - 2 for poly in obj.data.polygons) for obj in meshes),
    }


def manifest_path(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(Path.cwd().resolve())).replace("\\", "/")
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def export_asset(asset_name: str, category: str, runtime_root: Path, source_root: Path, preview_root: Path) -> Dict[str, object]:
    category_dir = runtime_root / category
    source_dir = source_root / category
    preview_dir = preview_root / category
    category_dir.mkdir(parents=True, exist_ok=True)
    source_dir.mkdir(parents=True, exist_ok=True)
    preview_dir.mkdir(parents=True, exist_ok=True)

    runtime_objects = list(bpy.context.scene.objects)
    for obj in runtime_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next((o for o in runtime_objects if o.type == "ARMATURE"), runtime_objects[0])

    fbx_path = category_dir / f"{asset_name}.fbx"
    glb_path = source_dir / f"{asset_name}.glb"
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path), use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False,
        bake_anim=False, mesh_smooth_type="FACE", use_mesh_modifiers=True,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path), export_format="GLB", use_selection=True,
        export_animations=False, export_apply=True,
    )
    stats = mesh_stats()
    blend_path = source_dir / f"{asset_name}.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)
    add_preview_stage()
    preview_path = preview_dir / f"{asset_name}.png"
    set_render(preview_path)
    return {
        "name": asset_name, "category": category,
        "fbx": manifest_path(fbx_path), "glb": manifest_path(glb_path),
        "blend": manifest_path(blend_path), "preview": manifest_path(preview_path),
        **stats,
    }


def main() -> None:
    args = parse_args()
    runtime_root = Path(args.output).resolve()
    source_root = Path(args.source_output).resolve()
    preview_root = Path(args.preview_output).resolve()
    manifest_file = Path(args.manifest).resolve()
    for path in (runtime_root, source_root, preview_root, manifest_file.parent):
        path.mkdir(parents=True, exist_ok=True)

    entries: List[Dict[str, object]] = []

    for asset_name, width in (("Keeper_0", 0.82), ("Keeper_1", 1.0), ("Keeper_2", 1.16)):
        reset_scene()
        build_humanoid(asset_name, "keeper", width, 2.35, "ink", "skin_2")
        entries.append(export_asset(asset_name, "Characters", runtime_root, source_root, preview_root))

    specialists = [
        ("Mara_Rook", "mara", 1.00, 2.34, "burgundy", "skin_3"),
        ("Toma_Reed", "toma", 0.96, 2.40, "olive", "skin_4"),
        ("Sefu_Anik", "sefu", 0.90, 2.30, "violet", "skin_1"),
        ("Elian_Thread", "elian", 1.05, 2.37, "ink", "skin_5"),
        ("Sen_Osei", "sen", 0.93, 2.43, "burgundy", "skin_0"),
        ("Nara_Voss", "nara", 0.98, 2.31, "paper", "skin_2"),
    ]
    for asset_name, role, width, height, coat, skin in specialists:
        reset_scene()
        arm = build_humanoid(asset_name, role, width, height, coat, skin)
        specialist_accessories(arm, role, height, width)
        entries.append(export_asset(asset_name, "Characters", runtime_root, source_root, preview_root))

    jury = [
        ("Lio_Jury", 0, "skin_4", "rust"),
        ("Amara_Jury", 1, "skin_1", "teal"),
        ("Kweku_Jury", 2, "skin_3", "paper"),
    ]
    for asset_name, index, skin, coat in jury:
        reset_scene()
        height = 1.72 + index * 0.05
        arm = build_humanoid(asset_name, "jury", 0.84 + index * 0.04, height, coat, skin)
        jury_accessories(arm, height, index)
        entries.append(export_asset(asset_name, "Characters", runtime_root, source_root, preview_root))

    creature_builders = [
        ("Avian", build_avian), ("BurdenBeast", build_burden_beast),
        ("Lantern", build_lantern), ("Serpentine", build_serpentine), ("Choir", build_choir),
    ]
    for asset_name, builder in creature_builders:
        reset_scene()
        arm = builder()
        add_guardrail_and_burden_demo(arm)
        entries.append(export_asset(asset_name, "Creatures", runtime_root, source_root, preview_root))

    manifest = {
        "schema": 1,
        "generator": "cloud_art/generate_idea_zoo_assets.py",
        "art_direction": "civic surrealism",
        "asset_count": len(entries),
        "characters": [entry for entry in entries if entry["category"] == "Characters"],
        "creatures": [entry for entry in entries if entry["category"] == "Creatures"],
        "palette": {key: list(value) for key, value in PALETTE.items()},
        "required_human_bones": HUMAN_BONES,
        "creature_sockets": ["HeadSocket", "AppetiteSocket", "BurdenSocket", "GuardrailRoot", "TailSocket", "EffectRoot"],
    }
    manifest_file.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"generated": len(entries), "manifest": str(manifest_file)}, indent=2))


if __name__ == "__main__":
    main()
