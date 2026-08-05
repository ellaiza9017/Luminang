using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using Supabase.Storage;
using System.IO;

public class AvatarManager : MonoBehaviour
{
    public static AvatarManager Instance { get; private set; }

    [Header("Settings")]
    public string bucketName = "avatars";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public async Task<string> CaptureAndUpload(string userId, RenderTexture source)
    {
        if (source == null)
        {
            Debug.LogError("[AvatarManager] FAILED: The RenderTexture (source) is null! Check your PortraitBooth settings.");
            return null;
        }

        Debug.Log($"[AvatarManager] Capturing portrait for {userId} (Texture Size: {source.width}x{source.height})");

        try
        {
            // 1. Convert RenderTexture to Texture2D
            Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            RenderTexture.active = source;
            tex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] bytes = tex.EncodeToPNG();
            Debug.Log($"[AvatarManager] Texture conversion complete. Byte count: {bytes.Length}");
            Destroy(tex); 

            // 2. Upload to Supabase Storage
            string fileName = $"{userId}.png";
            var storage = SupabaseManager.Instance.client.Storage.From(bucketName);
            
            Debug.Log($"[AvatarManager] Attempting upload to bucket '{bucketName}' as '{fileName}' (Upsert: True)...");
            var uploadResponse = await storage.Upload(bytes, fileName, new Supabase.Storage.FileOptions { Upsert = true });
            
            if (uploadResponse == null)
            {
                Debug.LogError("[AvatarManager] FAILED: Storage.Upload returned a null response.");
                return null;
            }
            
            // 3. Get the Public URL
            string publicUrl = storage.GetPublicUrl(fileName);
            
            // Add a timestamp to the URL to force clients (like Unity) to bypass their cache
            string cacheBusterUrl = $"{publicUrl}?t={System.DateTime.Now.Ticks}";
            Debug.Log($"[AvatarManager] UPLOAD SUCCESS! Public URL: {publicUrl}");

            // 4. Update the Profile table
            if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
            {
                var profile = UserProfileManager.Instance.CurrentProfile;
                profile.AvatarUrl = cacheBusterUrl; // Use the cache-buster URL
                await UserProfileManager.Instance.UpdateProfile(profile);
                Debug.Log($"[AvatarManager] Database updated with new Avatar URL (Cache-Busted: {cacheBusterUrl})");
            }
            else
            {
                Debug.LogWarning("[AvatarManager] UserProfileManager.CurrentProfile is null. Profile table not updated.");
            }

            return cacheBusterUrl;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AvatarManager] CRITICAL ERROR: {ex.Message}");
            if (ex.Message.Contains("403") || ex.Message.Contains("Policy"))
            {
                Debug.LogError("[AvatarManager] TIP: This looks like a PERMISSIONS issue. Go to Supabase Storage > Policies and allow Uploads!");
            }
            return null;
        }
    }

    private static System.Collections.Generic.Dictionary<string, Texture2D> _avatarCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

    /// <summary>
    /// Downloads an avatar from a URL and caches it in memory.
    /// Used by leaderboards and other UI panels.
    /// </summary>
    public async Task<Texture2D> GetAvatarTexture(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        // Strip query params for the cache key (so "?t=timestamp" doesn't create duplicate cache entries)
        string cacheKey = url.Split('?')[0];

        if (_avatarCache.ContainsKey(cacheKey))
        {
            return _avatarCache[cacheKey];
        }

        try
        {
            var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AvatarManager] Failed to download avatar from {url}: {request.error}");
                return null;
            }

            Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
            if (texture != null)
            {
                _avatarCache[cacheKey] = texture;
                return texture;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AvatarManager] Exception downloading avatar: {ex.Message}");
        }

        return null;
    }
}

