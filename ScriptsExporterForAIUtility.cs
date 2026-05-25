using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.PackageManager;
using UnityEngine;

internal static class ScriptsExporterForAIUtility
{
    internal const string Separator = "==============================================================================";

    internal static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }

    internal static string GetProjectRoot()
    {
        DirectoryInfo parent = Directory.GetParent(Application.dataPath);
        return parent != null ? parent.FullName : Application.dataPath;
    }

    internal static string GetProjectName(ScriptsExporterForAISettings settings)
    {
        if (settings != null && !string.IsNullOrWhiteSpace(settings.projectName))
            return settings.projectName.Trim();

        return new DirectoryInfo(GetProjectRoot()).Name;
    }

    internal static bool ShouldExclude(string path, IEnumerable<string> excludedPathKeywords)
    {
        string normalized = NormalizePath(path);

        if (excludedPathKeywords == null)
            return false;

        foreach (string keyword in excludedPathKeywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            string normalizedKeyword = NormalizePath(keyword.Trim());

            if (normalized.IndexOf(normalizedKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    internal static bool IsAllowedExtension(string path, IEnumerable<string> allowedExtensions)
    {
        string extension = Path.GetExtension(path);

        if (string.IsNullOrEmpty(extension) || allowedExtensions == null)
            return false;

        foreach (string allowedExtension in allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(allowedExtension))
                continue;

            string normalizedAllowed = allowedExtension.Trim();
            if (!normalizedAllowed.StartsWith(".", StringComparison.Ordinal))
                normalizedAllowed = "." + normalizedAllowed;

            if (string.Equals(extension, normalizedAllowed, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    internal static IEnumerable<PackageInfo> GetIncludedPackages(IEnumerable<string> packageNames)
    {
        if (packageNames == null)
            yield break;

        HashSet<string> included = new HashSet<string>(
            packageNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
            StringComparer.OrdinalIgnoreCase);

        if (included.Count == 0)
            yield break;

        foreach (PackageInfo packageInfo in PackageInfo.GetAllRegisteredPackages())
        {
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.name))
                continue;

            if (!included.Contains(packageInfo.name))
                continue;

            if (string.IsNullOrEmpty(packageInfo.resolvedPath) || !Directory.Exists(packageInfo.resolvedPath))
                continue;

            yield return packageInfo;
        }
    }

    internal static string GetGitCommitShortHash(bool enabled)
    {
        if (!enabled)
            return "DISABLED";

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                WorkingDirectory = GetProjectRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                if (process == null)
                    return "UNKNOWN";

                string output = process.StandardOutput.ReadToEnd().Trim();

                if (!process.WaitForExit(2000))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Ignore process termination failures.
                    }

                    return "TIMEOUT";
                }

                return string.IsNullOrEmpty(output) ? "UNKNOWN" : output;
            }
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    internal static bool LooksBinary(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return false;

        int checkLength = Math.Min(bytes.Length, 8000);

        for (int i = 0; i < checkLength; i++)
        {
            if (bytes[i] == 0)
                return true;
        }

        return false;
    }

    internal static string TrimTextIfNeeded(string text, int maxFileCharCount, out bool trimmed)
    {
        trimmed = false;

        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (maxFileCharCount <= 0 || text.Length <= maxFileCharCount)
            return text;

        trimmed = true;
        return text.Substring(0, maxFileCharCount) + "\n\n<TRIMMED: file exceeded MaxFileCharCount>";
    }

    internal static void AppendLineNumberedText(StringBuilder sb, string text, bool includeLineNumbers)
    {
        string normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (includeLineNumbers)
                sb.AppendLine($"{(i + 1).ToString("D4")}: {lines[i]}");
            else
                sb.AppendLine(lines[i]);
        }
    }

    internal static void AppendCommonHeader(StringBuilder sb, string exportKind, ScriptsExporterForAISettings settings, int candidateCount)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        sb.AppendLine(Separator);
        sb.AppendLine($"PROJECT: {GetProjectName(settings)}");
        sb.AppendLine($"EXPORT_KIND: {exportKind}");
        sb.AppendLine($"GENERATED_AT: {now:yyyy-MM-dd HH:mm zzz}");
        sb.AppendLine($"UNITY_VERSION: {Application.unityVersion}");
        sb.AppendLine($"GIT_COMMIT: {GetGitCommitShortHash(settings == null || settings.includeGitCommit)}");

        if (candidateCount >= 0)
            sb.AppendLine($"CANDIDATE_FILE_COUNT: {candidateCount}");

        sb.AppendLine(Separator);
    }
}
