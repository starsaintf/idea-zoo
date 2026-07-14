using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdeaZoo.Intelligence;
using IdeaZoo.Runtime;
using IdeaZoo.Story;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdeaZoo.Mobile
{
    public enum MobileQualityTier { Eco30, Balanced45, Quality60 }
    public enum MobileIssueSeverity { Info, Warning, Failure }

    [Serializable]
    public sealed class MobileIssueRecord
    {
        public string Code = string.Empty;
        public MobileIssueSeverity Severity;
        public string Message = string.Empty;
        public double TimeSeconds;
    }

    [Serializable]
    public sealed class MobileTelemetryReport
    {
        public string SessionId = string.Empty;
        public string DeviceModel = string.Empty;
        public string OperatingSystem = string.Empty;
        public MobileQualityTier QualityTier;
        public int TargetFps;
        public float RenderScale = 1f;
        public double DurationSeconds;
        public double AverageFps;
        public double P50FrameMs;
        public double P95FrameMs;
        public double P99FrameMs;
        public double FirstMinuteFps;
        public double LastMinuteFps;
        public double PerformanceDecay;
        public long PeakAllocatedMemoryBytes;
        public int LowMemoryEvents;
        public int PauseResumeCycles;
        public int SafeAreaChanges;
        public int InterruptedTouches;
        public int KeyboardAvoidanceMoves;
        public bool PassedSustainedPerformance;
        public List<MobileIssueRecord> Issues = new List<MobileIssueRecord>();
        public string ExportedAtUtc = string.Empty;
    }

    public static class MobileQualityController
    {
        public static MobileQualityTier Current { get; private set; } = MobileQualityTier.Eco30;
        public static float CurrentRenderScale { get; private set; } = 0.72f;

        public static void Apply(MobileQualityTier tier)
        {
            Current = tier;
            if (tier == MobileQualityTier.Eco30)
            {
                Application.targetFrameRate = 30;
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowDistance = 18f;
                QualitySettings.lodBias = 0.65f;
                CurrentRenderScale = 0.68f;
            }
            else if (tier == MobileQualityTier.Balanced45)
            {
                Application.targetFrameRate = 45;
                QualitySettings.antiAliasing = 2;
                QualitySettings.shadowDistance = 28f;
                QualitySettings.lodBias = 0.86f;
                CurrentRenderScale = 0.82f;
            }
            else
            {
                Application.targetFrameRate = 60;
                QualitySettings.antiAliasing = 2;
                QualitySettings.shadowDistance = 42f;
                QualitySettings.lodBias = 1.08f;
                CurrentRenderScale = 1f;
            }
            QualitySettings.vSyncCount = 0;
            ScalableBufferManager.ResizeBuffers(CurrentRenderScale, CurrentRenderScale);
            PlayerPrefs.SetInt("iz_mobile_quality", (int)tier);
            PlayerPrefs.Save();
        }

        public static void Restore()
        {
            var tier = (MobileQualityTier)Mathf.Clamp(PlayerPrefs.GetInt("iz_mobile_quality", (int)MobileQualityTier.Eco30), 0, 2);
            Apply(tier);
        }

        public static void ReduceForPressure()
        {
            if (Current == MobileQualityTier.Quality60) Apply(MobileQualityTier.Balanced45);
            else if (Current == MobileQualityTier.Balanced45) Apply(MobileQualityTier.Eco30);
            else
            {
                CurrentRenderScale = Mathf.Max(0.58f, CurrentRenderScale - 0.05f);
                ScalableBufferManager.ResizeBuffers(CurrentRenderScale, CurrentRenderScale);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class MobileFrameTelemetry : MonoBehaviour
    {
        private readonly List<float> _allFrames = new List<float>(36000);
        private readonly Queue<float> _recentFrames = new Queue<float>(900);
        private readonly List<float> _firstMinute = new List<float>(3600);
        private readonly List<MobileIssueRecord> _issues = new List<MobileIssueRecord>();
        private string _sessionId;
        private float _started;
        private long _peakMemory;
        private int _lowMemory;
        private int _pauseCycles;
        private int _safeAreaChanges;
        private int _interruptedTouches;
        private int _keyboardMoves;
        private Rect _lastSafeArea;
        private bool _paused;
        private float _nextPressureReview;

        public IReadOnlyList<MobileIssueRecord> Issues { get { return _issues; } }

        private void Awake()
        {
            _sessionId = "mobile-" + Guid.NewGuid().ToString("N");
            _started = Time.realtimeSinceStartup;
            _lastSafeArea = Screen.safeArea;
            Application.lowMemory += OnLowMemory;
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }

        private void Update()
        {
            if (_paused) return;
            var frame = Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 1f);
            _allFrames.Add(frame);
            _recentFrames.Enqueue(frame);
            while (_recentFrames.Count > 900) _recentFrames.Dequeue();
            if (Time.realtimeSinceStartup - _started <= 60f) _firstMinute.Add(frame);
            _peakMemory = Math.Max(_peakMemory, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
            if (Screen.safeArea != _lastSafeArea) { _safeAreaChanges++; _lastSafeArea = Screen.safeArea; }
            if (Time.unscaledTime >= _nextPressureReview)
            {
                _nextPressureReview = Time.unscaledTime + 12f;
                ReviewPressure();
            }
        }

        public void RecordInterruptedTouch(string reason)
        {
            _interruptedTouches++;
            AddIssue("TOUCH_INTERRUPTED", MobileIssueSeverity.Info, reason);
        }

        public void RecordKeyboardMove() { _keyboardMoves++; }

        public void RecordPause(bool paused)
        {
            if (paused) _paused = true;
            else if (_paused) { _paused = false; _pauseCycles++; }
        }

        public MobileTelemetryReport BuildReport()
        {
            var duration = Math.Max(0.001, Time.realtimeSinceStartup - _started);
            var sorted = _allFrames.OrderBy(value => value).ToArray();
            var recent = _recentFrames.ToArray();
            var firstFps = Fps(_firstMinute);
            var lastFps = Fps(recent);
            var decay = firstFps <= 0 ? 0 : Math.Max(0, (firstFps - lastFps) / firstFps);
            var target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 30;
            var report = new MobileTelemetryReport
            {
                SessionId = _sessionId,
                DeviceModel = SystemInfo.deviceModel,
                OperatingSystem = SystemInfo.operatingSystem,
                QualityTier = MobileQualityController.Current,
                TargetFps = target,
                RenderScale = MobileQualityController.CurrentRenderScale,
                DurationSeconds = duration,
                AverageFps = Fps(_allFrames),
                P50FrameMs = Percentile(sorted, 0.50f) * 1000.0,
                P95FrameMs = Percentile(sorted, 0.95f) * 1000.0,
                P99FrameMs = Percentile(sorted, 0.99f) * 1000.0,
                FirstMinuteFps = firstFps,
                LastMinuteFps = lastFps,
                PerformanceDecay = decay,
                PeakAllocatedMemoryBytes = _peakMemory,
                LowMemoryEvents = _lowMemory,
                PauseResumeCycles = _pauseCycles,
                SafeAreaChanges = _safeAreaChanges,
                InterruptedTouches = _interruptedTouches,
                KeyboardAvoidanceMoves = _keyboardMoves,
                PassedSustainedPerformance = duration >= 600 && lastFps >= target * 0.82 && decay <= 0.18 && _lowMemory == 0,
                Issues = new List<MobileIssueRecord>(_issues),
                ExportedAtUtc = DateTime.UtcNow.ToString("O")
            };
            if (duration < 600) report.Issues.Add(Issue("TEST_TOO_SHORT", MobileIssueSeverity.Warning, "The sustained test must run for at least ten minutes.", duration));
            if (decay > 0.18) report.Issues.Add(Issue("PERFORMANCE_DECAY", MobileIssueSeverity.Failure, "The last minute slowed by more than 18% relative to the first minute.", duration));
            return report;
        }

        public string ExportReport()
        {
            var folder = Path.Combine(Application.persistentDataPath, "idea-zoo-mobile-qa");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, _sessionId + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(BuildReport(), true));
            return path;
        }

        private void ReviewPressure()
        {
            if (_recentFrames.Count < 120) return;
            var fps = Fps(_recentFrames);
            var target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 30;
            if (fps < target * 0.72)
            {
                AddIssue("SUSTAINED_FRAME_PRESSURE", MobileIssueSeverity.Warning, "Recent frame rate fell below 72% of target; reducing presentation pressure.");
                MobileQualityController.ReduceForPressure();
            }
        }

        private void OnLowMemory()
        {
            _lowMemory++;
            AddIssue("LOW_MEMORY", MobileIssueSeverity.Failure, "The operating system issued a low-memory warning.");
            MobileQualityController.ReduceForPressure();
            Resources.UnloadUnusedAssets();
        }

        private void AddIssue(string code, MobileIssueSeverity severity, string message)
        {
            _issues.Add(Issue(code, severity, message, Time.realtimeSinceStartup - _started));
        }

        private static MobileIssueRecord Issue(string code, MobileIssueSeverity severity, string message, double time)
        {
            return new MobileIssueRecord { Code = code, Severity = severity, Message = message, TimeSeconds = time };
        }

        private static double Fps(IEnumerable<float> frames)
        {
            var values = frames as ICollection<float> ?? frames.ToArray();
            return values.Count == 0 ? 0 : 1.0 / Math.Max(0.0001, values.Average());
        }

        private static double Percentile(float[] sorted, float percentile)
        {
            if (sorted == null || sorted.Length == 0) return 0;
            var index = Mathf.Clamp(Mathf.CeilToInt((sorted.Length - 1) * percentile), 0, sorted.Length - 1);
            return sorted[index];
        }
    }

    [DisallowMultipleComponent]
    public sealed class MobileKeyboardAvoidance : MonoBehaviour
    {
        private RectTransform _safeRoot;
        private Vector2 _basePosition;
        private MobileFrameTelemetry _telemetry;
        private bool _moved;

        public void Build(RectTransform safeRoot, MobileFrameTelemetry telemetry)
        {
            _safeRoot = safeRoot;
            _telemetry = telemetry;
            if (_safeRoot != null) _basePosition = _safeRoot.anchoredPosition;
        }

        private void LateUpdate()
        {
            if (_safeRoot == null || EventSystem.current == null) return;
            var selected = EventSystem.current.currentSelectedGameObject;
            var input = selected != null ? selected.GetComponent<InputField>() : null;
            var keyboard = TouchScreenKeyboard.visible ? TouchScreenKeyboard.area : Rect.zero;
            if (input == null || keyboard.height <= 0)
            {
                if (_moved) _safeRoot.anchoredPosition = Vector2.Lerp(_safeRoot.anchoredPosition, _basePosition, Time.unscaledDeltaTime * 12f);
                _moved = false;
                return;
            }
            var rect = input.transform as RectTransform;
            if (rect == null) return;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var bottom = RectTransformUtility.WorldToScreenPoint(null, corners[0]).y;
            var required = keyboard.yMax + 24f - bottom;
            if (required > 0f)
            {
                _safeRoot.anchoredPosition = _basePosition + Vector2.up * Mathf.Min(required, Screen.height * 0.42f);
                if (!_moved) _telemetry?.RecordKeyboardMove();
                _moved = true;
            }
        }
    }

    [Serializable]
    internal sealed class MobileResumeCheckpoint
    {
        public string RecordId = string.Empty;
        public int Stage;
        public string Objective = string.Empty;
        public string SavedAtUtc = string.Empty;
        public string IntelligenceReportId = string.Empty;
        public int CampaignChapter;
    }

    [DisallowMultipleComponent]
    public sealed class MobileRuntimeRecovery : MonoBehaviour
    {
        private IdeaZooGame _game;
        private CampaignDirector _campaign;
        private IdeaIntelligenceRuntimeDirector _intelligence;
        private MobileFrameTelemetry _telemetry;
        private string _checkpointPath;

        public void Build(MobileFrameTelemetry telemetry)
        {
            _telemetry = telemetry;
            _checkpointPath = Path.Combine(Application.persistentDataPath, "idea-zoo-mobile-resume.json");
        }

        private IEnumerator Start()
        {
            for (var frame = 0; frame < 480; frame++)
            {
                _game = FindFirstObjectByType<IdeaZooGame>();
                _campaign = FindFirstObjectByType<CampaignDirector>();
                _intelligence = FindFirstObjectByType<IdeaIntelligenceRuntimeDirector>();
                if (_game != null) yield break;
                yield return null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            _telemetry?.RecordPause(paused);
            if (_game != null && _game.Keeper != null)
            {
                _game.Keeper.ResetTransientInput();
                _telemetry?.RecordInterruptedTouch(paused ? "Application entered the background." : "Application resumed and transient input was cleared.");
            }
            if (paused) SaveCheckpoint();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _game != null && _game.Keeper != null) _game.Keeper.ResetTransientInput();
        }

        private void OnApplicationQuit()
        {
            SaveCheckpoint();
            _telemetry?.ExportReport();
        }

        private void SaveCheckpoint()
        {
            if (_checkpointPath == null || _game == null || _game.Director == null) return;
            var profile = _game.Director.Profile;
            var checkpoint = new MobileResumeCheckpoint
            {
                RecordId = profile != null ? profile.RecordId : string.Empty,
                Stage = (int)_game.Director.Stage,
                Objective = profile != null ? profile.Promise : string.Empty,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                IntelligenceReportId = _intelligence != null && _intelligence.LastReport != null ? _intelligence.LastReport.ReportId : string.Empty,
                CampaignChapter = _campaign != null && _campaign.State != null ? (int)_campaign.State.Chapter : 0
            };
            var temporary = _checkpointPath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(checkpoint, true));
            if (File.Exists(_checkpointPath)) File.Copy(_checkpointPath, _checkpointPath + ".backup", true);
            if (File.Exists(_checkpointPath)) File.Delete(_checkpointPath);
            File.Move(temporary, _checkpointPath);
        }
    }

    [Serializable]
    public sealed class MobileAbuseScenario
    {
        public string Id = string.Empty;
        public string Action = string.Empty;
        public string RequiredOutcome = string.Empty;
    }

    public static class MobileAbuseTestCatalog
    {
        public static readonly MobileAbuseScenario[] Scenarios =
        {
            Scenario("incomplete-intake", "Submit every intake field in a partially completed state.", "No invalid submission advances the case or dismisses the form."),
            Scenario("double-evidence", "Double-tap every evidence strength and submit control.", "Each habitat records exactly once."),
            Scenario("cancel-molt", "Open, edit, cancel and reopen the Molt House repeatedly.", "The original profile and accumulated evidence remain intact."),
            Scenario("force-close-save", "Terminate the app during an archive or checkpoint replacement.", "The previous valid archive loads from backup and corrupt data is quarantined."),
            Scenario("rotation-keyboard", "Rotate while the keyboard is open on the lowest intake field.", "The focused field remains visible inside the safe area."),
            Scenario("interrupted-camera", "Background the app while dragging the camera and joystick.", "All touch owners clear on pause and resume."),
            Scenario("three-cases", "Complete three ideas without restarting the app.", "No prior creature, staff reaction, ruling or report leaks into the next case."),
            Scenario("long-unicode", "Paste long multilingual text, emoji and combining characters into every field.", "Input remains editable, saved and exportable without truncation corruption."),
            Scenario("storage-eviction", "Remove local cached files while preserving the application container.", "Missing evidence files are reported without corrupting specimen records."),
            Scenario("ten-minute-thermal-proxy", "Play a representative route for ten minutes in ECO mode.", "Last-minute FPS remains at least 82% of target and within 18% of the first minute."),
            Scenario("fifteen-minute-route", "Repeat intake, traversal, four habitats, Molt and ruling for fifteen minutes.", "Memory growth stabilizes and no low-memory warning occurs."),
            Scenario("offline-remote", "Disable connectivity while optional remote intelligence is enabled.", "The local reasoner continues and no raw evidence leaves the device.")
        };

        private static MobileAbuseScenario Scenario(string id, string action, string outcome) { return new MobileAbuseScenario { Id = id, Action = action, RequiredOutcome = outcome }; }
    }

    [DisallowMultipleComponent]
    public sealed class MobileProductionDirector : MonoBehaviour
    {
        private MobileFrameTelemetry _telemetry;

        private IEnumerator Start()
        {
            MobileQualityController.Restore();
            _telemetry = gameObject.AddComponent<MobileFrameTelemetry>();
            var recovery = gameObject.AddComponent<MobileRuntimeRecovery>();
            recovery.Build(_telemetry);
            for (var frame = 0; frame < 480; frame++)
            {
                var hud = FindFirstObjectByType<IdeaZooHud>();
                if (hud != null)
                {
                    var canvas = hud.GetComponentInChildren<Canvas>(true);
                    var safeRoot = canvas != null ? canvas.GetComponentsInChildren<SafeAreaFitter>(true).FirstOrDefault() : null;
                    if (safeRoot != null)
                    {
                        var keyboard = gameObject.AddComponent<MobileKeyboardAvoidance>();
                        keyboard.Build(safeRoot.transform as RectTransform, _telemetry);
                    }
                    yield break;
                }
                yield return null;
            }
        }

        public MobileTelemetryReport Snapshot() { return _telemetry != null ? _telemetry.BuildReport() : null; }
        public string Export() { return _telemetry != null ? _telemetry.ExportReport() : string.Empty; }
    }

    public static class MobileProductionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<MobileProductionDirector>() != null) return;
            var root = new GameObject("IdeaZoo_Mobile_Production_QA");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.AddComponent<MobileProductionDirector>();
        }
    }
}
