# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HaiyaBox is a C# plugin for the AEAssist framework in Final Fantasy XIV. It provides automation tools, raid utilities, and geometry-based positioning systems for the game. The project targets .NET 9.0 Windows and uses the Dalamud plugin framework.

## Build Commands

### Building the Plugin
```bash
# Build in Debug mode
dotnet build HaiyaBox.sln -c Debug

# Build in Release mode
dotnet build HaiyaBox.sln -c Release

# Clean and rebuild
dotnet clean HaiyaBox.sln
dotnet build HaiyaBox.sln
```

### Output Location
Both Debug and Release configurations output to:
`F:\FF14act\AEAssist 国服 1024\Plugins\HaiyaBox`

### Environment Variables
- `AEPath`: If set, points to the AEAssist installation directory. Otherwise defaults to `..\..\..\AE\AEAssistCNVersion\AEAssist\`

## Architecture

### Core Structure
- **Plugin/**: Main plugin entry point and initialization
  - `AutoRaidHelper.cs`: Main plugin class implementing IAEPlugin
- **UI/**: User interface components for different functionality tabs
  - `AutomationTab.cs`: Main automation interface and logic
  - `GeometryTab.cs`: Positioning and geometry utilities
  - `FaGeneralSettingTab.cs`: General settings
  - `DebugPrintTab.cs`: Debug output
  - `BlackListTab.cs`: Blacklist management
- **Settings/**: Configuration management
  - `FullAutoSettings.cs`: Global settings singleton with JSON persistence
- **Triggers/**: AEAssist trigger system integration
  - `TriggerAction/`: Custom trigger actions (positioning, BMR launching)
  - `TriggerCondition/`: Custom trigger conditions (position detection)
- **Hooks/**: Game memory and function hooks
  - `ActorControlHook.cs`: Actor control interception
- **Utils/**: Utility functions
  - `GeometryUtilsXZ.cs`: XZ plane geometry calculations
  - `Utilities.cs`: General utility functions

### Key Dependencies
- **AEAssist**: Main combat automation framework
- **ECommons**: Dalamud convenience library
- **Dalamud**: FFXIV plugin framework
- **FFXIVClientStructs**: Direct game memory access

### Configuration System
- Settings stored per-character in JSON format
- Path: `Share.CurrentDirectory\..\..\Settings\HaiyaBox\FullAutoSettings\{LocalContentId}.json`
- Thread-safe singleton pattern with read-only fallback

### Trigger System
The plugin integrates with AEAssist's trigger system to provide:
- Role-based positioning (MT, ST, H1, H2, D1, D2, D3, D4)
- Position detection and validation
- BMR (battle maneuver system) automation

## Development Notes

### Safety Considerations
- This is a game automation plugin that interacts with FFXIV memory
- Uses unsafe code blocks for direct memory access
- Hook-based approach for intercepting game functions

### Chinese Naming
Many files and classes use Chinese names reflecting the plugin's origin and target audience. Key examples:
- `指定职能tp指定位置.cs` (Role-specified position teleporting)
- `检测目标位置.cs` (Target position detection)
- `启动bmr.cs` (Launch BMR system)