using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luminang.UI.Announcements
{
    public class AnnouncementDetailsManager : MonoBehaviour
    {
        [Header("UI References")]
        public Image IconImage;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI DateTimeText;
        public TextMeshProUGUI DetailsText;
        public GameObject ArchiveButton;
        public GameObject DeleteButton;

        [Header("Claim Rewards")]
        public Button ClaimButton;
        public TextMeshProUGUI ClaimButtonText;
        public GameObject CoinIconImage;
        public TextMeshProUGUI CoinsAttachedText;

        [Header("Icons configuration")]
        public Sprite SystemIcon;
        public Sprite UpdateIcon;
        public Sprite MaintenanceIcon;

        private AnnouncementModel _currentData;
        private AnnouncementManager _mainManager;

        private void Awake()
        {
            if (_currentData == null)
            {
                ClearDetails();
            }
        }

        private void Start()
        {
            _mainManager = FindFirstObjectByType<AnnouncementManager>();
        }

        public void ShowDetails(AnnouncementModel data)
        {
            Debug.Log($"[AnnouncementDetailsManager] ShowDetails called for: {data?.Title}. ActiveSelf: {gameObject.activeSelf}");
            gameObject.SetActive(true);
            _currentData = data;

            if (TitleText != null) TitleText.text = data.Title;
            if (DetailsText != null) DetailsText.text = data.Details;
            
            // Format: July 2, 2026 • 8:30 AM
            if (DateTimeText != null) 
            {
                DateTimeText.text = data.ParsedDate.ToString("MMMM d, yyyy \u2022 h:mm tt");
            }

            if (IconImage != null)
            {
                IconImage.gameObject.SetActive(true);
                switch (data.Type)
                {
                    case AnnouncementType.System:
                        IconImage.sprite = SystemIcon;
                        break;
                    case AnnouncementType.Update:
                        IconImage.sprite = UpdateIcon;
                        break;
                    case AnnouncementType.Maintenance:
                        IconImage.sprite = MaintenanceIcon;
                        break;
                }
            }

            // Hide Archive button if it's already archived
            if (ArchiveButton != null)
            {
                ArchiveButton.SetActive(data.State != AnnouncementState.Archived);
            }

            if (DeleteButton != null)
            {
                DeleteButton.SetActive(true);
            }

            // Handle Claim Button State
            if (ClaimButton != null)
            {
                if (data.AttachedCoins <= 0)
                {
                    ClaimButton.gameObject.SetActive(false);
                    if (CoinIconImage != null) CoinIconImage.SetActive(false);
                    if (CoinsAttachedText != null) CoinsAttachedText.gameObject.SetActive(false);
                }
                else
                {
                    ClaimButton.gameObject.SetActive(true);
                    if (CoinIconImage != null) CoinIconImage.SetActive(true);
                    if (CoinsAttachedText != null) 
                    {
                        CoinsAttachedText.gameObject.SetActive(true);
                        CoinsAttachedText.text = data.AttachedCoins.ToString("N0");
                    }

                    var img = ClaimButton.GetComponent<Image>();
                    if (data.IsClaimed)
                    {
                        ClaimButton.interactable = false;
                        if (img != null) img.color = new Color(0.7f, 0.7f, 0.7f, 0.5f); // Grayed out, lower opacity
                        if (ClaimButtonText != null) ClaimButtonText.text = "CLAIMED";
                    }
                    else
                    {
                        ClaimButton.interactable = true;
                        if (img != null) 
                        {
                            if (ColorUtility.TryParseHtmlString("#D8FF94", out Color parsedColor))
                            {
                                img.color = parsedColor;
                            }
                        }
                        if (ClaimButtonText != null) ClaimButtonText.text = "CLAIM";
                    }
                }
            }
        }

        public void ClearDetails()
        {
            _currentData = null;
            if (TitleText != null) TitleText.text = "";
            if (DetailsText != null) DetailsText.text = "";
            if (DateTimeText != null) DateTimeText.text = "";
            if (IconImage != null) IconImage.gameObject.SetActive(false);
            if (ArchiveButton != null) ArchiveButton.SetActive(false);
            if (DeleteButton != null) DeleteButton.SetActive(false);
            if (ClaimButton != null) ClaimButton.gameObject.SetActive(false);
            if (CoinIconImage != null) CoinIconImage.SetActive(false);
            if (CoinsAttachedText != null) CoinsAttachedText.gameObject.SetActive(false);
        }

        public void OnClickArchive()
        {
            if (_currentData == null || _mainManager == null) return;

            GenericModal modal = GenericModal.Instance;
            if (modal == null || modal.gameObject == null || !modal.gameObject.scene.IsValid())
            {
                modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);
            }

            if (modal != null)
            {
                modal.ShowConfirm(
                    "Archive this announcement?",
                    "Yes",
                    () => {
                        _mainManager.ArchiveAnnouncement(_currentData.Id);
                        ClearDetails();
                    },
                    "No"
                );
            }
            else
            {
                Debug.LogWarning("[AnnouncementDetailsManager] GenericModal not found in hierarchy! Archiving immediately.");
                _mainManager.ArchiveAnnouncement(_currentData.Id);
                ClearDetails();
            }
        }

        public void OnClickDelete()
        {
            if (_currentData == null || _mainManager == null) return;

            GenericModal modal = GenericModal.Instance;
            if (modal == null || modal.gameObject == null || !modal.gameObject.scene.IsValid())
            {
                modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);
            }

            if (modal != null)
            {
                modal.ShowConfirm(
                    "Delete this announcement? This cannot be undone.",
                    "Yes",
                    () => {
                        _mainManager.DeleteAnnouncement(_currentData.Id);
                        ClearDetails();
                    },
                    "No"
                );
            }
            else
            {
                Debug.LogWarning("[AnnouncementDetailsManager] GenericModal not found in hierarchy! Deleting immediately.");
                _mainManager.DeleteAnnouncement(_currentData.Id);
                ClearDetails();
            }
        }

        public void OnClickClaim()
        {
            if (_currentData == null || _mainManager == null || _currentData.IsClaimed) return;

            // Capture reference before modal closes (lambda may run after this frame)
            var dataToClaimId = _currentData.Id;
            var coinsToClaim = _currentData.AttachedCoins;

            System.Action doClaimAction = () =>
            {
                // Mark locally FIRST so any list refresh can't reset it
                _currentData.IsClaimed = true;
                // Push to manager's in-memory database too
                _mainManager.ClaimReward(dataToClaimId);
                // Refresh just this panel
                ShowDetails(_currentData);
                // Show loading then update coins when done
                StartCoroutine(ClaimWithLoadingRoutine(dataToClaimId, coinsToClaim));
            };

            GenericModal modal = GenericModal.Instance;
            if (modal == null || modal.gameObject == null || !modal.gameObject.scene.IsValid())
            {
                modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);
            }

            if (modal != null)
            {
                modal.ShowConfirm(
                    $"Claim {coinsToClaim} Coins?",
                    "Claim",
                    doClaimAction,
                    "Cancel"
                );
            }
            else
            {
                Debug.LogWarning("[AnnouncementDetailsManager] GenericModal not found in hierarchy — claiming directly.");
                doClaimAction();
            }
        }

        private System.Collections.IEnumerator ClaimWithLoadingRoutine(string notifId, int coins)
        {
            // 1. Show crystal bounce loading
            if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Show();

            // 2. Fire the async claim and wait for it
            var task = AnnouncementService.Instance.ClaimRewardAsync(notifId, coins);
            while (!task.IsCompleted) yield return null;

            // 3. Hide loading
            if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Hide();

            // 4. Update the Player Info Panel now that coins are in CurrentProfile
            PlayerInfoPanel infoPanel = FindFirstObjectByType<PlayerInfoPanel>();
            if (infoPanel != null) infoPanel.UpdatePanelData();
        }
    }
}
