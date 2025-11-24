# Repository Guidelines

## Project Structure & Module Organization
FullAuto hosts two plugin families.
- `HaiyaBox/` is the AEAssist automation plugin. Runtime code lives under `Hooks/`, `Plugin/`, `Triggers/`, `UI/`, and `Utils/`; preset data stays in `Settings/` and `TimeLine/`. `HaiyaBox.csproj` targets `net9.0-windows` and resolves AEAssist/ECommons via the `AEPath` variable.
- `Splatoon/` contains the Dalamud solution (`Splatoon.sln`). Core logic sits in `Splatoon/`, automation lives in `SplatoonScripts/`, distributable overlays live in `Presets/`, and `docs/` plus `meta/` back documentation and release metadata. Always initialize submodules (`ECommons`, `PunishLib`, `NightmareUI`, `ffxiv_pictomancy`, `FFXIVClientStructs`) after pulling.

## Build, Test, and Development Commands
- `git submodule update --init --recursive` – fetch shared Splatoon dependencies.
- `dotnet restore Splatoon/Splatoon.sln` – hydrate NuGet packages for every project.
- `dotnet build Splatoon/Splatoon.sln -c Debug` – daily validation; switch to `Release` and rerun before packaging or regenerating script manifests.
- `dotnet build HaiyaBox.sln -c Release` – produce the AEAssist plugin. Export `AEPath=C:\path\to\AEAssist\` if the default relative folder is missing.

## Coding Style & Naming Conventions
Use four-space indentation, Allman braces, `PascalCase` types/methods, `camelCase` fields, and alphabetized `using` lists. Keep new features self-contained (`HaiyaBox.Triggers.*`, `Splatoon.Modules.*`) and match the surrounding IPC/UI patterns. Respect `Splatoon/CONTRIBUTING.md`: no drive-by refactors, no opcode-to-hook swaps, introduce new render elements across every engine, and never reset persisted configuration.

## Testing Guidelines
No automated tests exist; rely on manual smoke passes. For Splatoon, load a Dalamud dev client, install the plugin, exercise `/sf`, preset imports, and every render engine touched. For HaiyaBox, run inside AEAssist, walk the impacted tabs (`几何计算`, `自动化`, `事件记录`, etc.), and capture trigger timelines or logs that prove the change before opening a PR.

## Commit & Pull Request Guidelines
Commits are short, imperative statements (for example `删除危险区绘制`) that group related edits and avoid generated preset noise. Each PR must confirm CLA acceptance, describe the affected plugin, list manual test evidence or screenshots, link the relevant issue or script ID, and call out dependency or render-engine risks so maintainers can plan releases.

## Security & Configuration Tips
Keep proprietary AEAssist binaries and personal output paths out of source control. Store secrets and Dalamud keys in local environment variables. Only edit `revocationList.txt` and `versions.txt` while preparing a release so revocations stay authoritative.
