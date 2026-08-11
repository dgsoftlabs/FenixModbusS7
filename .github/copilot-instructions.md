# Copilot Instructions

## Project Guidelines
- User prefers chart UI labels/descriptions in English (EN).
- User expects UI fixes to be complete, specifically no unnecessary right-side empty space in the property grid.
- Support only the .pse project format; do not support legacy .psx formats for saving, but open legacy .psx files for conversion.
- Legacy .psf files should be openable via conversion, but saving should remain only in the new format (.pse).
- Upgrade target framework to .NET 10.0 (LTS).
- Force migration execution; proceed aggressively without pausing.
- Replace anything incompatible with a compatible alternative during migration.
- Use the same libraries pattern for FenixServer as Fenix where possible.
- Convert FenixServer to WPF while keeping the same project references/libraries pattern and ensuring it implements the same functionality as the previous WinForms version.
- Switch FenixServer to WPF and adopt the same library pattern as Fenix, ensuring it maintains the same functionality as the previous WinForms version.
- Replace all WinForms controls with full WPF controls.
- Adopt a new ASP.NET Core server-hosted approach in this application.
- Perform a clean WPF migration incrementally, replacing parts one by one.
- Replace .png assets with emoji in the UI.
- Prefer code-based endpoint handling over script evaluation. 
- Ensure all server endpoint data access is code-based and remove script-based access paths.
- Use only the existing old endpoint model; do not introduce or label separate new/legacy endpoint variants.

## Code Style
- Style definitions should only be placed in the style file (Themes/Default.xaml), not inline in views.
- Limit the number of UI styles to only a small, necessary set instead of many granular style entries. Keep UI styling limited to a small set of necessary shared styles; avoid many granular style keys.