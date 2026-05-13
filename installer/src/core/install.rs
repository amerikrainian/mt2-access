//! Zip download/extraction and install bookkeeping.
//!
//! This installer is adapted with permission from the SayTheSpire2 installer:
//! https://github.com/bradjrenshaw/say-the-spire2

use std::collections::BTreeSet;
use std::fs;
use std::io::{Cursor, Read};
use std::path::Path;

use serde::{Deserialize, Serialize};

use super::paths::{
    appdata_mod_dir, install_manifest_file, is_bootstrap_path, is_mod_owned_path, normalized,
    should_skip_zip_entry, version_file, INSTALLER_USER_AGENT,
};

#[derive(Default, Deserialize, Serialize)]
pub struct InstallManifest {
    pub files: Vec<String>,
}

pub fn get_installed_version() -> Option<String> {
    fs::read_to_string(version_file())
        .ok()
        .map(|s| s.trim().to_string())
}

pub fn save_installed_version(version: &str) -> Result<(), String> {
    let dir = appdata_mod_dir();
    fs::create_dir_all(&dir).map_err(|e| format!("Failed to create directory: {}", e))?;
    fs::write(version_file(), version).map_err(|e| format!("Failed to write version: {}", e))
}

pub fn read_install_manifest() -> InstallManifest {
    let path = install_manifest_file();
    if !path.exists() {
        return InstallManifest::default();
    }

    match fs::read_to_string(&path) {
        Ok(json) => serde_json::from_str(&json).unwrap_or_default(),
        Err(_) => InstallManifest::default(),
    }
}

pub fn save_install_manifest(manifest: &InstallManifest) -> Result<(), String> {
    let dir = appdata_mod_dir();
    fs::create_dir_all(&dir).map_err(|e| format!("Failed to create directory: {}", e))?;
    let json = serde_json::to_string_pretty(manifest)
        .map_err(|e| format!("Failed to serialize install manifest: {}", e))?;
    fs::write(install_manifest_file(), json)
        .map_err(|e| format!("Failed to write install manifest: {}", e))
}

pub fn download_and_extract(
    url: &str,
    game_path: &Path,
    progress: impl Fn(u32),
) -> Result<(), String> {
    let client = reqwest::blocking::Client::builder()
        .user_agent(INSTALLER_USER_AGENT)
        .timeout(std::time::Duration::from_secs(120))
        .build()
        .map_err(|e| format!("Failed to create HTTP client: {}", e))?;

    let resp = client
        .get(url)
        .send()
        .map_err(|e| format!("Download failed: {}", e))?;

    if !resp.status().is_success() {
        return Err(format!("Download returned status {}", resp.status()));
    }

    let total = resp.content_length().unwrap_or(0);
    let mut reader = resp;
    let mut buffer = Vec::new();
    let mut downloaded: u64 = 0;
    let mut buf = [0u8; 8192];

    loop {
        let n = reader
            .read(&mut buf)
            .map_err(|e| format!("Read error: {}", e))?;
        if n == 0 {
            break;
        }

        buffer.extend_from_slice(&buf[..n]);
        downloaded += n as u64;
        if total > 0 {
            progress((downloaded * 100 / total) as u32);
        }
    }

    extract_zip(&buffer, game_path)
}

pub fn install_from_file(zip_path: &Path, game_path: &Path) -> Result<(), String> {
    let data = fs::read(zip_path).map_err(|e| format!("Failed to read zip: {}", e))?;
    extract_zip(&data, game_path)
}

pub fn extract_zip(data: &[u8], dest: &Path) -> Result<(), String> {
    let cursor = Cursor::new(data);
    let mut archive =
        zip::ZipArchive::new(cursor).map_err(|e| format!("Failed to open zip: {}", e))?;

    let previous_manifest = read_install_manifest();
    let previously_owned: BTreeSet<String> = previous_manifest.files.into_iter().collect();
    let mut installed_files: BTreeSet<String> = previously_owned.clone();

    for i in 0..archive.len() {
        let mut file = archive
            .by_index(i)
            .map_err(|e| format!("Failed to read zip entry: {}", e))?;

        let Some(enclosed_name) = file.enclosed_name() else {
            continue;
        };
        let relative = normalize_path(&enclosed_name);
        if relative.is_empty() || should_skip_zip_entry(&relative) {
            continue;
        }

        let out_path = dest.join(&enclosed_name);
        if file.is_dir() || file.name().ends_with('/') {
            fs::create_dir_all(&out_path)
                .map_err(|e| format!("Failed to create dir {}: {}", relative, e))?;
            continue;
        }

        let existed_before = out_path.exists();
        let owned_before = previously_owned.contains(&relative);
        let mod_owned = is_mod_owned_path(&relative);
        let bootstrap = is_bootstrap_path(&relative);

        if existed_before && !owned_before && !mod_owned && bootstrap {
            continue;
        }

        if existed_before && !owned_before && !mod_owned && !bootstrap {
            continue;
        }

        if let Some(parent) = out_path.parent() {
            fs::create_dir_all(parent)
                .map_err(|e| format!("Failed to create parent dir for {}: {}", relative, e))?;
        }

        let mut out_file = fs::File::create(&out_path)
            .map_err(|e| format!("Failed to create file {}: {}", relative, e))?;
        std::io::copy(&mut file, &mut out_file)
            .map_err(|e| format!("Failed to write file {}: {}", relative, e))?;

        if !existed_before || owned_before || mod_owned {
            installed_files.insert(relative);
        }
    }

    save_install_manifest(&InstallManifest {
        files: installed_files.into_iter().collect(),
    })
}

fn normalize_path(path: &Path) -> String {
    normalized(&path.to_string_lossy())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Write;

    #[test]
    fn extract_zip_creates_mod_files() {
        let dir = tempfile::tempdir().unwrap();
        let mut zip_buf = Vec::new();
        {
            let cursor = Cursor::new(&mut zip_buf);
            let mut writer = zip::ZipWriter::new(cursor);
            let options = zip::write::SimpleFileOptions::default();
            writer
                .start_file("BepInEx/plugins/MonsterTrainAccessibility.dll", options)
                .unwrap();
            writer.write_all(b"dll").unwrap();
            writer.finish().unwrap();
        }

        extract_zip(&zip_buf, dir.path()).unwrap();
        assert!(dir
            .path()
            .join("BepInEx/plugins/MonsterTrainAccessibility.dll")
            .exists());
    }

    #[test]
    fn extract_zip_skips_existing_bootstrap_file() {
        let dir = tempfile::tempdir().unwrap();
        fs::write(dir.path().join("winhttp.dll"), "existing").unwrap();

        let mut zip_buf = Vec::new();
        {
            let cursor = Cursor::new(&mut zip_buf);
            let mut writer = zip::ZipWriter::new(cursor);
            let options = zip::write::SimpleFileOptions::default();
            writer.start_file("winhttp.dll", options).unwrap();
            writer.write_all(b"new").unwrap();
            writer.finish().unwrap();
        }

        extract_zip(&zip_buf, dir.path()).unwrap();
        assert_eq!(
            fs::read_to_string(dir.path().join("winhttp.dll")).unwrap(),
            "existing"
        );
    }
}
