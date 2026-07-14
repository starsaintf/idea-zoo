#!/usr/bin/env python3
from __future__ import annotations
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
files = [
    ROOT / "unity/Assets/IdeaZoo/Core/IdeaIntelligenceCore.cs",
    ROOT / "unity/Assets/IdeaZoo/Intelligence/IdeaIntelligenceRuntime.cs",
]
failures: list[str] = []

def check(value: bool, message: str) -> None:
    if not value:
        failures.append(message)

sources = []
for path in files:
    if not path.is_file():
        failures.append(f"missing intelligence source: {path.relative_to(ROOT)}")
        sources.append("")
    else:
        sources.append(path.read_text(encoding="utf-8"))
source = "\n".join(sources)

for token in [
    "IIdeaIntelligenceProvider", "LocalIdeaIntelligenceProvider", "IdeaIntelligenceReport",
    "AssumptionChallenge", "StakeholderSimulation", "ExperimentRecommendation", "EvidenceInterpretation",
    "EvidenceVault", "IdeaVersionHistory", "OptionalRemoteIntelligenceClient", "VoiceIdeaCapture",
    "ImportPrivateFile", "IndependentlyVerified", "SuggestedRuling", "The player, not the intelligence layer",
    "decision-record.md", "ContentHash", "Sha256", "IncludeRawSources = false"
]:
    check(token in source, f"Idea Lab intelligence pass is missing {token}")
for kind in ["Interview", "Commitment", "Prototype", "Metric", "Document", "Link", "Cost", "Observation"]:
    check(kind in source, f"evidence artifact type is missing {kind}")
for experiment in ["problem-interviews", "costly-commitment", "concierge-core", "refusal-drill", "hostile-owner"]:
    check(experiment in source, f"high-information experiment is missing {experiment}")
for stakeholder in ["First user", "Payer", "Maintainer", "Reluctant participant", "Powerful later owner", "Regulator or community"]:
    check(stakeholder in source, f"stakeholder simulation is missing {stakeholder}")
check("20 * 1024 * 1024" in source, "private evidence vault has no file-size boundary")
check(".corrupt-" in source and ".backup" in source and ".tmp" in source, "evidence/version recovery is incomplete")
check("Uri.IsWellFormedUriString" in source, "optional remote endpoint is not validated")
check("Authorization" in source and "sessionToken" in source, "optional remote authorization bridge is incomplete")
check("Microphone.Start" in source and "Microphone.End" in source, "voice intake capture is incomplete")
for path, text in zip(files, sources):
    stripped = re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', text, flags=re.M | re.S)
    check(stripped.count("{") == stripped.count("}"), f"unbalanced braces in {path.relative_to(ROOT)}")
    check("TODO" not in text and "NotImplementedException" not in text, f"unfinished intelligence code in {path.relative_to(ROOT)}")

if failures:
    print("UNITY_INTELLIGENCE_PASS_FAIL", file=sys.stderr)
    for failure in failures:
        print("- " + failure, file=sys.stderr)
    raise SystemExit(1)
print("UNITY_INTELLIGENCE_PASS_PASS")
