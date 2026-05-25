using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

public sealed class ScriptToTextExporter : EditorWindow
{
    [MenuItem("Tools/Scripts Exporter For AI/Export All C# Scripts to TXT")]
    public static void ExportScripts()
    {
        string outputPath = EditorUtility.SaveFilePanel(
            "Save Scripts as TXT",
            string.Empty,
            "Project_Scripts_Combined",
            "txt");

        if (string.IsNullOrEmpty(outputPath))
            return;

        ScriptsExporterForAISettings settings = ScriptsExporterForAISettings.instance;
        settings.EnsureDefaults();

        List<ScriptFileEntry> scriptFiles = CollectScriptFiles(settings)
            .OrderBy(x => x.DisplayPath)
            .ToList();

        StringBuilder sb = new StringBuilder();
        ScriptsExporterForAIUtility.AppendCommonHeader(sb, "Scripts Snapshot", settings, scriptFiles.Count);
        sb.AppendLine("IMPORTANT:");
        sb.AppendLine("Treat older Project_Scripts_Combined.txt files as stale context.");
        sb.AppendLine("If this snapshot conflicts with older conversation context, prefer this snapshot.");
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine();

        int exportedCount = 0;
        int errorCount = 0;
        int trimmedCount = 0;

        foreach (ScriptFileEntry scriptFile in scriptFiles)
        {
            try
            {
                if (!File.Exists(scriptFile.FullPath))
                    continue;

                string text = File.ReadAllText(scriptFile.FullPath, Encoding.UTF8);
                bool trimmed;
                text = ScriptsExporterForAIUtility.TrimTextIfNeeded(text, settings.maxFileCharCount, out trimmed);

                AppendFile(sb, scriptFile, text, trimmed, settings);
                exportedCount++;

                if (trimmed)
                    trimmedCount++;
            }
            catch (Exception e)
            {
                AppendError(sb, scriptFile, e, settings);
                errorCount++;
            }
        }

        AppendSummary(sb, exportedCount, trimmedCount, errorCount);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Export Complete",
            $"Exported: {exportedCount}\nTrimmed: {trimmedCount}\nErrors: {errorCount}\n\n{outputPath}",
            "OK");
    }

    private static IEnumerable<ScriptFileEntry> CollectScriptFiles(ScriptsExporterForAISettings settings)
    {
        string assetRoot = ScriptsExporterForAIUtility.NormalizePath(Application.dataPath);

        foreach (string path in Directory.GetFiles(assetRoot, "*.cs", SearchOption.AllDirectories))
        {
            string normalized = ScriptsExporterForAIUtility.NormalizePath(path);
            string displayPath = "Assets" + normalized.Replace(assetRoot, string.Empty);

            if (ScriptsExporterForAIUtility.ShouldExclude(displayPath, settings.excludedPathKeywords))
                continue;

            yield return new ScriptFileEntry
            {
                FullPath = normalized,
                DisplayPath = displayPath
            };
        }

        foreach (PackageInfo packageInfo in ScriptsExporterForAIUtility.GetIncludedPackages(settings.includedPackageNames))
        {
            string packageRoot = ScriptsExporterForAIUtility.NormalizePath(packageInfo.resolvedPath);

            foreach (string path in Directory.GetFiles(packageRoot, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = ScriptsExporterForAIUtility.NormalizePath(path);
                string displayPath = $"Packages/{packageInfo.name}" + normalized.Replace(packageRoot, string.Empty);

                if (ScriptsExporterForAIUtility.ShouldExclude(displayPath, settings.excludedPathKeywords))
                    continue;

                yield return new ScriptFileEntry
                {
                    FullPath = normalized,
                    DisplayPath = displayPath
                };
            }
        }
    }

    private static void AppendFile(
        StringBuilder sb,
        ScriptFileEntry file,
        string text,
        bool trimmed,
        ScriptsExporterForAISettings settings)
    {
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine($"FILE: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");

        if (settings.includeFullPath)
            sb.AppendLine($"FULL_PATH: {file.FullPath}");

        sb.AppendLine($"TRIMMED: {trimmed}");
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);

        ScriptsExporterForAIUtility.AppendLineNumberedText(sb, text, settings.includeLineNumbers);

        sb.AppendLine();
        sb.AppendLine();
    }

    private static void AppendError(
        StringBuilder sb,
        ScriptFileEntry file,
        Exception exception,
        ScriptsExporterForAISettings settings)
    {
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine($"EXPORT_ERROR: {Path.GetFileName(file.FullPath)}");
        sb.AppendLine($"PATH: {file.DisplayPath}");

        if (settings.includeFullPath)
            sb.AppendLine($"FULL_PATH: {file.FullPath}");

        sb.AppendLine(exception.ToString());
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine();
    }

    private static void AppendSummary(StringBuilder sb, int exportedCount, int trimmedCount, int errorCount)
    {
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
        sb.AppendLine("SUMMARY");
        sb.AppendLine($"EXPORTED_COUNT: {exportedCount}");
        sb.AppendLine($"TRIMMED_COUNT: {trimmedCount}");
        sb.AppendLine($"ERROR_COUNT: {errorCount}");
        sb.AppendLine(ScriptsExporterForAIUtility.Separator);
    }

    private sealed class ScriptFileEntry
    {
        public string FullPath;
        public string DisplayPath;
    }
}
