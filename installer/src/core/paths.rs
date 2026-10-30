// Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
// https://github.com/rashadnaqeeb/NonVisualCalculus

use std::path::{Path, PathBuf};

// The full release list, newest first: one call serves both the latest-version
// lookup and the per-version release notes shown after an update.
pub const GITHUB_RELEASES_URL: &str =
    "https://api.github.com/repos/amerikrainian/mt2-access/releases?per_page=100";
pub const MOD_ZIP_PREFIX: &str = "MonsterTrainAccessibility-v";
pub const MOD_ZIP_SUFFIX: &str = ".zip";
pub const GAME_EXES: &[&str] = &["MonsterTrain2.exe"];
pub const GAME_FOLDERS: &[&str] = &["Monster Train 2"];
// The game is Unity on Mono: its code is Assembly-CSharp.dll in the data folder's Managed dir, in
// every install, so together with the exe it identifies the game dir.
pub const GAME_ASSEMBLY_MARKER: &str = "MonsterTrain2_Data/Managed/Assembly-CSharp.dll";
pub const PLUGIN_REL: &str =
    "BepInEx/plugins/MonsterTrainAccessibility/MonsterTrainAccessibility.dll";
pub const MANIFEST_REL: &str = "BepInEx/config/MonsterTrainAccessibility/install.json";
pub const BACKUPS_REL: &str = "BepInEx/config/MonsterTrainAccessibility/backups";

// The single-DLL layout every release before 0.3.0 shipped (the main branch), straight into
// BepInEx/plugins. Its plugin has the same assembly name and BepInEx GUID as the folder layout's
// host, so the two must never load side by side: an install over it removes these files.
pub const LEGACY_PLUGIN_REL: &str = "BepInEx/plugins/MonsterTrainAccessibility.dll";
pub const LEGACY_FILES: &[&str] = &[
    LEGACY_PLUGIN_REL,
    // Shipped by both layouts; adopted so the new copy replaces it without a backup.
    "prism.dll",
    "BepInEx/plugins/MonsterTrainAccessibility.deps.json",
    "BepInEx/plugins/MonsterTrainAccessibility.pdb",
    // Its System.Text.Json family, shipped loose into plugins. Removed only beside the legacy
    // plugin (or listed by the legacy installer), so a copy another mod placed alone is kept.
    "BepInEx/plugins/System.Text.Json.dll",
    "BepInEx/plugins/System.Text.Encodings.Web.dll",
    "BepInEx/plugins/System.Memory.dll",
    "BepInEx/plugins/System.Buffers.dll",
    "BepInEx/plugins/System.Runtime.CompilerServices.Unsafe.dll",
    "BepInEx/plugins/Microsoft.Bcl.AsyncInterfaces.dll",
    "BepInEx/plugins/System.Numerics.Vectors.dll",
    "BepInEx/plugins/System.Threading.Tasks.Extensions.dll",
    // Its strings (the folder layout reads lang/<code>.txt beside the plugin).
    "BepInEx/plugins/Localization/Strings/de.json",
    "BepInEx/plugins/Localization/Strings/en.json",
    "BepInEx/plugins/Localization/Strings/es.json",
    "BepInEx/plugins/Localization/Strings/fr.json",
    "BepInEx/plugins/Localization/Strings/ja.json",
    "BepInEx/plugins/Localization/Strings/ko.json",
    "BepInEx/plugins/Localization/Strings/pt.json",
    "BepInEx/plugins/Localization/Strings/ru.json",
    "BepInEx/plugins/Localization/Strings/zh-CN.json",
    "BepInEx/plugins/Localization/Strings/zh-TW.json",
];
// The manual both layouts ship; the legacy one's pages are adopted so stale ones are pruned.
pub const DOCS_DIR_REL: &str = "MT2AccessibilityDocs";
// The legacy installer's state, under %APPDATA%: `version` (also written by the legacy plugin on
// every load) and `install_manifest.json` ({"files": [...]}, the game-relative files it owned).
pub const LEGACY_STATE_DIR: &str = "MonsterTrainAccessibility";
pub const LEGACY_STATE_FILES: &[&str] = &["version", "install_manifest.json"];

pub fn manifest_path(game_dir: &Path) -> PathBuf {
    game_dir.join(MANIFEST_REL)
}

/// %APPDATA%\MonsterTrainAccessibility, where the legacy installer kept its state.
pub fn legacy_state_dir() -> Option<PathBuf> {
    std::env::var_os("APPDATA").map(|appdata| PathBuf::from(appdata).join(LEGACY_STATE_DIR))
}

pub fn normalize_rel(path: &str) -> String {
    path.replace('\\', "/").trim_start_matches("./").to_string()
}

pub fn required_loader_files() -> &'static [&'static str] {
    &[
        "winhttp.dll",
        "doorstop_config.ini",
        "BepInEx/core/BepInEx.dll",
        PLUGIN_REL,
    ]
}
