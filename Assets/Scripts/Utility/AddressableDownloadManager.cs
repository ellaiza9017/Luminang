using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class AddressableDownloadManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI statusText;

    [Header("Loading Groups (Assign the CheckOrLoad Images)")]
    public Image envLoadingIcon;
    public Image npcLoadingIcon;
    public Image audioLoadingIcon;
    public Image minigameLoadingIcon;

    [Header("Sprites")]
    public Sprite loadingSprite;
    public Sprite checkmarkSprite;
    
    [Header("Spin Settings")]
    public float spinSpeed = 360f; // degrees per second

    private AsyncOperationHandle downloadHandle;
    private float currentPercent = 0f;

    // The heavy scenes we packed into Addressables
    private string[] remoteKeys = new string[] { "Calle_Crisologo", "Magellan_s_Cross" };

    void Start()
    {
        // Add SSL Bypass to completely prevent "Curl error 60: Cert verify failed" on older Androids / VPNs
        Addressables.WebRequestOverride = (UnityEngine.Networking.UnityWebRequest req) => 
        {
            req.certificateHandler = new BypassCertificateHandler();
        };

        // Explicitly set all icons to the loading sprite at the start
        if (loadingSprite != null)
        {
            if (envLoadingIcon != null) envLoadingIcon.sprite = loadingSprite;
            if (npcLoadingIcon != null) npcLoadingIcon.sprite = loadingSprite;
            if (audioLoadingIcon != null) audioLoadingIcon.sprite = loadingSprite;
            if (minigameLoadingIcon != null) minigameLoadingIcon.sprite = loadingSprite;
        }

        if (progressBar != null) progressBar.value = 0f;
        if (progressText != null) progressText.text = "0%";
        if (statusText != null) statusText.text = "Checking for updates...";

        StartCoroutine(CheckAndDownloadDependencies());
    }

    void Update()
    {
        // Handle the spinning animation based on current progress chunks
        // 0-25%: Env, 25-50%: NPC, 50-75%: Audio, 75-100%: Minigames
        
        if (currentPercent < 0.25f && envLoadingIcon != null)
        {
            envLoadingIcon.rectTransform.Rotate(Vector3.forward, -spinSpeed * Time.deltaTime);
        }
        else if (currentPercent >= 0.25f && currentPercent < 0.50f && npcLoadingIcon != null)
        {
            npcLoadingIcon.rectTransform.Rotate(Vector3.forward, -spinSpeed * Time.deltaTime);
        }
        else if (currentPercent >= 0.50f && currentPercent < 0.75f && audioLoadingIcon != null)
        {
            audioLoadingIcon.rectTransform.Rotate(Vector3.forward, -spinSpeed * Time.deltaTime);
        }
        else if (currentPercent >= 0.75f && currentPercent < 1.0f && minigameLoadingIcon != null)
        {
            minigameLoadingIcon.rectTransform.Rotate(Vector3.forward, -spinSpeed * Time.deltaTime);
        }
    }

    private void UpdateCheckmarks()
    {
        // If passed a threshold, set the rotation back to 0 and apply the checkmark sprite
        if (currentPercent >= 0.25f && envLoadingIcon != null && envLoadingIcon.sprite != checkmarkSprite)
        {
            envLoadingIcon.rectTransform.localRotation = Quaternion.identity;
            envLoadingIcon.sprite = checkmarkSprite;
        }
        
        if (currentPercent >= 0.50f && npcLoadingIcon != null && npcLoadingIcon.sprite != checkmarkSprite)
        {
            npcLoadingIcon.rectTransform.localRotation = Quaternion.identity;
            npcLoadingIcon.sprite = checkmarkSprite;
        }

        if (currentPercent >= 0.75f && audioLoadingIcon != null && audioLoadingIcon.sprite != checkmarkSprite)
        {
            audioLoadingIcon.rectTransform.localRotation = Quaternion.identity;
            audioLoadingIcon.sprite = checkmarkSprite;
        }

        if (currentPercent >= 1.0f && minigameLoadingIcon != null && minigameLoadingIcon.sprite != checkmarkSprite)
        {
            minigameLoadingIcon.rectTransform.localRotation = Quaternion.identity;
            minigameLoadingIcon.sprite = checkmarkSprite;
        }
    }

    private IEnumerator CheckAndDownloadDependencies()
    {
        Debug.Log("[AddressableDownloadManager] Initializing Addressables...");
        yield return Addressables.InitializeAsync();

        // GitHub converts apostrophes (') to dots (.) in release asset filenames when uploaded.
        // e.g. "Magellan_s_Cross.bundle" is stored as "magellan.s_cross.bundle" on GitHub.
        // This transform makes every Addressables download URL match what GitHub actually stored.
        Addressables.ResourceManager.InternalIdTransformFunc = (loc) =>
        {
            if (loc.InternalId.Contains("'"))
                return loc.InternalId.Replace("'", ".");
            return loc.InternalId;
        };

        // Check if GitHub has a newer catalog (needed when you upload new content)
        // If catalogs match (same build), this is instant and does nothing.
        // If you pushed new bundles and rebuilt the catalog, this picks them up.
        Debug.Log("[AddressableDownloadManager] Checking for catalog updates...");
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result != null && checkHandle.Result.Count > 0)
        {
            Debug.Log($"[AddressableDownloadManager] Found {checkHandle.Result.Count} catalog update(s). Updating...");
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
            yield return updateHandle;
            if (updateHandle.IsValid()) Addressables.Release(updateHandle);
        }
        else
        {
            Debug.Log("[AddressableDownloadManager] Catalog is already up to date.");
        }
        if (checkHandle.IsValid()) Addressables.Release(checkHandle);

        Debug.Log("[AddressableDownloadManager] Checking download size for all remote environments...");

        // Hide UI initially while we check
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (envLoadingIcon != null) envLoadingIcon.gameObject.SetActive(false);
        if (npcLoadingIcon != null) npcLoadingIcon.gameObject.SetActive(false);
        if (audioLoadingIcon != null) audioLoadingIcon.gameObject.SetActive(false);
        if (minigameLoadingIcon != null) minigameLoadingIcon.gameObject.SetActive(false);

        // 1. Convert string array to a List of objects for the Addressables API
        System.Collections.Generic.List<object> keysToDownload = new System.Collections.Generic.List<object>(remoteKeys);

        var sizeHandle = Addressables.GetDownloadSizeAsync(keysToDownload);
        yield return sizeHandle;

        long downloadSize = sizeHandle.Result;
        Addressables.Release(sizeHandle);

        if (downloadSize > 0)
        {
            Debug.Log($"[AddressableDownloadManager] Needs to download {downloadSize / (1024f * 1024f):F2} MB.");
            
            // Turn UI back on since we actually need to download
            if (progressBar != null) progressBar.gameObject.SetActive(true);
            if (progressText != null) progressText.gameObject.SetActive(true);
            if (statusText != null) statusText.gameObject.SetActive(true);
            if (envLoadingIcon != null) envLoadingIcon.gameObject.SetActive(true);
            if (npcLoadingIcon != null) npcLoadingIcon.gameObject.SetActive(true);
            if (audioLoadingIcon != null) audioLoadingIcon.gameObject.SetActive(true);
            if (minigameLoadingIcon != null) minigameLoadingIcon.gameObject.SetActive(true);

            if (statusText != null) statusText.text = "Downloading Resources...";

            bool allSucceeded = true;

            for (int keyIndex = 0; keyIndex < remoteKeys.Length; keyIndex++)
            {
                string key = remoteKeys[keyIndex];

                // Check if THIS specific key still needs downloading (might already be cached)
                var keySizeHandle = Addressables.GetDownloadSizeAsync(key);
                yield return keySizeHandle;
                long keySize = keySizeHandle.Result;
                Addressables.Release(keySizeHandle);

                if (keySize <= 0)
                {
                    Debug.Log($"[AddressableDownloadManager] '{key}' already cached. Skipping.");
                    // Advance progress for this key's share
                    currentPercent = (keyIndex + 1f) / remoteKeys.Length;
                    if (progressBar != null) progressBar.value = currentPercent;
                    if (progressText != null) progressText.text = Mathf.RoundToInt(currentPercent * 100f) + "%";
                    UpdateCheckmarks();
                    continue;
                }

                Debug.Log($"[AddressableDownloadManager] Downloading '{key}' ({keySize / (1024f * 1024f):F1} MB)...");
                if (statusText != null) statusText.text = $"Downloading {key.Replace("_", " ")}...";

                int maxRetries = 3;
                bool keySucceeded = false;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    if (attempt > 1)
                    {
                        Debug.Log($"[AddressableDownloadManager] '{key}' retry {attempt}/{maxRetries}...");
                        if (statusText != null) statusText.text = $"Retrying {key.Replace("_", " ")}... ({attempt}/{maxRetries})";
                        yield return new WaitForSeconds(2f);
                    }

                    downloadHandle = Addressables.DownloadDependenciesAsync(key);

                    while (!downloadHandle.IsDone)
                    {
                        if (downloadHandle.IsValid())
                        {
                            // Map this key's download progress to its share of the total bar
                            float keyProgress = downloadHandle.PercentComplete;
                            currentPercent = (keyIndex + keyProgress) / remoteKeys.Length;

                            if (progressBar != null) progressBar.value = currentPercent;
                            if (progressText != null) progressText.text = Mathf.RoundToInt(currentPercent * 100f) + "%";
                            UpdateCheckmarks();
                        }
                        yield return null;
                    }

                    if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log($"[AddressableDownloadManager] '{key}' downloaded successfully!");
                        Addressables.Release(downloadHandle);
                        keySucceeded = true;
                        break;
                    }
                    else
                    {
                        Debug.LogWarning($"[AddressableDownloadManager] '{key}' attempt {attempt} failed.");
                        Addressables.Release(downloadHandle);
                    }
                }

                if (!keySucceeded)
                {
                    allSucceeded = false;
                    break;
                }

                // Mark this key as fully done
                currentPercent = (keyIndex + 1f) / remoteKeys.Length;
                if (progressBar != null) progressBar.value = currentPercent;
                if (progressText != null) progressText.text = Mathf.RoundToInt(currentPercent * 100f) + "%";
                UpdateCheckmarks();
            }

            if (!allSucceeded)
            {
                if (statusText != null) statusText.text = "Download failed. Please check your internet and relaunch.";
                yield break;
            }

            // Final 100%
            currentPercent = 1.0f;
            if (progressBar != null) progressBar.value = 1f;
            if (progressText != null) progressText.text = "100%";
            UpdateCheckmarks();

            if (statusText != null) statusText.text = "Starting Game...";
            
            // Wait just a split second so the player sees the 100% checkmarks
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            // If it's already downloaded, skip straight to the next scene INSTANTLY
            // The UI remains hidden, so they never see the download screen!
            Debug.Log("[AddressableDownloadManager] No download needed. Skipping instantly to next scene.");
        }

        Debug.Log("[AddressableDownloadManager] Transitioning to MainLoadingScene...");
        SceneManager.LoadScene("MainLoadingScene");
    }
}

public class BypassCertificateHandler : UnityEngine.Networking.CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Return true to accept all SSL certificates (bypasses Curl error 60 on old Androids/VPNs)
        return true;
    }
}
