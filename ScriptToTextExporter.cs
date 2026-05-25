using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

public class ScriptToTextExporter : EditorWindow
{
    // 제외할 경로 키워드 목록
    private static readonly string[] ExcludedPaths = new[]
    {
        "/Spine/",
        "/External/",
        "/Samples~/",
        "/Documentation~/",
    };

    // 포함할 패키지 이름 목록
    // Package Manager의 name 기준: com.xxx.yyy
    private static readonly string[] IncludedPackageNames = new[]
    {
        "com.shared.command-framework",
        "com.more.core",
    };

    [MenuItem("Tools/Export All C# Scripts to TXT")]
    public static void ExportScripts()
    {
        string outputPath = EditorUtility.SaveFilePanel("Save Scripts as TXT", "", "Project_Scripts_Combined", "txt");
        if (string.IsNullOrEmpty(outputPath)) return;

        List<ScriptFileEntry> scriptFiles = new List<ScriptFileEntry>();

        // 1. Assets 내부 스크립트 수집
        string[] assetScriptPaths = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (string path in assetScriptPaths)
        {
            string normalizedPath = path.Replace('\\', '/');

            if (ShouldExclude(normalizedPath)) continue;

            string relativePath = "Assets" + normalizedPath.Replace(Application.dataPath.Replace('\\', '/'), "");

            scriptFiles.Add(new ScriptFileEntry
            {
                FullPath = normalizedPath,
                DisplayPath = relativePath
            });
        }

        // 2. 특정 Unity Package 스크립트 수집
        foreach (UnityEditor.PackageManager.PackageInfo packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
        {
            if (!IncludedPackageNames.Contains(packageInfo.name)) continue;
            if (string.IsNullOrEmpty(packageInfo.resolvedPath)) continue;
            if (!Directory.Exists(packageInfo.resolvedPath)) continue;

            string packageRoot = packageInfo.resolvedPath.Replace('\\', '/');

            string[] packageScriptPaths = Directory.GetFiles(packageRoot, "*.cs", SearchOption.AllDirectories);

            foreach (string path in packageScriptPaths)
            {
                string normalizedPath = path.Replace('\\', '/');

                if (ShouldExclude(normalizedPath)) continue;

                string relativePath = $"Packages/{packageInfo.name}" + normalizedPath.Replace(packageRoot, "");

                scriptFiles.Add(new ScriptFileEntry
                {
                    FullPath = normalizedPath,
                    DisplayPath = relativePath
                });
            }
        }

        StringBuilder combinedContent = new StringBuilder();
        int fileCount = 0;

        combinedContent.AppendLine("==============================================================================");
        combinedContent.AppendLine("PROJECT: Hemi-Sphere");
        combinedContent.AppendLine("EXPORT_KIND: Scripts Snapshot");
        combinedContent.AppendLine($"GENERATED_AT: {DateTime.Now:yyyy-MM-dd HH:mm} KST");
        combinedContent.AppendLine($"UNITY_VERSION: {Application.unityVersion}");
        combinedContent.AppendLine($"GIT_COMMIT: {GetGitCommitShortHash()}");
        combinedContent.AppendLine("==============================================================================");
        combinedContent.AppendLine("IMPORTANT:\n•\t이 파일보다 오래된 Project_Scripts_Combined.txt는 폐기된 컨텍스트다.\n•\t이전 대화의 코드 구조와 충돌하면 이 파일을 우선한다.");

        foreach (ScriptFileEntry scriptFile in scriptFiles.OrderBy(x => x.DisplayPath))
        {
            string fileName = Path.GetFileName(scriptFile.FullPath);
            string[] lines = File.ReadAllLines(scriptFile.FullPath);

            combinedContent.AppendLine("==============================================================================");
            combinedContent.AppendLine($"FILE: {fileName}");
            combinedContent.AppendLine($"PATH: {scriptFile.DisplayPath}");
            combinedContent.AppendLine("==============================================================================");

            for (int i = 0; i < lines.Length; i++)
            {
                combinedContent.AppendLine($"{(i + 1).ToString("D4")}: {lines[i]}");
            }

            combinedContent.AppendLine("\n\n");
            fileCount++;
        }

        File.WriteAllText(outputPath, combinedContent.ToString(), Encoding.UTF8);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Export Complete", $"{fileCount}개의 스크립트가 성공적으로 추출되었습니다!\n경로: {outputPath}", "OK");
    }

    private static bool ShouldExclude(string normalizedPath)
    {
        return ExcludedPaths.Any(excluded => normalizedPath.Contains(excluded));
    }

    private static string GetGitCommitShortHash()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "-C \"" + Application.dataPath + "/..\" rev-parse --short HEAD",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null) return "<unknown>";

                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return string.IsNullOrEmpty(output) ? "<unknown>" : output;
            }
        }
        catch
        {
            return "<unknown>";
        }
    }

    private class ScriptFileEntry
    {
        public string FullPath;
        public string DisplayPath;
    }
}
