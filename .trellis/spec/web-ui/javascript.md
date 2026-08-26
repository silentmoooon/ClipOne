# JavaScript Conventions

The JS layer (e.g., `html/js/main.js`) handles rendering the clip history, managing keyboard shortcuts within the UI, and storing data locally.

## Storage

We use the browser's native `localStorage` to persist the clip history, avoiding round-trips to the C# backend on startup.

```javascript
// Storing
window.localStorage.setItem("data", JSON.stringify(clipObj));

// Loading on ready
var str = window.localStorage.getItem("data");
if (str != null) {
    clipObj = JSON.parse(str);
}
```

## Messaging the Host

We invoke WPF functions via `window.chrome.webview.postMessage()`. The argument must be a string containing a command prefix and a pipe `|`.

```javascript
// Example: requesting to close
window.chrome.webview.postMessage("esc|1");

// Example: requesting to paste a value
window.chrome.webview.postMessage(
    "PasteValue|" + encodeURIComponent(JSON.stringify(obj))
);
```

## Keyboard Management

The JS layer binds to `keydown` on the document `body`.

- **ESC**: Closes the window (`postMessage("esc|1")`).
- **Enter**: Pastes the currently selected item.
- **Numbers (1-9) & Letters (A-Z)**: Direct paste shortcut.
- **Space**: Pastes index 0.

## DOM Library

We use standard jQuery for DOM manipulation (`$()`) and `jquery.nicescroll.min.js` for custom scrollbars. 

Do not introduce large frameworks like React or Vue, as this project relies on a lightweight jQuery architecture.
