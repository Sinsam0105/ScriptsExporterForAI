using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

public sealed class UnitySerializedAssetTextExporter : EditorWindow
{
    [MenuItem("Tools/Scripts Exporter For AI/Export Unity Serialized Assets to TXT")]
    public static void Export()
    {
        string outputPath = EditorUtility.SaveFilePanel(
            "Save Unity Serialized Assets as TXT",
            string.Empty,
            "Project_UnitySerializedAssets_Combined",
            "txt");

        if (string.IsNullOrEmpty(outputPath))
            return;

        ScriptsExporterForAISettings settings = ScriptsExporterForAISettings.instance;
        settings.EnsureDefaults();

        List<SerializedAssetFileEntry> files = CollectAssetFiles(settings)
            .Where(x => !ScriptsExporterForAIUtility.ShouldExclude(x.DisplayPath, settings.excludedPathKeywords))
            .Where(x => ScriptsExporterForAIUtility.IsAllowedExtension(x.FullPath, settings.serializedAssetExtensions))
            .OrderBy(x => x.DisplayPath)
            .ToList();

        StringBuilder sb = new StringBuilder();

        ScriptsExporterForAIUtility.AppendCommonHeader(sb, "Unity Serialized Assets Snapshot", settings, files.Count);
        sb.AppendLine("IMPORTANT:");
        sb.AppendLine("This exporter does NOT open scenes.");
        sb.AppendLine("This exporter does NOT load prefab contents.");
        sb.AppendLine("This exporter reads .unity/.prefab/.asset/etc as raw serialized text.");
        sb.AppendLine("Works best when Unity Project Settings > Editor > Asset Serialization Mode is Force Text.");
        sb.AppendLine("Binary assets are skipped.");
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine();

        int exportedCount = 0;
        int skippedBinaryCount = 0;
        int trimmedCount = 0;
        int errorCount = 0;

        foreach (SerializedAssetFileEntry file in files)
        {
            try
            {
                if (!File.Exists(file.FullPath))
                    continue;

                byte[] bytes = File.ReadAllBytes(file.FullPath);

                if (ScriptsExporterForAIUtility.LooksBinary(bytes))
                {
                    AppendSkippedBinary(sb, file, settings);
                    skippedBinaryCount++;
                    continue;
                }

                string text = Encoding.UTF8.GetString(bytes);
                bool trimmed;
                text = ScriptsExporterForAIUtility.TrimTextIfNeeded(text, settings.maxFileCharCount, out trimmed);

                AppendFile(sb, file, text, trimmed, settings);
                exportedCount++;

                if (trimmed)
                    trimmedCount++;
            }
            catch (Exception e)
            {
                AppendError(sb, file, e, settings);
                errorCount++;
            }
        }

        AppendSummary(sb, exportedCount, skippedBinaryCount, trimmedCount, errorCount);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Export Complete",
            $"Exported: {exportedCount}\nSkipped Binary: {skippedBinaryCount}\nTrimmed: {trimmedCount}\nErrors: {errorCount}\n\n{outputPath}",
            "OK");
    }

    private static IEnumerable<SerializedAssetFileEntry> CollectAssetFiles(ScriptsExporterForAISettings settings)
    {
        string root = ScriptsExporterForAIUtility.NormalizePath(Application.dataPath);

        foreach (string path in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
        {
            string normalized = ScriptsExporterForAIUtility.NormalizePath(path);
            string displayPath = "Assets" + normalized.Replace(root, string.Empty);

            yield return new SerializedAssetFileEntry
            {
                FullPath = normalized,
                DisplayPath = displayPath
            };
        }

        foreach (PackageInfo packageInfo in ScriptsExporterForAIUtility.GetIncludedPackages(settings.includedPackageNames))
        {
            string packageRoot = ScriptsExporterForAIUtility.NormalizePath(packageInfo.resolvedPath);

            foreach (string path in Directory.GetFiles(packageRoot, "*.*", SearchOption.AllDirectories))
            {
                string normalized = ScriptsExporterForAIUtility.NormalizePath(path);
                string displayPath = $"Packages/{packageInfo.name}" + normalized.Replace(packageRoot, string.Empty);

                yield return new SerializedAssetFileEntry
                {
                    FullPath = normalized,
                    DisplayPath = displayPath
                };
            }
        }
    }

    private static void AppendFile(
        StringBuilder sb,
        SerializedAssetFileEntry file,
        string text,
        bool trimmed,
        ScriptsExporterForAISettings settings)
    {
        string extension = Path.GetExtension(file.FullPath);

        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine($"FILE: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");
        sb.AppendLine($"EXTENSION: {extension}");

        if (settings.includeFullPath)
            sb.AppendLine($"FULL_PATH: {file.FullPath}");

        sb.AppendLine($"TRIMMED: {trimmed}");
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);

        ScriptsExporterForAIUtility.AppendLineNumberedText(sb, text, settings.includeLineNumbers);

        sb.AppendLine();
        sb.AppendLine();
    }

    private static void AppendSkippedBinary(
        StringBuilder sb,
        SerializedAssetFileEntry file,
        ScriptsExporterForAISettings settings)
    {
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine($"SKIPPED_BINARY_FILE: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");

        if (settings.includeFullPath)
            sb.AppendLine($"FULL_PATH: {file.FullPath}");

        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine();
    }

    private static void AppendError(
        StringBuilder sb,
        SerializedAssetFileEntry file,
        Exception e,
        ScriptsExporterForAISettings settings)
    {
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine($"EXPORT_ERROR: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");

        if (settings.includeFullPath)
            sb.AppendLine($"FULL_PATH: {file.FullPath}");

        sb.AppendLine(e.ToString());
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine();
    }

    private static void AppendSummary(
        StringBuilder sb,
        int exportedCount,
        int skippedBinaryCount,
        int trimmedCount,
        int errorCount)
    {
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine("SUMMARY");
        sb.AppendLine($"EXPORTED_COUNT: {exportedCount}");
        sb.AppendLine($"SKIPPED_BINARY_COUNT: {skippedBinaryCount}");
        sb.AppendLine($"TRIMMED_COUNT: {trimmedCount}");
        sb.AppendLine($"ERROR_COUNT: {errorCount}");
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
    }

    private sealed class SerializedAssetFileEntry
    {
        public string FullPath;
        public string DisplayPath;
    }
}
