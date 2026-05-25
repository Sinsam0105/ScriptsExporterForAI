using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[FilePath("ProjectSettings/ScriptsExporterForAISettings.asset", FilePathAttribute.Location.ProjectFolder)]
internal sealed class ScriptsExporterForAISettings : ScriptableSingleton<ScriptsExporterForAISettings>
{
    [Tooltip("Displayed in the export header. Leave empty to use the Unity project folder name.")]
    public string projectName = string.Empty;

    [Tooltip("Path keywords to exclude from script and serialized asset exports.")]
    public List<string> excludedPathKeywords = new List<string>
    {
        "/Spine/",
        "/External/",
        "/Samples~/",
        "/Documentation~/",
        "/Library/",
        "/Temp/",
        "/Obj/",
        "/Logs/",
        "/UserSettings/"
    };

    [Tooltip("Package names to include in addition to the Assets folder. Use Package Manager package names such as com.company.package.")]
    public List<string> includedPackageNames = new List<string>
    {
        "com.shared.command-framework",
        "com.more.core"
    };

    [Tooltip("Serialized Unity asset extensions to export as raw text.")]
    public List<string> serializedAssetExtensions = new List<string>
    {
        ".unity",
        ".prefab",
        ".asset",
        ".mat",
        ".controller",
        ".overrideController",
        ".playable",
        ".anim",
        ".inputactions"
    };

    [Tooltip("Maximum characters exported per file. Set to 0 or less to disable trimming.")]
    public int maxFileCharCount = 300000;

    [Tooltip("Include absolute local file paths in the export. Disabled by default to avoid leaking machine-specific paths.")]
    public bool includeFullPath = false;

    [Tooltip("Prefix exported content lines with 1-based line numbers.")]
    public bool includeLineNumbers = true;

    [Tooltip("Include the current git commit short hash in the export header when git is available.")]
    public bool includeGitCommit = true;

    internal void EnsureDefaults()
    {
        if (excludedPathKeywords == null)
            excludedPathKeywords = new List<string>();

        if (includedPackageNames == null)
            includedPackageNames = new List<string>();

        if (serializedAssetExtensions == null)
            serializedAssetExtensions = new List<string>();
    }

    internal void SaveSettings()
    {
        EnsureDefaults();
        Save(true);
    }

    internal void ResetToDefaults()
    {
        projectName = string.Empty;
        excludedPathKeywords = new List<string>
        {
            "/Spine/",
            "/External/",
            "/Samples~/",
            "/Documentation~/",
            "/Library/",
            "/Temp/",
            "/Obj/",
            "/Logs/",
            "/UserSettings/"
        };
        includedPackageNames = new List<string>
        {
            "com.shared.command-framework",
            "com.more.core"
        };
        serializedAssetExtensions = new List<string>
        {
            ".unity",
            ".prefab",
            ".asset",
            ".mat",
            ".controller",
            ".overrideController",
            ".playable",
            ".anim",
            ".inputactions"
        };
        maxFileCharCount = 300000;
        includeFullPath = false;
        includeLineNumbers = true;
        includeGitCommit = true;
    }

    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        SettingsProvider provider = new SettingsProvider("Project/Scripts Exporter For AI", SettingsScope.Project)
        {
            label = "Scripts Exporter For AI",
            guiHandler = searchContext =>
            {
                ScriptsExporterForAISettings settings = instance;
                settings.EnsureDefaults();

                SerializedObject serializedSettings = new SerializedObject(settings);
                serializedSettings.Update();

                EditorGUILayout.HelpBox(
                    "Configure how ScriptsExporterForAI collects scripts, packages, and serialized Unity assets.",
                    MessageType.Info);

                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(projectName)), new GUIContent("Project Name"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(excludedPathKeywords)), new GUIContent("Excluded Path Keywords"), true);
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(includedPackageNames)), new GUIContent("Included Package Names"), true);
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(serializedAssetExtensions)), new GUIContent("Serialized Asset Extensions"), true);
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(maxFileCharCount)), new GUIContent("Max File Char Count"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(includeFullPath)), new GUIContent("Include Full Path"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(includeLineNumbers)), new GUIContent("Include Line Numbers"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(includeGitCommit)), new GUIContent("Include Git Commit"));

                if (serializedSettings.ApplyModifiedProperties())
                    settings.SaveSettings();

                EditorGUILayout.Space();

                if (GUILayout.Button("Reset to Defaults"))
                {
                    settings.ResetToDefaults();
                    settings.SaveSettings();
                }
            },
            keywords = new HashSet<string>(new[]
            {
                "scripts",
                "exporter",
                "ai",
                "serialized",
                "assets",
                "packages"
            })
        };

        return provider;
    }
}
