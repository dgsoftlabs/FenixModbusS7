# Copilot Instructions

## Project Guidelines
- User prefers chart UI labels/descriptions in English (EN).
- User expects UI fixes to be complete, specifically no unnecessary right-side empty space in the property grid.
- Support only the .pse project format; do not support legacy .psx/.psf formats.
- Upgrade target framework to .NET 10.0 (LTS).
- Force migration execution; proceed aggressively without pausing.
- Replace anything incompatible with a compatible alternative during migration.
- Use the same libraries pattern for FenixServer as FenixModbusS7 where possible.

## Code Style
- Style definitions should only be placed in the style file (Themes/Default.xaml), not inline in views.
- Limit the number of UI styles to only a small, necessary set instead of many granular style entries. Keep UI styling limited to a small set of necessary shared styles; avoid many granular style keys.