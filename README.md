<div align="center">

# SpecterDesk

### Your screen stays public. Your workspace doesn't.

**A private overlay workspace for Windows that keeps selected tools visible locally while excluding them — and your replacement cursor — from supported screen captures.**

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows11&logoColor=white)](#requirements)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](#build-from-source)
[![WebView2](https://img.shields.io/badge/WebView2-Embedded%20browser-0F6CBD)](#private-workspace)
[![License](https://img.shields.io/badge/License-MIT-2F3136)](LICENSE)

</div>

---

## What is SpecterDesk?

SpecterDesk is a lightweight Windows desktop utility built around a simple idea: **keep private tools available on your own display while presenting a clean screen to others**.

It combines a capture-excluded floating workspace, cursor-free screen sharing, silent screenshots, global hotkeys, and a compact system tray controller in one application.

SpecterDesk grew out of the smaller **GhostCursor** proof-of-concept. GhostCursor remains a separate repository focused on cursor exclusion; SpecterDesk is the broader desktop workspace application.

## Highlights

- **Private always-on-top workspace** — ChatGPT, a browser, and notes in a local floating panel.
- **Capture exclusion** — the private workspace is excluded from supported Windows capture paths.
- **Cursor-free sharing** — keep a usable replacement cursor locally while supported screen shares receive no cursor overlay.
- **Silent screenshots** — capture the desktop directly to the clipboard and save a local copy.
- **Global hotkeys** — control the app without interrupting a presentation.
- **System tray controls** — keep the app out of the way when not needed.
- **Obsidian UI** — compact, dark, minimal interface designed for Windows desktop use.
- **Persistent WebView2 profile** — the embedded browser can keep its own session state.

## Tested scenarios

The cursor/workspace exclusion approach has been tested by the project author with full-screen sharing in:

- Google Meet
- Microsoft Teams
- Chrome-based screen sharing
- Zoom

> Compatibility can vary by Windows version, application version, GPU/capture backend, and capture method. SpecterDesk does **not** claim to be invisible to every possible recording, remote-desktop, or capture system.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl + Alt + H` | Enable cursor-free sharing mode |
| `Ctrl + Alt + R` | Restore the normal system cursor |
| `Ctrl + Alt + P` | Show / hide the private workspace |
| `Ctrl + Shift + Alt + S` | Silent screenshot → clipboard + file |
| `Ctrl + Alt + Q` | Exit safely |

## Private workspace

The workspace provides three primary surfaces:

**ChatGPT** — opens ChatGPT in an embedded WebView2 browser.

**Browser** — general private browsing inside the capture-excluded window.

**Notes** — a local private notes surface for presentation prompts, reminders, snippets, or checklists.

The workspace is topmost so it can remain above a maximized application while you continue interacting with your desktop.

## How it works

SpecterDesk uses documented Windows APIs rather than injecting code into meeting applications.

At a high level:

1. Standard system cursor shapes are temporarily replaced with transparent cursors.
2. SpecterDesk draws a local replacement cursor in its own topmost overlay window.
3. That overlay window requests `WDA_EXCLUDEFROMCAPTURE` through `SetWindowDisplayAffinity`.
4. The private workspace uses the same capture-exclusion mechanism.
5. Supported capture paths therefore omit SpecterDesk's private surfaces while they remain visible on the physical display.

This design lets applications continue using their normal **Share entire screen** workflow.

## Architecture

```text
SpecterDesk
├─ Core
│  ├─ cursor replacement / restoration
│  ├─ Win32 capture exclusion
│  ├─ global hotkeys
│  └─ screenshot service
├─ Workspace
│  ├─ WebView2 browser
│  ├─ ChatGPT shortcut
│  └─ private notes
└─ UI
   ├─ Obsidian dashboard
   ├─ responsive workspace layout
   └─ system tray controls
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for more detail.

## Requirements

- Windows 10 version 2004 or later, or Windows 11
- x64 recommended
- .NET 10 Desktop Runtime when using a framework-dependent build
- Microsoft Edge WebView2 Runtime

Most current Windows 11 systems already include WebView2 Runtime.

## Build from source

### 1. Clone

```powershell
git clone https://github.com/LEX310Dex/SpecterDesk.git
cd SpecterDesk
```

### 2. Restore and build

```powershell
dotnet restore
dotnet build -c Release
```

### 3. Run

```powershell
.\bin\Release\net10.0-windows\SpecterDesk.exe
```

If the project executable is still named `GhostCursorV6.exe` in an early source snapshot, rename the project/output during the branding pass before publishing a binary release.

## Publish a self-contained Windows build

```powershell
dotnet publish -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false `
  -o .\publish
```

## Safety and privacy notes

SpecterDesk's capture exclusion is a **presentation/privacy convenience**, not a security boundary. `SetWindowDisplayAffinity` is designed to influence supported Windows capture behavior; it cannot guarantee protection against every capture method, physical camera, administrator-level inspection, or external recording device.

Always test your intended meeting/capture application before relying on exclusion for sensitive material.

If SpecterDesk is terminated while cursor-free mode is active, use its recovery path or sign out/restart Windows if necessary to reload your configured cursor scheme.

## Roadmap

- [ ] Installer / portable release packaging
- [ ] Signed releases
- [ ] Custom SpecterDesk icon and branding assets
- [ ] Configurable hotkeys
- [ ] Light / dark / system theme
- [ ] Better multi-monitor and mixed-DPI behavior
- [ ] Automatic update checks
- [ ] Workspace presets
- [ ] Optional compact command palette

## Relationship to GhostCursor

[`GhostCursor`](https://github.com/LEX310Dex/GhostCursor) is the original minimal Win32 proof-of-concept focused on cursor exclusion.

**SpecterDesk** is the second-generation application: the private workspace, browser, notes, screenshots, tray controller, dashboard, and polished desktop experience live here.

## License

MIT — see [`LICENSE`](LICENSE).

---

<div align="center">

**SpecterDesk** — *Your private layer above the desktop.*

</div>
