#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
px = ROOT / "unity/Assets/IdeaZoo/PlayerExperience"
files = {
    "core": px / "PlayerExperienceCore.cs",
    "tactile": px / "PlayerExperienceTactile.cs",
    "accessibility": px / "PlayerExperienceAccessibility.cs",
    "hud": px / "PlayerExperienceHud.cs",
    "world": px / "PlayerExperienceWorldPass.cs",
    "director": px / "PlayerExperienceDirector.cs",
    "rank": px / "PlayerExperienceRankGuidance.cs",
    "webgl_builder": ROOT / "unity/Assets/IdeaZoo/Editor/IdeaZooWebGLBuilder.cs",
    "depth_hud": ROOT / "unity/Assets/IdeaZoo/Gameplay/GameplayDepthHud.cs",
    "depth_director": ROOT / "unity/Assets/IdeaZoo/Gameplay/GameplayDepthDirector.cs",
    "edit_tests": ROOT / "unity/Assets/IdeaZoo/Tests/EditMode/PlayerExperienceEditorTests.cs",
    "play_tests": ROOT / "unity/Assets/IdeaZoo/Tests/PlayMode/IdeaZooCloudPlayModeTests.cs",
    "doc": ROOT / "unity/PLAYER_EXPERIENCE_V1.md",
}

missing = [str(path.relative_to(ROOT)) for path in files.values() if not path.exists()]
if missing:
    raise SystemExit("Missing Player Experience V1 files: " + ", ".join(missing))

texts = {name: path.read_text(encoding="utf-8") for name, path in files.items()}

checks = {
    "authored tutorial enters real intake": all(token in texts["core"] for token in (
        "The Neighbourhood Umbrella Library", "public static IdeaIntake Intake()", "Maintenance =", "Harm ="
    )) and "GetMethod(\"BeginCase\"" in texts["director"],
    "guided completion opens normal zoo": "record != null && record.Tutorial" in texts["director"] and "_service.DismissOnboarding()" in texts["director"],
    "eight hidden archetypes": texts["core"].count("IdeaArchetype.") >= 8 and "public enum IdeaArchetype" in texts["core"],
    "archetypes alter live encounters": "DecorateEncounter" in texts["director"] and "PlayerExperienceArchetypeCatalog.Decorate" in texts["director"] and "definition = PlayerExperienceDirector.DecorateEncounter" in texts["depth_director"],
    "rank changes uncertainty not costs": "PlayerExperienceRankGuidance.Apply" in texts["director"] and "FOUNDER MODE" in texts["rank"] and "GameplayImpact" not in texts["rank"],
    "twelve tactile rounds": all(token in texts["tactile"] for token in ("Interview(int round)", "Prototype(int round)", "Feasibility(int round)", "Jury(int round)")) and texts["tactile"].count("return Spec(") >= 12,
    "bounded tactile pool": "new List<Button>(6)" in texts["depth_hud"] and "for (var i = 0; i < 6; i++)" in texts["depth_hud"],
    "tactile evidence has consequences": "ConsumeTactileOutcome" in texts["depth_hud"] and "_resources.Apply(tactile.Impact)" in texts["depth_director"] and "RecordTactileOutcome" in texts["depth_director"],
    "visible consequences pooled": "MaximumVisibleConsequences = 8" in texts["world"] and "for (var i = 0; i < MaximumVisibleConsequences; i++)" in texts["world"],
    "one existing world reused": "_game.World.GetComponent<PlayerExperienceWorldPass>()" in texts["director"],
    "contextual character response": "CharacterPerformanceRig" in texts["director"] and "PlayerExperienceReactionCatalog" in texts["core"],
    "accessibility controls": all(token in texts["hud"] for token in (
        "TEXT SIZE", "REDUCED MOTION", "HIGH CONTRAST", "LARGE TOUCH TARGETS", "DECISION FOCUS MODE", "HAPTIC FEEDBACK"
    )),
    "compact screen content scrolls": "PlayerExperienceViewport" in texts["hud"] and "ScrollRect" in texts["hud"] and "FitMode.PreferredSize" in texts["hud"],
    "decision focus protects logic": "SetDecisionFocus(true)" in texts["depth_hud"] and "SetDecisionFocus(false)" in texts["depth_hud"],
    "mobile haptics excluded from webgl": "#if UNITY_IOS || UNITY_ANDROID" in texts["accessibility"] and "Handheld.Vibrate()" in texts["accessibility"],
    "history capped": "if (Cases.Count > 20)" in texts["core"],
    "durable webgl report": "BuildPipeline.BuildPlayer" in texts["webgl_builder"] and "unity-webgl-report.txt" in texts["webgl_builder"] and "report.steps" in texts["webgl_builder"],
    "single canonical playmode coverage": "Player Experience V1 did not boot" in texts["play_tests"] and "PLAYER_EXPERIENCE_CONSEQUENCES" in texts["play_tests"],
    "editor coverage": "EveryEncounterRoundHasBoundedTactilePreparation" in texts["edit_tests"] and "AllEightHiddenArchetypesExist" in texts["edit_tests"],
}

failed = [name for name, passed in checks.items() if not passed]
if failed:
    raise SystemExit("Player Experience V1 contract failed: " + ", ".join(failed))

for name, path in files.items():
    if path.suffix != ".cs":
        continue
    text = texts[name]
    if text.count("{") != text.count("}"):
        raise SystemExit(f"Unbalanced braces in {path.relative_to(ROOT)}")

# Runtime Update methods may only observe cached state. They may not construct UI/worlds,
# scan hierarchies, run reflection lookups or allocate LINQ collections.
for name in ("director", "depth_director"):
    text = texts[name]
    pieces = text.split("private void Update()", 1)
    if len(pieces) != 2:
        continue
    body = pieces[1].split("\n        }", 1)[0]
    forbidden = ["new GameObject", "Instantiate(", "GetComponentsInChildren", "GetMethod(", "GetField(", ".ToArray(", ".ToList("]
    hits = [token for token in forbidden if token in body]
    if hits:
        raise SystemExit(f"{name} Update performs heavy work: {', '.join(hits)}")

print("UNITY_PLAYER_EXPERIENCE_V1_SOURCE_PASS")
