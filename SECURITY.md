# Security and privacy model

SpecterDesk uses Windows display-affinity APIs to request exclusion of its private windows from supported capture paths.

This is not a DRM system and is not a universal security boundary.

Do not assume SpecterDesk can prevent:

- physical cameras pointed at the display
- privileged/admin-level inspection
- all remote desktop software
- every third-party capture driver
- external capture hardware

If you discover behavior that exposes a window through a capture method SpecterDesk is expected to support, please open a GitHub issue with reproduction details and avoid including sensitive content in screenshots or logs.
