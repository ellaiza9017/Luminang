using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Luminang.UI.Announcements
{
    public class AnnouncementManager : MonoBehaviour
    {
        [Header("UI References")]
        public Transform ContentContainer; // The parent inside the ScrollView
        public GameObject AnnouncementItemPrefab; // The prefab with AnnouncementItemUI script
        public AnnouncementTabGroup TabGroup; // Reference to update tab counts
        public AnnouncementDetailsManager DetailsManager; // Reference to details panel

        [Header("Empty States")]
        public GameObject emptyStateLeft;
        public GameObject emptyStateRight;
        public List<GameObject> objectsToHideLeft;
        public List<GameObject> objectsToHideRight;

        private List<AnnouncementModel> _database = new List<AnnouncementModel>();
        private List<GameObject> _instantiatedItems = new List<GameObject>();
        
        private string _currentSelectedTab = "Inbox";
        private bool _dataLoaded = false;
        private string _selectedAnnouncementId;

        private void Start()
        {
            StartCoroutine(LoadDataCoroutine());
        }

        private IEnumerator LoadDataCoroutine()
        {
            var task = AnnouncementService.Instance.FetchAnnouncementsAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsCompletedSuccessfully)
            {
                _database = task.Result;
                _dataLoaded = true;
                Debug.Log($"[AnnouncementManager] Loaded {_database.Count} announcements from Supabase.");
            }
            else
            {
                Debug.LogError("[AnnouncementManager] Failed to load announcements.");
                _database = new List<AnnouncementModel>();
                _dataLoaded = true;
            }

            PopulateList();
        }

        public void SetSelectedAnnouncement(string id)
        {
            _selectedAnnouncementId = id;
            foreach (var item in _instantiatedItems)
            {
                var ui = item.GetComponent<AnnouncementItemUI>();
                if (ui != null && ui.GetCurrentData() != null)
                {
                    ui.SetSelected(ui.GetCurrentData().Id == _selectedAnnouncementId);
                }
            }
        }

        public void ArchiveAnnouncement(string id)
        {
            if (!_dataLoaded) return;
            var announcement = _database.FirstOrDefault(a => a.Id == id);
            if (announcement != null)
            {
                announcement.State = AnnouncementState.Archived;
                // Fire-and-forget DB update
                _ = AnnouncementService.Instance.MarkAsArchivedAsync(id);
                RefreshCounts();
                PopulateList();
            }
        }

        public void DeleteAnnouncement(string id)
        {
            if (!_dataLoaded) return;
            var announcement = _database.FirstOrDefault(a => a.Id == id);
            if (announcement != null)
            {
                _database.Remove(announcement);
                // Fire-and-forget archive in DB (soft-delete via archived)
                _ = AnnouncementService.Instance.MarkAsArchivedAsync(id);
                RefreshCounts();
                PopulateList();
            }
        }

        /// <summary>Call this when the player opens a notification to mark it as read.</summary>
        public void MarkAsRead(string id)
        {
            if (!_dataLoaded) return;
            var announcement = _database.FirstOrDefault(a => a.Id == id);
            if (announcement != null && announcement.State == AnnouncementState.Unread)
            {
                announcement.State = AnnouncementState.Read;
                _ = AnnouncementService.Instance.MarkAsReadAsync(id);
                RefreshCounts();
            }
        }

        /// <summary>Call this when the player taps the "Claim Reward" button.</summary>
        public void ClaimReward(string id)
        {
            if (!_dataLoaded) return;
            var announcement = _database.FirstOrDefault(a => a.Id == id);
            if (announcement == null || announcement.IsClaimed || announcement.AttachedCoins <= 0) return;

            // Mark in-memory only — the actual DB write is done by the calling coroutine
            announcement.IsClaimed = true;
        }

        public void OnTabChanged(string tabName)
        {
            if (!_dataLoaded) return;
            _currentSelectedTab = tabName;
            PopulateList();
        }

        public void RefreshCounts()
        {
            if (!_dataLoaded) return;
            if (TabGroup == null || TabGroup.TabButtons == null) return;

            int inboxCount = _database.Count(a => a.State != AnnouncementState.Archived);
            int unreadCount = _database.Count(a => a.State == AnnouncementState.Unread);
            int archivedCount = _database.Count(a => a.State == AnnouncementState.Archived);

            foreach (var tab in TabGroup.TabButtons)
            {
                if (tab.TabName.ToLower() == "inbox")
                    tab.UpdateCount(inboxCount);
                else if (tab.TabName.ToLower() == "unread")
                    tab.UpdateCount(unreadCount);
                else if (tab.TabName.ToLower() == "archived")
                    tab.UpdateCount(archivedCount);
            }
        }

        private void PopulateList()
        {
            if (!_dataLoaded) return;
            RefreshCounts();

            // Clear existing UI items
            foreach (var item in _instantiatedItems)
                Destroy(item);
            _instantiatedItems.Clear();

            // Filter data based on selected tab
            List<AnnouncementModel> filteredList;
            switch (_currentSelectedTab.ToLower())
            {
                case "inbox":
                    filteredList = _database.Where(a => a.State != AnnouncementState.Archived).ToList();
                    break;
                case "unread":
                    filteredList = _database.Where(a => a.State == AnnouncementState.Unread).ToList();
                    break;
                case "archived":
                    filteredList = _database.Where(a => a.State == AnnouncementState.Archived).ToList();
                    break;
                default:
                    filteredList = _database;
                    break;
            }

            // Sort by Date (newest first)
            filteredList = filteredList.OrderByDescending(a => a.ParsedDate).ToList();

            // Empty state toggles
            if (filteredList.Count == 0)
            {
                if (emptyStateLeft != null) emptyStateLeft.SetActive(true);
                if (emptyStateRight != null) emptyStateRight.SetActive(true);
                if (objectsToHideLeft != null) foreach (var obj in objectsToHideLeft) if (obj != null) obj.SetActive(false);
                if (objectsToHideRight != null) foreach (var obj in objectsToHideRight) if (obj != null) obj.SetActive(false);
            }
            else
            {
                if (emptyStateLeft != null) emptyStateLeft.SetActive(false);
                if (emptyStateRight != null) emptyStateRight.SetActive(false);
                if (objectsToHideLeft != null) foreach (var obj in objectsToHideLeft) if (obj != null) obj.SetActive(true);
                if (objectsToHideRight != null) foreach (var obj in objectsToHideRight) if (obj != null) obj.SetActive(true);
            }

            // Resolve selection ID
            if (filteredList.Count > 0)
            {
                if (string.IsNullOrEmpty(_selectedAnnouncementId) || !filteredList.Any(a => a.Id == _selectedAnnouncementId))
                    _selectedAnnouncementId = filteredList[0].Id;
            }
            else
            {
                _selectedAnnouncementId = null;
            }

            // Instantiate items
            foreach (var data in filteredList)
            {
                GameObject newObj = Instantiate(AnnouncementItemPrefab, ContentContainer);
                AnnouncementItemUI uiComponent = newObj.GetComponent<AnnouncementItemUI>();
                if (uiComponent != null)
                {
                    uiComponent.Setup(data);
                    uiComponent.SetSelected(data.Id == _selectedAnnouncementId);
                }
                _instantiatedItems.Add(newObj);
            }

            // Auto-select to populate the right panel
            if (DetailsManager == null)
            {
                DetailsManager = FindFirstObjectByType<AnnouncementDetailsManager>();
            }

            if (DetailsManager != null)
            {
                if (!string.IsNullOrEmpty(_selectedAnnouncementId))
                {
                    var selectedData = filteredList.FirstOrDefault(a => a.Id == _selectedAnnouncementId);
                    if (selectedData != null)
                    {
                        DetailsManager.ShowDetails(selectedData);
                        // Auto mark as read when selected
                        MarkAsRead(_selectedAnnouncementId);
                    }
                }
                else
                {
                    DetailsManager.ClearDetails();
                }
            }
        }
    }
}
