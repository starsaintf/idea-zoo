using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;
using UnityEngine.Networking;

namespace IdeaZoo.Intelligence
{
    [Serializable]
    internal sealed class EvidenceArtifactCollection { public List<EvidenceArtifact> Items = new List<EvidenceArtifact>(); }
    [Serializable]
    internal sealed class IdeaVersionCollection { public List<IdeaVersionSnapshot> Items = new List<IdeaVersionSnapshot>(); }

    public sealed class EvidenceVault
    {
        private readonly string _root;
        private readonly string _index;
        private readonly List<EvidenceArtifact> _artifacts = new List<EvidenceArtifact>();
        public IReadOnlyList<EvidenceArtifact> Artifacts { get { return _artifacts; } }

        public EvidenceVault()
        {
            _root = Path.Combine(Application.persistentDataPath, "idea-zoo-evidence");
            _index = Path.Combine(_root, "evidence-index.json");
            Directory.CreateDirectory(_root);
            Load();
        }

        public EvidenceArtifact AddText(EvidenceArtifactKind kind, string title, string summary, string source, bool verified)
        {
            if (string.IsNullOrWhiteSpace(summary)) throw new ArgumentException("Evidence needs a summary.");
            var clean = summary.Trim();
            var artifact = new EvidenceArtifact
            {
                ArtifactId = "artifact-" + Guid.NewGuid().ToString("N"),
                Kind = kind,
                Title = string.IsNullOrWhiteSpace(title) ? kind.ToString() : title.Trim(),
                Summary = clean,
                Source = source == null ? string.Empty : source.Trim(),
                ContentHash = IdeaIntelligenceHash.Sha256(clean + "|" + source),
                IndependentlyVerified = verified,
                RecordedAtUtc = DateTime.UtcNow
            };
            _artifacts.Add(artifact);
            Persist();
            return artifact;
        }

        public EvidenceArtifact ImportPrivateFile(EvidenceArtifactKind kind, string sourcePath, string title, string summary, bool verified)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) throw new FileNotFoundException("Evidence file was not found.", sourcePath);
            var info = new FileInfo(sourcePath);
            if (info.Length > 20 * 1024 * 1024) throw new InvalidOperationException("Evidence files are limited to 20 MB in the local vault.");
            var extension = Path.GetExtension(info.Name).ToLowerInvariant();
            var allowed = new[] { ".txt", ".md", ".json", ".csv", ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".wav", ".m4a" };
            if (!allowed.Contains(extension)) throw new InvalidOperationException("This evidence file type is not accepted by the private vault.");
            var bytes = File.ReadAllBytes(sourcePath);
            string contentHash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                contentHash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
            var id = "artifact-" + Guid.NewGuid().ToString("N");
            var destination = Path.Combine(_root, id + extension);
            File.WriteAllBytes(destination, bytes);
            var artifact = new EvidenceArtifact
            {
                ArtifactId = id,
                Kind = kind,
                Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(info.Name) : title.Trim(),
                Summary = summary == null ? string.Empty : summary.Trim(),
                Source = destination,
                ContentHash = contentHash,
                IndependentlyVerified = verified,
                RecordedAtUtc = DateTime.UtcNow
            };
            _artifacts.Add(artifact);
            Persist();
            return artifact;
        }

        public bool Remove(string artifactId)
        {
            var artifact = _artifacts.FirstOrDefault(item => item.ArtifactId == artifactId);
            if (artifact == null) return false;
            if (!string.IsNullOrWhiteSpace(artifact.Source) && artifact.Source.StartsWith(_root, StringComparison.Ordinal) && File.Exists(artifact.Source)) File.Delete(artifact.Source);
            _artifacts.Remove(artifact);
            Persist();
            return true;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_index)) return;
                var collection = JsonUtility.FromJson<EvidenceArtifactCollection>(File.ReadAllText(_index));
                if (collection != null && collection.Items != null) _artifacts.AddRange(collection.Items);
            }
            catch
            {
                try { File.Move(_index, _index + ".corrupt-" + DateTime.UtcNow.Ticks); } catch { }
            }
        }

        private void Persist()
        {
            var temporary = _index + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(new EvidenceArtifactCollection { Items = _artifacts }, true));
            if (File.Exists(_index)) File.Copy(_index, _index + ".backup", true);
            if (File.Exists(_index)) File.Delete(_index);
            File.Move(temporary, _index);
        }
    }

    public sealed class IdeaVersionHistory
    {
        private readonly string _path;
        private readonly List<IdeaVersionSnapshot> _versions = new List<IdeaVersionSnapshot>();
        public IReadOnlyList<IdeaVersionSnapshot> Versions { get { return _versions; } }

        public IdeaVersionHistory(string recordId)
        {
            var safe = string.IsNullOrWhiteSpace(recordId) ? "unassigned" : recordId.Replace("/", "-").Replace("\\", "-");
            _path = Path.Combine(Application.persistentDataPath, "idea-zoo-versions-" + safe + ".json");
            Load();
        }

        public IdeaVersionSnapshot Capture(IdeaProfile profile, string reason)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            var previous = _versions.LastOrDefault();
            if (previous != null && previous.Promise == profile.Promise && previous.Audience == profile.Audience && previous.Guardrails.SequenceEqual(profile.Guardrails)) return previous;
            var version = new IdeaVersionSnapshot
            {
                VersionId = "version-" + (_versions.Count + 1).ToString("D3") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Promise = profile.Promise,
                Audience = profile.Audience,
                Guardrails = new List<string>(profile.Guardrails),
                ChangeReason = string.IsNullOrWhiteSpace(reason) ? "Captured from the living specimen." : reason.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };
            _versions.Add(version);
            Persist();
            return version;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                var collection = JsonUtility.FromJson<IdeaVersionCollection>(File.ReadAllText(_path));
                if (collection != null && collection.Items != null) _versions.AddRange(collection.Items);
            }
            catch { try { File.Move(_path, _path + ".corrupt-" + DateTime.UtcNow.Ticks); } catch { } }
        }

        private void Persist()
        {
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(new IdeaVersionCollection { Items = _versions }, true));
            if (File.Exists(_path)) File.Copy(_path, _path + ".backup", true);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporary, _path);
        }
    }

    [Serializable]
    internal sealed class RemoteIntelligenceRequest
    {
        public IdeaProfile Profile;
        public List<EvidenceArtifact> Evidence = new List<EvidenceArtifact>();
        public List<IdeaVersionSnapshot> Versions = new List<IdeaVersionSnapshot>();
        public bool IncludeRawSources;
    }

    public sealed class OptionalRemoteIntelligenceClient
    {
        public string Endpoint { get; private set; }
        private string _sessionToken = string.Empty;
        public bool ExplicitlyEnabled { get; private set; }

        public void Configure(string endpoint, string sessionToken, bool enabled)
        {
            Endpoint = endpoint == null ? string.Empty : endpoint.Trim();
            _sessionToken = sessionToken ?? string.Empty;
            ExplicitlyEnabled = enabled && Uri.IsWellFormedUriString(Endpoint, UriKind.Absolute);
        }

        public IEnumerator Analyze(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> evidence, IReadOnlyList<IdeaVersionSnapshot> versions, Action<IdeaIntelligenceReport> completed, Action<string> failed)
        {
            if (!ExplicitlyEnabled) { failed?.Invoke("Remote intelligence is disabled. The private local reasoner remains active."); yield break; }
            var requestBody = new RemoteIntelligenceRequest
            {
                Profile = profile,
                Evidence = evidence.Select(item => new EvidenceArtifact
                {
                    ArtifactId = item.ArtifactId,
                    Kind = item.Kind,
                    Title = item.Title,
                    Summary = item.Summary,
                    Source = string.Empty,
                    ContentHash = item.ContentHash,
                    IndependentlyVerified = item.IndependentlyVerified,
                    RecordedAtUtc = item.RecordedAtUtc
                }).ToList(),
                Versions = versions.ToList(),
                IncludeRawSources = false
            };
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody));
            using (var request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrWhiteSpace(_sessionToken)) request.SetRequestHeader("Authorization", "Bearer " + _sessionToken);
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { failed?.Invoke("Remote intelligence failed: " + request.error); yield break; }
                IdeaIntelligenceReport report = null;
                try { report = JsonUtility.FromJson<IdeaIntelligenceReport>(request.downloadHandler.text); }
                catch (Exception exception) { failed?.Invoke("Remote response was invalid: " + exception.Message); yield break; }
                if (report == null) { failed?.Invoke("Remote response contained no report."); yield break; }
                completed?.Invoke(report);
            }
        }
    }

    public interface IVoiceIdeaTranscriber
    {
        string ProviderName { get; }
        IEnumerator Transcribe(AudioClip clip, Action<string> completed, Action<string> failed);
    }

    [DisallowMultipleComponent]
    public sealed class VoiceIdeaCapture : MonoBehaviour
    {
        public bool Recording { get; private set; }
        public event Action<AudioClip> RecordingCompleted;
        private AudioClip _clip;
        private string _device;

        public bool Begin(int maximumSeconds = 90)
        {
            if (Recording || Microphone.devices.Length == 0) return false;
            _device = Microphone.devices[0];
            _clip = Microphone.Start(_device, false, Mathf.Clamp(maximumSeconds, 5, 180), 16000);
            Recording = _clip != null;
            return Recording;
        }

        public AudioClip End()
        {
            if (!Recording) return null;
            var position = Microphone.GetPosition(_device);
            Microphone.End(_device);
            Recording = false;
            if (_clip == null || position <= 0) return null;
            RecordingCompleted?.Invoke(_clip);
            return _clip;
        }
    }

    [DisallowMultipleComponent]
    public sealed class IdeaIntelligenceRuntimeDirector : MonoBehaviour
    {
        public IdeaIntelligenceReport LastReport { get; private set; }
        public EvidenceVault Vault { get; private set; }
        public IdeaVersionHistory Versions { get; private set; }
        public event Action<IdeaIntelligenceReport> ReportChanged;

        private readonly IIdeaIntelligenceProvider _local = new LocalIdeaIntelligenceProvider();
        private IdeaZooGame _game;
        private IdeaProfile _profile;
        private int _evidenceCount = -1;
        private int _revisionCount = -1;

        private IEnumerator Start()
        {
            Vault = new EvidenceVault();
            for (var frame = 0; frame < 480; frame++)
            {
                _game = FindFirstObjectByType<IdeaZooGame>();
                if (_game != null) yield break;
                yield return null;
            }
        }

        private void Update()
        {
            if (_game == null || _game.Director == null || _game.Director.Profile == null) return;
            var current = _game.Director.Profile;
            if (!ReferenceEquals(_profile, current))
            {
                _profile = current;
                Versions = new IdeaVersionHistory(_profile.RecordId);
                Versions.Capture(_profile, "The idea entered the Whisper Gate.");
                Regenerate();
                return;
            }
            if (_profile.Evidence.Count != _evidenceCount || _profile.Revisions.Count != _revisionCount)
            {
                if (_profile.Revisions.Count != _revisionCount) Versions.Capture(_profile, "The creature completed a Molt.");
                Regenerate();
            }
        }

        public IdeaIntelligenceReport Regenerate()
        {
            if (_profile == null || Vault == null || Versions == null) return null;
            LastReport = _local.Analyze(_profile, Vault.Artifacts, Versions.Versions);
            _evidenceCount = _profile.Evidence.Count;
            _revisionCount = _profile.Revisions.Count;
            Export(LastReport);
            ReportChanged?.Invoke(LastReport);
            return LastReport;
        }

        public string Export(IdeaIntelligenceReport report)
        {
            if (report == null) return string.Empty;
            var folder = Path.Combine(Application.persistentDataPath, "idea-zoo-reports");
            Directory.CreateDirectory(folder);
            var safeId = string.IsNullOrWhiteSpace(report.RecordId) ? report.ReportId : report.RecordId;
            var jsonPath = Path.Combine(folder, safeId + "-intelligence.json");
            var markdownPath = Path.Combine(folder, safeId + "-decision-record.md");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(markdownPath, ToMarkdown(report));
            return markdownPath;
        }

        private static string ToMarkdown(IdeaIntelligenceReport report)
        {
            var text = new StringBuilder();
            text.AppendLine("# Idea Zoo decision record");
            text.AppendLine();
            text.AppendLine("## Thesis"); text.AppendLine(report.PlainThesis); text.AppendLine();
            text.AppendLine("## Strongest case for"); text.AppendLine(report.StrongestCaseFor); text.AppendLine();
            text.AppendLine("## Strongest case against"); text.AppendLine(report.StrongestCaseAgainst); text.AppendLine();
            text.AppendLine("## Buyer and user"); text.AppendLine(report.BuyerUserGap); text.AppendLine();
            text.AppendLine("## Suggested ruling"); text.AppendLine(report.SuggestedRuling + " — " + report.Recommendation); text.AppendLine();
            text.AppendLine("## Assumptions that could kill it");
            foreach (var challenge in report.Challenges) text.AppendLine("- **" + challenge.Assumption + "** Failure signal: " + challenge.FailureSignal);
            text.AppendLine(); text.AppendLine("## Highest-information experiments");
            foreach (var experiment in report.Experiments.Take(5)) text.AppendLine("- **" + experiment.Title + "** — " + experiment.Method + " Pass: " + experiment.PassCondition);
            text.AppendLine(); text.AppendLine("## Uncertainties");
            foreach (var uncertainty in report.Uncertainties) text.AppendLine("- " + uncertainty);
            text.AppendLine(); text.AppendLine("The intelligence layer advises. The Keeper issues the ruling.");
            return text.ToString();
        }
    }

    public static class IdeaIntelligenceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<IdeaIntelligenceRuntimeDirector>() != null) return;
            var root = new GameObject("IdeaZoo_Intelligence");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.AddComponent<IdeaIntelligenceRuntimeDirector>();
            root.AddComponent<VoiceIdeaCapture>();
        }
    }
}
