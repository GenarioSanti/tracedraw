# BannerCut integration packaging

TraceDraw remains a native Rust application. This directory packages the
existing independent Windows .NET Framework helper used by the BannerCut
macros. It is built separately and communicates through the user's local
mailbox. CameraCut and its drivers are not redistributed.

The installer registers the four macros in the desktop profile, preserving
existing settings with backups. It asks for CameraCut.exe if not found.
The Store profile is not modified. Close TraceDraw before installing.
The helper requests Windows authorization only when sending or configuring.

RC7 skips embedded image pixels when reading exported SVG pages. It accepts
pages up to 128 MB and retains a 16 MB geometry limit. Selected images are
still rejected. The machine's physical cut remains under user control.

Build with build.ps1; run BannerCutIntegrado.exe --selftest <test-directory>.
The Windows installer workflow runs the Rust and helper tests before packaging.
A successful automated build is not a visual or physical plotter validation.

Uninstalling the editor preserves the separately installed helper and user
settings. Use Desativar integracao in the BannerCut start menu to remove the
macro registration and automatic helper startup; then exit its tray icon.
