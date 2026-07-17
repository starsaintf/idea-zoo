#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace IdeaZoo.EditorTools
{
    internal static class WebGLGraphicsProfile
    {
        private const string LegacyLightProbeSystem = "LegacyLightProbes";
        private static bool _active;

        public static bool IsActive => _active;

        public static IDisposable Activate(StringBuilder log)
        {
            if (_active) throw new InvalidOperationException("The WebGL graphics profile is already active.");

            var modifiedAssets = new List<PipelineAssetSetting>();
            foreach (var asset in FindPipelineAssets())
            {
                var settings = new SerializedObject(asset);
                var lightProbeSystem = settings.FindProperty("m_LightProbeSystem");
                if (lightProbeSystem == null || lightProbeSystem.propertyType != SerializedPropertyType.Enum)
                    continue;

                var legacyIndex = Array.IndexOf(lightProbeSystem.enumNames, LegacyLightProbeSystem);
                if (legacyIndex < 0)
                    throw new InvalidOperationException("The active URP asset does not expose LegacyLightProbes.");
                if (lightProbeSystem.enumValueIndex == legacyIndex)
                    continue;

                modifiedAssets.Add(new PipelineAssetSetting(asset, lightProbeSystem.enumValueIndex));
                lightProbeSystem.enumValueIndex = legacyIndex;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            _active = true;
            log.AppendLine("WebGL graphics profile: baked lighting, reflection probes, legacy light probes, no adaptive probe volumes.");
            log.AppendLine("WebGL URP assets switched to legacy light probes: " + modifiedAssets.Count);
            return new Scope(modifiedAssets);
        }

        private static IEnumerable<ScriptableObject> FindPipelineAssets()
        {
            return AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                .Where(asset => asset != null)
                .Distinct();
        }

        private sealed class Scope : IDisposable
        {
            private readonly List<PipelineAssetSetting> _modifiedAssets;
            private bool _disposed;

            public Scope(List<PipelineAssetSetting> modifiedAssets)
            {
                _modifiedAssets = modifiedAssets;
            }

            public void Dispose()
            {
                if (_disposed) return;

                for (var index = _modifiedAssets.Count - 1; index >= 0; index--)
                {
                    var setting = _modifiedAssets[index];
                    var serializedAsset = new SerializedObject(setting.Asset);
                    var lightProbeSystem = serializedAsset.FindProperty("m_LightProbeSystem");
                    if (lightProbeSystem == null || lightProbeSystem.propertyType != SerializedPropertyType.Enum)
                        continue;

                    lightProbeSystem.enumValueIndex = setting.Value;
                    serializedAsset.ApplyModifiedPropertiesWithoutUndo();
                }

                _active = false;
                _disposed = true;
            }
        }

        private readonly struct PipelineAssetSetting
        {
            public PipelineAssetSetting(ScriptableObject asset, int value)
            {
                Asset = asset;
                Value = value;
            }

            public ScriptableObject Asset { get; }
            public int Value { get; }
        }
    }

    internal sealed class WebGLProbeVolumeShaderStripper : IPreprocessShaders, IPreprocessComputeShaders
    {
        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (WebGLGraphicsProfile.IsActive && IsProbeVolumeShader(shader == null ? string.Empty : shader.name))
                data.Clear();
        }

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            if (WebGLGraphicsProfile.IsActive && IsProbeVolumeShader(shader == null ? string.Empty : shader.name))
                data.Clear();
        }

        private static bool IsProbeVolumeShader(string shaderName)
        {
            return shaderName.IndexOf("ProbeVolume", StringComparison.OrdinalIgnoreCase) >= 0
                || shaderName.IndexOf("Probe Volume", StringComparison.OrdinalIgnoreCase) >= 0
                || shaderName.IndexOf("VoxelizeScene", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
#endif
