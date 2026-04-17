# Copilot Instructions

## Project Guidelines
- User prefers chart UI labels/descriptions in English (EN).
- User expects UI fixes to be complete, specifically no unnecessary right-side empty space in the property grid.
- Support only the .pse project format; do not support legacy .psx/.psf formats.

## Code Style
- Style definitions should only be placed in the style file (Themes/Default.xaml), not inline in views.
- Limit the number of UI styles to only a small, necessary set instead of many granular style entries. Keep UI styling limited to a small set of necessary shared styles; avoid many granular style keys.