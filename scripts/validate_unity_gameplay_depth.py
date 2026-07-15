#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
core = ROOT / "unity/Assets/IdeaZoo/Gameplay/GameplayDepthCore.cs"
director = ROOT / "unity/Assets/IdeaZoo/Gameplay/GameplayDepthDirector.cs"
hud = ROOT / "unity/Assets/IdeaZoo/Gameplay/GameplayDepthHud.cs"
memory = ROOT / "unity/Assets/IdeaZoo/Gameplay/GameplayMemory.cs"
performance = ROOT / "unity/Assets/IdeaZoo/Gameplay/GameplayPerformanceGovernor.cs"
edit_tests = ROOT / "unity/Assets/IdeaZoo/Tests/EditMode/GameplayDepthEditorTests.cs"
play_tests = ROOT / "unity/Assets/IdeaZoo/Tests/PlayMode/GameplayDepthPlayModeTests.cs"
doc = ROOT / "unity/GAMEPLAY_DEPTH_PRODUCTION.md"

required = [core, director, hud, memory, performance, edit_tests, play_tests, doc]
missing = [str(path.relative_to(ROOT)) for path in required if not path.exists()]
if missing:
    raise SystemExit("Missing gameplay-depth files: " + ", ".join(missing))

texts = {path.name: path.read_text(encoding="utf-8") for path in required}
core_text = texts[core.name]
director_text = texts[director.name]
hud_text = texts[hud.name]
memory_text = texts[memory.name]
performance_text = texts[performance.name]

checks = {
    "four playable evidence encounters": all(token in core_text for token in (
        "CustomerInterview", "PrototypeTrial", "FeasibilityAudit", "ChildrensJury"
    )),
    "five disruptive events": all(token in core_text for token in (
        "CompetitorLaunch", "CostShock", "AudiencePivot", "PrivacyConcern", "PrematureAttention"
    )),
    "limited resources": all(token in core_text for token in (
        "public int Time = 18", "public int Trust = 8", "public int Momentum = 7", "public int Evidence"
    )),
    "real choice consequences": all(token in core_text for token in (
        "Desirability", "Feasibility", "Viability", "Safety", "TestScore"
    )),
    "persistent player tendency": "GameplayTendencyRecord" in memory_text and "DominantTendency" in memory_text,
    "persistent scars": "BuildScars" in memory_text and "Scars" in memory_text,
    "bounded save history": "MaximumSavedCases" in memory_text and "RemoveRange" in memory_text,
    "pooled archive cards": "GameplayMemoryCard_" in memory_text and "MaximumVisibleMemoryCards" in memory_text,
    "pooled encounter buttons": "ChoicePool" in hud_text and "for (var i = 0; i < 4; i++)" in hud_text,
    "cached compatibility adapter": "GetMethod(\"RecordEvidence\"" in director_text and "GetField(\"_currentTest\"" in director_text,
    "single existing world reused": "_game.World.GetComponent<GameplayMemoryWorldPass>()" in director_text,
    "adaptive sustained frame protection": all(token in performance_text for token in (
        "_badSamples >= 3", "_goodSamples >= 8", "ReduceNonessentialLoad", "RestoreQuality"
    )),
    "mobile target retained": "MobileTargetFps = 30" in performance_text,
    "editor coverage": "EveryEvidenceHabitatIsPlayableAndBounded" in texts[edit_tests.name],
    "playmode ownership coverage": "GameplayDepthBootsOnceAndReusesTheExistingWorld" in texts[play_tests.name],
}

failed = [name for name, passed in checks.items() if not passed]
if failed:
    raise SystemExit("Gameplay-depth contract failed: " + ", ".join(failed))

for path in required[:-1]:
    text = path.read_text(encoding="utf-8")
    if text.count("{") != text.count("}"):
        raise SystemExit(f"Unbalanced braces in {path.relative_to(ROOT)}")

# Update loops may sample counters and state, but may not build UI, worlds or collections.
for label, text in (("director", director_text), ("performance", performance_text)):
    update = text.split("private void Update()", 1)
    if len(update) == 2:
        body = update[1].split("\n        }", 1)[0]
        forbidden = ["new GameObject", "Instantiate(", "GetComponentsInChildren", ".ToList(", ".ToArray("]
        hits = [token for token in forbidden if token in body]
        if hits:
            raise SystemExit(f"{label} Update allocates or scans heavily: {', '.join(hits)}")

print("UNITY_GAMEPLAY_DEPTH_SOURCE_PASS")
