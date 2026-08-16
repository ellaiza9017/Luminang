using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Global manager that controls the Talk Button UI.
/// It automatically detects nearby InteractableNPCs and shows the button.
/// Attach this to an empty GameObject in your scene or directly to your Canvas.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The shared Talk Button in your Canvas.")]
    public Button talkButton;

    [Tooltip("The Text component inside the Talk Button.")]
    public TextMeshProUGUI buttonText;

    [Header("Audio Settings")]
    [Tooltip("The AudioSource to play the SFX from. If left empty, it will try to find one on this GameObject.")]
    public AudioSource sfxSource;
    [Tooltip("The SFX clip to play when the talk button is clicked (e.g. BubbleClick).")]
    public AudioClip talkButtonSFX;

    [Header("Player Settings")]
    [Tooltip("Tag of your player character.")]
    public string playerTag = "Player";

    private Transform _playerTransform;
    
    // Keeps track of all interactables in the scene to avoid expensive FindObjects calls
    private List<InteractableBase> _allInteractables = new List<InteractableBase>();
    
    // The interactable we are currently closest to
    private InteractableBase _currentNearest = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("[InteractionManager] Could not find Player. Check your Player Tag.");
        }

        if (talkButton != null)
        {
            // Bind the button click to our central handler
            talkButton.onClick.AddListener(OnButtonClicked);
            talkButton.gameObject.SetActive(false); // Hide initially
        }
        else
        {
            Debug.LogWarning("[InteractionManager] Talk Button is not assigned!");
        }
    }

    void Update()
    {
        if (talkButton == null) return;


        // Professional Player Detection: Find the object with the CharacterController (the real mover)
        if (_playerTransform == null || !_playerTransform.gameObject.activeInHierarchy)
        {
            var controllers = GameObject.FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
            foreach (var cc in controllers)
            {
                if (cc.gameObject.CompareTag(playerTag) && cc.gameObject.activeInHierarchy)
                {
                    _playerTransform = cc.transform;
                    break;
                }
            }
            if (_playerTransform == null) return; 
        }

        // Hide button during dialogue or if HUD is suppressed
        bool isHUDSuppressed = HUDManager.Instance != null && !HUDManager.Instance.IsHUDAllowed;
        bool isInDialogue = DialogueManager.Instance != null && DialogueManager.Instance.IsInDialogue;
        
        if (isInDialogue || isHUDSuppressed)
        {
            if (talkButton.gameObject.activeSelf) talkButton.gameObject.SetActive(false);
            _currentNearest = null;
            return;
        }

        InteractableBase nearest = null;
        float shortestDistance = float.MaxValue;

        foreach (var interactable in _allInteractables)
        {
            if (interactable == null || !interactable.isActiveAndEnabled || !interactable.interactionEnabled) continue;

            float dist = Vector3.Distance(_playerTransform.position, interactable.transform.position);
            
            if (dist <= interactable.interactionDistance && dist < shortestDistance)
            {
                shortestDistance = dist;
                nearest = interactable;
            }
        }

        // Log distance to console to see what's happening
        if (Time.frameCount % 120 == 0 && nearest != null)
        {
            Debug.Log($"[InteractionManager] Nearest: {nearest.gameObject.name}, Distance: {shortestDistance:F2}");
        }

        if (nearest != _currentNearest)
        {
            _currentNearest = nearest;
        }

        bool shouldShowButton = _currentNearest != null && !isHUDSuppressed;

        if (talkButton.gameObject.activeSelf != shouldShowButton)
        {
            talkButton.gameObject.SetActive(shouldShowButton);
            
            if (shouldShowButton && buttonText != null && _currentNearest != null)
            {
                buttonText.text = _currentNearest.promptText;
            }
        }
    }

    private void OnButtonClicked()
    {
        Debug.Log($"[InteractionManager] OnButtonClicked! _currentNearest is {(_currentNearest != null ? _currentNearest.gameObject.name : "NULL")}");
        
        // Play SFX
        if (talkButtonSFX != null)
        {
            if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
            if (sfxSource != null) sfxSource.PlayOneShot(talkButtonSFX);
        }

        if (_currentNearest != null)
        {
            // Hide the button so they can't spam it during dialogue
            talkButton.gameObject.SetActive(false);
            
            // Tell the interactable to do its thing
            _currentNearest.Interact();
            
            // We temporarily clear nearest so the button stays hidden 
            // until ResetInteraction() is called or we walk away and come back.
            _currentNearest = null;
        }
    }

    // ── Public API for Interactables ────────────────────────────────────────

    public void RegisterInteractable(InteractableBase i)
    {
        if (!_allInteractables.Contains(i))
            _allInteractables.Add(i);
    }

    public void UnregisterInteractable(InteractableBase i)
    {
        if (_allInteractables.Contains(i))
            _allInteractables.Remove(i);
    }
    
    /// <summary>
    /// Call this when dialogue finishes so the button can appear again
    /// if the player is still standing there.
    /// </summary>
    public void ForceCheckProximity()
    {
        // Forces the Update loop to re-evaluate nearest interactable next frame
        _currentNearest = null; 
    }
}
