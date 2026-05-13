//! Uninstall only files recorded as installer-owned.
//!
//! This installer is adapted with permission from the SayTheSpire2 installer:
//! https://github.com/bradjrenshaw/say-the-spire2

use std::fs;
use std::path::{Path, PathBuf};

use super::install::{read_install_manifest, save_install_manifest, InstallManifest};
use super::paths::{appdata_mod_dir, install_manifest_file, version_file};

pub fn uninstall_mod(game_path: &Path) -> Vec<String> {
    let manifest = read_install_manifest();
    let mut removed = Vec::new();
    let mut kept = Vec::new();

    for file in manifest.files {
        let path = game_path.join(&file);
        if path.exists() {
            if fs::remove_file(&path).is_ok() {
                removed.push(file);
                remove_empty_parents(game_path, path.parent());
            } else {
                kept.push(file);
            }
        }
    }

    let _ = save_install_manifest(&InstallManifest { files: kept });
    if read_install_manifest().files.is_empty() {
        let _ = fs::remove_file(install_manifest_file());
    }

    removed
}

pub fn remove_installer_state() -> Result<(), String> {
    let _ = fs::remove_file(version_file());
    let _ = fs::remove_file(install_manifest_file());
    let dir = appdata_mod_dir();
    if dir.exists() && is_dir_empty(&dir) {
        fs::remove_dir_all(&dir).map_err(|e| format!("Failed to remove installer state: {}", e))?;
    }
    Ok(())
}

fn remove_empty_parents(game_path: &Path, parent: Option<&Path>) {
    let Some(parent) = parent else {
        return;
    };

    let mut current = PathBuf::from(parent);
    while current.starts_with(game_path) && current != game_path {
        if !is_dir_empty(&current) {
            break;
        }

        if fs::remove_dir(&current).is_err() {
            break;
        }

        let Some(next) = current.parent() else {
            break;
        };
        current = next.to_path_buf();
    }
}

fn is_dir_empty(path: &Path) -> bool {
    fs::read_dir(path)
        .map(|mut entries| entries.next().is_none())
        .unwrap_or(false)
}
