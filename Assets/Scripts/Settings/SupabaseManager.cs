using UnityEngine;
using Supabase;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;


public class SupabaseManager : MonoBehaviour
{
    public static SupabaseManager Instance { get; private set; }

    [Header("Supabase Credentials")]
    public string supabaseUrl;
    public string supabaseKey;

    public Client client;

    // Event to notify when Google login/callback is finished
    public event Action<bool> OnGoogleLoginComplete;

    private void LoadCredentials()
    {
        TextAsset configAsset = Resources.Load<TextAsset>("SupabaseConfig");
        if (configAsset != null)
        {
            string[] lines = configAsset.text.Split('\n');
            if (lines.Length >= 2)
            {
                supabaseUrl = lines[0].Trim();
                supabaseKey = lines[1].Trim();
            }
        }
        else
        {
            Debug.LogError("[Supabase] Missing SupabaseConfig.txt in Resources!");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // 1. Setup Infrastructure
            UnityMainThreadDispatcher.CheckInstance();
            
            // Add the Redirect Listener component
            if (GetComponent<UnityRedirectListener>() == null)
            {
                gameObject.AddComponent<UnityRedirectListener>();
            }
            if (GetComponent<UserProfileManager>() == null)
            {
                gameObject.AddComponent<UserProfileManager>();
            }
            if (GetComponent<SceneFader>() == null)
            {
                gameObject.AddComponent<SceneFader>();
            }

            LoadCredentials();
            InitializeSupabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSupabase()
    {
        // 2. Configure Unity-specific options
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true,
            SessionHandler = new UnitySessionHandler()
        };

        client = new Client(supabaseUrl, supabaseKey, options);
        Debug.Log("<color=green>[Supabase] Client Initialized with Unity Support!</color>");
    }

    /// <summary>
    /// Processes the URL returned by the browser (from Editor or Mobile).
    /// </summary>
    public async void ProcessResultUrl(string url)
    {
        try
        {
            Debug.Log($"<color=cyan>[Supabase] Processing callback URL: {url}</color>");
            Debug.Log($"[Supabase] URL contains 'access_token': {url.Contains("access_token")}");
            Debug.Log($"[Supabase] URL contains 'code': {url.Contains("code=")}");
            Debug.Log($"[Supabase] URL contains 'error': {url.Contains("error")}");

            // Standard library method to convert URL -> Session
            var session = await client.Auth.GetSessionFromUrl(new Uri(url), true);
            
            Debug.Log($"[Supabase] GetSessionFromUrl returned. Session is null: {session == null}");
            if (session != null)
            {
                Debug.Log($"[Supabase] Session.User is null: {session.User == null}");
                if (session.User != null)
                    Debug.Log($"[Supabase] User ID: {session.User.Id}, Email: {session.User.Email}");
            }

            if (session != null && session.User != null)
            {
                Debug.Log("<color=green>[Supabase] Session caught successfully!</color>");
                OnGoogleLoginComplete?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("[Supabase] Session or User was null after GetSessionFromUrl.");
                OnGoogleLoginComplete?.Invoke(false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Supabase] Error processing callback: {ex.Message}");
            Debug.LogError($"[Supabase] Stack trace: {ex.StackTrace}");
            OnGoogleLoginComplete?.Invoke(false);
        }
    }
}

// =====================================================
// CENTRAL DATABASE MODELS
// =====================================================
[Postgrest.Attributes.Table("profiles")]
public class ProfileModel : Postgrest.Models.BaseModel
{
    // --- Identity ---
    [Postgrest.Attributes.PrimaryKey("id", false)]
    public string Id { get; set; }

    [Postgrest.Attributes.Column("email")]
    public string Email { get; set; }

    [Postgrest.Attributes.Column("username")]
    public string Username { get; set; }

    [Postgrest.Attributes.Column("avatar_url")]
    public string AvatarUrl { get; set; }

    [Postgrest.Attributes.Column("username_finalized_at")]
    public DateTime? UsernameFinalizedAt { get; set; }

    // --- Economy ---
    [Postgrest.Attributes.Column("coins")]
    public int Coins { get; set; }

    [Postgrest.Attributes.Column("overall_coins")]
    public int OverallCoins { get; set; }

    // --- Onboarding Flags ---
    [Postgrest.Attributes.Column("onboarding_completed")]
    public bool OnboardingCompleted { get; set; }

    [Postgrest.Attributes.Column("has_created_character")]
    public bool HasCreatedCharacter { get; set; }

    [Postgrest.Attributes.Column("has_completed_tutorial")]
    public bool HasCompletedTutorial { get; set; }

    [Postgrest.Attributes.Column("has_seen_prologue")]
    public bool HasSeenPrologue { get; set; }

    [Postgrest.Attributes.Column("has_seen_ilocos_intro")]
    public bool HasSeenIlocosIntro { get; set; }

    [Postgrest.Attributes.Column("has_seen_cebu_intro")]
    public bool HasSeenCebuIntro { get; set; }

    // --- Customization ---
    [Postgrest.Attributes.Column("equipped_outfit")]
    public object EquippedOutfit { get; set; }

    // --- Last Known Position (for resuming) ---
    [Postgrest.Attributes.Column("last_language_id")]
    public int? LastLanguageId { get; set; }

    [Postgrest.Attributes.Column("last_category_id")]
    public int? LastCategoryId { get; set; }

    // --- Autosave: Completed Objectives ---
    [Postgrest.Attributes.Column("completed_objectives_ilokano")]
    public List<string> CompletedObjectivesIlokano { get; set; } = new List<string>();

    [Postgrest.Attributes.Column("completed_objectives_cebuano")]
    public List<string> CompletedObjectivesCebuano { get; set; } = new List<string>();

    // --- Autosave: Journal / Unlocked Phrases ---
    [Postgrest.Attributes.Column("unlocked_phrases_ilokano")]
    public List<string> UnlockedPhrasesIlokano { get; set; } = new List<string>();

    [Postgrest.Attributes.Column("unlocked_phrases_cebuano")]
    public List<string> UnlockedPhrasesCebuano { get; set; } = new List<string>();

    // --- Account Status ---
    [Postgrest.Attributes.Column("status")]
    public string Status { get; set; }

    [Postgrest.Attributes.Column("suspension_reason")]
    public string SuspensionReason { get; set; }

    [Postgrest.Attributes.Column("suspension_duration")]
    public string SuspensionDuration { get; set; }

    [Postgrest.Attributes.Column("last_active")]
    public DateTime? LastActive { get; set; }
}

[Postgrest.Attributes.Table("user_inventory")]
public class InventoryModel : Postgrest.Models.BaseModel
{
    [Postgrest.Attributes.PrimaryKey("id", false)]
    public string Id { get; set; }

    [Postgrest.Attributes.Column("user_id")]
    public string UserId { get; set; }

    [Postgrest.Attributes.Column("item_name")]
    public string ItemName { get; set; }

    [Postgrest.Attributes.Column("slot")]
    public string Slot { get; set; }
}

// =====================================================
// NOTIFICATION / ANNOUNCEMENT MODELS
// =====================================================

/// <summary>
/// Maps to the 'admin_notifications' table — the global announcements created by admins.
/// </summary>
[Postgrest.Attributes.Table("admin_notifications")]
public class AdminNotificationModel : Postgrest.Models.BaseModel
{
    [Postgrest.Attributes.PrimaryKey("id", false)]
    public string Id { get; set; }

    [Postgrest.Attributes.Column("title")]
    public string Title { get; set; }

    [Postgrest.Attributes.Column("body")]
    public string Body { get; set; }

    [Postgrest.Attributes.Column("type")]
    public string Type { get; set; } // "info", "update", "maintenance", etc.

    [Postgrest.Attributes.Column("attached_coins")]
    public int AttachedCoins { get; set; }

    [Postgrest.Attributes.Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Maps to the 'user_notifications' table — a per-player record tracking
/// whether each announcement has been read, claimed, or archived.
/// </summary>
[Postgrest.Attributes.Table("user_notifications")]
public class UserNotificationModel : Postgrest.Models.BaseModel
{
    [Postgrest.Attributes.PrimaryKey("id", false)]
    public string Id { get; set; }

    [Postgrest.Attributes.Column("user_id")]
    public string UserId { get; set; }

    [Postgrest.Attributes.Column("notification_id")]
    public string NotificationId { get; set; }

    [Postgrest.Attributes.Column("is_read")]
    public bool IsRead { get; set; }

    [Postgrest.Attributes.Column("is_claimed")]
    public bool IsClaimed { get; set; }

    [Postgrest.Attributes.Column("is_archived")]
    public bool IsArchived { get; set; }

    [Postgrest.Attributes.Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Postgrest.Attributes.Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}
