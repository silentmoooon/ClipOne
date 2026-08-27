# C# Photino / Native AOT Development Guidelines

> Best practices for the host app in ClipOne.

---

## Overview

The C# Photino host manages application lifecycle, intercepts global clipboard events via Win32 `WM_CLIPBOARDUPDATE`, registers system-wide hotkeys via `RegisterHotKey`, manages the native tray icon, and hosts the WebView2 environment without any WPF dependencies.

## Guidelines Index

| Guide | Description |
|-------|-------------|
| [Clipboard Logic](./clipboard.md) | How native clipboard formats are parsed and written |
| [Photino / WebView2 Interop](./interop.md) | How the C# host communicates with the JS UI |
