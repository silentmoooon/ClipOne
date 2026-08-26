# C# WPF Development Guidelines

> Best practices for the host app in ClipOne.

---

## Overview

The C# WPF host manages the application lifecycle, intercepts global clipboard events, registers system-wide hotkeys, and hosts the WebView2 environment for the UI. 

## Guidelines Index

| Guide | Description |
|-------|-------------|
| [Clipboard Logic](./clipboard.md) | How clipboard formats are parsed and inserted |
| [WebView2 Interop](./interop.md) | How the WPF host communicates with the JS skin |
