//! The window icon
//!
//! The window takes the icon of the executable it is running inside rather than a picture of its own. That keeps one
//! source of truth: whatever the build stamps into the executable is what the taskbar shows, so the two cannot drift
//! apart. Replacing the icon is then a matter of replacing the file the build embeds, not of also feeding a copy to this
//! layer.
//!
//! Everything here is best effort. A window without an icon is not a failure worth reporting, and no path through this
//! module is allowed to stop the window from coming up.

/// window_icon builds the icon for a window, or None when there is nothing to take it from
pub fn window_icon() -> Option<winit::window::Icon> {
    #[cfg(windows)]
    {
        windows_icon()
    }

    #[cfg(not(windows))]
    {
        None
    }
}

/// The icon group the build writes into the executable's resources
#[cfg(windows)]
const ICON_RESOURCE_ID: u16 = 1;

/// IMAGE_ICON the resource type LoadImageW expects
#[cfg(windows)]
const IMAGE_ICON: u32 = 1;

/// LR_DEFAULTSIZE lets the system pick the size its own icon slots use instead of a number this layer would have to guess
#[cfg(windows)]
const LR_DEFAULTSIZE: u32 = 0x0000_0040;

/// DIB_RGB_COLORS the pixel data is requested in, rather than a palette index and a colour table
#[cfg(windows)]
const DIB_RGB_COLORS: u32 = 0;

/// BI_RGB the only compression accepted here; icons are never stored compressed
#[cfg(windows)]
const BI_RGB: u32 = 0;

#[cfg(windows)]
#[repr(C)]
struct IconInfo {
    is_icon: i32,
    hot_x: u32,
    hot_y: u32,
    mask: *mut std::ffi::c_void,
    color: *mut std::ffi::c_void,
}

#[cfg(windows)]
#[repr(C)]
struct Bitmap {
    kind: u32,
    width: i32,
    height: i32,
    width_bytes: u32,
    planes: u16,
    bits_pixel: u16,
    bits: *mut std::ffi::c_void,
}

#[cfg(windows)]
#[repr(C)]
struct BitmapInfoHeader {
    size: u32,
    width: i32,
    height: i32,
    planes: u16,
    bit_count: u16,
    compression: u32,
    size_image: u32,
    x_pels_per_meter: i32,
    y_pels_per_meter: i32,
    clr_used: u32,
    clr_important: u32,
}

#[cfg(windows)]
#[repr(C)]
#[derive(Clone, Copy, Default)]
struct RgbQuad {
    blue: u8,
    green: u8,
    red: u8,
    reserved: u8,
}

#[cfg(windows)]
#[repr(C)]
struct BitmapInfo {
    header: BitmapInfoHeader,
    colors: [RgbQuad; 1],
}

#[cfg(windows)]
#[link(name = "kernel32")]
extern "system" {
    fn GetModuleHandleW(name: *const u16) -> *mut std::ffi::c_void;
}

#[cfg(windows)]
#[link(name = "user32")]
extern "system" {
    fn LoadImageW(
        instance: *mut std::ffi::c_void,
        name: *const u16,
        kind: u32,
        cx: i32,
        cy: i32,
        flags: u32,
    ) -> *mut std::ffi::c_void;
    fn GetIconInfo(icon: *mut std::ffi::c_void, info: *mut IconInfo) -> i32;
    fn DestroyIcon(icon: *mut std::ffi::c_void) -> i32;
    fn GetDC(window: *mut std::ffi::c_void) -> *mut std::ffi::c_void;
    fn ReleaseDC(window: *mut std::ffi::c_void, dc: *mut std::ffi::c_void) -> i32;
}

#[cfg(windows)]
#[link(name = "gdi32")]
extern "system" {
    fn GetObjectW(object: *mut std::ffi::c_void, size: i32, out: *mut std::ffi::c_void) -> i32;
    fn GetDIBits(
        dc: *mut std::ffi::c_void,
        bitmap: *mut std::ffi::c_void,
        start: u32,
        lines: u32,
        bits: *mut std::ffi::c_void,
        info: *mut BitmapInfo,
        usage: u32,
    ) -> i32;
    fn DeleteObject(object: *mut std::ffi::c_void) -> i32;
}

/// windows_icon reads the icon resource out of the running executable
#[cfg(windows)]
fn windows_icon() -> Option<winit::window::Icon> {
    unsafe {
        // A null name asks for the executable itself, which is the module carrying the stamped-in icon
        let module = GetModuleHandleW(std::ptr::null());
        if module.is_null() {
            return None;
        }

        // The resource id arrives as a pointer rather than a string whenever it fits in a word, so the low id is cast
        let icon = LoadImageW(
            module,
            ICON_RESOURCE_ID as usize as *const u16,
            IMAGE_ICON,
            0,
            0,
            LR_DEFAULTSIZE,
        );
        if icon.is_null() {
            return None;
        }

        let result = icon_to_rgba(icon);
        // The handle came from LoadImageW, so this module owns it; the window keeps its own copy of the pixels
        DestroyIcon(icon);
        result
    }
}

/// icon_to_rgba pulls the colour bitmap out of an icon and hands back top-down RGBA bytes
#[cfg(windows)]
unsafe fn icon_to_rgba(icon: *mut std::ffi::c_void) -> Option<winit::window::Icon> {
    let mut info: IconInfo = std::mem::zeroed();
    if GetIconInfo(icon, &mut info) == 0 {
        return None;
    }

    let pixels = read_color_bitmap(info.color);

    // Both bitmaps are freshly created by GetIconInfo and belong to the caller from here on
    if !info.mask.is_null() {
        DeleteObject(info.mask);
    }
    if !info.color.is_null() {
        DeleteObject(info.color);
    }

    let (pixels, width, height) = pixels?;
    winit::window::Icon::from_rgba(pixels, width, height).ok()
}

/// read_color_bitmap copies a bitmap's pixels out as RGBA
#[cfg(windows)]
unsafe fn read_color_bitmap(bitmap: *mut std::ffi::c_void) -> Option<(Vec<u8>, u32, u32)> {
    if bitmap.is_null() {
        return None;
    }

    let mut size: Bitmap = std::mem::zeroed();
    if GetObjectW(
        bitmap,
        std::mem::size_of::<Bitmap>() as i32,
        &mut size as *mut _ as *mut std::ffi::c_void,
    ) == 0
    {
        return None;
    }

    let width = size.width.unsigned_abs();
    let stated = size.height.unsigned_abs();
    if width == 0 || stated == 0 {
        return None;
    }

    // An icon bitmap is stored with the AND mask sharing its allocation, which shows up here as double the height
    let height = if stated == width * 2 { stated / 2 } else { stated };

    let mut header: BitmapInfo = std::mem::zeroed();
    header.header.size = std::mem::size_of::<BitmapInfoHeader>() as u32;
    header.header.width = width as i32;
    // Negative keeps the rows top-down, which is the order the icon expects
    header.header.height = -(height as i32);
    header.header.planes = 1;
    header.header.bit_count = 32;
    header.header.compression = BI_RGB;

    let mut pixels = vec![0u8; width as usize * height as usize * 4];
    let dc = GetDC(std::ptr::null_mut());
    if dc.is_null() {
        return None;
    }

    let copied = GetDIBits(
        dc,
        bitmap,
        0,
        height,
        pixels.as_mut_ptr() as *mut std::ffi::c_void,
        &mut header,
        DIB_RGB_COLORS,
    );
    ReleaseDC(std::ptr::null_mut(), dc);

    if copied == 0 {
        return None;
    }

    // The bitmap comes out blue first
    for pixel in pixels.chunks_exact_mut(4) {
        pixel.swap(0, 2);
    }

    // Some icons carry no alpha channel at all and encode their transparency in the mask instead; treating those as
    // opaque is wrong at the edges but far better than handing back a fully transparent icon
    if pixels.chunks_exact(4).all(|pixel| pixel[3] == 0) {
        for pixel in pixels.chunks_exact_mut(4) {
            pixel[3] = 0xFF;
        }
    }

    Some((pixels, width, height))
}
