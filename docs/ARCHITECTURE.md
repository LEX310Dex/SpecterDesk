# SpecterDesk architecture

SpecterDesk deliberately separates capture/privacy primitives from UI code.

## Core principles

### 1. Do not modify third-party meeting applications

SpecterDesk does not inject into Chrome, Teams, Zoom, or Meet. Instead it controls only windows and cursors owned by SpecterDesk.

### 2. Preserve the proven cursor path

The cursor-free mode temporarily installs transparent system cursor shapes and renders a replacement pointer in a SpecterDesk-owned topmost overlay.

The overlay is configured for capture exclusion.

### 3. Private tools live inside SpecterDesk-owned windows

The WebView2 browser and notes workspace live inside a top-level SpecterDesk window. That lets the application apply capture affinity to the private workspace without attempting to control unrelated application windows.

### 4. UI is replaceable

The dashboard/workspace presentation layer should not alter cursor replacement, screenshot, or capture-exclusion logic.

## Main components

- `Native` — Win32 interop and display-affinity helpers
- `CursorLayer` — local replacement cursor and system-cursor lifecycle
- `ScreenshotService` — full-desktop capture and clipboard integration
- `GhostHub` / future `SpecterHub` — private WebView2 + notes workspace
- `DashboardForm` — user-facing control center
- recovery/watchdog component — cursor restoration safety path
