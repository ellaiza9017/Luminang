using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Luminang.UI.Announcements;

/// <summary>
/// Handles all Supabase calls for the Announcements / Inbox system.
/// This combines data from 'admin_notifications' (the content) and
/// 'user_notifications' (the per-player read/claimed/archived state)
/// into a single AnnouncementModel list that the UI can consume directly.
/// </summary>
public class AnnouncementService : MonoBehaviour
{
    public static AnnouncementService Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Fetches all announcements for the current player.
    /// Returns a merged list combining admin content with the player's personal read/claimed state.
    /// </summary>
    public async Task<List<AnnouncementModel>> FetchAnnouncementsAsync()
    {
        var result = new List<AnnouncementModel>();

        try
        {
            var user = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (user == null)
            {
                Debug.LogWarning("[AnnouncementService] No logged-in user. Skipping fetch.");
                return result;
            }

            // 1. Fetch the player's personal notification records
            var userNotifResponse = await SupabaseManager.Instance.client
                .From<UserNotificationModel>()
                .Where(x => x.UserId == user.Id)
                .Get();

            var userNotifs = userNotifResponse?.Models ?? new List<UserNotificationModel>();

            if (userNotifs.Count == 0)
            {
                Debug.Log("[AnnouncementService] No notifications found for this user.");
                return result;
            }

            // 2. Fetch the global admin announcements
            var adminNotifResponse = await SupabaseManager.Instance.client
                .From<AdminNotificationModel>()
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var adminNotifs = adminNotifResponse?.Models ?? new List<AdminNotificationModel>();

            // 3. Merge: for each user notification, find the matching admin announcement
            foreach (var userNotif in userNotifs)
            {
                // Skip deleted/null items
                if (userNotif.DeletedAt.HasValue) continue;

                var adminNotif = adminNotifs.Find(a => a.Id == userNotif.NotificationId);
                if (adminNotif == null) continue;

                // Map the DB type string to our AnnouncementType enum
                AnnouncementType type = adminNotif.Type?.ToLower() switch
                {
                    "update"      => AnnouncementType.Update,
                    "maintenance" => AnnouncementType.Maintenance,
                    _             => AnnouncementType.System
                };

                // Map read/archived state to our AnnouncementState enum
                AnnouncementState state;
                if (userNotif.IsArchived)
                    state = AnnouncementState.Archived;
                else if (userNotif.IsRead)
                    state = AnnouncementState.Read;
                else
                    state = AnnouncementState.Unread;

                result.Add(new AnnouncementModel
                {
                    Id          = userNotif.Id,           // Use the user_notification ID for updates
                    NotificationId = adminNotif.Id,
                    Type        = type,
                    Title       = adminNotif.Title,
                    Details     = adminNotif.Body,
                    DateString  = userNotif.CreatedAt.ToString("o"), // Pulled from the user's notification
                    State       = state,
                    AttachedCoins = adminNotif.AttachedCoins,
                    IsClaimed   = userNotif.IsClaimed,
                });
            }

            Debug.Log($"[AnnouncementService] Fetched {result.Count} announcements.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[AnnouncementService] Failed to fetch: " + ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Marks a user_notification row as read in the database.
    /// </summary>
    public async Task MarkAsReadAsync(string userNotificationId)
    {
        try
        {
            await SupabaseManager.Instance.client
                .From<UserNotificationModel>()
                .Where(x => x.Id == userNotificationId)
                .Set(x => x.IsRead, true)
                .Update();
            Debug.Log($"[AnnouncementService] Marked {userNotificationId} as read.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[AnnouncementService] MarkAsRead failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Marks a user_notification as archived in the database.
    /// </summary>
    public async Task MarkAsArchivedAsync(string userNotificationId)
    {
        try
        {
            await SupabaseManager.Instance.client
                .From<UserNotificationModel>()
                .Where(x => x.Id == userNotificationId)
                .Set(x => x.IsArchived, true)
                .Update();
            Debug.Log($"[AnnouncementService] Archived {userNotificationId}.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[AnnouncementService] MarkAsArchived failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Claims the coin reward for a notification.
    /// Marks is_claimed = true and adds coins to the player's profile.
    /// </summary>
    public async Task<bool> ClaimRewardAsync(string userNotificationId, int coinsToAdd)
    {
        try
        {
            // 1. Mark as claimed in DB
            await SupabaseManager.Instance.client
                .From<UserNotificationModel>()
                .Where(x => x.Id == userNotificationId)
                .Set(x => x.IsClaimed, true)
                .Set(x => x.IsRead, true)
                .Update();

            // 2. Add coins to the player's local profile and sync to DB
            if (UserProfileManager.Instance?.CurrentProfile != null)
            {
                UserProfileManager.Instance.CurrentProfile.Coins += coinsToAdd;
                UserProfileManager.Instance.CurrentProfile.OverallCoins += coinsToAdd;
                await UserProfileManager.Instance.UpdateProfile(UserProfileManager.Instance.CurrentProfile);
            }

            Debug.Log($"[AnnouncementService] Reward claimed! +{coinsToAdd} coins.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[AnnouncementService] ClaimReward failed: " + ex.Message);
            return false;
        }
    }
}
