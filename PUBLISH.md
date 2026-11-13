# Publish SpecterDesk as a new GitHub project

Keep `LEX310Dex/GhostCursor` unchanged.

## Recommended repository

- Repository: `SpecterDesk`
- Title: **SpecterDesk — Private Overlay Workspace for Windows**
- Tagline: **Your screen stays public. Your workspace doesn't.**
- Description: `Private Windows overlay workspace with capture exclusion, cursor-free sharing, WebView2 tools, notes, screenshots, and global hotkeys.`

## Suggested GitHub topics

`windows` `csharp` `dotnet` `winforms` `webview2` `desktop-app` `screen-sharing` `overlay` `privacy` `productivity` `system-tray` `win32`

## Create the new repository from your working source

Do this from the *working v0.7.2 project directory*, not the original GhostCursor repository.

```powershell
$source = "C:\Users\slima\OneDrive\Bureau\GhostCursor\v06-staging\GhostCursor_v0.6"
$dest   = "C:\Users\slima\OneDrive\Bureau\SpecterDesk"

New-Item -ItemType Directory -Force $dest | Out-Null

robocopy $source $dest /E /XD bin obj backup-ui-* "*UI-Backups*" /XF *.user *.log

Set-Location $dest

# Copy the branding/docs from this publish kit into $dest before continuing.
```

Then initialize a fresh repository so SpecterDesk has its own history:

```powershell
Remove-Item -Recurse -Force .git -ErrorAction SilentlyContinue

git init
git branch -M main
git add .
git commit -m "Initial beta: SpecterDesk private overlay workspace"

gh repo create SpecterDesk --public `
  --description "Private Windows overlay workspace with capture exclusion, cursor-free sharing, WebView2 tools, notes, screenshots, and global hotkeys." `
  --source . --remote origin --push
```

## Recommended first release

Tag: `v0.1.0-beta.1`

Title: **SpecterDesk 0.1 Beta — Your private layer above the desktop**

Use the content from `RELEASE_NOTES_v0.1.0-beta.1.md` for the release notes.
