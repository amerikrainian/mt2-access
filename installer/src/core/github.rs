//! GitHub release lookup.
//!
//! This installer is adapted with permission from the SayTheSpire2 installer:
//! https://github.com/bradjrenshaw/say-the-spire2

use serde::Deserialize;

use super::paths::{GITHUB_API_URL, GITHUB_RELEASES_URL, INSTALLER_USER_AGENT};

#[derive(Debug, Deserialize, Clone)]
pub struct ReleaseInfo {
    pub tag_name: String,
    #[serde(default, deserialize_with = "deserialize_null_default")]
    pub body: String,
    #[serde(default)]
    pub prerelease: bool,
    #[serde(default)]
    pub assets: Vec<Asset>,
}

#[derive(Debug, Deserialize, Clone)]
pub struct Asset {
    pub name: String,
    pub browser_download_url: String,
}

pub fn fetch_latest_release() -> Result<ReleaseInfo, String> {
    let client = reqwest::blocking::Client::builder()
        .user_agent(INSTALLER_USER_AGENT)
        .timeout(std::time::Duration::from_secs(15))
        .build()
        .map_err(|e| format!("Failed to create HTTP client: {}", e))?;

    let resp = client
        .get(GITHUB_API_URL)
        .send()
        .map_err(|e| format!("Failed to connect to GitHub: {}", e))?;

    if !resp.status().is_success() {
        return Err(format!("GitHub API returned status {}", resp.status()));
    }

    resp.json::<ReleaseInfo>()
        .map_err(|e| format!("Failed to parse release info: {}", e))
}

pub fn fetch_all_releases() -> Result<Vec<ReleaseInfo>, String> {
    let client = reqwest::blocking::Client::builder()
        .user_agent(INSTALLER_USER_AGENT)
        .timeout(std::time::Duration::from_secs(15))
        .build()
        .map_err(|e| format!("Failed to create HTTP client: {}", e))?;

    let resp = client
        .get(GITHUB_RELEASES_URL)
        .send()
        .map_err(|e| format!("Failed to connect to GitHub: {}", e))?;

    if !resp.status().is_success() {
        return Err(format!("GitHub API returned status {}", resp.status()));
    }

    resp.json::<Vec<ReleaseInfo>>()
        .map_err(|e| format!("Failed to parse releases: {}", e))
}

pub fn find_zip_asset(assets: &[Asset]) -> Option<&Asset> {
    assets.iter().find(|a| a.name.ends_with(".zip"))
}

fn deserialize_null_default<'de, D>(deserializer: D) -> Result<String, D::Error>
where
    D: serde::Deserializer<'de>,
{
    Ok(Option::<String>::deserialize(deserializer)?.unwrap_or_default())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn find_zip_asset_with_zip() {
        let assets = vec![
            Asset {
                name: "readme.txt".to_string(),
                browser_download_url: "https://example.com/readme.txt".to_string(),
            },
            Asset {
                name: "MonsterTrainAccessibility-v0.1.3.zip".to_string(),
                browser_download_url: "https://example.com/mod.zip".to_string(),
            },
        ];
        let result = find_zip_asset(&assets);
        assert!(result.is_some());
        assert_eq!(result.unwrap().name, "MonsterTrainAccessibility-v0.1.3.zip");
    }

    #[test]
    fn find_zip_asset_no_zip() {
        let assets = vec![
            Asset {
                name: "readme.txt".to_string(),
                browser_download_url: "https://example.com/readme.txt".to_string(),
            },
            Asset {
                name: "source.tar.gz".to_string(),
                browser_download_url: "https://example.com/source.tar.gz".to_string(),
            },
        ];
        assert!(find_zip_asset(&assets).is_none());
    }

    #[test]
    fn find_zip_asset_empty() {
        let assets: Vec<Asset> = vec![];
        assert!(find_zip_asset(&assets).is_none());
    }

    #[test]
    fn find_zip_asset_first_zip() {
        let assets = vec![
            Asset {
                name: "first.zip".to_string(),
                browser_download_url: "https://example.com/first.zip".to_string(),
            },
            Asset {
                name: "second.zip".to_string(),
                browser_download_url: "https://example.com/second.zip".to_string(),
            },
        ];
        let result = find_zip_asset(&assets).unwrap();
        assert_eq!(result.name, "first.zip");
    }

    #[test]
    fn deserialize_release_info() {
        let json = r#"{
            "tag_name": "v0.1.3",
            "body": "Some release notes",
            "assets": [
                {
                    "name": "mod.zip",
                    "browser_download_url": "https://example.com/mod.zip"
                }
            ]
        }"#;
        let info: ReleaseInfo = serde_json::from_str(json).unwrap();
        assert_eq!(info.tag_name, "v0.1.3");
        assert_eq!(info.body, "Some release notes");
        assert_eq!(info.assets.len(), 1);
        assert_eq!(info.assets[0].name, "mod.zip");
    }

    #[test]
    fn deserialize_release_info_missing_optional_fields() {
        let json = r#"{"tag_name": "v0.1.0"}"#;
        let info: ReleaseInfo = serde_json::from_str(json).unwrap();
        assert_eq!(info.tag_name, "v0.1.0");
        assert_eq!(info.body, "");
        assert!(info.assets.is_empty());
    }

    #[test]
    fn deserialize_release_info_null_body() {
        let json = r#"{"tag_name": "v0.1.0", "body": null}"#;
        let info: ReleaseInfo = serde_json::from_str(json).unwrap();
        assert_eq!(info.body, "");
    }
}
