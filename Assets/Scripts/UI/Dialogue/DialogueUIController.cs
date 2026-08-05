using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles the visual display of the Dialogue System.
/// Supports Next/Prev navigation, typewriter skip, and button press animations.
/// </summary>
public class DialogueUIController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject dialoguePanel;
    public Transform choicesContainer;

    [Header("Text Elements")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Prefabs")]
    public GameObject choiceButtonPrefab;

    [Header("Optional UI")]
    public GameObject movementUI;
    public GameObject choicesGroup;

    [Header("Navigation Buttons")]
    [Tooltip("The 'Next >>' button. Assign in Inspector.")]
    public Button nextButton;
    [Tooltip("The '<< Prev' button. Assign in Inspector.")]
    public Button prevButton;
    [Tooltip("The 'Translate' button/icon. Assign in Inspector.")]
    public Button translateButton;

    [Header("Panel Pop-in Animation")]
    public float panelPopDuration = 0.35f;
    public AnimationCurve panelPopCurve = new AnimationCurve(
        new Keyframe(0f,    0f,   0f, 3f),
        new Keyframe(0.65f, 1.08f, 0f, 0f),
        new Keyframe(1f,    1f,   0f, 0f)
    );

    [Header("Choices Reveal Animation")]
    public float dialogueMoveUpDistance = 150f;
    public float dialogueMoveDuration = 0.35f;
    private float _dialogueOriginalY;
    private bool _isDialoguePosCaptured = false;

    [Header("Portrait Settings")]
    public Image speakerPortraitImage;
    public float portraitSlideDuration = 0.4f;
    [Tooltip("How much of its own width the portrait slides (1.0 = full width).")]
    public float portraitSlideFactor = 0.5f; 
    [Tooltip("If true, the portrait slides in from the left. Otherwise, from the right.")]
    public bool slideFromLeft = true;

    [Header("Typewriter Effect")]
    [Tooltip("Seconds per character. Set to 0 to show instantly.")]
    public float typingSpeed = 0.02f;

    [Header("Button Press Animation")]
    [Tooltip("How much the button squishes on press (0.85 = 15% smaller).")]
    public float buttonPressScale    = 0.85f;
    public float buttonAnimDuration  = 0.12f;

    // ── Private State ────────────────────────────────────────────────
    private List<GameObject>              _activeChoiceButtons = new List<GameObject>();
    private Coroutine                     _showSequenceCoroutine;
    private Coroutine                     _typeTextCoroutine;
    private bool                          _isTyping  = false;
    private bool                          _skipTyping = false;
    private string                        _fullText  = "";
    private System.Action<DialogueChoice> _onChoiceSelected;
    private List<DialogueChoice>          _currentChoices = new List<DialogueChoice>();
    private Vector2                       _portraitOriginalPos;
    private bool                          _isPortraitPosCaptured = false;
    private Coroutine                     _portraitCoroutine;
    private Sprite                        _lastPortrait;
    private string                        _translatedText = "";
    private bool                          _isTranslatedShowing = false;

    void Awake()
    {
        HideDialogue();

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrevClicked);

        if (translateButton != null)
            translateButton.onClick.AddListener(OnTranslateClicked);

        if (speakerPortraitImage != null)
        {
            // Start hidden - original pos will be captured when first needed
            SetPortraitVisibility(false, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────

    public string injectedPrefixText = "";

    /// <summary>
    /// Displays a dialogue node. Called by DialogueManager.
    /// </summary>
    public void DisplayNode(DialogueNode node, System.Action<DialogueChoice> onChoiceSelected, bool skipAnimation = false)
    {
        Debug.Log($"<color=magenta>[DialogueUIController] DisplayNode -> Displaying Node: '{(node != null ? node.name : "NULL")}', Text: '{(node != null ? node.dialogueText : "")}'</color>");
        _onChoiceSelected = onChoiceSelected;
        _currentChoices   = node.choices;
        _fullText         = injectedPrefixText + node.dialogueText;
        _translatedText   = node.translatedText;
        
        // Reset prefix after consuming
        injectedPrefixText = "";
        
        _isTranslatedShowing = false;
        _skipTyping       = skipAnimation; // If skipping, we force it here

        // Show/Hide translate button
        if (translateButton != null)
            translateButton.gameObject.SetActive(!string.IsNullOrEmpty(_translatedText));

        if (speakerNameText != null)
            speakerNameText.text = string.IsNullOrEmpty(node.speakerName) ? "" : node.speakerName;

        // Clear text immediately so no placeholder shows during pop-in
        if (dialogueText != null) dialogueText.text = "";

        ClearChoices();

        // Spawn choice buttons if there are any choices with text
        int visibleChoices = 0;
        if (node.choices != null)
        {
            foreach (var choice in node.choices)
            {
                if (string.IsNullOrWhiteSpace(choice.choiceText)) continue;

                visibleChoices++;
                GameObject obj = Instantiate(choiceButtonPrefab, choicesContainer);
                obj.SetActive(true);
                _activeChoiceButtons.Add(obj);

                var btnText = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = choice.choiceText;

                var btn = obj.GetComponent<Button>();
                if (btn != null)
                {
                    DialogueChoice cached = choice;
                    btn.onClick.AddListener(() =>
                    {
                        if (gameObject.activeInHierarchy)
                        {
                            StartCoroutine(ButtonPressAnim(btn.transform));
                        }
                        _onChoiceSelected?.Invoke(cached);
                    });
                }
            }
        }
        
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(visibleChoices == 0);
        }
        
        // Show the Mic button if one is injected AND this node requires STT (and not handled by InSceneLessonController)
        STTVoiceVisualizerAdapter micAdapter = GetComponentInChildren<STTVoiceVisualizerAdapter>(true);
        if (micAdapter != null)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null &&
                (InSceneLessonController.Instance == null || !InSceneLessonController.Instance.IsLessonActive))
            {
                micAdapter.ShowAndPrepare();
            }
            else
            {
                micAdapter.gameObject.SetActive(false);
            }
        }

        // Update Portrait
        if (speakerPortraitImage != null)
        {
            if (node.speakerPortrait != _lastPortrait)
            {
                UpdatePortrait(node.speakerPortrait);
                _lastPortrait = node.speakerPortrait;
            }
        }

        // We ALWAYS keep the next button active initially so the player can click it to skip the typing animation.
        // It will be disabled at the very end of the TypeText coroutine if this node has choices or requires STT.
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
        }

        ShowDialogue(true, skipAnimation);
    }

    /// <summary>
    /// Forces the microphone to start recording programmatically, e.g. when a choice with "StartSTT" is clicked.
    /// </summary>
    public void ToggleSTTRecording(DialogueChoice choice)
    {
        STTVoiceVisualizerAdapter micAdapter = GetComponentInChildren<STTVoiceVisualizerAdapter>(true);
        if (micAdapter == null)
        {
            micAdapter = gameObject.AddComponent<STTVoiceVisualizerAdapter>();
        }
        
        if (micAdapter != null)
        {
            micAdapter.ShowAndPrepare();
            micAdapter.OnMicClicked();
            
            if (micAdapter.isRecording)
            {
                SetChoiceButtonText(choice, "Done Speaking");
            }
            else
            {
                HideChoicesOnly();
            }
        }
    }

    public void SetChoiceButtonText(DialogueChoice choice, string text)
    {
        foreach (var obj in _activeChoiceButtons)
        {
            var btnText = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                // Match by exact text or if it's the STT button
                if (btnText.text.Trim() == choice.choiceText.Trim() || choice.choiceEvent == "StartSTT")
                {
                    btnText.text = text;
                    choice.choiceText = text; // update it so we don't lose track
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Called by DialogueManager to update whether the Prev button is interactable.
    /// </summary>
    public void SetNavigation(bool canGoBack)
    {
        // Hide completely on first node, show when there is history
        if (prevButton != null)
            prevButton.gameObject.SetActive(canGoBack);
    }

    public void UpdateSTTStatus(string statusMarkup)
    {
        if (dialogueText != null)
        {
            // Remove previous status if it exists by splitting at the first newline after the main text, or simply just re-assigning fullText + status
            dialogueText.text = _fullText + "\n\n" + statusMarkup;
        }
    }

    public void ShowDialogue(bool show, bool skipAnimation = false)
    {
        if (show)
        {
            if (_showSequenceCoroutine != null) StopCoroutine(_showSequenceCoroutine);
            if (_typeTextCoroutine != null) StopCoroutine(_typeTextCoroutine);
            bool isAlreadyOpen = dialoguePanel.activeSelf;
            _showSequenceCoroutine = StartCoroutine(ShowSequence(isAlreadyOpen, skipAnimation));
        }
        else
        {
            if (_showSequenceCoroutine != null) StopCoroutine(_showSequenceCoroutine);
            if (_typeTextCoroutine != null) StopCoroutine(_typeTextCoroutine);
            _showSequenceCoroutine = null;
            _typeTextCoroutine = null;
            _isTyping   = false;
            _skipTyping = false;

            dialoguePanel.SetActive(false);
            if (_isDialoguePosCaptured)
            {
                RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _dialogueOriginalY);
            }

            if (choicesGroup != null)
            {
                choicesGroup.SetActive(false);
                choicesGroup.transform.localScale = Vector3.one;
            }

            // Hide Portrait
            SetPortraitVisibility(false, false);
            _lastPortrait = null;
        }

        if (movementUI != null)
        {
            if (show)
            {
                movementUI.SetActive(false);
            }
            else
            {
                // Only re-enable movement if a lesson or intro panel IS NOT currently active
                bool isLessonActive = LessonManager.Instance != null && 
                                     LessonManager.Instance.lessonPanel != null && 
                                     LessonManager.Instance.lessonPanel.activeInHierarchy;
                
                bool isIntroPanelActive = LessonIntroPanel.Instance != null &&
                                          LessonIntroPanel.Instance.panelRoot != null &&
                                          LessonIntroPanel.Instance.panelRoot.activeInHierarchy;
                                     
                if (!isLessonActive && !isIntroPanelActive)
                {
                    movementUI.SetActive(true);
                }
            }
        }
    }

    public void HideDialogue()
    {
        ShowDialogue(false);
        ClearChoices();
    }

    /// <summary>
    /// Hides only the choices area and dialogue panel visuals WITHOUT
    /// showing the movement UI. Used during wrong-answer animations
    /// where the player is still considered to be "in dialogue".
    /// </summary>
    public void HideChoicesOnly()
    {
        if (_showSequenceCoroutine != null) StopCoroutine(_showSequenceCoroutine);
        _isTyping   = false;
        _skipTyping = false;

        if (choicesGroup != null)
        {
            choicesGroup.SetActive(false);
            choicesGroup.transform.localScale = Vector3.one;
        }

        // NOTE: We intentionally do NOT call movementUI.SetActive(true) here.
        // The player is still in dialogue — UI should stay hidden.
        ClearChoices();
    }

    /// <summary>
    /// Hides the entire dialogue UI (panel, portrait, choices) but intentionally
    /// keeps the movement UI hidden because a minigame is taking over.
    /// </summary>
    public void HideDialogueForMinigame()
    {
        if (_showSequenceCoroutine != null) StopCoroutine(_showSequenceCoroutine);
        if (_typeTextCoroutine != null) StopCoroutine(_typeTextCoroutine);
        _showSequenceCoroutine = null;
        _typeTextCoroutine = null;
        _isTyping   = false;
        _skipTyping = false;

        dialoguePanel.SetActive(false);
        if (_isDialoguePosCaptured)
        {
            RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _dialogueOriginalY);
        }

        if (choicesGroup != null)
        {
            choicesGroup.SetActive(false);
            choicesGroup.transform.localScale = Vector3.one;
        }

        SetPortraitVisibility(false, false);
        _lastPortrait = null;
        
        ClearChoices();
    }

    // ─────────────────────────────────────────────────────────────────
    // Button Handlers
    // ─────────────────────────────────────────────────────────────────

    private void OnNextClicked()
    {
        StartCoroutine(ButtonPressAnim(nextButton.transform));

        if (_isTyping)
        {
            // First click: skip the typewriter, show full text immediately
            _skipTyping = true;
        }
        else
        {
            // Auto-advance if there are 0 visible choices
            if (_currentChoices.Count == 0)
            {
                _onChoiceSelected?.Invoke(null); // Ends dialogue
            }
            else
            {
                // If there's a choice but it's hidden (empty text), pick the first one
                _onChoiceSelected?.Invoke(_currentChoices[0]);
            }
        }
    }

    private void OnPrevClicked()
    {
        StartCoroutine(ButtonPressAnim(prevButton.transform));
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.GoToPreviousNode();
    }

    private void OnTranslateClicked()
    {
        if (string.IsNullOrEmpty(_translatedText)) return;

        _isTranslatedShowing = !_isTranslatedShowing;
        
        // If we are still typing, skip to the end of the text
        if (_isTyping) _skipTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = _isTranslatedShowing ? _translatedText : _fullText;
        }

        StartCoroutine(ButtonPressAnim(translateButton.transform));
    }

    // ─────────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowSequence(bool isAlreadyOpen, bool skipAnimation)
    {
        if (dialoguePanel == null) yield break; // Safety Check

        // Don't flicker the group if we're skipping
        if (choicesGroup != null && !skipAnimation) choicesGroup.SetActive(false);
        dialoguePanel.SetActive(true);

        // Reset position before pop-in if not captured
        if (!_isDialoguePosCaptured)
        {
            _dialogueOriginalY = dialoguePanel.GetComponent<RectTransform>().anchoredPosition.y;
            _isDialoguePosCaptured = true;
        }
        
        RectTransform dialogRT = dialoguePanel.GetComponent<RectTransform>();

        if (!isAlreadyOpen && !skipAnimation)
        {
            dialogRT.anchoredPosition = new Vector2(dialogRT.anchoredPosition.x, _dialogueOriginalY);
            yield return StartCoroutine(PopInPanel());
        }
        else
        {
            dialoguePanel.transform.localScale = Vector3.one;
            var cg = dialoguePanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;

            // If the panel is currently UP from a previous choice, slide it down first to act as a curtain
            if (!skipAnimation && dialogRT.anchoredPosition.y > _dialogueOriginalY + 10f)
            {
                yield return StartCoroutine(MoveDialoguePanelDown());
            }
            else if (skipAnimation)
            {
                dialogRT.anchoredPosition = new Vector2(dialogRT.anchoredPosition.x, _dialogueOriginalY);
            }
        }

        if (typingSpeed > 0 && !skipAnimation)
        {
            if (_typeTextCoroutine != null) StopCoroutine(_typeTextCoroutine);
            _typeTextCoroutine = StartCoroutine(TypeText(_fullText));
            yield return _typeTextCoroutine;
        }
        else
            if (dialogueText != null) dialogueText.text = _fullText;

        // Only show the choices area when there are actual choices to pick from
        bool hasValidChoices = _activeChoiceButtons.Count > 0 && choicesGroup != null;
        
        Debug.Log($"<color=cyan>[DialogueUIController] hasValidChoices = {hasValidChoices} (Active Buttons: {_activeChoiceButtons.Count}). Moving UP? {hasValidChoices && !skipAnimation}</color>");

        if (hasValidChoices)
        {
            // Ensure choices are normal scale
            choicesGroup.transform.localScale = Vector3.one;
            
            RectTransform containerRT = choicesContainer.GetComponent<RectTransform>();
            
            if (containerRT != null)
            {
                SetupChoicesLayout(containerRT);
            }

            // Calculate rows mathematically based on string length and number of choices
            int maxLen = 0;
            foreach (var choice in _currentChoices)
            {
                if (!string.IsNullOrEmpty(choice.choiceText) && choice.choiceText.Length > maxLen)
                    maxLen = choice.choiceText.Length;
            }
            bool useGrid = maxLen <= 15 && _activeChoiceButtons.Count > 1;
            
            int numChoices = _activeChoiceButtons.Count;
            int rows = useGrid ? Mathf.CeilToInt((float)numChoices / 2f) : numChoices;
            
            // The user requested we just calculate the height of the rows directly!
            float dynamicDistance = (rows * 75f) + (Mathf.Max(0, rows - 1) * 9f) + 15f;

            choicesGroup.SetActive(true);
            Canvas.ForceUpdateCanvases();

            if (!skipAnimation)
            {
                yield return StartCoroutine(MoveDialoguePanelUp(dynamicDistance));
            }
            else
            {
                dialogRT.anchoredPosition = new Vector2(dialogRT.anchoredPosition.x, _dialogueOriginalY + dynamicDistance);
            }
        }
    }

    private void SetupChoicesLayout(RectTransform choicesRT)
    {
        if (choicesContainer == null) return;
        
        UnityEngine.UI.VerticalLayoutGroup vLayout = choicesContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        UnityEngine.UI.GridLayoutGroup grid = choicesContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        
        // Save layout settings so we don't lose them when switching
        RectOffset padding = null;
        float spacing = 9f; // default spacing
        
        if (vLayout != null) { padding = vLayout.padding; spacing = vLayout.spacing; }
        else if (grid != null) { padding = grid.padding; spacing = grid.spacing.y; }

        int maxLen = 0;
        foreach (var choice in _currentChoices)
        {
            if (!string.IsNullOrEmpty(choice.choiceText) && choice.choiceText.Length > maxLen)
                maxLen = choice.choiceText.Length;
        }

        // If all texts are short (<=15 chars) and we have multiple choices, use 2-column Grid
        bool useGrid = maxLen <= 15 && _activeChoiceButtons.Count > 1;

        if (useGrid)
        {
            if (vLayout != null) UnityEngine.Object.DestroyImmediate(vLayout);
            if (grid == null) grid = choicesContainer.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.LowerCenter; // Place from bottom
            
            if (padding != null)
            {
                grid.padding = padding;
                grid.spacing = new Vector2(spacing, spacing);
            }
            
            float totalWidth = choicesRT.rect.width - grid.padding.left - grid.padding.right;
            float cellWidth = (totalWidth - grid.spacing.x) / 2f;
            grid.cellSize = new Vector2(cellWidth, 75f); // Use the default button height
        }
        else
        {
            if (grid != null) UnityEngine.Object.DestroyImmediate(grid);
            if (vLayout == null) vLayout = choicesContainer.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            
            vLayout.childAlignment = TextAnchor.LowerCenter; // Group everything at the bottom
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;

            if (padding != null)
            {
                vLayout.padding = padding;
                vLayout.spacing = spacing;
            }
        }
    }

    // Removed CalculateTargetDialogueY as we are using direct mathematical row height

    private IEnumerator PopInPanel()
    {
        var cg = dialoguePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialoguePanel.AddComponent<CanvasGroup>();

        Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);
        dialoguePanel.transform.localScale = startScale;
        cg.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < panelPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t      = Mathf.Clamp01(elapsed / panelPopDuration);
            float curved = panelPopCurve.Evaluate(t);
            dialoguePanel.transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, curved);
            cg.alpha = Mathf.Clamp01(t / 0.5f);
            yield return null;
        }

        dialoguePanel.transform.localScale = Vector3.one;
        cg.alpha = 1f;
    }

    private IEnumerator TypeText(string sentence)
    {
        _isTyping   = true;
        _skipTyping = false;

        if (dialogueText != null)
        {
            dialogueText.text = "";
            foreach (char c in sentence.ToCharArray())
            {
                if (_skipTyping)
                {
                    // Skip pressed — jump to full text immediately
                    dialogueText.text = sentence;
                    break;
                }
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        _isTyping   = false;
        _skipTyping = false;
        
        // Now that typing is finished, hide the next button if there are choices or STT required.
        if (nextButton != null)
        {
            bool requiresSTT = DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null;
            bool hasChoices = _activeChoiceButtons != null && _activeChoiceButtons.Count > 0;
            nextButton.gameObject.SetActive(!hasChoices && !requiresSTT);
        }
    }

    private IEnumerator MoveDialoguePanelUp(float distance)
    {
        RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, _dialogueOriginalY + distance);
        
        float elapsed = 0f;
        while (elapsed < dialogueMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dialogueMoveDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            yield return null;
        }
        rt.anchoredPosition = endPos;
    }

    private IEnumerator MoveDialoguePanelDown()
    {
        RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, _dialogueOriginalY);
        
        float elapsed = 0f;
        while (elapsed < dialogueMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dialogueMoveDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            yield return null;
        }
        rt.anchoredPosition = endPos;
    }

    private void UpdatePortrait(Sprite newPortrait)
    {
        if (speakerPortraitImage == null) return;

        if (_portraitCoroutine != null) StopCoroutine(_portraitCoroutine);
        _portraitCoroutine = StartCoroutine(AnimatePortrait(newPortrait));
    }

    private void SetPortraitVisibility(bool visible, bool immediate)
    {
        if (speakerPortraitImage == null) return;

        // LAZY CAPTURE: Wait until the screen resolution is settled to grab the home position
        if (!_isPortraitPosCaptured && !immediate)
        {
            _portraitOriginalPos = speakerPortraitImage.rectTransform.anchoredPosition;
            _isPortraitPosCaptured = true;
        }

        if (immediate)
        {
            if (_portraitCoroutine != null) StopCoroutine(_portraitCoroutine);
            
            float width = speakerPortraitImage.rectTransform.rect.width;
            float offset = slideFromLeft ? -width * portraitSlideFactor : width * portraitSlideFactor;
            speakerPortraitImage.rectTransform.anchoredPosition = _portraitOriginalPos + new Vector2(offset, 0);
            
            var cg = speakerPortraitImage.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = visible ? 1f : 0f;
            
            speakerPortraitImage.gameObject.SetActive(visible);
        }
        else
        {
            // If hiding, we just slide it out
            if (!visible)
            {
                if (_portraitCoroutine != null) StopCoroutine(_portraitCoroutine);
                _portraitCoroutine = StartCoroutine(AnimatePortrait(null));
            }
        }
    }

    private IEnumerator AnimatePortrait(Sprite nextPortrait)
    {
        if (speakerPortraitImage == null) yield break;

        // LAZY CAPTURE: Wait until the screen resolution is settled to grab the home position
        if (!_isPortraitPosCaptured)
        {
            _portraitOriginalPos = speakerPortraitImage.rectTransform.anchoredPosition;
            _isPortraitPosCaptured = true;
        }

        RectTransform rt = speakerPortraitImage.rectTransform;
        CanvasGroup cg = speakerPortraitImage.GetComponent<CanvasGroup>();
        if (cg == null) cg = speakerPortraitImage.gameObject.AddComponent<CanvasGroup>();

        float width = rt.rect.width;
        float offset = slideFromLeft ? -width * portraitSlideFactor : width * portraitSlideFactor;
        Vector2 hiddenPos = _portraitOriginalPos + new Vector2(offset, 0);
        
        // If we are currently hidden and showing a new portrait
        if (nextPortrait != null && !speakerPortraitImage.gameObject.activeSelf)
        {
            speakerPortraitImage.sprite = nextPortrait;
            speakerPortraitImage.gameObject.SetActive(true);
            rt.anchoredPosition = hiddenPos;
            cg.alpha = 0f;
        }

        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = (nextPortrait != null) ? _portraitOriginalPos : hiddenPos;
        float startAlpha = cg.alpha;
        float targetAlpha = (nextPortrait != null) ? 1f : 0f;

        // If we are swapping portraits while already visible
        if (nextPortrait != null && speakerPortraitImage.gameObject.activeSelf && speakerPortraitImage.sprite != nextPortrait)
        {
            // Quick fade out/in or just swap
            // For now, let's just swap the sprite and keep animating to target
            speakerPortraitImage.sprite = nextPortrait;
        }

        float elapsed = 0f;
        while (elapsed < portraitSlideDuration)
        {
            if (speakerPortraitImage == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / portraitSlideDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // Ease out

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            yield return null;
        }

        if (speakerPortraitImage != null)
        {
            rt.anchoredPosition = targetPos;
            cg.alpha = targetAlpha;

            if (nextPortrait == null)
            {
                speakerPortraitImage.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator ButtonPressAnim(Transform btn)
    {
        if (btn == null) yield break;

        Vector3 original  = Vector3.one;
        Vector3 squish    = Vector3.one * buttonPressScale;
        float   half      = buttonAnimDuration / 2f;
        float   elapsed   = 0f;

        // Squish down
        while (elapsed < half)
        {
            if (btn == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            btn.localScale = Vector3.Lerp(original, squish, elapsed / half);
            yield return null;
        }

        elapsed = 0f;

        // Bounce back
        while (elapsed < half)
        {
            if (btn == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            btn.localScale = Vector3.Lerp(squish, original, elapsed / half);
            yield return null;
        }

        if (btn != null) btn.localScale = original;
    }


    private void ClearChoices()
    {
        foreach (var btn in _activeChoiceButtons)
        {
            if (btn != null) 
            {
                // Unparent immediately so it doesn't affect the layout group calculation in the same frame
                btn.transform.SetParent(null);
                btn.SetActive(false);
                Destroy(btn);
            }
        }
        _activeChoiceButtons.Clear();
    }
}
