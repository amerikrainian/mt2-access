//! Accessible GUI installer.
//!
//! This installer is adapted with permission from the SayTheSpire2 installer:
//! https://github.com/bradjrenshaw/say-the-spire2

use std::cell::RefCell;
use std::path::PathBuf;
use std::rc::Rc;

use wxdragon::prelude::*;

use crate::core::{detect, github, install, uninstall};

struct State {
    release: Option<github::ReleaseInfo>,
    all_releases: Vec<github::ReleaseInfo>,
}

pub fn run() {
    wxdragon::main(|_app| {
        let frame = Frame::builder()
            .with_title("Monster Train 2 Accessibility Installer")
            .with_size(Size::new(650, 500))
            .build();

        let panel = Panel::builder(&frame).build();
        let main_sizer = BoxSizer::builder(Orientation::Vertical).build();

        let status = StaticText::builder(&panel)
            .with_label("Detecting game directory...")
            .build();

        let path_sizer = BoxSizer::builder(Orientation::Horizontal).build();
        let path_label = StaticText::builder(&panel)
            .with_label("Game directory:")
            .build();
        let path_input = TextCtrl::builder(&panel).build();
        let browse_btn = Button::builder(&panel).with_label("Browse...").build();

        path_sizer.add(&path_label, 0, SizerFlag::All, 4);
        path_sizer.add(&path_input, 1, SizerFlag::Expand | SizerFlag::All, 4);
        path_sizer.add(&browse_btn, 0, SizerFlag::All, 4);

        let log = TextCtrl::builder(&panel)
            .with_style(
                TextCtrlStyle::MultiLine | TextCtrlStyle::ReadOnly | TextCtrlStyle::WordWrap,
            )
            .build();

        let btn_sizer = BoxSizer::builder(Orientation::Horizontal).build();
        let install_btn = Button::builder(&panel).with_label("Install").build();
        let install_file_btn = Button::builder(&panel)
            .with_label("Install from file...")
            .build();
        let uninstall_btn = Button::builder(&panel).with_label("Uninstall").build();

        btn_sizer.add_stretch_spacer(1);
        btn_sizer.add(&install_btn, 0, SizerFlag::All, 4);
        btn_sizer.add(&install_file_btn, 0, SizerFlag::All, 4);
        btn_sizer.add(&uninstall_btn, 0, SizerFlag::All, 4);

        main_sizer.add(&status, 0, SizerFlag::Expand | SizerFlag::All, 8);
        main_sizer.add_sizer(
            &path_sizer,
            0,
            SizerFlag::Expand | SizerFlag::Left | SizerFlag::Right,
            8,
        );
        main_sizer.add(&log, 1, SizerFlag::Expand | SizerFlag::All, 8);
        main_sizer.add_sizer(&btn_sizer, 0, SizerFlag::Expand | SizerFlag::All, 4);

        panel.set_sizer(main_sizer, true);

        install_btn.enable(false);
        install_file_btn.enable(false);
        uninstall_btn.enable(false);

        let state = Rc::new(RefCell::new(State {
            release: None,
            all_releases: Vec::new(),
        }));

        if let Some(detected) = detect::detect_game_path() {
            path_input.set_value(&detected.to_string_lossy());
            log_append(&log, &format!("Game directory: {}", detected.display()));
            update_state(
                &status,
                &install_btn,
                &install_file_btn,
                &uninstall_btn,
                &detected,
                &state,
                &log,
            );
        } else {
            status.set_label("Game directory not found. Please browse to select it.");
            log_append(&log, "Could not auto-detect game directory.");
        }

        match github::fetch_all_releases() {
            Ok(releases) => {
                let latest = releases.iter().find(|release| !release.prerelease).cloned();
                if let Some(info) = latest.as_ref() {
                    log_append(&log, &format!("Latest version: {}", info.tag_name));
                }
                {
                    let mut state_mut = state.borrow_mut();
                    state_mut.all_releases = releases;
                    state_mut.release = latest;
                }
                let path = PathBuf::from(path_input.get_value());
                if detect::validate_game_path(&path) {
                    update_state(
                        &status,
                        &install_btn,
                        &install_file_btn,
                        &uninstall_btn,
                        &path,
                        &state,
                        &log,
                    );
                }
            }
            Err(e) => {
                log_append(&log, &format!("Failed to check for updates: {}", e));
                status.set_label("Could not connect to GitHub. Install/update unavailable.");
            }
        }

        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            browse_btn.on_click(move |_| {
                let dialog =
                    DirDialog::builder(&frame_c, "Select Monster Train 2 game directory", "")
                        .build();
                if dialog.show_modal() != ID_OK {
                    return;
                }

                let Some(path_str) = dialog.get_path() else {
                    return;
                };
                let path = PathBuf::from(&path_str);
                apply_game_path(
                    &path,
                    &path_input_c,
                    &status_c,
                    &install_btn_c,
                    &install_file_btn_c,
                    &uninstall_btn_c,
                    &log_c,
                    &state_c,
                );
            });
        }

        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let browse_btn_c = browse_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            install_btn.on_click(move |_| {
                let game_path = PathBuf::from(path_input_c.get_value());
                let borrow = state_c.borrow();
                if borrow.all_releases.is_empty() {
                    return;
                }

                let choices: Vec<String> = borrow
                    .all_releases
                    .iter()
                    .map(|release| {
                        if release.prerelease {
                            format!("{} (pre-release)", release.tag_name)
                        } else {
                            release.tag_name.clone()
                        }
                    })
                    .collect();
                let choice_refs: Vec<&str> = choices.iter().map(|choice| choice.as_str()).collect();
                drop(borrow);

                let dialog = SingleChoiceDialog::builder(
                    &frame_c,
                    "Select a version to install:",
                    "Choose Version",
                    &choice_refs,
                )
                .build();

                if dialog.show_modal() != ID_OK {
                    return;
                }
                let selection = dialog.get_selection();
                if selection < 0 {
                    return;
                }

                let borrow = state_c.borrow();
                let info = &borrow.all_releases[selection as usize];
                if !confirm_release_notes(&frame_c, info) {
                    return;
                }

                let Some(asset) = github::find_zip_asset(&info.assets) else {
                    log_append(&log_c, "Error: No .zip asset found in the release.");
                    return;
                };

                let url = asset.browser_download_url.clone();
                let version = info.tag_name.clone();
                drop(borrow);

                set_buttons_enabled(
                    false,
                    &install_btn_c,
                    &install_file_btn_c,
                    &uninstall_btn_c,
                    &browse_btn_c,
                );
                log_append(&log_c, "Downloading...");
                status_c.set_label("Downloading...");

                let result = install::download_and_extract(&url, &game_path, |_pct| {});

                set_buttons_enabled(
                    true,
                    &install_btn_c,
                    &install_file_btn_c,
                    &uninstall_btn_c,
                    &browse_btn_c,
                );

                match result {
                    Ok(()) => {
                        if let Err(e) = install::save_installed_version(&version) {
                            log_append(&log_c, &format!("Warning: {}", e));
                        }
                        log_append(
                            &log_c,
                            &format!("Successfully installed version {}.", version),
                        );
                        update_state(
                            &status_c,
                            &install_btn_c,
                            &install_file_btn_c,
                            &uninstall_btn_c,
                            &game_path,
                            &state_c,
                            &log_c,
                        );
                        MessageDialog::builder(
                            &frame_c,
                            &format!(
                                "Monster Train 2 Accessibility version {} installed successfully.",
                                version
                            ),
                            "Installation Complete",
                        )
                        .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconInformation)
                        .build()
                        .show_modal();
                    }
                    Err(e) => {
                        log_append(&log_c, &format!("Error: {}", e));
                        MessageDialog::builder(&frame_c, &e, "Installation Failed")
                            .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconError)
                            .build()
                            .show_modal();
                    }
                }
            });
        }

        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            install_file_btn.on_click(move |_| {
                let game_path = PathBuf::from(path_input_c.get_value());
                let dialog = FileDialog::builder(&frame_c)
                    .with_message("Select mod zip file")
                    .with_wildcard("Zip files (*.zip)|*.zip")
                    .with_style(FileDialogStyle::Open | FileDialogStyle::FileMustExist)
                    .build();

                if dialog.show_modal() != ID_OK {
                    return;
                }
                let Some(zip_path_str) = dialog.get_path() else {
                    return;
                };
                let zip_path = PathBuf::from(&zip_path_str);

                match install::install_from_file(&zip_path, &game_path) {
                    Ok(()) => {
                        log_append(
                            &log_c,
                            &format!(
                                "Installed from {}.",
                                zip_path.file_name().unwrap_or_default().to_string_lossy()
                            ),
                        );
                        update_state(
                            &status_c,
                            &install_btn_c,
                            &install_file_btn_c,
                            &uninstall_btn_c,
                            &game_path,
                            &state_c,
                            &log_c,
                        );
                        MessageDialog::builder(
                            &frame_c,
                            "Monster Train 2 Accessibility installed successfully from file.",
                            "Installation Complete",
                        )
                        .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconInformation)
                        .build()
                        .show_modal();
                    }
                    Err(e) => {
                        log_append(&log_c, &format!("Error: {}", e));
                        MessageDialog::builder(&frame_c, &e, "Error")
                            .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconError)
                            .build()
                            .show_modal();
                    }
                }
            });
        }

        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            uninstall_btn.on_click(move |_| {
                let game_path = PathBuf::from(path_input_c.get_value());
                let dialog = MessageDialog::builder(
                    &frame_c,
                    "Remove files installed by the Monster Train 2 Accessibility installer?",
                    "Confirm Uninstall",
                )
                .with_style(MessageDialogStyle::YesNo | MessageDialogStyle::IconQuestion)
                .build();

                if dialog.show_modal() != ID_YES {
                    return;
                }

                let removed = uninstall::uninstall_mod(&game_path);
                if removed.is_empty() {
                    log_append(&log_c, "No installer-owned mod files found to remove.");
                } else {
                    log_append(&log_c, &format!("Removed: {}", removed.join(", ")));
                }

                let remove_state = MessageDialog::builder(
                    &frame_c,
                    "Also remove installer version and manifest state?",
                    "Remove Installer State",
                )
                .with_style(MessageDialogStyle::YesNo | MessageDialogStyle::IconQuestion)
                .build()
                .show_modal();

                if remove_state == ID_YES {
                    match uninstall::remove_installer_state() {
                        Ok(()) => log_append(&log_c, "Removed installer state."),
                        Err(e) => log_append(&log_c, &format!("Error: {}", e)),
                    }
                }

                update_state(
                    &status_c,
                    &install_btn_c,
                    &install_file_btn_c,
                    &uninstall_btn_c,
                    &game_path,
                    &state_c,
                    &log_c,
                );
                MessageDialog::builder(
                    &frame_c,
                    "Monster Train 2 Accessibility has been uninstalled.",
                    "Uninstall Complete",
                )
                .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconInformation)
                .build()
                .show_modal();
            });
        }

        frame.show(true);
    })
    .expect("Failed to start application");
}

fn confirm_release_notes(parent: &impl WxWidget, info: &github::ReleaseInfo) -> bool {
    if info.body.is_empty() {
        return true;
    }

    MessageDialog::builder(
        parent,
        &format!(
            "Release notes for {}:\n\n{}\n\nProceed?",
            info.tag_name, info.body
        ),
        &format!("Install {}", info.tag_name),
    )
    .with_style(MessageDialogStyle::YesNo | MessageDialogStyle::IconQuestion)
    .build()
    .show_modal()
        == ID_YES
}

fn apply_game_path(
    path: &std::path::Path,
    path_input: &TextCtrl,
    status: &StaticText,
    install_btn: &Button,
    install_file_btn: &Button,
    uninstall_btn: &Button,
    log: &TextCtrl,
    state: &Rc<RefCell<State>>,
) {
    if !detect::validate_game_path(path) {
        log_append(log, &format!("Invalid game directory: {}", path.display()));
        status.set_label("Invalid game directory. Please browse to select it.");
        return;
    }

    path_input.set_value(&path.to_string_lossy());
    log_append(log, &format!("Game directory: {}", path.display()));
    update_state(
        status,
        install_btn,
        install_file_btn,
        uninstall_btn,
        path,
        state,
        log,
    );
}

fn update_state(
    status: &StaticText,
    install_btn: &Button,
    install_file_btn: &Button,
    uninstall_btn: &Button,
    game_path: &std::path::Path,
    state: &Rc<RefCell<State>>,
    log: &TextCtrl,
) {
    let mod_installed = detect::is_mod_installed(game_path);
    let installed_version = install::get_installed_version();
    let has_valid_path = detect::validate_game_path(game_path);

    install_file_btn.enable(has_valid_path);
    uninstall_btn.enable(mod_installed);

    if mod_installed {
        log_append(
            log,
            &format!(
                "Installed version: {}",
                installed_version.as_deref().unwrap_or("unknown")
            ),
        );
    }

    let borrow = state.borrow();
    if let Some(info) = borrow.release.as_ref() {
        let latest = &info.tag_name;
        if !has_valid_path {
            install_btn.enable(false);
            return;
        }

        if !mod_installed {
            install_btn.set_label("Install");
            install_btn.enable(true);
            status.set_label(&format!("Ready to install version {}.", latest));
        } else if is_up_to_date(installed_version.as_deref(), latest) {
            install_btn.set_label("Install");
            install_btn.enable(true);
            status.set_label(&format!(
                "Monster Train 2 Accessibility is up to date (version {}). Select Install to choose a version.",
                latest
            ));
        } else {
            install_btn.set_label("Update");
            install_btn.enable(true);
            status.set_label(&format!(
                "Update available: {} to {}",
                installed_version.as_deref().unwrap_or("unknown"),
                latest
            ));
        }
    }
}

fn set_buttons_enabled(
    enabled: bool,
    install_btn: &Button,
    install_file_btn: &Button,
    uninstall_btn: &Button,
    browse_btn: &Button,
) {
    install_btn.enable(enabled);
    install_file_btn.enable(enabled);
    uninstall_btn.enable(enabled);
    browse_btn.enable(enabled);
}

fn parse_version(s: &str) -> Option<semver::Version> {
    let trimmed = s
        .strip_prefix('v')
        .or_else(|| s.strip_prefix('V'))
        .unwrap_or(s);
    semver::Version::parse(trimmed).ok()
}

fn is_up_to_date(installed: Option<&str>, latest: &str) -> bool {
    let Some(installed) = installed else {
        return false;
    };
    match (parse_version(installed), parse_version(latest)) {
        (Some(installed_version), Some(latest_version)) => installed_version >= latest_version,
        _ => installed == latest,
    }
}

fn log_append(log: &TextCtrl, msg: &str) {
    let current = log.get_value();
    if current.is_empty() {
        log.set_value(msg);
    } else {
        log.set_value(&format!("{}\n{}", current, msg));
    }
}
