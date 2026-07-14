#!/usr/bin/env python3
from __future__ import annotations
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PATH = ROOT / "unity/Assets/IdeaZoo/Mobile/MobileProductionQA.cs"
failures: list[str] = []

def check(value: bool, message: str) -> None:
    if not value:
        failures.append(message)

source = PATH.read_text(encoding="utf-8") if PATH.is_file() else ""
if not source:
    failures.append("mobile production source is missing")

for token in [
    "MobileQualityTier", "Eco30", "Balanced45", "Quality60", "MobileFrameTelemetry",
    "P95FrameMs", "P99FrameMs", "PerformanceDecay", "PassedSustainedPerformance",
    "MobileKeyboardAvoidance", "TouchScreenKeyboard.area", "MobileRuntimeRecovery",
    "OnApplicationPause", "ResetTransientInput", "MobileAbuseTestCatalog", "MobileProductionDirector",
    "ScalableBufferManager.ResizeBuffers", "Application.lowMemory", "Profiler.GetTotalAllocatedMemoryLong",
    ".backup", ".tmp", "idea-zoo-mobile-resume.json", "idea-zoo-mobile-qa"
]:
    check(token in source, f"mobile production pass is missing {token}")
for scenario in [
    "incomplete-intake", "double-evidence", "cancel-molt", "force-close-save", "rotation-keyboard",
    "interrupted-camera", "three-cases", "long-unicode", "storage-eviction",
    "ten-minute-thermal-proxy", "fifteen-minute-route", "offline-remote"
]:
    check(scenario in source, f"mobile abuse scenario is missing {scenario}")
check("duration >= 600" in source, "sustained performance cannot pass only after ten minutes")
check("lastFps >= target * 0.82" in source, "last-minute frame threshold is missing")
check("decay <= 0.18" in source, "performance-decay threshold is missing")
check("20" not in source or "MobileIssue" in source, "mobile issue reporting is incomplete")
check("Screen.safeArea" in source, "safe-area change tracking is missing")
check("Microphone" not in source, "mobile QA should not own voice intake")
stripped = re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', source, flags=re.M | re.S)
check(stripped.count("{") == stripped.count("}"), "mobile production source has unbalanced braces")
check("TODO" not in source and "NotImplementedException" not in source, "unfinished mobile code remains")

if failures:
    print("UNITY_MOBILE_PASS_FAIL", file=sys.stderr)
    for failure in failures:
        print("- " + failure, file=sys.stderr)
    raise SystemExit(1)
print("UNITY_MOBILE_PASS_PASS")
