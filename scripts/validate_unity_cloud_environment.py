#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity"
failures: list[str] = []


def check(condition: bool, message: str) -> None:
    if not condition:
        failures.append(message)


def read(path: pathlib.Path) -> str:
    if not path.is_file():
        failures.append(f"missing required file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


paths = {
    "mesh_factory": UNITY / "Assets/IdeaZoo/Presentation/CivicAuthoredMeshFactory.cs",
    "environment": UNITY / "Assets/IdeaZoo/Presentation/AuthoredEnvironmentPass.cs",
    "budget": UNITY / "Assets/IdeaZoo/Presentation/MobilePresentationBudget.cs",
    "baker": UNITY / "Assets/IdeaZoo/Editor/IdeaZooPresentationBaker.cs",
    "edit_test": UNITY / "Assets/IdeaZoo/Tests/EditMode/IdeaZooCloudEditorTests.cs",
    "edit_asmdef": UNITY / "Assets/IdeaZoo/Tests/EditMode/IdeaZoo.EditorCloudTests.asmdef",
    "play_test": UNITY / "Assets/IdeaZoo/Tests/PlayMode/IdeaZooCloudPlayModeTests.cs",
    "play_asmdef": UNITY / "Assets/IdeaZoo/Tests/PlayMode/IdeaZoo.PlayModeCloudTests.asmdef",
    "cloud_workflow": ROOT / ".github/workflows/unity-cloud-integration.yml",
    "license_workflow": ROOT / ".github/workflows/unity-license-request.yml",
}
texts = {name: read(path) for name, path in paths.items()}

factory = texts["mesh_factory"]
for method in ["FoldedPanel", "ArchRing", "TubeArc", "ClassificationSeal", "StitchedFrame", "Ribbon"]:
    check(f"Mesh {method}(" in factory, f"custom mesh factory is missing {method}")
check("mesh.SetVertices" in factory and "mesh.SetTriangles" in factory, "custom meshes do not assign geometry")
check("RecalculateNormals" in factory and "RecalculateTangents" in factory and "RecalculateBounds" in factory, "custom meshes do not finalize normals, tangents and bounds")

art = texts["environment"]
for department in [
    "01_WHISPER_GATE", "02_HATCHERY_ROTUNDA", "03_CENTRAL_ARCHIVE_WALK",
    "04_DESIRE_YARD", "05_COMMITMENT_PADDOCK", "06_BURROWER_TUNNEL",
    "07_REFUSAL_GATE", "08_MOLT_HOUSE", "09_SEALED_BOARD_WING", "10_DECISION_GARDEN",
]:
    check(department in art, f"authored environment pass does not cover {department}")
for primitive in ["FoldedPanel", "ArchRing", "TubeArc", "ClassificationSeal", "StitchedFrame", "Ribbon"]:
    check(f"CivicAuthoredMeshFactory.{primitive}" in art, f"environment does not use authored {primitive} meshes")
for tier in ["Landmark", "Department", "Decorative"]:
    check(f"AuthoredDetailTier.{tier}" in art, f"authored detail tier is missing {tier}")
check("LODGroup" in art and "new LOD(" in art, "authored environment lacks mobile LOD groups")
check("AUTHORED_ENVIRONMENT_KIT" in art, "authored environment root is missing")
check("RuntimeInitializeOnLoadMethod" in art, "authored environment does not auto-install at runtime")

budget = texts["budget"]
check("GetComponentInParent<AuthoredEnvironmentDetail>" in budget, "mobile budget ignores authored semantic tiers")
check("_landmarks" in budget and "landmarkDistance" in budget, "mobile budget does not protect landmark visibility")
check("AuthoredEnvironmentPass" in budget, "mobile budget may initialize before authored geometry exists")

baker = texts["baker"]
check("PersistGeneratedMeshes" in baker, "editor baker does not persist generated meshes")
check("AssetDatabase.CreateAsset" in baker, "editor baker does not create mesh assets")
check('ArtRoot + "/Meshes"' in baker, "editor baker has no authored mesh asset folder")
check("AuthoredEnvironmentPass" in baker, "editor baker omits authored environment geometry")
check("AssetDatabase.GetAssetPath(filter.sharedMesh)" in baker, "baked validation does not require persistent meshes")

edit_test = texts["edit_test"]
check("ProductionAssembliesImport" in edit_test, "cloud EditMode import test is missing")
check("PresentationBakerCreatesReviewableAssets" in edit_test, "cloud prefab bake test is missing")
check("Validate Baked Assets" in edit_test, "cloud tests do not invoke baked-asset validation")

play_test = texts["play_test"]
check("UnityTest" in play_test and "WhisperGateBootsCompleteRuntime" in play_test, "cloud PlayMode boot test is missing")
check("stationCount" in play_test and "specialistCount" in play_test, "PlayMode test does not validate the inhabited world")
check("10_DECISION_GARDEN" in play_test, "PlayMode test does not verify the full district")

for asmdef_name in ["edit_asmdef", "play_asmdef"]:
    asmdef = texts[asmdef_name]
    check('"TestAssemblies"' in asmdef, f"{asmdef_name} is not marked as a Unity test assembly")
    check('"UNITY_INCLUDE_TESTS"' in asmdef, f"{asmdef_name} lacks the Unity test define")

cloud = texts["cloud_workflow"]
for token in [
    "game-ci/unity-test-runner@v4", "game-ci/unity-builder@v4", "projectPath: unity",
    "unityVersion: 6000.5.0f1", "testMode: all", "targetPlatform: WebGL",
    "UNITY_LICENSE", "UNITY_EMAIL", "UNITY_PASSWORD", "idea-zoo-cloud-baked-presentation",
]:
    check(token in cloud, f"cloud integration workflow is missing {token}")
check("license-preflight" in cloud and "blocked-report" in cloud, "cloud workflow does not report missing licensing honestly")
check("allowDirtyBuild: true" in cloud, "WebGL build may reject cloud-baked assets as a dirty workspace")

license_flow = texts["license_workflow"]
check("unityci/editor:ubuntu-6000.5.0f1-base-3" in license_flow, "browser-only Unity activation does not pin the cloud editor image")
check("-createManualActivationFile" in license_flow, "browser-only Unity activation request command is missing")
check("idea-zoo-unity-manual-activation-request" in license_flow, "activation request is not uploaded as an artifact")
check("unity-license-output/*.alf" in license_flow, "activation workflow does not require an actual .alf file")

for name, source in texts.items():
    if not name.endswith("workflow") and paths[name].suffix == ".cs":
        stripped = re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', source, flags=re.M | re.S)
        check(stripped.count("{") == stripped.count("}"), f"unbalanced braces in {paths[name].relative_to(ROOT)}")
        check("NotImplementedException" not in source, f"unfinished code path in {paths[name].relative_to(ROOT)}")
        check("TODO" not in source, f"TODO remains in {paths[name].relative_to(ROOT)}")

if failures:
    print("UNITY_CLOUD_ENVIRONMENT_VALIDATION_FAIL", file=sys.stderr)
    for failure in failures:
        print(f"- {failure}", file=sys.stderr)
    raise SystemExit(1)

print("UNITY_CLOUD_ENVIRONMENT_VALIDATION_PASS")
