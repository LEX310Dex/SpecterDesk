# Contributing to SpecterDesk

Thanks for your interest in SpecterDesk.

## Before opening a pull request

1. Build the project in Release mode.
2. Confirm normal cursor restoration works after enabling/disabling cursor-free mode.
3. Confirm the private workspace still receives `WDA_EXCLUDEFROMCAPTURE`.
4. Avoid changing capture/cursor behavior as part of purely visual changes.
5. Keep UI changes DPI-aware and avoid absolute positioning where possible.

## Preferred pull requests

Small, focused pull requests are easier to test. Good areas include:

- UI polish
- multi-monitor / DPI fixes
- accessibility
- documentation
- build / packaging improvements
- crash recovery

## Bug reports

Include:

- Windows version
- display scaling percentage
- number of monitors
- meeting/capture application
- whether the issue affects the local display, captured output, or both
- reproducible steps
