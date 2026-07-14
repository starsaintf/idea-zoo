#!/usr/bin/env python3
"""Refined visual pass for the deterministic Idea Zoo Blender generator.

This module preserves every asset ID, bone name, socket and export contract from
`generate_idea_zoo_assets.py`, while replacing the first blockout-like humanoids
with clearer bipedal silhouettes and strengthening the five creature families.
"""
from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector

import generate_idea_zoo_assets as base

_original_set_render = base.set_render


def _scale(height: float) -> float:
    return height / 2.35


def _front_eye(name: str, x: float, z: float, s: float, arm) -> None:
    eye = base.add_sphere(name, (x * s, -0.255 * s, z * s), (0.043 * s, 0.030 * s, 0.052 * s), "teal", 10)
    base.parent_to_bone(eye, arm, "Head")


def build_hair(arm, head_z: float, width: float) -> None:
    s = head_z / 2.15
    # Five deliberately different phone-scale silhouettes. Runtime selects one.
    cap = base.add_sphere("Hair_0_Cap", (0, 0.025 * s, head_z + 0.17 * s), (0.29 * width * s, 0.25 * s, 0.13 * s), "ink", 12)
    base.parent_to_bone(cap, arm, "Head")

    crown = base.add_sphere("Hair_1_Crown", (0, 0.035 * s, head_z + 0.20 * s), (0.30 * width * s, 0.25 * s, 0.16 * s), "ink", 12)
    base.parent_to_bone(crown, arm, "Head")
    knot = base.add_sphere("Hair_1_Knot", (0, 0.05 * s, head_z + 0.39 * s), (0.11 * s, 0.10 * s, 0.13 * s), "ink", 10)
    base.parent_to_bone(knot, arm, "Head")

    for sign in (-1, 1):
        puff = base.add_sphere(f"Hair_2_Puff_{'L' if sign > 0 else 'R'}", (sign * 0.25 * width * s, 0.03 * s, head_z + 0.18 * s), (0.16 * s, 0.15 * s, 0.18 * s), "ink", 10)
        base.parent_to_bone(puff, arm, "Head")

    for index in range(4):
        ridge = base.add_cone(f"Hair_3_Ridge_{index}", ((index - 1.5) * 0.11 * s, 0.03 * s, head_z + 0.26 * s), 0.075 * s, 0.025 * s, 0.24 * s, "ink", 8)
        base.parent_to_bone(ridge, arm, "Head")

    for index in range(5):
        angle = index * math.tau / 5
        coil = base.add_sphere(f"Hair_4_Coil_{index}", (math.cos(angle) * 0.21 * width * s, math.sin(angle) * 0.15 * s, head_z + 0.23 * s), (0.105 * s,) * 3, "ink", 10)
        base.parent_to_bone(coil, arm, "Head")


def build_lenses(arm, chest_z: float, width: float) -> None:
    s = chest_z / 1.55
    for index in range(3):
        x = 0.35 * width * s
        z = chest_z - index * 0.02 * s
        radius = (0.11 + index * 0.025) * s
        ring = base.add_cylinder(f"Lens_{index}_Frame", (x, -0.405 * s, z), radius, 0.035 * s, "brass", 16)
        ring.rotation_euler = (math.radians(90), 0, 0)
        base.parent_to_bone(ring, arm, "Chest")
        glass = base.add_sphere(f"Lens_{index}_Glass", (x, -0.425 * s, z), (radius * 0.78, radius * 0.36, radius * 0.78), "glass", 12)
        base.parent_to_bone(glass, arm, "Chest")


def build_humanoid(name: str, role: str, body_width: float, height: float, coat: str, skin: str):
    arm = base.humanoid_armature(name, height, body_width)
    s = _scale(height)
    head_z = 2.08 * s
    chest_z = 1.53 * s

    coat_body = base.add_cone(
        "FieldCoat" if role == "keeper" else "Coat",
        (0, 0.03 * s, 1.27 * s),
        0.43 * body_width * s,
        0.31 * body_width * s,
        1.17 * s,
        coat,
        16,
    )
    base.parent_to_bone(coat_body, arm, "Spine")

    chest_panel = base.add_cube("CivicApron", (0, -0.34 * s, 1.37 * s), (0.30 * body_width * s, 0.045 * s, 0.38 * s), "paper")
    base.parent_to_bone(chest_panel, arm, "Chest")
    belt = base.add_cylinder("FieldBelt", (0, 0.02 * s, 1.03 * s), 0.36 * body_width * s, 0.075 * s, "brass", 16)
    base.parent_to_bone(belt, arm, "Spine")

    neck = base.add_cylinder("NeckMesh", (0, 0, 1.84 * s), 0.105 * s, 0.20 * s, skin, 12)
    base.parent_to_bone(neck, arm, "Neck")
    head = base.add_sphere("HeadMesh", (0, 0, head_z), (0.27 * s, 0.235 * s, 0.31 * s), skin, 16)
    base.parent_to_bone(head, arm, "Head")
    _front_eye("IntentEye_Left", 0.085, 2.10, s, arm)
    _front_eye("IntentEye_Right", -0.085, 2.10, s, arm)
    nose = base.add_cone("CivicNose", (0, -0.257 * s, 2.04 * s), 0.048 * s, 0.012 * s, 0.12 * s, skin, 8)
    nose.rotation_euler = (math.radians(90), 0, 0)
    base.parent_to_bone(nose, arm, "Head")

    shoulder_x = 0.34 * body_width * s
    for side, sign in (("Left", 1), ("Right", -1)):
        shoulder = base.add_sphere(f"{side}ShoulderPad", (sign * shoulder_x, 0, 1.72 * s), (0.16 * s, 0.14 * s, 0.15 * s), coat, 10)
        base.parent_to_bone(shoulder, arm, f"{side}Shoulder")
        upper = base.cylinder_between(f"{side}Arm", (sign * shoulder_x, 0, 1.68 * s), (sign * 0.58 * body_width * s, -0.02 * s, 1.42 * s), 0.092 * s, coat, 10)
        base.parent_to_bone(upper, arm, f"{side}UpperArm")
        lower = base.cylinder_between(f"{side}Forearm", (sign * 0.58 * body_width * s, -0.02 * s, 1.42 * s), (sign * 0.68 * body_width * s, -0.08 * s, 1.11 * s), 0.078 * s, coat, 10)
        base.parent_to_bone(lower, arm, f"{side}LowerArm")
        hand = base.add_sphere(f"{side}HandMesh", (sign * 0.69 * body_width * s, -0.09 * s, 1.03 * s), (0.095 * s, 0.075 * s, 0.11 * s), skin, 10)
        base.parent_to_bone(hand, arm, f"{side}Hand")

        leg_x = sign * 0.18 * body_width * s
        upper_leg = base.cylinder_between(f"{side}Thigh", (leg_x, 0.02 * s, 0.82 * s), (leg_x, -0.01 * s, 0.45 * s), 0.12 * s, "ink", 10)
        base.parent_to_bone(upper_leg, arm, f"{side}UpperLeg")
        lower_leg = base.cylinder_between(f"{side}Shin", (leg_x, -0.01 * s, 0.45 * s), (leg_x, -0.04 * s, 0.14 * s), 0.105 * s, "ink", 10)
        base.parent_to_bone(lower_leg, arm, f"{side}LowerLeg")
        foot = base.add_cube(f"{side}FootMesh", (leg_x, -0.15 * s, 0.075 * s), (0.13 * s, 0.22 * s, 0.075 * s), "ink")
        base.parent_to_bone(foot, arm, f"{side}Foot")

    build_hair(arm, head_z, body_width)
    if role == "keeper":
        build_lenses(arm, chest_z, body_width)
        for index, key in enumerate(("teal", "rust", "brass", "paper", "moss")):
            plate = base.add_cube(f"RulingPlate_{index}", ((index - 2) * 0.09 * s, -0.395 * s, 1.35 * s), (0.034 * s, 0.018 * s, 0.11 * s), key)
            base.parent_to_bone(plate, arm, "Chest")
        thread = base.cylinder_between("ContainmentThread", (0.35 * body_width * s, -0.41 * s, 1.49 * s), (0.58 * body_width * s, -0.38 * s, 1.06 * s), 0.014 * s, "teal", 8)
        base.parent_to_bone(thread, arm, "Chest")

    base.add_empty("HeadSocket", (0, 0, head_z), arm, "Head")
    base.add_empty("RightHandSocket", (-0.69 * body_width * s, -0.09 * s, 1.03 * s), arm, "RightHand")
    base.add_empty("LeftHandSocket", (0.69 * body_width * s, -0.09 * s, 1.03 * s), arm, "LeftHand")
    return arm


def specialist_accessories(arm, role: str, height: float, width: float) -> None:
    s = _scale(height)
    right = (-0.76 * width * s, -0.10 * s, 1.02 * s)
    if role == "mara":
        cape = base.add_cone("RookCape", (0, 0.25 * s, 1.33 * s), 0.58 * width * s, 0.34 * width * s, 1.28 * s, "paper", 12)
        cape.scale.y = 0.18
        base.parent_to_bone(cape, arm, "Chest")
        stem = base.add_cylinder("HatchFork_Stem", (right[0], right[1], 0.83 * s), 0.035 * s, 1.02 * s, "brass", 10)
        base.parent_to_bone(stem, arm, "RightHand")
        fork = base.add_cube("HatchFork_Tines", (right[0], right[1], 0.31 * s), (0.24 * s, 0.035 * s, 0.045 * s), "teal")
        base.parent_to_bone(fork, arm, "RightHand")
        for sign in (-1, 1):
            tine = base.add_cube(f"HatchFork_Tine_{sign}", (right[0] + sign * 0.21 * s, right[1], 0.40 * s), (0.035 * s, 0.035 * s, 0.15 * s), "teal")
            base.parent_to_bone(tine, arm, "RightHand")
    elif role == "toma":
        sash = base.add_cube("RecallSash", (0.10 * s, -0.405 * s, 1.38 * s), (0.085 * s, 0.022 * s, 0.63 * s), "teal")
        sash.rotation_euler.y = math.radians(-14)
        base.parent_to_bone(sash, arm, "Chest")
        staff = base.add_cylinder("ReleaseStaff", (right[0], right[1], 0.72 * s), 0.038 * s, 1.48 * s, "brass", 10)
        base.parent_to_bone(staff, arm, "RightHand")
        light = base.add_sphere("RecallLight", (right[0], right[1], 1.47 * s), (0.13 * s,) * 3, "teal", 12)
        base.parent_to_bone(light, arm, "RightHand")
    elif role == "sefu":
        for index in range(4):
            vial = base.add_sphere(f"AppetiteVial_{index}", ((index - 1.5) * 0.13 * s, -0.415 * s, 1.27 * s), (0.055 * s, 0.045 * s, 0.095 * s), "teal" if index % 2 else "rust", 10)
            base.parent_to_bone(vial, arm, "Spine")
        frame = base.add_cylinder("AppetiteLens_Frame", (right[0], -0.16 * s, 1.15 * s), 0.19 * s, 0.035 * s, "brass", 16)
        frame.rotation_euler = (math.radians(90), 0, 0)
        base.parent_to_bone(frame, arm, "RightHand")
        glass = base.add_sphere("AppetiteLens_Glass", (right[0], -0.18 * s, 1.15 * s), (0.14 * s, 0.055 * s, 0.14 * s), "glass", 12)
        base.parent_to_bone(glass, arm, "RightHand")
    elif role == "elian":
        spool = base.add_cylinder("MoltSpool", (right[0], -0.12 * s, 1.05 * s), 0.22 * s, 0.16 * s, "brass", 14)
        spool.rotation_euler = (math.radians(90), 0, 0)
        base.parent_to_bone(spool, arm, "RightHand")
        for sign in (-1, 1):
            rail = base.cylinder_between(f"SurgicalFrame_{sign}", (sign * 0.50 * width * s, 0.25 * s, 0.62 * s), (sign * 0.50 * width * s, 0.25 * s, 1.65 * s), 0.025 * s, "paper", 8)
            base.parent_to_bone(rail, arm, "Spine")
        cross = base.cylinder_between("SurgicalFrame_Cross", (-0.50 * width * s, 0.25 * s, 1.62 * s), (0.50 * width * s, 0.25 * s, 1.62 * s), 0.022 * s, "teal", 8)
        base.parent_to_bone(cross, arm, "Chest")
    elif role == "sen":
        forecast = base.add_cube("ForecastCoat", (0, 0.22 * s, 1.35 * s), (0.54 * width * s, 0.055 * s, 0.72 * s), "ink")
        base.parent_to_bone(forecast, arm, "Chest")
        for index in range(3):
            frame = base.add_cube(f"CounterfactualFrame_{index}", (right[0] + (index - 1) * 0.14 * s, -0.20 * s, 1.16 * s + (index % 2) * 0.13 * s), (0.10 * s, 0.025 * s, 0.16 * s), "teal" if index == 1 else "paper")
            base.parent_to_bone(frame, arm, "RightHand")
    elif role == "nara":
        plate = base.add_cube("WhiteRoomPlate", (0, -0.405 * s, 1.34 * s), (0.29 * width * s, 0.025 * s, 0.39 * s), "paper_light")
        base.parent_to_bone(plate, arm, "Chest")
        handle = base.add_cylinder("MercyBell_Handle", (right[0], -0.10 * s, 1.13 * s), 0.034 * s, 0.36 * s, "brass", 10)
        base.parent_to_bone(handle, arm, "RightHand")
        bell = base.add_cone("MercyBell", (right[0], -0.10 * s, 0.83 * s), 0.19 * s, 0.075 * s, 0.28 * s, "rust", 16)
        base.parent_to_bone(bell, arm, "RightHand")


def jury_accessories(arm, height: float, index: int) -> None:
    s = _scale(height)
    key = ("rust", "teal", "paper")[index]
    plate = base.add_cube("QuestionPlate", (0, -0.355 * s, 1.28 * s), (0.31 * s, 0.028 * s, 0.25 * s), key)
    base.parent_to_bone(plate, arm, "Chest")
    question = base.add_cone("QuestionMark", (0, -0.39 * s, 1.31 * s), 0.07 * s, 0.025 * s, 0.20 * s, "ink", 8)
    base.parent_to_bone(question, arm, "Chest")


def _finish_creature(arm, family: str):
    # Strong family accents added without changing the established skeleton contract.
    if family == "Avian":
        beak = base.add_cone("CivicBeak", (0, -0.70, 1.54), 0.14, 0.025, 0.34, "brass", 10)
        beak.rotation_euler = (math.radians(90), 0, 0)
        base.parent_to_bone(beak, arm, "Head")
        for name in ("LeftWing", "RightWing"):
            obj = bpy.data.objects.get(name)
            if obj is not None:
                obj.scale.x *= 1.25
                obj.rotation_euler.y += math.radians(18 if name.startswith("Left") else -18)
    elif family == "BurdenBeast":
        for sign in (-1, 1):
            crate = base.add_cube(f"SideLoad_{sign}", (sign * 0.64, 0.12, 1.17), (0.25, 0.45, 0.22), "brass")
            base.parent_to_bone(crate, arm, "Body")
        brow = base.add_cube("WorkingBrow", (0, -1.23, 1.10), (0.38, 0.12, 0.10), "rust")
        base.parent_to_bone(brow, arm, "Head")
    elif family == "Lantern":
        handle = base.cylinder_between("LanternHandle", (-0.36, 0, 1.72), (0.36, 0, 1.72), 0.045, "brass", 10)
        base.parent_to_bone(handle, arm, "Body")
        for sign in (-1, 1):
            rib = base.cylinder_between(f"LanternRib_{sign}", (sign * 0.55, 0, 0.55), (sign * 0.55, 0, 1.40), 0.025, "paper", 8)
            base.parent_to_bone(rib, arm, "Body")
    elif family == "Serpentine":
        for index in range(4):
            plate = base.add_cube(f"ArchivePlate_{index}", ((index - 1.5) * 0.16, -0.48 + index * 0.18, 0.72 + index * 0.22), (0.13, 0.04, 0.16), "paper")
            base.parent_to_bone(plate, arm, f"Spine_{min(index + 1, 6)}")
    elif family == "Choir":
        ring = base.add_cylinder("ConductorRing", (0, 0, 1.02), 0.52, 0.035, "brass", 24)
        base.parent_to_bone(ring, arm, "Body")
    return arm


def build_avian():
    return _finish_creature(base._v1_build_avian(), "Avian")


def build_burden_beast():
    return _finish_creature(base._v1_build_burden_beast(), "BurdenBeast")


def build_lantern():
    return _finish_creature(base._v1_build_lantern(), "Lantern")


def build_serpentine():
    return _finish_creature(base._v1_build_serpentine(), "Serpentine")


def build_choir():
    return _finish_creature(base._v1_build_choir(), "Choir")


def set_render(preview_path: Path) -> None:
    # Show one clean customization choice in review renders while exporting all variants.
    for obj in bpy.context.scene.objects:
        if obj.name.startswith("Hair_"):
            obj.hide_render = not obj.name.startswith("Hair_1_")
        elif obj.name.startswith("Lens_"):
            obj.hide_render = not obj.name.startswith("Lens_0_")
    _original_set_render(preview_path)


def install_refinement() -> None:
    base._v1_build_avian = base.build_avian
    base._v1_build_burden_beast = base.build_burden_beast
    base._v1_build_lantern = base.build_lantern
    base._v1_build_serpentine = base.build_serpentine
    base._v1_build_choir = base.build_choir
    base.build_hair = build_hair
    base.build_lenses = build_lenses
    base.build_humanoid = build_humanoid
    base.specialist_accessories = specialist_accessories
    base.jury_accessories = jury_accessories
    base.build_avian = build_avian
    base.build_burden_beast = build_burden_beast
    base.build_lantern = build_lantern
    base.build_serpentine = build_serpentine
    base.build_choir = build_choir
    base.set_render = set_render


if __name__ == "__main__":
    install_refinement()
    base.main()
