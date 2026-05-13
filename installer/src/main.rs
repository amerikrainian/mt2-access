#![windows_subsystem = "windows"]

// Monster Train 2 Accessibility installer.
//
// This installer is adapted with permission from the SayTheSpire2 installer:
// https://github.com/bradjrenshaw/say-the-spire2

mod cli;
mod core;
mod gui;

fn main() {
    if std::env::args().any(|a| a == "--cli") {
        attach_console();
        cli::run();
    } else {
        gui::run();
    }
}

/// Attach to the parent console so --cli mode can do stdin/stdout.
fn attach_console() {
    #[cfg(target_os = "windows")]
    unsafe {
        windows_sys::Win32::System::Console::AttachConsole(
            windows_sys::Win32::System::Console::ATTACH_PARENT_PROCESS,
        );
    }
}
