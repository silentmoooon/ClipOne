# AGENTS.md — ClipOne

High-signal context for agents working in this repo.

## Project

- **What**: Windows clipboard enhancer (WPF + WebView2 UI). Press a global hotkey (default `Alt+V`) to show a history popup and paste previous clips.
- **Stack**: C# WPF on .NET 9 (`net9.0-windows10.0.20348.0`), x64 only.
- **Entry point**: `App.xaml` → `view/MainWindow.xaml`.

## Architecture

- **Hybrid UI**: WPF window hosts a WebView2 control that renders `html/index.html`. The entire skin system is HTML/CSS/JS (jQuery-based), not XAML.
- **C# ↔ JS bridge**: WebView2 message passing.
  - C# receives via `CoreWebView2.WebMessageReceived` (see `MainWindow.xaml.cs`).
  - C# calls JS via `ExecuteScriptAsync(...)`.
  - Key message prefixes in JS: `PasteValue|`, `PasteValueList|`, `SetToClipBoard|`, `esc|`.
- **Clipboard logic**: `service/ClipService.cs` handles QQ rich text, WeChat rich text, HTML, images (incl. GIF), files, and plain text. Uses Win32 clipboard APIs and retries on `OpenClipboard` failures.
- **Config**: `service/ConfigService.cs` reads/writes `config/settings.json` at runtime (relative to working directory).
- **Single instance**: Enforced in `App.xaml.cs` by counting processes with the same module name.

## Build / Run / Publish

```powershell
# Build
dotnet build
dotnet build -c Release

# Run (Debug)
dotnet run

# Publish (uses profile: Release/x64/ReadyToRun/win-x64, not self-contained)
dotnet publish -c Release
```

- Output platform is **x64** only (`Platforms=x64`).
- Publish profile: `Properties/PublishProfiles/FolderProfile.pubxml`.

## Important file behaviors

- `html/index.html`, `html/js/*`, and `html/css/*` are copied to the output directory on every build (`CopyToOutputDirectory=Always`).
- Skin switching (`ChangeSkin` in `MainWindow.xaml.cs`) **rewrites `html/index.html` on disk** to update the final `<link>` tag pointing to the selected CSS folder.
- `Environment.CurrentDirectory` is set to `AppDomain.CurrentDomain.BaseDirectory` on startup so relative paths resolve correctly.

## Legacy / stale artifacts

- `packages.config` and `App.config` still exist but reflect the old .NET Framework build. The real source of truth is the SDK-style `ClipOne.csproj` with `PackageReference`.
- Do not treat `packages.config` as the dependency list.

## Dependencies

- `H.NotifyIcon.Wpf` — tray icon
- `Microsoft.Web.WebView2` — embedded browser
- `HtmlAgilityPack` — parsing QQ rich-text HTML
- `Newtonsoft.Json` — serialization

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
