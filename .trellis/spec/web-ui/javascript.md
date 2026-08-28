# JavaScript Conventions

The JS layer (`html/js/main.js`) handles rendering the clip history, managing keyboard shortcuts and mouse interactions within the WebView2 UI, managing theming, and communicating with the C# host.

## Architecture

We use **100% native standard ES6+ Web APIs** without any third-party dependencies (Zero Dependency).
State and actions are encapsulated in a single controller object `ClipApp`.

Do NOT introduce external frameworks (React, Vue, jQuery, etc.) as the project requires minimal memory footprint and zero bundle overhead.

## Messaging the Host (IPC)

We communicate with the Photino/WebView2 host via `window.chrome.webview.postMessage()`. The argument must be a string containing a command prefix and a pipe `|`.

```javascript
// Example: requesting to close window
window.chrome.webview.postMessage("esc|1");

// Example: requesting to paste a value
window.chrome.webview.postMessage(
    "PasteValue|" + encodeURIComponent(JSON.stringify(obj))
);
```

Host-to-Web messages are received through `window.chrome.webview.addEventListener('message', e => ...)` with typed payloads (`history`, `add`, `hotkeySettings`, `show`, `showTrayMenu`, `changeSkin`).

## Keyboard & Navigation Management

- **ESC**: Closes search if open, then requests window close (`postMessage("esc|1")`).
- **ArrowDown / ArrowUp**: Moves selection highlight up/down and scrolls into view automatically.
- **PageDown / PageUp**: Multi-row jump navigation (+5 / -5).
- **Home / End**: Moves selection to top / bottom.
- **Enter**: Pastes the currently selected item in visible list.
- **Numbers (1-9) & Letters (A-Z)**: Direct paste shortcut for visible items.
- **Space**: Pastes index 0 directly.
- **Delete / Backspace**: Deletes the selected item.
- **Ctrl+F**: Toggles search bar focus.
- **Shift + Click**: Selects a range of items, pastes continuously upon releasing Shift.
- **Ctrl + Click**: Multi-selects discrete items, pastes combined upon releasing Ctrl.
