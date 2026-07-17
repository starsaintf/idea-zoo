#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
builder = ROOT / "unity/Assets/IdeaZoo/Editor/IdeaZooWebGLBuilder.cs"
profile = ROOT / "unity/Assets/IdeaZoo/Editor/WebGLGraphicsProfile.cs"

for path in (builder, profile):
    if not path.exists():
        raise SystemExit(f"Missing WebGL graphics configuration: {path.relative_to(ROOT)}")

builder_text = builder.read_text(encoding="utf-8")
profile_text = profile.read_text(encoding="utf-8")

checks = {
    "builder activates the profile": "using (WebGLGraphicsProfile.Activate(log))" in builder_text,
    "profile uses legacy light probes": "LegacyLightProbes" in profile_text,
    "profile restores desktop settings": "serializedAsset.ApplyModifiedPropertiesWithoutUndo()" in profile_text,
    "profile strips regular probe volume shaders": "IPreprocessShaders" in profile_text and "VoxelizeScene" in profile_text,
    "profile strips probe volume compute shaders": "IPreprocessComputeShaders" in profile_text,
    "profile keeps compatible lighting": "baked lighting, reflection probes, legacy light probes" in profile_text,
}

failed = [name for name, passed in checks.items() if not passed]
if failed:
    raise SystemExit("WebGL graphics profile contract failed: " + ", ".join(failed))

if profile_text.count("{") != profile_text.count("}"):
    raise SystemExit("Unbalanced braces in " + str(profile.relative_to(ROOT)))

print("UNITY_WEBGL_GRAPHICS_PROFILE_PASS")
