using UnityEngine;

/// <summary>
/// A specialized pickup script that only glows when a quest is active.
/// Inherits from InteractableBase to work with the Talk/Interact button system.
/// </summary>
public class InteractablePickup : InteractableBase
{
    [Header("Quest Settings")]
    [Tooltip("The exact objective text (e.g. 'Collect all weaving materials').")]
    public string requiredObjective = "Collect yarns";
    
    [Tooltip("Optional: The OFFICIAL JSON objective that, once reached, means these items should be permanently hidden. (e.g. 'Return to Sally')")]
    public string completionAnchorObjective = "";
    
    public bool hideOnPickup = true; // New Toggle!
    public bool canBeClicked = true; // New Toggle!
    public bool autoAddProgress = true; // Automatically increment the counter!
    
    [Header("Glow Appearance (HDR)")]
    [ColorUsage(true, true)]
    public Color glowColor = new Color(4f, 3.5f, 0.5f, 1f);
    public float pulseSpeed = 3f;
    public float streakHeight = 1.5f;
    public Vector3 streakOffset = Vector3.zero; // New!

    [Header("URP Materials/Shaders")]
    [Tooltip("Optional custom Material to use for the light streak. If assigned, this is used directly.")]
    public Material streakMaterial;
    [Tooltip("Optional Shader to use when creating the streak material dynamically (e.g. Universal Render Pipeline/Unlit).")]
    public Shader unlitShader;

    private Renderer _renderer;
    private Material _mat;
    private LineRenderer _line;
    private Material _lineMat;
    private bool _matchesObjective = false;
    private bool _hasBeenPickedUp = false;
    private string _pickupKey;

    protected virtual void Awake()
    {
        // Create a unique key for this specific pickup based on its name and position
        _pickupKey = $"Pickup_{gameObject.scene.name}_{gameObject.name}_{Mathf.RoundToInt(transform.position.x * 100f)}";
        _hasBeenPickedUp = PlayerPrefs.GetInt(_pickupKey, 0) == 1;

        SetupVisuals();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ObjectiveManager.OnObjectiveChanged += HandleObjectiveChanged;
    }

    protected override void Start()
    {
        base.Start();
        if (ObjectiveManager.Instance != null)
            HandleObjectiveChanged(ObjectiveManager.Instance.CurrentObjective);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ObjectiveManager.OnObjectiveChanged -= HandleObjectiveChanged;
    }

    private void HandleObjectiveChanged(string newObjective)
    {
        if (_line == null) return; // Not yet initialized by Awake

        string cleanRequired = requiredObjective.Trim();
        string cleanNew = newObjective != null ? newObjective.Trim() : "";
        string anchor = string.IsNullOrEmpty(completionAnchorObjective) ? cleanRequired : completionAnchorObjective.Trim();

        // DETERMINE CHRONOLOGICAL STATE
        // We check the anchor against the master JSON list.
        bool isPast = ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsObjectiveChronologicallyPast(anchor);
        
        _matchesObjective = !string.IsNullOrEmpty(cleanNew) && 
                            cleanNew.StartsWith(cleanRequired, System.StringComparison.OrdinalIgnoreCase);

        // 1. RESTART LOGIC: If the quest just started (or restarted at 0/X), wipe the pickup memory
        if (_matchesObjective && 
            (cleanNew.Equals(cleanRequired, System.StringComparison.OrdinalIgnoreCase) ||
             cleanNew.StartsWith(cleanRequired + " (0/", System.StringComparison.OrdinalIgnoreCase)))
        {
            _hasBeenPickedUp = false;
            PlayerPrefs.DeleteKey(_pickupKey);
        }

        interactionEnabled = _matchesObjective && canBeClicked;

        if (_matchesObjective)
        {
            // ACTIVE QUEST
            if (_hasBeenPickedUp)
            {
                // Already picked up during this active quest
                SetVisible(false);
                interactionEnabled = false;
                if (_line != null) _line.gameObject.SetActive(false);
            }
            else
            {
                // Ready to be picked up
                SetVisible(true);
                if (_line != null) _line.gameObject.SetActive(true);
                // Color is handled in Update()
            }
        }
        else if (isPast)
        {
            // POST-QUEST (Chronologically)
            // They are past this objective in the current timeline. Hide it forever.
            SetVisible(false);
            interactionEnabled = false;
            if (_line != null) _line.gameObject.SetActive(false);
            if (_mat != null) _mat.SetColor("_EmissionColor", Color.black);
        }
        else
        {
            // PRE-QUEST (Chronologically)
            // Even if they picked this up yesterday, they are replaying a chapter BEFORE this item.
            // Chronologically, it hasn't been picked up yet in this timeline. Make it visible!
            SetVisible(true);
            interactionEnabled = false; // Cannot click it yet
            if (_line != null) _line.gameObject.SetActive(false);
            if (_mat != null) _mat.SetColor("_EmissionColor", Color.black);
        }
    }

    public override void Interact()
    {
        if (!interactionEnabled || _hasBeenPickedUp) return;

        Debug.Log($"[InteractablePickup] {gameObject.name} interaction triggered!");
        
        _hasBeenPickedUp = true;
        PlayerPrefs.SetInt(_pickupKey, 1);
        PlayerPrefs.Save();

        OnInteract?.Invoke();

        if (autoAddProgress && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.AddProgress();
        }
        
        if (hideOnPickup)
        {
            SetVisible(false);
            if (_line != null) _line.gameObject.SetActive(false);
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (_renderer != null) _renderer.enabled = isVisible;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = isVisible;
        foreach (Transform child in transform)
        {
            if (child.name != "_QuestStreak") child.gameObject.SetActive(isVisible);
        }
    }

    private void Update()
    {
        if (!_matchesObjective || _hasBeenPickedUp || !_renderer.enabled) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float intensity = Mathf.Lerp(1f, 8f, t);

        if (_mat != null) _mat.SetColor("_EmissionColor", glowColor * intensity);

        if (_line != null)
        {
            _line.startWidth = Mathf.Lerp(0.02f, 0.15f, t);
            _line.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Lerp(0.1f, 0.8f, t));
        }
    }

    private void SetupVisuals()
    {
        // 1. Setup Emission
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _mat = _renderer.material;
            _mat.EnableKeyword("_EMISSION");
        }

        // 2. Setup Light Streak
        GameObject streakGO = new GameObject("_QuestStreak");
        streakGO.transform.SetParent(transform);
        streakGO.transform.localPosition = streakOffset;

        _line = streakGO.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.useWorldSpace = false;
        _line.SetPosition(0, Vector3.zero);
        _line.SetPosition(1, Vector3.up * streakHeight);
        _line.endWidth = 0f;

        if (streakMaterial != null)
        {
            _lineMat = new Material(streakMaterial);
        }
        else
        {
            Shader targetShader = unlitShader;
            if (targetShader == null)
            {
                targetShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (targetShader == null)
            {
                targetShader = Shader.Find("Unlit/Color");
            }

            if (targetShader != null)
            {
                _lineMat = new Material(targetShader);
            }
            else
            {
                Debug.LogError("[InteractablePickup] No material or shader assigned for streak, and fallback shaders could not be found!");
            }
        }

        if (_lineMat != null)
        {
            if (streakMaterial == null)
            {
                _lineMat.SetFloat("_Surface", 1);
                _lineMat.SetFloat("_Blend", 3);
                _lineMat.SetColor("_BaseColor", glowColor);
            }
            _line.material = _lineMat;
        }
        
        streakGO.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
        if (_lineMat != null) Destroy(_lineMat);
    }
}
