using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdeaZoo.Core;
using UnityEngine;

namespace IdeaZoo.Runtime
{
    [Serializable]
    public sealed class SpecimenArchiveEnvelope
    {
        public List<SpecimenRecord> Records = new List<SpecimenRecord>();
    }

    [Serializable]
    public sealed class SpecimenRecord
    {
        public string RecordId;
        public string SavedAtUtc;
        public string Title;
        public string PlainIdea;
        public string Problem;
        public string Promise;
        public string Audience;
        public string Payer;
        public string CreatureName;
        public string Class;
        public string BoardClass;
        public string Appetite;
        public string HiddenBurden;
        public string FinalRuling;
        public string VerdictReason;
        public float Desirability;
        public float Feasibility;
        public float Viability;
        public float Safety;
        public float Evidence;
        public string[] Guardrails;
        public string[] NextActions;
        public EvidenceSnapshot[] EvidenceRecords;
        public MoltSnapshot[] Revisions;
    }

    [Serializable]
    public sealed class EvidenceSnapshot
    {
        public string TestId;
        public int Strength;
        public string Note;
        public string RecordedAtUtc;
    }

    [Serializable]
    public sealed class MoltSnapshot
    {
        public string PreviousPromise;
        public string RevisedPromise;
        public string PreviousAudience;
        public string RevisedAudience;
        public string[] Guardrails;
        public string RecordedAtUtc;
    }

    public sealed class SpecimenArchive
    {
        public string LastBackupPath { get; private set; } = string.Empty;
        public string LastCorruptPath { get; private set; } = string.Empty;
        public string PathName { get { return Path.Combine(Application.persistentDataPath, "idea-zoo-specimens.json"); } }

        public bool Save(IdeaProfile profile, out string error)
        {
            error = string.Empty;
            LastBackupPath = string.Empty;
            LastCorruptPath = string.Empty;
            try
            {
                var envelope = LoadRecovering();
                envelope.Records.Add(ToRecord(profile));
                var json = JsonUtility.ToJson(envelope, true);
                var target = PathName;
                var temporary = target + ".tmp";
                var backup = target + ".backup";

                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(temporary, json);
                if (File.Exists(target))
                {
                    File.Copy(target, backup, true);
                    LastBackupPath = backup;
                    File.Delete(target);
                }
                File.Move(temporary, target);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public SpecimenArchiveEnvelope LoadRecovering()
        {
            if (!File.Exists(PathName)) return new SpecimenArchiveEnvelope();
            try
            {
                var raw = File.ReadAllText(PathName);
                if (string.IsNullOrWhiteSpace(raw)) return new SpecimenArchiveEnvelope();
                var envelope = JsonUtility.FromJson<SpecimenArchiveEnvelope>(raw);
                if (envelope == null || envelope.Records == null) throw new InvalidDataException("The archive root is invalid.");
                return envelope;
            }
            catch
            {
                var corrupt = PathName + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                File.Copy(PathName, corrupt, true);
                LastCorruptPath = corrupt;
                return new SpecimenArchiveEnvelope();
            }
        }

        private static SpecimenRecord ToRecord(IdeaProfile profile)
        {
            return new SpecimenRecord
            {
                RecordId = profile.RecordId,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                Title = profile.Title,
                PlainIdea = profile.PlainIdea,
                Problem = profile.Problem,
                Promise = profile.Promise,
                Audience = profile.Audience,
                Payer = profile.Payer,
                CreatureName = profile.CreatureName,
                Class = profile.Class.ToString(),
                BoardClass = profile.BoardClass.ToString(),
                Appetite = profile.Appetite.ToString(),
                HiddenBurden = profile.HiddenBurden,
                FinalRuling = profile.FinalRuling.HasValue ? profile.FinalRuling.Value.ToString() : string.Empty,
                VerdictReason = profile.VerdictReason,
                Desirability = (float)profile.Metrics.Desirability,
                Feasibility = (float)profile.Metrics.Feasibility,
                Viability = (float)profile.Metrics.Viability,
                Safety = (float)profile.Metrics.Safety,
                Evidence = (float)profile.Metrics.Evidence,
                Guardrails = profile.Guardrails.ToArray(),
                NextActions = profile.NextActions.ToArray(),
                EvidenceRecords = profile.Evidence.Select(record => new EvidenceSnapshot
                {
                    TestId = record.TestId,
                    Strength = record.Strength,
                    Note = record.Note,
                    RecordedAtUtc = record.RecordedAtUtc.ToString("O")
                }).ToArray(),
                Revisions = profile.Revisions.Select(revision => new MoltSnapshot
                {
                    PreviousPromise = revision.PreviousPromise,
                    RevisedPromise = revision.RevisedPromise,
                    PreviousAudience = revision.PreviousAudience,
                    RevisedAudience = revision.RevisedAudience,
                    Guardrails = revision.Guardrails.ToArray(),
                    RecordedAtUtc = revision.RecordedAtUtc.ToString("O")
                }).ToArray()
            };
        }
    }
}
