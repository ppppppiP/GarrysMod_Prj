using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuildValidation
{
    private const string SessionStageKey = "GarrysMod.WebGLValidation.Stage.v2";
    private const string OutputPath = "C:/Users/artem/Documents/Codex/2026-08-31/d-unity-projects-garrysmod-prj/work/WebGL-optimization-smoke-20260831";

    [MenuItem("Tools/Optimization/Validate WebGL Build")]
    private static void ScheduleFromMenu()
    {
        SessionState.SetInt(SessionStageKey, 0);
        EditorApplication.update -= RunWhenReady;
        EditorApplication.update += RunWhenReady;
    }

    private static void RunWhenReady()
    {
        if (Application.isBatchMode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        int stage = SessionState.GetInt(SessionStageKey, 0);
        if (stage >= 3)
        {
            EditorApplication.update -= RunWhenReady;
            return;
        }

        if (stage == 0)
        {
            SessionState.SetInt(SessionStageKey, 1);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[WebGLValidation] Переключение платформы на WebGL...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                return;
            }
        }

        if (SessionState.GetInt(SessionStageKey, 0) == 1 && EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            SessionState.SetInt(SessionStageKey, 2);
            try
            {
                BuildWebGL();
            }
            finally
            {
                Debug.Log("[WebGLValidation] Возврат платформы на Windows...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            }
            return;
        }

        if (SessionState.GetInt(SessionStageKey, 0) == 2 && EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
        {
            SessionState.SetInt(SessionStageKey, 3);
            EditorApplication.update -= RunWhenReady;
            Debug.Log("[WebGLValidation] Проверка завершена, активная платформа возвращена на Windows.");
        }
    }

    private static void BuildWebGL()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && File.Exists(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("Нет доступных сцен в Build Settings.");

        Directory.CreateDirectory(OutputPath);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport
        };

        Debug.Log($"[WebGLValidation] Сборка {scenes.Length} сцен в {OutputPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        string result =
            $"Result={summary.result}\n" +
            $"Scenes={scenes.Length}\n" +
            $"Errors={summary.totalErrors}\n" +
            $"Warnings={summary.totalWarnings}\n" +
            $"SizeBytes={summary.totalSize}\n" +
            $"Duration={summary.totalTime}\n";

        File.WriteAllText(Path.Combine(OutputPath, "build-validation.txt"), result);
        Debug.Log($"[WebGLValidation] {result.Replace(Environment.NewLine, " | ")}");

        if (summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"WebGL build failed: {summary.result}, errors={summary.totalErrors}");
    }
}
