//! Monster Train 2 installer constants.
//!
//! This installer is adapted with permission from the SayTheSpire2 installer:
//! https://github.com/bradjrenshaw/say-the-spire2

use std::path::PathBuf;

pub const GITHUB_API_URL: &str =
    "https://api.github.com/repos/amerikrainian/mt2-access/releases/latest";
pub const GITHUB_RELEASES_URL: &str =
    "https://api.github.com/repos/amerikrainian/mt2-access/releases";
pub const GAME_DIR_NAME: &str = "Monster Train 2";
pub const INSTALLER_USER_AGENT: &str = "MonsterTrainAccessibilityInstaller";
pub const BEPINEX_BOOTSTRAP_URL: &str =
    "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.4/BepInEx_win_x64_5.4.23.4.zip";

pub const MOD_DLL: &str = "BepInEx/plugins/MonsterTrainAccessibility.dll";

pub const MOD_OWNED_PATHS: &[&str] = &[
    "BepInEx/plugins/MonsterTrainAccessibility.dll",
    "BepInEx/plugins/MonsterTrainAccessibility.deps.json",
    "BepInEx/plugins/System.Text.Json.dll",
    "BepInEx/plugins/System.Text.Encodings.Web.dll",
    "BepInEx/plugins/System.Memory.dll",
    "BepInEx/plugins/System.Buffers.dll",
    "BepInEx/plugins/System.Runtime.CompilerServices.Unsafe.dll",
    "BepInEx/plugins/Microsoft.Bcl.AsyncInterfaces.dll",
    "BepInEx/plugins/System.Numerics.Vectors.dll",
    "BepInEx/plugins/System.Threading.Tasks.Extensions.dll",
];

pub const MOD_OWNED_PREFIXES: &[&str] = &["BepInEx/plugins/Localization/", "BepInEx/core/"];

pub const SKIP_ZIP_PREFIXES: &[&str] = &["BepInEx/cache/"];

pub const SKIP_ZIP_FILES: &[&str] = &[
    "BepInEx/LogOutput.log",
    "BepInEx/plugins/MonsterTrainAccessibility.pdb",
    "readme.html",
];

/// Files we used to ship or may install as bootstrap/shared files. Uninstall
/// only removes these if the install manifest says this installer created them.
pub const BOOTSTRAP_FILES: &[&str] = &[
    ".doorstop_version",
    "doorstop_config.ini",
    "winhttp.dll",
    "prism.dll",
];

pub fn user_data_dir() -> PathBuf {
    if cfg!(target_os = "windows") {
        dirs::config_dir()
            .unwrap_or_else(|| PathBuf::from("C:\\Users\\Default\\AppData\\Roaming"))
            .join("MonsterTrainAccessibility")
    } else if cfg!(target_os = "macos") {
        dirs::home_dir()
            .unwrap_or_else(|| PathBuf::from("/"))
            .join("Library")
            .join("Application Support")
            .join("MonsterTrainAccessibility")
    } else {
        dirs::home_dir()
            .unwrap_or_else(|| PathBuf::from("/"))
            .join(".local")
            .join("share")
            .join("MonsterTrainAccessibility")
    }
}

pub fn appdata_mod_dir() -> PathBuf {
    user_data_dir()
}

pub fn version_file() -> PathBuf {
    appdata_mod_dir().join("version")
}

pub fn install_manifest_file() -> PathBuf {
    appdata_mod_dir().join("install_manifest.json")
}

pub fn steam_defaults() -> Vec<PathBuf> {
    if cfg!(target_os = "windows") {
        vec![PathBuf::from("C:\\Program Files (x86)\\Steam")]
    } else if cfg!(target_os = "macos") {
        let home = dirs::home_dir().unwrap_or_else(|| PathBuf::from("/"));
        vec![home
            .join("Library")
            .join("Application Support")
            .join("Steam")]
    } else {
        let home = dirs::home_dir().unwrap_or_else(|| PathBuf::from("/"));
        vec![
            home.join(".steam").join("steam"),
            home.join(".local").join("share").join("Steam"),
        ]
    }
}

pub fn normalized(path: &str) -> String {
    path.replace('\\', "/")
}

pub fn is_mod_owned_path(path: &str) -> bool {
    let path = normalized(path);
    MOD_OWNED_PATHS.iter().any(|p| *p == path)
        || MOD_OWNED_PREFIXES
            .iter()
            .any(|prefix| path.starts_with(prefix))
}

pub fn should_skip_zip_entry(path: &str) -> bool {
    let path = normalized(path);
    SKIP_ZIP_FILES.iter().any(|p| *p == path)
        || SKIP_ZIP_PREFIXES
            .iter()
            .any(|prefix| path.starts_with(prefix))
}

pub fn is_bootstrap_path(path: &str) -> bool {
    let path = normalized(path);
    BOOTSTRAP_FILES.iter().any(|p| *p == path)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn user_data_dir_is_not_empty() {
        let dir = user_data_dir();
        assert!(dir.to_string_lossy().contains("MonsterTrainAccessibility"));
    }

    #[test]
    fn mod_owned_paths_cover_plugin_and_localization() {
        assert!(is_mod_owned_path(
            "BepInEx/plugins/MonsterTrainAccessibility.dll"
        ));
        assert!(is_mod_owned_path(
            "BepInEx/plugins/Localization/strings/en.json"
        ));
        assert!(is_mod_owned_path("BepInEx/core/BepInEx.dll"));
    }

    #[test]
    fn skip_entries_cover_logs_and_cache() {
        assert!(should_skip_zip_entry("BepInEx/LogOutput.log"));
        assert!(should_skip_zip_entry(
            "BepInEx/cache/chainloader_typeloader.dat"
        ));
        assert!(!should_skip_zip_entry("BepInEx/core/BepInEx.dll"));
    }
}
