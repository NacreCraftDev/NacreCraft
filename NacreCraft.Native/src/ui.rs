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

/// The request channel, created the first time a window is asked for
/// None records a thread that could not be started, so the next request does not try again
static SENDER: OnceLock<Option<Sender<Request>>> = OnceLock::new();

/// The event loop proxy, used to wake a waiting loop when a request arrives
static PROXY: OnceLock<EventLoopProxy<UserEvent>> = OnceLock::new();

/// One report to show
pub struct Request {
    pub title: String,
    pub summary: String,
    pub report_path: PathBuf,
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
    let event_loop = match EventLoop::<UserEvent>::with_user_event().build() {
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
    report_path: PathBuf,
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

        let body = match std::fs::read_to_string(&request.report_path) {
            Ok(text) => text,
            Err(error) => format!("(the report could not be read: {error})"),
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
            ui.heading(&shown.title);
            ui.add_space(2.0);
            ui.label(&shown.summary);
            ui.separator();

            // The report owns the middle of the window and scrolls on its own, so the buttons stay where they are no
            // matter how long the report runs
            let scroll_height = (ui.available_height() - 44.0).max(80.0);
            egui::ScrollArea::vertical()
                .max_height(scroll_height)
                .auto_shrink([false, false])
                .show(ui, |ui| {
                    ui.add(egui::Label::new(egui::RichText::new(shown.body.as_str()).monospace()));
                });

            ui.separator();
            ui.horizontal(|ui| {
                if ui.button(&shown.label_open_folder).clicked() {
                    open_report_location(&shown.report_path);
                }

                let copied = shown
                    .copied_at
                    .is_some_and(|at| at.elapsed() < COPIED_HINT);
                let copy_label = if copied { &shown.label_copied } else { &shown.label_copy };
                if ui.button(copy_label).clicked() {
                    ui.ctx().copy_text(shown.body.clone());
                    shown.copied_at = Some(Instant::now());
                }

                if ui.button(&shown.label_close).clicked() {
                    close = true;
                }
            });
        });

        unsafe {
            use glow::HasContext as _;
            if let Some(gl) = self.gl.as_ref() {
                gl.clear_color(0.10, 0.10, 0.12, 1.0);
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
        .with_inner_size(winit::dpi::LogicalSize { width: 760.0, height: 560.0 })
        .with_title("NacreCraft")
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
fn open_report_location(path: &Path) {
    #[cfg(target_os = "windows")]
    let spawned = std::process::Command::new("explorer")
        .arg(format!("/select,\"{}\"", path.display()))
        .spawn();

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
