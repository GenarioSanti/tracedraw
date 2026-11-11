# BannerCut macro icons and Windows installer

This fork recognizes four exact macro keys in SSPrintBannerCut, module
BannerCut: Enviar, Marcas, Configurar and Limite. Their original vector icons
show a blade, registration corners, a gear and a boundary with nodes.
Icons scale with the toolbar and use the current theme colour. Other macros
keep their text buttons. Tooltips use Portuguese or English; other languages
currently fall back to English for these four entries.

Only presentation changes in the Rust editor. Dispatch, native Boundary,
document geometry and machine communication retain their existing behaviour.

The BannerCut Windows installer workflow on branch bannercut-installer tests
the workspace, builds the editor and independent Windows integration helper,
tests the helper, builds an Inno Setup installer and publishes a prerelease.
The installer uses a separate per-user application directory and registers
macros in the desktop TraceDraw profile. It does not replace the Store app.
Close TraceDraw first. Existing desktop settings receive backups. CameraCut
must already be installed; its path is requested if it cannot be found.
The installer includes the RC7 fix for large embedded images. It never starts
a physical cut. See integrations/bannercut/README.md for helper details.

Tests cover exact key matching and painting within button bounds at 16, 24
and 32 points, in light and dark colours. Helper tests cover large embedded
images, selection rejection, settings preservation and macro registration.
Visual inspection at 100% and 150%, tooltip checks and physical plotter
validation remain required before calling the preview fully validated.
