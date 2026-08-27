# AGENTS.md — ClipOne

High-signal context for agents working in this repo.

## Project

- **What**: Windows clipboard enhancer (Photino.NET + WebView2 UI). Press a global hotkey (default `Win+V` / `Alt+V`) to show a history popup and paste previous clips.
- **Stack**: C# on .NET 10 (`net10.0-windows10.0.26100.0`), Native AOT, x64 only.
- **Entry point**: `Program.cs`.

## Architecture

- **Lightweight UI**: Photino.NET window hosts WebView2 control that renders `html/index.html`. No WPF or XAML.
- **C# ↔ JS bridge**:
  - Photino WebMessage passing: `window.RegisterWebMessageReceivedHandler` and `window.SendWebMessage`.
  - JS messages: `PasteValue|`, `PasteValueList|`, `SetToClipBoard|`, `SaveHotkey|`, `esc|`.
  - C# messages: `{"type": "history", "data": ...}`, `{"type": "add", "data": ...}`, `{"type": "hotkeySettings", "data": ...}`.
- **Serialization**: Zero-reflection `System.Text.Json` source generator (`service/ClipJsonContext.cs`).
- **Clipboard logic**: `service/ClipService.cs` handles QQ rich text, WeChat rich text, HTML, images (BMP/DIB base64), files, and plain text using native Win32/WinRT APIs.
- **Tray & Hotkeys**: `util/TrayIconManager.cs` (native Win32 `Shell_NotifyIcon` + popup menu) and `util/HotKeyManager.cs` (`RegisterHotKey`).
- **Config**: `service/ConfigService.cs` reads/writes `config/settings.json` via source-generated JSON serializer.
- **Single instance**: Enforced in `Program.cs` via global named `Mutex`.

## Build / Run / Publish

```powershell
# Build
dotnet build
dotnet build -c Release

# Run (Debug)
dotnet run

# Publish Native AOT Release
dotnet publish -c Release -r win-x64
```

- Output platform is **x64** only (`Platforms=x64`).
- Native AOT enabled (`PublishAot=true`).

## Important file behaviors

- `html/index.html`, `html/js/*`, and `html/css/*` are copied to the output directory on every build (`CopyToOutputDirectory=Always`).
- Skin switching (`ChangeSkin` in `Program.cs`) rewrites `html/index.html` on disk to update the final `<link>` tag pointing to the selected CSS folder.
- `Environment.CurrentDirectory` is set to `AppDomain.CurrentDomain.BaseDirectory` on startup so relative paths resolve correctly.

## Dependencies

- `Photino.NET` — lightweight cross-platform desktop browser window host
- `HtmlAgilityPack` — parsing QQ rich-text HTML

## No tests

There are no test projects in this repository.
<!-- TRELLIS:START -->
# Trellis Instructions

These instructions are for AI assistants working in this project.

This project is managed by Trellis. The working knowledge you need lives under `.trellis/`:

- `.trellis/workflow.md` — development phases, when to create tasks, skill routing
- `.trellis/spec/` — package- and layer-scoped coding guidelines (read before writing code in a given layer)
- `.trellis/workspace/` — per-developer journals and session traces
- `.trellis/tasks/` — active and archived tasks (PRDs, research, jsonl context)

If a Trellis command is available on your platform (e.g. `/trellis:finish-work`, `/trellis:continue`), prefer it over manual steps. Not every platform exposes every command.

If you're using Codex or another agent-capable tool, additional project-scoped helpers may live in:
- `.agents/skills/` — reusable Trellis skills
- `.codex/agents/` — optional custom subagents

Managed by Trellis. Edits outside this block are preserved; edits inside may be overwritten by a future `trellis update`.

<!-- TRELLIS:END -->
