//! The error window for a crash or a failed mod load
//!
//! The managed side decides what to say and hands the text over; this module only draws it. It lives in the native
//! layer because the cases it has to cover are the ones where the managed side is already gone: the report is written
//! on the way down, and a window owned by the managed runtime would die with it.
//!
//! The window runs on a thread of its own with its own event loop. That loop stays alive between requests and serves
//! them one at a time, so a second failure does not need a second loop and the first window does not keep the process
//! from reporting the next one.
//!
//! Every failure here is reported back to the caller rather than raised: a window that cannot come up must never be
//! what stops the report on disk from being written.

use crate::logging;
use egui_glow::egui_winit::winit;
use std::num::NonZeroU32;
use std::path::{Path, PathBuf};
use std::sync::mpsc::{self, Receiver, Sender};
use std::sync::{Arc, OnceLock};
use std::time::{Duration, Instant};

use winit::application::ApplicationHandler;
use winit::event::{StartCause, WindowEvent};
use winit::event_loop::{ActiveEventLoop, ControlFlow, EventLoop, EventLoopProxy};
use winit::raw_window_handle::HasWindowHandle as _;
use winit::window::WindowId;

/// How long the caller waits for the window thread to become ready before giving up on it
const STARTUP_TIMEOUT: Duration = Duration::from_secs(30);

/// How long the copy button keeps saying it copied before going back to its normal label
const COPIED_HINT: Duration = Duration::from_secs(2);

/// Window background, painted by the panel the whole layout sits in
const PANEL_FILL: egui::Color32 = egui::Color32::from_rgb(0x16, 0x16, 0x1A);

/// The report sits in its own recessed box, so the frame around it has somewhere to show
const REPORT_FILL: egui::Color32 = egui::Color32::from_rgb(0x0D, 0x0D, 0x11);
const REPORT_BORDER: egui::Color32 = egui::Color32::from_rgb(0x36, 0x36, 0x42);

/// Tone for the summary line, which is context rather than the headline
const MUTED: egui::Color32 = egui::Color32::from_rgb(0xB2, 0xB2, 0xBE);

/// The request channel, created the first time a window is asked for
/// None records a thread that could not be started, so the next request does not try again
static SENDER: OnceLock<Option<Sender<Request>>> = OnceLock::new();

/// The event loop proxy, used to wake a waiting loop when a request arrives
static PROXY: OnceLock<EventLoopProxy<UserEvent>> = OnceLock::new();

/// One report to show
pub struct Request {
    pub title: String,
    pub summary: String,
    /// Text to draw as-is; when it is absent the window falls back to reading report_path
    pub body: Option<String>,
    /// Report file to read and to locate with the open-folder button; absent when the caller has no file
    pub report_path: Option<PathBuf>,
    pub label_open_folder: String,
    pub label_copy: String,
    pub label_copied: String,
    pub label_close: String,
    /// Present when the caller wants to wait for the window to be closed; dropped when the window is done with it
    pub done: Option<Sender<()>>,
}

/// Events the loop handles besides window input
enum UserEvent {
    /// A request is waiting on the channel
    Wake,
    /// egui asked for a repaint after a delay
    Redraw(Duration),
}

/// show hands one report over to the window thread
///
/// Returns 0 once the request was queued. Anything else means the window will not appear, and the caller should carry
/// on with the report on disk alone
pub fn show(request: Request) -> i32 {
    let Some(sender) = SENDER.get_or_init(start) else {
        return 1;
    };

    if sender.send(request).is_err() {
        return 2;
    }

    match PROXY.get() {
        Some(proxy) => match proxy.send_event(UserEvent::Wake) {
            Ok(()) => 0,
            Err(_) => 3,
        },
        None => 4,
    }
}

/// start brings up the window thread and waits until its event loop is ready to be woken
/// The wait matters: a request sent before the proxy exists would never wake anything, and the caller would sit there
/// behind a window that never opens
fn start() -> Option<Sender<Request>> {
    let (sender, receiver) = mpsc::channel::<Request>();
    let (ready_sender, ready_receiver) = mpsc::channel::<bool>();

    let spawned = std::thread::Builder::new()
        .name("ncn-error-window".to_string())
        .spawn(move || run(receiver, ready_sender));

    if let Err(error) = spawned {
        logging::write(&format!("the error window thread did not start: {error}"));
        return None;
    }

    match ready_receiver.recv_timeout(STARTUP_TIMEOUT) {
        Ok(true) => Some(sender),
        Ok(false) => None,
        Err(_) => {
            logging::write("the error window thread was not ready in time, the report stays on disk");
            None
        }
    }
}

/// run owns the event loop for the lifetime of the process
fn run(receiver: Receiver<Request>, ready: Sender<bool>) {
    let mut builder = EventLoop::<UserEvent>::with_user_event();

    // The window lives on a dedicated thread rather than the process main thread, because the host already owns the
    // main thread and this component must stay out of its way. Windows only tolerates that when the builder is told to
    // allow it; without the flag the loop panics outright instead of failing to build
    #[cfg(windows)]
    {
        use winit::platform::windows::EventLoopBuilderExtWindows as _;
        builder.with_any_thread(true);
    }

    let event_loop = match builder.build() {
        Ok(event_loop) => event_loop,
        Err(error) => {
            logging::write(&format!("the error window event loop could not be created: {error}"));
            let _ = ready.send(false);
            return;
        }
    };

    let _ = PROXY.set(event_loop.create_proxy());
    let _ = ready.send(true);

    let mut runner = Runner {
        receiver,
        window: None,
        gl: None,
        egui: None,
        current: None,
        repaint_delay: Duration::MAX,
    };

    if let Err(error) = event_loop.run_app(&mut runner) {
        logging::write(&format!("the error window event loop stopped: {error}"));
    }
}

/// What the window is currently showing
struct Shown {
    title: String,
    summary: String,
    body: String,
    report_path: Option<PathBuf>,
    label_open_folder: String,
    label_copy: String,
    label_copied: String,
    label_close: String,
    copied_at: Option<Instant>,
    done: Option<Sender<()>>,
}

struct Runner {
    receiver: Receiver<Request>,
    window: Option<GlutinWindow>,
    gl: Option<Arc<glow::Context>>,
    egui: Option<egui_glow::EguiGlow>,
    current: Option<Shown>,
    repaint_delay: Duration,
}

impl Runner {
    /// pump opens the next waiting request when no window is up
    /// A request that cannot be shown is dropped, which is also what releases anyone waiting on it
    fn pump(&mut self, event_loop: &ActiveEventLoop) {
        if self.window.is_some() {
            return;
        }

        while let Ok(request) = self.receiver.try_recv() {
            match self.open(event_loop, request) {
                Ok(()) => return,
                Err(message) => logging::write(&format!("the error window could not be shown: {message}")),
            }
        }
    }

    /// open builds a window with its own OpenGL context and starts showing one report
    fn open(&mut self, event_loop: &ActiveEventLoop, request: Request) -> Result<(), String> {
        let (window, gl) = create_window(event_loop)?;
        let gl = Arc::new(gl);
        let egui = egui_glow::EguiGlow::new(event_loop, Arc::clone(&gl), None, None, true);

        // egui asks for repaints through this; the loop is woken with a proxy because it may be parked in Wait
        if let Some(proxy) = PROXY.get() {
            let proxy = egui::mutex::Mutex::new(proxy.clone());
            egui.egui_ctx.set_request_repaint_callback(move |info| {
                let _ = proxy.lock().send_event(UserEvent::Redraw(info.delay));
            });
        }

        install_fonts(&egui.egui_ctx);
        apply_style(&egui.egui_ctx);

        // A caller that has no report file hands the text over directly; only a crash path reads it back off disk
        let body = match request.body {
            Some(text) => text,
            None => match request.report_path.as_ref() {
                Some(path) => match std::fs::read_to_string(path) {
                    Ok(text) => text,
                    Err(error) => format!("(the report could not be read: {error})"),
                },
                None => String::new(),
            },
        };

        window.window.set_title(&request.title);
        window.window.set_visible(true);
        window.window.request_redraw();

        self.current = Some(Shown {
            title: request.title,
            summary: request.summary,
            body,
            report_path: request.report_path,
            label_open_folder: request.label_open_folder,
            label_copy: request.label_copy,
            label_copied: request.label_copied,
            label_close: request.label_close,
            copied_at: None,
            done: request.done,
        });
        self.window = Some(window);
        self.gl = Some(gl);
        self.egui = Some(egui);
        Ok(())
    }

    /// draw runs one egui frame and paints it; returns whether the user asked to close
    fn draw(&mut self) -> bool {
        let Some(window) = self.window.as_ref() else {
            return false;
        };
        let (Some(egui), Some(shown)) = (self.egui.as_mut(), self.current.as_mut()) else {
            return false;
        };

        let mut close = false;
        egui.run(&window.window, |ui| {
            // The ui handed in by run_ui covers the whole window with no margin and no background of its own, so every
            // widget would sit flush against the window edge. The panels below supply both

            // The action row is a panel of its own: reserving room for it by hand is what pushed it off the bottom,
            // and a panel claims its height before the report measures what is left
            egui::Panel::bottom("ncn-error-actions")
                .frame(background_frame(egui::Margin { left: 20, right: 20, top: 12, bottom: 16 }))
                .show(ui, |ui| {
                    // Right aligned, so the actions sit where a dialog puts them instead of trailing the text
                    ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                        if ui.button(&shown.label_close).clicked() {
                            close = true;
                        }

                        let copied = shown
                            .copied_at
                            .is_some_and(|at| at.elapsed() < COPIED_HINT);
                        let copy_label = if copied { &shown.label_copied } else { &shown.label_copy };
                        if ui.button(copy_label).clicked() {
                            ui.ctx().copy_text(shown.body.clone());
                            shown.copied_at = Some(Instant::now());
                        }

                        // Nothing to locate when the text was handed over directly, so the button is left out
                        if let Some(path) = shown.report_path.as_ref() {
                            if ui.button(&shown.label_open_folder).clicked() {
                                open_report_location(path);
                            }
                        }
                    });
                });

            egui::Panel::top("ncn-error-header")
                .frame(background_frame(egui::Margin { left: 20, right: 20, top: 18, bottom: 12 }))
                .show(ui, |ui| {
                    ui.heading(&shown.title);
                    ui.add_space(4.0);
                    ui.label(egui::RichText::new(&shown.summary).size(15.0).color(MUTED));
                });

            // CentralPanel has to come last, once the other two have taken their share
            egui::CentralPanel::default()
                .frame(background_frame(egui::Margin { left: 20, right: 20, top: 0, bottom: 0 }))
                .show(ui, |ui| {
                    let report = egui::Frame::new()
                        .fill(REPORT_FILL)
                        .stroke(egui::Stroke::new(1.0, REPORT_BORDER))
                        .corner_radius(egui::CornerRadius::same(6))
                        .inner_margin(egui::Margin::same(14));

                    report.show(ui, |ui| {
                        // The card fills what is left over, minus its own padding and border
                        let inner_height = (ui.available_height() - 30.0).max(60.0);
                        ui.set_min_height(inner_height);
                        egui::ScrollArea::vertical()
                            .auto_shrink([false, false])
                            .show(ui, |ui| {
                                // The size is spelled out rather than inherited: a monospace override otherwise keeps
                                // whatever size the body style carries, tying the report to the label settings
                                ui.add(egui::Label::new(
                                    egui::RichText::new(shown.body.as_str()).monospace().size(15.0),
                                ));
                            });
                    });
                });
        });

        unsafe {
            use glow::HasContext as _;
            if let Some(gl) = self.gl.as_ref() {
                // Matches the panel fill set in apply_style, so no strip of a different shade shows at the edges
                gl.clear_color(0.086, 0.086, 0.102, 1.0);
                gl.clear(glow::COLOR_BUFFER_BIT);
            }
        }

        egui.paint(&window.window);
        let _ = window.swap_buffers();
        close
    }

    /// control_flow parks the loop unless a frame is due
    fn control_flow(&self) -> ControlFlow {
        if self.window.is_none() {
            return ControlFlow::Wait;
        }

        if self.repaint_delay.is_zero() {
            if let Some(window) = self.window.as_ref() {
                window.window.request_redraw();
            }
            return ControlFlow::Poll;
        }

        match Instant::now().checked_add(self.repaint_delay) {
            Some(deadline) => ControlFlow::WaitUntil(deadline),
            None => ControlFlow::Wait,
        }
    }

    /// dismiss tears the window down and releases anyone waiting on it
    fn dismiss(&mut self) {
        self.release();
        if let Some(shown) = self.current.take() {
            if let Some(done) = shown.done {
                // The caller wakes up here and carries on with whatever it was doing before the report went up
                let _ = done.send(());
            }
        }
        self.repaint_delay = Duration::MAX;
    }

    /// release drops the graphics resources; egui goes first because destroying it needs the context still current
    fn release(&mut self) {
        if let Some(mut egui) = self.egui.take() {
            egui.destroy();
        }
        self.gl = None;
        self.window = None;
    }
}

impl ApplicationHandler<UserEvent> for Runner {
    fn resumed(&mut self, event_loop: &ActiveEventLoop) {
        self.pump(event_loop);
    }

    fn user_event(&mut self, event_loop: &ActiveEventLoop, event: UserEvent) {
        match event {
            UserEvent::Wake => self.pump(event_loop),
            UserEvent::Redraw(delay) => self.repaint_delay = delay,
        }
    }

    fn window_event(&mut self, _event_loop: &ActiveEventLoop, _id: WindowId, event: WindowEvent) {
        match event {
            WindowEvent::CloseRequested | WindowEvent::Destroyed => self.dismiss(),
            WindowEvent::Resized(size) => {
                if let Some(window) = self.window.as_ref() {
                    window.resize(size);
                }
            }
            WindowEvent::RedrawRequested => {
                if self.draw() {
                    self.dismiss();
                }
            }
            other => {
                let repaint = match (self.egui.as_mut(), self.window.as_ref()) {
                    (Some(egui), Some(window)) => egui.on_window_event(&window.window, &other).repaint,
                    _ => false,
                };
                if repaint {
                    if let Some(window) = self.window.as_ref() {
                        window.window.request_redraw();
                    }
                }
            }
        }
    }

    fn about_to_wait(&mut self, event_loop: &ActiveEventLoop) {
        // A request can arrive while the loop is parked; this is where the next one is picked up
        self.pump(event_loop);
        event_loop.set_control_flow(self.control_flow());
    }

    fn new_events(&mut self, _event_loop: &ActiveEventLoop, cause: StartCause) {
        if let StartCause::ResumeTimeReached { .. } = cause {
            if let Some(window) = self.window.as_ref() {
                window.window.request_redraw();
            }
        }
    }

    fn exiting(&mut self, _event_loop: &ActiveEventLoop) {
        self.release();
    }
}

/// A window together with the OpenGL context and surface drawn into it
struct GlutinWindow {
    window: winit::window::Window,
    context: glutin::context::PossiblyCurrentContext,
    //Kept so the GL library and the display connection outlive the surface built over them; dropping it early would
    //tear the context down along with it
    #[allow(dead_code)]
    display: glutin::display::Display,
    surface: glutin::surface::Surface<glutin::surface::WindowSurface>,
}

impl GlutinWindow {
    fn resize(&self, size: winit::dpi::PhysicalSize<u32>) {
        use glutin::surface::GlSurface as _;
        self.surface.resize(
            &self.context,
            size.width.try_into().unwrap_or(NonZeroU32::MIN),
            size.height.try_into().unwrap_or(NonZeroU32::MIN),
        );
    }

    fn swap_buffers(&self) -> glutin::error::Result<()> {
        use glutin::surface::GlSurface as _;
        self.surface.swap_buffers(&self.context)
    }
}

/// create_window builds the window, its OpenGL context and the glow loader over the same connection
fn create_window(event_loop: &ActiveEventLoop) -> Result<(GlutinWindow, glow::Context), String> {
    use glutin::context::NotCurrentGlContext as _;
    use glutin::display::GetGlDisplay as _;
    use glutin::display::GlDisplay as _;
    use glutin::prelude::GlSurface as _;

    let attributes = winit::window::WindowAttributes::default()
        .with_resizable(true)
        .with_inner_size(winit::dpi::LogicalSize { width: 900.0, height: 640.0 })
        .with_title("NacreCraft")
        // Taken from the executable rather than shipped alongside it, so the taskbar entry and the file on disk always
        // show the same icon; a missing one simply leaves the window without
        .with_window_icon(crate::icon::window_icon())
        // Kept hidden until the first frame is painted, so the window never flashes an empty background
        .with_visible(false);

    let template = glutin::config::ConfigTemplateBuilder::new()
        .prefer_hardware_accelerated(None)
        .with_depth_size(0)
        .with_stencil_size(0)
        .with_transparency(false);

    let (window, config) = glutin_winit::DisplayBuilder::new()
        .with_preference(glutin_winit::ApiPreference::FallbackEgl)
        .with_window_attributes(Some(attributes.clone()))
        .build(event_loop, template, |mut configs| {
            // An empty iterator here means the driver offers nothing this window can draw into
            configs.next().unwrap_or_else(|| panic!("no OpenGL configuration matched"))
        })
        .map_err(|error| format!("no OpenGL configuration: {error}"))?;

    let display = config.display();
    let raw_handle = match window.as_ref() {
        Some(window) => Some(
            window
                .window_handle()
                .map_err(|error| format!("window handle: {error}"))?
                .as_raw(),
        ),
        None => None,
    };

    let context_attributes = glutin::context::ContextAttributesBuilder::new().build(raw_handle);
    let fallback_attributes = glutin::context::ContextAttributesBuilder::new()
        .with_context_api(glutin::context::ContextApi::Gles(None))
        .build(raw_handle);

    let not_current = unsafe {
        display
            .create_context(&config, &context_attributes)
            .or_else(|_| display.create_context(&config, &fallback_attributes))
            .map_err(|error| format!("no OpenGL context: {error}"))?
    };

    let window = match window {
        Some(window) => window,
        None => glutin_winit::finalize_window(event_loop, attributes, &config)
            .map_err(|error| format!("no window: {error}"))?,
    };

    let size = window.inner_size();
    let width = NonZeroU32::new(size.width).unwrap_or(NonZeroU32::MIN);
    let height = NonZeroU32::new(size.height).unwrap_or(NonZeroU32::MIN);
    let surface_attributes = glutin::surface::SurfaceAttributesBuilder::<glutin::surface::WindowSurface>::new()
        .build(
            window
                .window_handle()
                .map_err(|error| format!("window handle: {error}"))?
                .as_raw(),
            width,
            height,
        );

    let surface = unsafe {
        display
            .create_window_surface(&config, &surface_attributes)
            .map_err(|error| format!("no window surface: {error}"))?
    };

    let context = not_current
        .make_current(&surface)
        .map_err(|error| format!("no current context: {error}"))?;
    let _ = surface.set_swap_interval(&context, glutin::surface::SwapInterval::Wait(NonZeroU32::MIN));

    let gl = unsafe {
        glow::Context::from_loader_function(|name| match std::ffi::CString::new(name) {
            Ok(name) => display.get_proc_address(&name),
            Err(_) => std::ptr::null(),
        })
    };

    Ok((GlutinWindow { window, context, display, surface }, gl))
}

/// install_fonts adds a system font that covers CJK, which the built-in set does not
/// Without it every Chinese string in the window would come out as boxes
fn install_fonts(ctx: &egui::Context) {
    let Some(bytes) = load_system_font() else {
        logging::write("no CJK system font was found, the error window falls back to the built-in font");
        return;
    };

    let mut fonts = egui::FontDefinitions::default();
    fonts
        .font_data
        .insert("ncn-cjk".to_owned(), Arc::new(egui::FontData::from_owned(bytes)));
    for family in [egui::FontFamily::Proportional, egui::FontFamily::Monospace] {
        fonts.families.entry(family).or_default().insert(0, "ncn-cjk".to_owned());
    }
    ctx.set_fonts(fonts);
}

/// apply_style tunes the theme for reading a report
/// The default theme draws body text at 12.5 points in a mid grey, which is the wrong thing to hand someone who has to
/// scan a stack of log lines; the sizes below are set for a window that is mostly text rather than mostly controls
fn apply_style(ctx: &egui::Context) {
    const FILL: egui::Color32 = egui::Color32::from_rgb(0x16, 0x16, 0x1A);

    // Pinned to dark: the fills below are picked against a dark background, and a light system theme would otherwise
    // flip the window away from them
    ctx.set_theme(egui::Theme::Dark);

    ctx.all_styles_mut(|style| {
        // An override beats the per-widget colours: labels, headings and the monospace body all read as one tone
        style.visuals.override_text_color = Some(egui::Color32::from_rgb(0xF2, 0xF2, 0xF2));

        style.text_styles = [
            (egui::TextStyle::Heading, egui::FontId::proportional(28.0)),
            (egui::TextStyle::Body, egui::FontId::proportional(17.0)),
            (egui::TextStyle::Monospace, egui::FontId::monospace(16.0)),
            (egui::TextStyle::Button, egui::FontId::proportional(17.0)),
            (egui::TextStyle::Small, egui::FontId::proportional(14.0)),
        ]
        .into();

        style.spacing.item_spacing = egui::vec2(10.0, 10.0);
        style.spacing.button_padding = egui::vec2(16.0, 8.0);
        style.spacing.scroll.bar_width = 12.0;

        style.visuals.panel_fill = FILL;
        style.visuals.window_fill = FILL;
        // The buttons stay visible against the panel instead of blending into it
        style.visuals.widgets.inactive.bg_fill = egui::Color32::from_rgb(0x2E, 0x2E, 0x36);
        style.visuals.widgets.hovered.bg_fill = egui::Color32::from_rgb(0x3C, 0x3C, 0x48);
        style.visuals.widgets.active.bg_fill = egui::Color32::from_rgb(0x4A, 0x4A, 0x58);
    });
}

/// background_frame is the shared panel background with the given padding
/// Every panel uses the same fill, so the window reads as one surface rather than three stacked bars
fn background_frame(margin: egui::Margin) -> egui::Frame {
    egui::Frame::new().fill(PANEL_FILL).inner_margin(margin)
}

/// load_system_font reads the first usable font it finds
/// Only single-face files are listed: the loader cannot open a .ttc collection, so those paths would fail at draw time
/// rather than here
fn load_system_font() -> Option<Vec<u8>> {
    const CANDIDATES: &[&str] = &[
        "C:/Windows/Fonts/simhei.ttf",
        "C:/Windows/Fonts/Deng.ttf",
        "C:/Windows/Fonts/simkai.ttf",
        "C:/Windows/Fonts/msyh.ttf",
        "/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttf",
        "/usr/share/fonts/opentype/noto/NotoSansCJKsc-Regular.otf",
    ];

    for path in CANDIDATES {
        match std::fs::read(path) {
            Ok(bytes) => {
                logging::write(&format!("the error window uses the system font at {path}"));
                return Some(bytes);
            }
            Err(_) => continue,
        }
    }

    None
}

/// open_report_location shows the report in the platform's file manager
/// The path goes in unquoted: the argument is passed through to explorer as one token, and quotes written here would
/// be escaped into it, which explorer cannot parse, so it falls back to whatever folder it last had open
fn open_report_location(path: &Path) {
    #[cfg(target_os = "windows")]
    let spawned = {
        // Pointing /select at a file that is gone lands explorer in some unrelated folder, so the folder itself is the
        // fallback rather than an unparseable selection
        let target = if path.exists() {
            format!("/select,{}", path.display())
        } else {
            path.parent().unwrap_or(path).display().to_string()
        };
        std::process::Command::new("explorer").arg(target).spawn()
    };

    #[cfg(target_os = "macos")]
    let spawned = std::process::Command::new("open").arg("-R").arg(path).spawn();

    #[cfg(all(unix, not(target_os = "macos")))]
    let spawned = std::process::Command::new("xdg-open")
        .arg(path.parent().unwrap_or(path))
        .spawn();

    if let Err(error) = spawned {
        logging::write(&format!("the report location could not be opened: {error}"));
    }
}
