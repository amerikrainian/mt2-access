//! Game directory detection.
//!
//! This installer is adapted with permission from the SayTheSpire2 installer:
//! https://github.com/bradjrenshaw/say-the-spire2

use std::path::{Path, PathBuf};

use regex::Regex;

use super::paths::{steam_defaults, GAME_DIR_NAME, MOD_DLL};

const VALIDATION_MARKERS: &[&str] = &["MonsterTrain2.exe", "MonsterTrain2_Data"];

pub fn detect_game_path() -> Option<PathBuf> {
    for steam_dir in steam_defaults() {
        let vdf_path = steam_dir.join("steamapps").join("libraryfolders.vdf");
        if vdf_path.exists() {
            if let Ok(content) = std::fs::read_to_string(&vdf_path) {
                for lib_path in parse_vdf_library_paths(&content) {
                    let game_path = lib_path
                        .join("steamapps")
                        .join("common")
                        .join(GAME_DIR_NAME);
                    if validate_game_path(&game_path) {
                        return Some(game_path);
                    }
                }
            }
        }

        let default_path = steam_dir
            .join("steamapps")
            .join("common")
            .join(GAME_DIR_NAME);
        if validate_game_path(&default_path) {
            return Some(default_path);
        }
    }

    None
}

pub fn parse_vdf_library_paths(content: &str) -> Vec<PathBuf> {
    let re = Regex::new(r#""path"\s+"([^"]+)""#).unwrap();
    re.captures_iter(content)
        .map(|cap| PathBuf::from(cap[1].replace("\\\\", "\\")))
        .collect()
}

pub fn validate_game_path(path: &Path) -> bool {
    VALIDATION_MARKERS
        .iter()
        .any(|marker| path.join(marker).exists())
}

pub fn is_mod_installed(game_path: &Path) -> bool {
    game_path.join(MOD_DLL).exists()
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;

    #[test]
    fn parse_vdf_single_path() {
        let content = r#"
        "0"
        {
            "path" "C:\\Program Files (x86)\\Steam"
        }
        "#;
        let paths = parse_vdf_library_paths(content);
        assert_eq!(paths, vec![PathBuf::from("C:\\Program Files (x86)\\Steam")]);
    }

    #[test]
    fn validate_game_path_with_exe() {
        let dir = tempfile::tempdir().unwrap();
        fs::write(dir.path().join("MonsterTrain2.exe"), "").unwrap();
        assert!(validate_game_path(dir.path()));
    }

    #[test]
    fn validate_game_path_empty_dir() {
        let dir = tempfile::tempdir().unwrap();
        assert!(!validate_game_path(dir.path()));
    }

    #[test]
    fn is_mod_installed_true() {
        let dir = tempfile::tempdir().unwrap();
        let plugins = dir.path().join("BepInEx").join("plugins");
        fs::create_dir_all(&plugins).unwrap();
        fs::write(plugins.join("MonsterTrainAccessibility.dll"), "").unwrap();
        assert!(is_mod_installed(dir.path()));
    }
}
