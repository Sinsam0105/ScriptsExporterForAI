# Scripts Exporter For AI

Unity editor tools for exporting C# scripts and serialized Unity assets into combined text files for AI-assisted project analysis.

## Requirements

- Unity 2021.3 or newer
- Git installed and available on `PATH` if you want commit hashes in export headers
- For serialized asset exports, `Edit > Project Settings > Editor > Asset Serialization > Mode` should preferably be `Force Text`

## Installation

Use Unity Package Manager:

1. Open `Window > Package Manager`.
2. Click `+`.
3. Select `Add package from git URL...`.
4. Enter this repository URL.

## Usage

Export scripts:

- `Tools > Scripts Exporter For AI > Export All C# Scripts to TXT`

Export serialized Unity assets:

- `Tools > Scripts Exporter For AI > Export Unity Serialized Assets to TXT`

The exporters create a single text file with file separators, relative project paths, optional line numbers, Unity version, and git commit metadata.

## Configuration

Open:

- `Edit > Project Settings > Scripts Exporter For AI`

Available settings:

- `Project Name`: shown in the export header. Empty uses the Unity project folder name.
- `Excluded Path Keywords`: path substrings skipped during export.
- `Included Package Names`: package names included in addition to `Assets`.
- `Serialized Asset Extensions`: file extensions exported by the serialized asset exporter.
- `Max File Char Count`: maximum characters per file. Set to `0` or less to disable trimming.
- `Include Full Path`: includes absolute local paths. Disabled by default to avoid leaking machine-specific paths.
- `Include Line Numbers`: prefixes file content lines with 1-based line numbers.
- `Include Git Commit`: includes the current git short hash when available.

## Notes

The serialized asset exporter reads asset files as raw text. It does not open scenes, load prefabs, or deserialize Unity objects. Binary-looking files are skipped.

## Limitations

- Binary detection is intentionally conservative and based on null-byte detection.
- Very large text files may be trimmed depending on `Max File Char Count`.
- The package is an editor-only tool and is not intended for runtime builds.
