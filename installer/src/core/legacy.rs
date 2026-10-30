//! The single-DLL layout every release before 0.3.0 shipped, and the installer that managed it.
//! An install over it adopts its files as the prior install's, so `install_from_zip` overwrites
//! them without backups and prunes the ones the folder layout no longer ships (the plugin itself,
//! its System.Text.Json family, its Localization strings, stale manual pages), exactly as an
//! upgrade between two managed versions prunes. Its plugin shares the folder layout's assembly name
//! and BepInEx GUID, so it must never survive an install.

use std::collections::BTreeSet;
use std::fs;
use std::path::Path;

use serde::Deserialize;

use super::install::safe_zip_entry_name;
use super::paths;

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct LegacyInstall {
    /// Game-relative files of the legacy install that exist in the game dir: the legacy installer's
    /// own list (when it left one), every file the legacy layout shipped, and the manual's pages.
    pub owned: Vec<String>,
    /// The version the legacy plugin last recorded, for display.
    pub version: Option<String>,
}

#[derive(Deserialize)]
struct LegacyManifest {
    #[serde(default)]
    files: Vec<String>,
}

/// The legacy install in `game_dir`, if its plugin is there. `state_dir` is the legacy
/// installer's state folder (`paths::legacy_state_dir()`); its file list only widens what is
/// adopted, since it is per user, not per game dir: the plugin's presence is the evidence.
pub fn detect(game_dir: &Path, state_dir: Option<&Path>) -> Option<LegacyInstall> {
    if !game_dir.join(paths::LEGACY_PLUGIN_REL).is_file() {
        return None;
    }

    let mut owned = BTreeSet::new();
    let mut adopt = |rel: &str| {
        let Some(safe) = safe_zip_entry_name(rel) else {
            return;
        };
        let rel = paths::normalize_rel(safe.to_string_lossy().as_ref());
        if !rel.is_empty() && game_dir.join(&rel).is_file() {
            owned.insert(rel);
        }
    };

    for rel in paths::LEGACY_FILES {
        adopt(rel);
    }
    if let Some(manifest) = state_dir.and_then(read_manifest) {
        for rel in &manifest.files {
            adopt(rel);
        }
    }
    let mut docs = Vec::new();
    collect_files(&game_dir.join(paths::DOCS_DIR_REL), game_dir, &mut docs);
    for rel in &docs {
        adopt(rel);
    }

    let version = state_dir
        .and_then(|dir| fs::read_to_string(dir.join("version")).ok())
        .map(|v| v.trim().to_string())
        .filter(|v| !v.is_empty());

    Some(LegacyInstall {
        owned: owned.into_iter().collect(),
        version,
    })
}

/// Remove the legacy installer's state files, and their folder once empty. Run after an install
/// replaced the legacy layout: the folder layout's state is its manifest in the game dir.
pub fn clear_state(state_dir: Option<&Path>) -> Result<(), String> {
    let Some(dir) = state_dir else {
        return Ok(());
    };
    for name in paths::LEGACY_STATE_FILES {
        let path = dir.join(name);
        if path.exists() {
            fs::remove_file(&path)
                .map_err(|e| format!("Failed to remove {}: {e}", path.display()))?;
        }
    }
    let _ = fs::remove_dir(dir);
    Ok(())
}

fn read_manifest(state_dir: &Path) -> Option<LegacyManifest> {
    let text = fs::read_to_string(state_dir.join("install_manifest.json")).ok()?;
    serde_json::from_str(&text).ok()
}

fn collect_files(dir: &Path, game_dir: &Path, out: &mut Vec<String>) {
    let Ok(entries) = fs::read_dir(dir) else {
        return;
    };
    for entry in entries.flatten() {
        let path = entry.path();
        if path.is_dir() {
            collect_files(&path, game_dir, out);
        } else if let Ok(rel) = path.strip_prefix(game_dir) {
            out.push(paths::normalize_rel(rel.to_string_lossy().as_ref()));
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn write(dir: &Path, rel: &str, content: &str) {
        let path = dir.join(rel);
        fs::create_dir_all(path.parent().unwrap()).unwrap();
        fs::write(path, content).unwrap();
    }

    #[test]
    fn no_legacy_plugin_is_no_legacy_install() {
        let dir = tempfile::tempdir().unwrap();
        // A facade another mod placed alone is not evidence, and is never adopted.
        write(dir.path(), "BepInEx/plugins/System.Memory.dll", "other mod");
        assert!(detect(dir.path(), None).is_none());
    }

    #[test]
    fn adopts_existing_legacy_files_manifest_entries_and_docs() {
        let game = tempfile::tempdir().unwrap();
        let state = tempfile::tempdir().unwrap();
        write(game.path(), paths::LEGACY_PLUGIN_REL, "old plugin");
        write(game.path(), "BepInEx/plugins/System.Text.Json.dll", "json");
        write(game.path(), "BepInEx/plugins/Localization/Strings/en.json", "{}");
        write(game.path(), "BepInEx/core/BepInEx.dll", "loader");
        write(game.path(), "MT2AccessibilityDocs/index.html", "docs");
        write(game.path(), "BepInEx/plugins/Other/Other.dll", "another mod");
        write(
            state.path(),
            "install_manifest.json",
            r#"{"files": ["BepInEx/core/BepInEx.dll", "BepInEx/core/gone.dll", "../outside.dll"]}"#,
        );
        write(state.path(), "version", "0.2.3\n");

        let legacy = detect(game.path(), Some(state.path())).unwrap();
        assert_eq!(
            legacy.owned,
            vec![
                "BepInEx/core/BepInEx.dll",
                "BepInEx/plugins/Localization/Strings/en.json",
                "BepInEx/plugins/MonsterTrainAccessibility.dll",
                "BepInEx/plugins/System.Text.Json.dll",
                "MT2AccessibilityDocs/index.html",
            ]
        );
        assert_eq!(legacy.version.as_deref(), Some("0.2.3"));
    }

    #[test]
    fn clear_state_removes_the_state_folder() {
        let parent = tempfile::tempdir().unwrap();
        let state = parent.path().join(paths::LEGACY_STATE_DIR);
        write(&state, "version", "0.2.3");
        write(&state, "install_manifest.json", "{}");
        clear_state(Some(&state)).unwrap();
        assert!(!state.exists());
    }
}
