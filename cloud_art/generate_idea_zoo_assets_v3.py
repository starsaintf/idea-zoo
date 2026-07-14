#!/usr/bin/env python3
"""Final proportion-safe cloud generator for The Idea Zoo.

Blender bone parenting can normalize primitive object scale during export. This
entry point applies primitive scale into mesh data before any object is attached
to a bone, preserving the modeled proportions in Blender previews, FBX and GLB.
It then installs the complete V2 visual refinement without changing IDs, rigs or
Unity socket contracts.
"""
from __future__ import annotations

import bpy

import generate_idea_zoo_assets as base
import generate_idea_zoo_assets_v2 as refined


def apply_scale(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


_original_cube = base.add_cube
_original_sphere = base.add_sphere
_original_cylinder = base.add_cylinder
_original_cone = base.add_cone


def add_cube(*args, **kwargs):
    return apply_scale(_original_cube(*args, **kwargs))


def add_sphere(*args, **kwargs):
    return apply_scale(_original_sphere(*args, **kwargs))


def add_cylinder(*args, **kwargs):
    return apply_scale(_original_cylinder(*args, **kwargs))


def add_cone(*args, **kwargs):
    return apply_scale(_original_cone(*args, **kwargs))


def add_preview_stage() -> None:
    # Lower, longer-lens review camera so limbs and props remain readable.
    base.add_cylinder("PreviewPlinth", (0, 0, -0.08), 1.15, 0.14, "ink", 48)
    bpy.ops.object.light_add(type="AREA", location=(4.2, -4.5, 5.2))
    key = bpy.context.object
    key.name = "PreviewKey"
    key.data.energy = 900
    key.data.shape = "DISK"
    key.data.size = 4.0
    bpy.ops.object.light_add(type="AREA", location=(-3.2, -1.0, 2.8))
    fill = bpy.context.object
    fill.name = "PreviewFill"
    fill.data.energy = 520
    fill.data.size = 3.2
    bpy.ops.object.camera_add(location=(4.8, -8.6, 2.35))
    camera = bpy.context.object
    camera.name = "PreviewCamera"
    camera.data.lens = 62
    target = refined.Vector((0, 0, 1.15))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera


def install() -> None:
    base.add_cube = add_cube
    base.add_sphere = add_sphere
    base.add_cylinder = add_cylinder
    base.add_cone = add_cone
    base.add_preview_stage = add_preview_stage
    refined.install_refinement()
    # The refinement installer replaces render behaviour but does not alter the
    # preview-stage function, so the proportion-safe camera remains installed.


if __name__ == "__main__":
    install()
    base.main()
