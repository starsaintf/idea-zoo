#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IdeaZoo.EditorTools
{
    public static class IdeaZooWebGLBuilder
    {
        private const string ReportRelativePath = "../unity-webgl-report.txt";

        public static void Build()
        {
            var reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, ReportRelativePath));
            var outputPath = CommandLineValue("customBuildPath");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/WebGL/IdeaZooWebGL"));

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var log = new StringBuilder(8192);
            log.AppendLine("Idea Zoo WebGL build report");
            log.AppendLine("Unity: " + Application.unityVersion);
            log.AppendLine("Output: " + outputPath);
            log.AppendLine("UTC: " + DateTime.UtcNow.ToString("O"));

            try
            {
                var scenes = EditorBuildSettings.scenes
                    .Where(scene => scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                    .Select(scene => scene.path)
                    .ToArray();

                if (scenes.Length == 0)
                    throw new InvalidOperationException("No enabled scenes exist in EditorBuildSettings.");

                log.AppendLine("Scenes:");
                foreach (var scene in scenes) log.AppendLine("- " + scene);

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                };

                using (WebGLGraphicsProfile.Activate(log))
                {
                    var report = BuildPipeline.BuildPlayer(options);
                    AppendReport(log, report);
                    File.WriteAllText(reportPath, log.ToString());

                    if (report == null || report.summary.result != BuildResult.Succeeded)
                        throw new InvalidOperationException("WebGL build failed. See unity-webgl-report.txt.");
                }
            }
            catch (Exception exception)
            {
                log.AppendLine();
                log.AppendLine("EXCEPTION");
                log.AppendLine(exception.ToString());
                File.WriteAllText(reportPath, log.ToString());
                Debug.LogException(exception);
                throw;
            }
        }

        private static void AppendReport(StringBuilder log, BuildReport report)
        {
            if (report == null)
            {
                log.AppendLine("BuildPipeline returned no BuildReport.");
                return;
            }

            log.AppendLine();
            log.AppendLine("SUMMARY");
            log.AppendLine("Result: " + report.summary.result);
            log.AppendLine("Errors: " + report.summary.totalErrors);
            log.AppendLine("Warnings: " + report.summary.totalWarnings);
            log.AppendLine("Size: " + report.summary.totalSize);
            log.AppendLine("Duration: " + report.summary.totalTime);

            if (report.steps == null) return;
            foreach (var step in report.steps)
            {
                log.AppendLine();
                log.AppendLine("STEP: " + step.name + " · " + step.duration);
                if (step.messages == null) continue;
                foreach (var message in step.messages)
                    log.AppendLine("[" + message.type + "] " + message.content);
            }
        }

        private static string CommandLineValue(string key)
        {
            var expected = "-" + key;
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length - 1; i++)
                if (string.Equals(arguments[i], expected, StringComparison.OrdinalIgnoreCase))
                    return arguments[i + 1];
            return string.Empty;
        }
    }
}
#endif
