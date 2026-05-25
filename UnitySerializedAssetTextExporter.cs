using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

public class UnitySerializedAssetTextExporter : EditorWindow
{
    private static readonly string[] ExcludedPathKeywords =
    {
        "/Spine/",
        "/External/",
        "/Samples~/",
        "/Documentation~/",
        "/Library/",
        "/Temp/",
        "/Obj/",
        "/Logs/",
        "/UserSettings/",
    };

    private static readonly string[] IncludedPackageNames =
    {
        "com.shared.command-framework",
        "com.more.core",
    };

    private static readonly string[] IncludedExtensions =
    {
        ".unity",
        ".prefab",
        ".asset",
        ".mat",
        ".controller",
        ".overrideController",
        ".playable",
        ".anim",
        ".inputactions",
    };

    private const int MaxFileCharCount = 300_000;

    [MenuItem("Tools/Export Unity Serialized Assets to TXT")]
    public static void Export()
    {
        string outputPath = EditorUtility.SaveFilePanel(
            "Save Unity Serialized Assets as TXT",
            "",
            "Project_UnitySerializedAssets_Combined",
            "txt");

        if (string.IsNullOrEmpty(outputPath))
            return;

        List<SerializedAssetFileEntry> files = new();

        CollectAssetFiles(files);
        CollectIncludedPackageFiles(files);

        files = files
            .Where(x => !ShouldExclude(x.FullPath))
            .Where(x => IncludedExtensions.Contains(Path.GetExtension(x.FullPath), StringComparer.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayPath)
            .ToList();

        StringBuilder sb = new();

        AppendHeader(sb, files.Count);

        int exportedCount = 0;
        int skippedBinaryCount = 0;
        int trimmedCount = 0;

        foreach (SerializedAssetFileEntry file in files)
        {
            try
            {
                if (!File.Exists(file.FullPath))
                    continue;

                byte[] bytes = File.ReadAllBytes(file.FullPath);

                if (LooksBinary(bytes))
                {
                    AppendSkippedBinary(sb, file);
                    skippedBinaryCount++;
                    continue;
                }

                string text = Encoding.UTF8.GetString(bytes);

                bool trimmed = false;
                if (text.Length > MaxFileCharCount)
                {
                    text = text.Substring(0, MaxFileCharCount)
                           + "\n\n<TRIMMED: file exceeded MaxFileCharCount>";
                    trimmed = true;
                    trimmedCount++;
                }

                AppendFile(sb, file, text, trimmed);
                exportedCount++;
            }
            catch (Exception e)
            {
                AppendError(sb, file, e);
            }
        }

        AppendSummary(sb, exportedCount, skippedBinaryCount, trimmedCount);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Export Complete",
            $"Exported: {exportedCount}\nSkipped Binary: {skippedBinaryCount}\nTrimmed: {trimmedCount}\n\n{outputPath}",
            "OK");
    }

    private static void CollectAssetFiles(List<SerializedAssetFileEntry> files)
    {
        string root = Application.dataPath.Replace('\\', '/');

        foreach (string path in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
        {
            string normalized = path.Replace('\\', '/');

            files.Add(new SerializedAssetFileEntry
            {
                FullPath = normalized,
                DisplayPath = "Assets" + normalized.Replace(root, "")
            });
        }
    }

    private static void CollectIncludedPackageFiles(List<SerializedAssetFileEntry> files)
    {
        foreach (UnityEditor.PackageManager.PackageInfo packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
        {
            if (!IncludedPackageNames.Contains(packageInfo.name))
                continue;

            if (string.IsNullOrEmpty(packageInfo.resolvedPath))
                continue;

            if (!Directory.Exists(packageInfo.resolvedPath))
                continue;

            string packageRoot = packageInfo.resolvedPath.Replace('\\', '/');

            foreach (string path in Directory.GetFiles(packageRoot, "*.*", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');

                files.Add(new SerializedAssetFileEntry
                {
                    FullPath = normalized,
                    DisplayPath = $"Packages/{packageInfo.name}" + normalized.Replace(packageRoot, "")
                });
            }
        }
    }

    private static void AppendHeader(StringBuilder sb, int candidateCount)
    {
        sb.AppendLine("==============================================================================");
        sb.AppendLine("PROJECT: Hemi-Sphere");
        sb.AppendLine("EXPORT_KIND: Unity Serialized Assets Snapshot");
        sb.AppendLine($"GENERATED_AT: {DateTime.Now:yyyy-MM-dd HH:mm} KST");
        sb.AppendLine($"UNITY_VERSION: {Application.unityVersion}");
        sb.AppendLine($"GIT_COMMIT: {GetGitCommitShortHash()}");
        sb.AppendLine($"CANDIDATE_FILE_COUNT: {candidateCount}");
        sb.AppendLine("==============================================================================");
        sb.AppendLine("IMPORTANT:");
        sb.AppendLine("• This exporter does NOT open scenes.");
        sb.AppendLine("• This exporter does NOT load prefab contents.");
        sb.AppendLine("• This exporter reads .unity/.prefab/.asset/etc as raw serialized text.");
        sb.AppendLine("• Works best when Unity Project Settings > Editor > Asset Serialization Mode is Force Text.");
        sb.AppendLine("• Binary assets are skipped.");
        sb.AppendLine("==============================================================================");
        sb.AppendLine();
    }

    private static void AppendFile(
        StringBuilder sb,
        SerializedAssetFileEntry file,
        string text,
        bool trimmed)
    {
        string extension = Path.GetExtension(file.FullPath);

        sb.AppendLine("==============================================================================");
        sb.AppendLine($"FILE: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");
        sb.AppendLine($"EXTENSION: {extension}");
        sb.AppendLine($"FULL_PATH: {file.FullPath}");
        sb.AppendLine($"TRIMMED: {trimmed}");
        sb.AppendLine("==============================================================================");

        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            sb.AppendLine($"{(i + 1).ToString("D4")}: {lines[i]}");
        }

        sb.AppendLine();
        sb.AppendLine();
    }

    private static void AppendSkippedBinary(StringBuilder sb, SerializedAssetFileEntry file)
    {
        sb.AppendLine("==============================================================================");
        sb.AppendLine($"SKIPPED_BINARY_FILE: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");
        sb.AppendLine($"FULL_PATH: {file.FullPath}");
        sb.AppendLine("==============================================================================");
        sb.AppendLine();
    }

    private static void AppendError(StringBuilder sb, SerializedAssetFileEntry file, Exception e)
    {
        sb.AppendLine("==============================================================================");
        sb.AppendLine($"EXPORT_ERROR: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");
        sb.AppendLine($"FULL_PATH: {file.FullPath}");
        sb.AppendLine(e.ToString());
        sb.AppendLine("==============================================================================");
        sb.AppendLine();
    }

    private static void AppendSummary(
        StringBuilder sb,
        int exportedCount,
        int skippedBinaryCount,
        int trimmedCount)
    {
        sb.AppendLine("==============================================================================");
        sb.AppendLine("SUMMARY");
        sb.AppendLine($"EXPORTED_COUNT: {exportedCount}");
        sb.AppendLine($"SKIPPED_BINARY_COUNT: {skippedBinaryCount}");
        sb.AppendLine($"TRIMMED_COUNT: {trimmedCount}");
        sb.AppendLine("==============================================================================");
    }

    private static bool ShouldExclude(string path)
    {
        string normalized = path.Replace('\\', '/');
        return ExcludedPathKeywords.Any(excluded => normalized.Contains(excluded));
    }

    private static bool LooksBinary(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return false;

        int checkLength = Math.Min(bytes.Length, 8000);

        for (int i = 0; i < checkLength; i++)
        {
            byte b = bytes[i];

            if (b == 0)
                return true;
        }

        return false;
    }

    private static string GetGitCommitShortHash()
    {
        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

            ProcessStartInfo psi = new()
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process process = Process.Start(psi);
            if (process == null)
                return "UNKNOWN";

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(2000);

            return string.IsNullOrEmpty(output) ? "UNKNOWN" : output;
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    private sealed class SerializedAssetFileEntry
    {
        public string FullPath;
        public string DisplayPath;
    }
}
