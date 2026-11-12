//! Original, theme-aware pictures for the BannerCut macro commands.
//! Dispatch keys stay unchanged; unrelated macros keep their text buttons.

use egui::{Color32, Painter, Pos2, Rect, Stroke};

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) enum Icon {
    Send,
    Marks,
    Settings,
    Boundary,
}

pub(crate) fn for_key(key: &str) -> Option<Icon> {
    match key.strip_prefix("menu.macro.SSPrintBannerCut|BannerCut|")? {
        "Enviar" => Some(Icon::Send),
        "Marcas" => Some(Icon::Marks),
        "Configurar" => Some(Icon::Settings),
        "Limite" => Some(Icon::Boundary),
        _ => None,
    }
}

impl Icon {
    pub(crate) fn label_key(self) -> &'static str {
        match self {
            Self::Send => "toolbar.bannercut_send",
            Self::Marks => "toolbar.bannercut_marks",
            Self::Settings => "toolbar.bannercut_settings",
            Self::Boundary => "toolbar.bannercut_boundary",
        }
    }
}

pub(crate) fn draw(painter: &Painter, rect: Rect, icon: Icon, color: Color32) {
    let size = rect.width().min(rect.height());
    let origin = rect.center() - egui::vec2(size, size) * 0.5;
    let point = |x: f32, y: f32| Pos2::new(origin.x + x * size / 24.0, origin.y + y * size / 24.0);
    let stroke = Stroke::new((size / 16.0).max(1.0), color);
    let line = |a: (f32, f32), b: (f32, f32)| {
        painter.line_segment([point(a.0, a.1), point(b.0, b.1)], stroke);
    };
    match icon {
        Icon::Send => {
            // Blade holder, collar and angled cutting tip.
            line((9.0, 3.0), (16.0, 3.0));
            line((9.0, 3.0), (9.0, 12.0));
            line((16.0, 3.0), (16.0, 12.0));
            line((7.0, 12.0), (18.0, 12.0));
            line((10.0, 12.0), (10.0, 21.0));
            line((15.0, 12.0), (15.0, 16.0));
            line((15.0, 16.0), (10.0, 21.0));
            line((4.0, 21.0), (7.0, 21.0));
            line((13.0, 21.0), (20.0, 21.0));
        }
        Icon::Marks => {
            for (x, y, dx, dy) in [
                (3.0, 3.0, 1.0, 1.0),
                (21.0, 3.0, -1.0, 1.0),
                (3.0, 21.0, 1.0, -1.0),
                (21.0, 21.0, -1.0, -1.0),
            ] {
                line((x, y), (x + dx * 5.0, y));
                line((x, y), (x, y + dy * 5.0));
            }
        }
        Icon::Settings => {
            painter.circle_stroke(point(12.0, 12.0), size * 6.0 / 24.0, stroke);
            painter.circle_stroke(point(12.0, 12.0), size * 2.2 / 24.0, stroke);
            for (dx, dy) in [
                (1.0, 0.0),
                (0.707, 0.707),
                (0.0, 1.0),
                (-0.707, 0.707),
                (-1.0, 0.0),
                (-0.707, -0.707),
                (0.0, -1.0),
                (0.707, -0.707),
            ] {
                line(
                    (12.0 + dx * 6.0, 12.0 + dy * 6.0),
                    (12.0 + dx * 9.0, 12.0 + dy * 9.0),
                );
            }
        }
        Icon::Boundary => {
            for (a, b) in [
                ((6.0, 4.0), (18.0, 4.0)),
                ((20.0, 6.0), (20.0, 18.0)),
                ((18.0, 20.0), (6.0, 20.0)),
                ((4.0, 18.0), (4.0, 6.0)),
            ] {
                line(a, b);
            }
            for (x, y) in [(4.0, 4.0), (20.0, 4.0), (4.0, 20.0), (20.0, 20.0)] {
                painter.rect_filled(
                    Rect::from_center_size(point(x, y), egui::vec2(size / 6.0, size / 6.0)),
                    0.0,
                    color,
                );
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn only_exact_bannercut_commands_get_icons() {
        for (name, icon) in [
            ("Enviar", Icon::Send),
            ("Marcas", Icon::Marks),
            ("Configurar", Icon::Settings),
            ("Limite", Icon::Boundary),
        ] {
            assert_eq!(
                for_key(&format!("menu.macro.SSPrintBannerCut|BannerCut|{name}")),
                Some(icon)
            );
        }
        for key in [
            "menu.file.export",
            "menu.macro.Other|BannerCut|Enviar",
            "menu.macro.SSPrintBannerCut|Other|Enviar",
            "menu.macro.SSPrintBannerCut|BannerCut|EnviarExtra",
            "",
        ] {
            assert_eq!(for_key(key), None);
        }
    }

    #[test]
    fn pictures_fit_the_button_at_small_and_large_sizes() {
        for size in [16.0, 24.0, 32.0] {
            for color in [Color32::BLACK, Color32::WHITE] {
                for icon in [Icon::Send, Icon::Marks, Icon::Settings, Icon::Boundary] {
                    let ctx = egui::Context::default();
                    let rect = Rect::from_min_size(egui::pos2(40.0, 40.0), egui::vec2(size, size));
                    let output = ctx.run_ui(egui::RawInput::default(), |ui| {
                        let painter = ui.ctx().layer_painter(egui::LayerId::new(
                            egui::Order::Foreground,
                            egui::Id::new("macro_icon_test"),
                        ));
                        draw(&painter, rect, icon, color);
                    });
                    assert!(!output.shapes.is_empty());
                    for shape in output.shapes {
                        let bounds = shape.shape.visual_bounding_rect();
                        assert!(rect.contains_rect(bounds), "{icon:?}: {bounds:?}");
                    }
                }
            }
        }
    }
}
