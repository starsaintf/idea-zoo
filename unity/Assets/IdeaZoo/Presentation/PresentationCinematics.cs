using System;
using System.Collections;
using System.Reflection;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace IdeaZoo.Presentation
{
    public enum PresentationShot
    {
        Hatch,
        Inspection,
        Molt,
        Decision,
        Ruling
    }

    public sealed class IdeaZooCameraShotAsset : PlayableAsset
    {
        public PresentationCameraRig Rig;
        public Transform Target;
        public PresentationShot Shot;
        public double ShotDuration = 1.4;

        public override double duration { get { return ShotDuration; } }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<IdeaZooCameraShotBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Rig = Rig;
            behaviour.Target = Target;
            behaviour.Shot = Shot;
            return playable;
        }
    }

    public sealed class IdeaZooCameraShotBehaviour : PlayableBehaviour
    {
        public PresentationCameraRig Rig;
        public Transform Target;
        public PresentationShot Shot;
        private bool _started;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (Rig == null) return;
            _started = true;
            Rig.BeginShot(Shot, Target);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!_started || Rig == null) return;
            var duration = Math.Max(0.001, playable.GetDuration());
            Rig.SampleShot((float)(playable.GetTime() / duration));
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_started || Rig == null) return;
            _started = false;
            Rig.EndShot();
        }
    }

    [DefaultExecutionOrder(950)]
    [DisallowMultipleComponent]
    public sealed class PresentationCameraRig : MonoBehaviour
    {
        private Camera _camera;
        private ThirdPersonKeeperController _keeper;
        private PlayableDirector _director;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _endPosition;
        private Quaternion _endRotation;
        private bool _shotActive;
        private bool _keeperWasEnabled;
        private RectTransform _topBar;
        private RectTransform _bottomBar;
        private CinemachineReflectionBridge _cinemachine;
        private IdeaZooCameraShotAsset _activeAsset;

        public bool ShotActive { get { return _shotActive; } }

        public void Build(Camera camera, ThirdPersonKeeperController keeper)
        {
            if (_director != null) return;
            _camera = camera;
            _keeper = keeper;
            _director = gameObject.AddComponent<PlayableDirector>();
            _director.playOnAwake = false;
            _director.extrapolationMode = DirectorWrapMode.None;
            BuildBars();
            _cinemachine = new CinemachineReflectionBridge();
            _cinemachine.TryInstall(camera);
        }

        public void Play(PresentationShot shot, Transform target, double duration)
        {
            if (_director == null || _camera == null || target == null) return;
            if (_director.state == PlayState.Playing) _director.Stop();
            if (_activeAsset != null) Destroy(_activeAsset);
            _activeAsset = ScriptableObject.CreateInstance<IdeaZooCameraShotAsset>();
            _activeAsset.name = "Runtime_" + shot + "_Sequence";
            _activeAsset.Rig = this;
            _activeAsset.Target = target;
            _activeAsset.Shot = shot;
            _activeAsset.ShotDuration = duration;
            _director.playableAsset = _activeAsset;
            _director.time = 0d;
            _director.Play();
        }

        public void BeginShot(PresentationShot shot, Transform target)
        {
            if (_camera == null || target == null) return;
            _shotActive = true;
            _startPosition = _camera.transform.position;
            _startRotation = _camera.transform.rotation;
            _keeperWasEnabled = _keeper != null && _keeper.enabled;
            if (_keeper != null) _keeper.enabled = false;
            ShowBars(true);

            var offset = ShotOffset(shot, target);
            _endPosition = target.position + offset;
            var lookPoint = target.position + TargetLift(shot);
            _endRotation = Quaternion.LookRotation((lookPoint - _endPosition).normalized, Vector3.up);
            _cinemachine.Activate(shot, _endPosition, _endRotation);
        }

        public void SampleShot(float normalized)
        {
            if (!_shotActive || _camera == null) return;
            var t = Mathf.Clamp01(normalized);
            var eased = t * t * (3f - 2f * t);
            if (!_cinemachine.Active)
            {
                _camera.transform.position = Vector3.Lerp(_startPosition, _endPosition, eased);
                _camera.transform.rotation = Quaternion.Slerp(_startRotation, _endRotation, eased);
            }
            SetBarAmount(Mathf.SmoothStep(0f, 1f, Mathf.Min(t * 4f, (1f - t) * 4f)));
        }

        public void EndShot()
        {
            if (!_shotActive) return;
            _shotActive = false;
            _cinemachine.Deactivate();
            if (_keeper != null) _keeper.enabled = _keeperWasEnabled;
            ShowBars(false);
        }

        private Vector3 ShotOffset(PresentationShot shot, Transform target)
        {
            if (shot == PresentationShot.Hatch) return new Vector3(4.8f, 2.7f, 5.8f);
            if (shot == PresentationShot.Inspection) return new Vector3(-3.2f, 1.5f, 2.8f);
            if (shot == PresentationShot.Molt) return new Vector3(2.4f, 1.35f, 2.2f);
            if (shot == PresentationShot.Decision) return new Vector3(0f, 9.5f, 10.5f);
            return new Vector3(3.8f, 2.4f, 4.6f);
        }

        private static Vector3 TargetLift(PresentationShot shot)
        {
            if (shot == PresentationShot.Decision) return Vector3.up * 1.0f;
            if (shot == PresentationShot.Ruling) return Vector3.up * 1.15f;
            return Vector3.up * 0.75f;
        }

        private void BuildBars()
        {
            var canvasObject = new GameObject("CinematicCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(896f, 414f);

            _topBar = Bar(canvasObject.transform, "TopBar", true);
            _bottomBar = Bar(canvasObject.transform, "BottomBar", false);
            ShowBars(false);
        }

        private static RectTransform Bar(Transform parent, string name, bool top)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            node.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.025f, 0.96f);
            return rect;
        }

        private void ShowBars(bool visible)
        {
            if (_topBar != null) _topBar.gameObject.SetActive(visible);
            if (_bottomBar != null) _bottomBar.gameObject.SetActive(visible);
            if (visible) SetBarAmount(0f);
        }

        private void SetBarAmount(float amount)
        {
            var height = Mathf.Lerp(0f, 38f, Mathf.Clamp01(amount));
            if (_topBar != null) _topBar.sizeDelta = new Vector2(0f, height);
            if (_bottomBar != null) _bottomBar.sizeDelta = new Vector2(0f, height);
        }
    }

    internal sealed class CinemachineReflectionBridge
    {
        private Behaviour _brain;
        private Component _virtualCamera;
        private PropertyInfo _priority;
        private bool _usable;

        public bool Active { get; private set; }

        public void TryInstall(Camera output)
        {
            try
            {
                var brainType = FindType("Unity.Cinemachine.CinemachineBrain");
                var cameraType = FindType("Unity.Cinemachine.CinemachineCamera");
                if (brainType == null || cameraType == null || output == null) return;
                _brain = output.GetComponent(brainType) as Behaviour;
                if (_brain == null) _brain = output.gameObject.AddComponent(brainType) as Behaviour;
                var cameraObject = new GameObject("Cinemachine_Story_Camera");
                _virtualCamera = cameraObject.AddComponent(cameraType);
                _priority = cameraType.GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance);
                _usable = _brain != null && _virtualCamera != null && _priority != null && _priority.PropertyType == typeof(int);
                if (!_usable)
                {
                    if (_brain != null) _brain.enabled = false;
                    UnityEngine.Object.Destroy(cameraObject);
                    _virtualCamera = null;
                }
                else
                {
                    _priority.SetValue(_virtualCamera, 0, null);
                    _brain.enabled = true;
                }
            }
            catch
            {
                _usable = false;
                if (_brain != null) _brain.enabled = false;
            }
        }

        public void Activate(PresentationShot shot, Vector3 position, Quaternion rotation)
        {
            if (!_usable || _virtualCamera == null) { Active = false; return; }
            try
            {
                _virtualCamera.transform.position = position;
                _virtualCamera.transform.rotation = rotation;
                _priority.SetValue(_virtualCamera, 100 + (int)shot, null);
                Active = true;
            }
            catch
            {
                Active = false;
                if (_brain != null) _brain.enabled = false;
            }
        }

        public void Deactivate()
        {
            if (_usable && _virtualCamera != null)
            {
                try { _priority.SetValue(_virtualCamera, 0, null); }
                catch { }
            }
            Active = false;
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }
    }

    [DefaultExecutionOrder(800)]
    [DisallowMultipleComponent]
    public sealed class IdeaZooPresentationDirector : MonoBehaviour
    {
        private IdeaZooGame _game;
        private CivicWorldArtPass _art;
        private StaffEnsemble _staff;
        private PresentationCameraRig _cameraRig;
        private CivicAudioBed _audio;
        private CaseStage _lastStage = (CaseStage)(-1);
        private string _lastRecord = string.Empty;
        private int _lastEvidenceCount;
        private Transform _worldRoot;

        private IEnumerator Start()
        {
            for (var attempt = 0; attempt < 120; attempt++)
            {
                _game = FindFirstObjectByType<IdeaZooGame>();
                if (_game != null && _game.World != null && _game.Keeper != null && _game.Creature != null) break;
                yield return null;
            }
            if (_game == null) yield break;

            _worldRoot = _game.World.transform;
            _art = _game.World.gameObject.AddComponent<CivicWorldArtPass>();
            _art.Build(_worldRoot);

            _staff = _game.World.gameObject.AddComponent<StaffEnsemble>();
            _staff.Build(_worldRoot, _game.Keeper.transform, _game.Creature.transform);

            var camera = FindFirstObjectByType<Camera>();
            _cameraRig = gameObject.AddComponent<PresentationCameraRig>();
            _cameraRig.Build(camera, _game.Keeper);

            _audio = gameObject.AddComponent<CivicAudioBed>();
            _audio.Build();
        }

        private void Update()
        {
            if (_game == null || _game.Director == null || _cameraRig == null) return;
            var director = _game.Director;
            if (director.Profile != null && director.Profile.RecordId != _lastRecord)
            {
                _lastRecord = director.Profile.RecordId;
                _lastEvidenceCount = 0;
            }

            if (director.Profile != null && director.Profile.Evidence.Count != _lastEvidenceCount)
            {
                _lastEvidenceCount = director.Profile.Evidence.Count;
                _audio.EvidencePulse();
                _staff.SignalAll();
                _cameraRig.Play(PresentationShot.Inspection, _game.Creature.transform, 0.72d);
            }

            if (director.Stage == _lastStage) return;
            _lastStage = director.Stage;
            _audio.Cue(director.Stage);
            StageChanged(director.Stage);
        }

        private void StageChanged(CaseStage stage)
        {
            if (stage == CaseStage.Hatching)
            {
                _cameraRig.Play(PresentationShot.Hatch, _game.Creature.transform, 1.35d);
                _staff.SignalAll();
            }
            else if (stage == CaseStage.Molt)
            {
                _cameraRig.Play(PresentationShot.Molt, _game.Creature.transform, 0.95d);
            }
            else if (stage == CaseStage.Decision)
            {
                var target = _game.World.DecisionRoot != null ? _game.World.DecisionRoot : _game.Creature.transform;
                _cameraRig.Play(PresentationShot.Decision, target, 1.15d);
                _staff.SignalAll();
            }
            else if (stage == CaseStage.Complete)
            {
                _cameraRig.Play(PresentationShot.Ruling, _game.Creature.transform, 1.45d);
                _staff.SignalAll();
            }
        }
    }

    public static class IdeaZooPresentationAutoLoad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPresentation()
        {
            if (UnityEngine.Object.FindFirstObjectByType<IdeaZooPresentationDirector>() != null) return;
            var root = new GameObject("IdeaZoo_Presentation_Director");
            root.AddComponent<IdeaZooPresentationDirector>();
            UnityEngine.Object.DontDestroyOnLoad(root);
        }
    }
}
