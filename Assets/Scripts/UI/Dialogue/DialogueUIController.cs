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

    [Header("Choices Curtain Drop Animation")]
    public float curtainDropDuration = 0.5f;
    public float curtainDelay        = 0.1f;

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

    // Paging
    private List<string>                  _pages = new List<string>();
    private List<string>                  _translatedPages = new List<string>();
    private int                           _currentPageIndex = 0;
    private bool                          _isSkippingAnimation = false;

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
        string rawText = injectedPrefixText + node.dialogueText;
        string rawTranslated = node.translatedText;
        
        if (TimeManager.Instance != null && rawText != null)
        {
            string greeting = TimeManager.Instance.GetGreetingTag();
            rawText = rawText.Replace("{Greeting}", greeting);
            
            string engGreeting = TimeManager.Instance.IsMorning ? "Good morning" : (TimeManager.Instance.IsAfternoon ? "Good afternoon" : "Good evening");
            if (rawTranslated != null) rawTranslated = rawTranslated.Replace("{Greeting}", engGreeting);
        }
        
        _fullText = rawText;
        _translatedText = rawTranslated;
        _isSkippingAnimation = skipAnimation;
        
        // Reset prefix after consuming
        injectedPrefixText = "";
        
        _isTranslatedShowing = true;
        _skipTyping       = skipAnimation; // If skipping, we force it here

        // Split full text and translated text into pages (sentences)
        _pages = SplitIntoSentences(_fullText);
        _translatedPages = SplitIntoSentences(_translatedText);
        
        if (_pages.Count == 0) _pages.Add("");
        _currentPageIndex = 0;

        // Show/Hide translate button
        if (translateButton != null)
            translateButton.gameObject.SetActive(false);

        if (speakerNameText != null)
        {
            string sName = node.speakerName;
            if (string.IsNullOrEmpty(sName) && DialogueManager.Instance != null)
            {
                InteractableNPC activeNPC = DialogueManager.Instance.GetActiveNPC();
                if (activeNPC != null)
                {
                    sName = activeNPC.gameObject.name
                        .Replace("_Rigged", "").Replace("_rigged", "").Replace("_Rrrigged", "")
                        .Replace("Vendor", "").Replace("barista", "").Trim();
                }
            }
            speakerNameText.text = sName;
        }

        // Clear text immediately so no placeholder shows during pop-in
        if (dialogueText != null) dialogueText.text = "";

        ClearChoices();

        // Spawn choice buttons if there are any choices with text
        if (node.choices != null)
        {
            int choiceIndex = 0;
            foreach (var choice in node.choices)
            {
                if (string.IsNullOrEmpty(choice.choiceText)) continue;

                GameObject obj = Instantiate(choiceButtonPrefab, choicesContainer);
                obj.SetActive(true);
                _activeChoiceButtons.Add(obj);

                var btnText = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    // If this node is a yes/no question, override labels with Ilocano
                    if (node.isYesNoChoice)
                    {
                        if (choiceIndex == 0) btnText.text = "Wen";
                        else if (choiceIndex == 1) btnText.text = "Saan";
                        else btnText.text = choice.choiceText;
                    }
                    else
                    {
                        btnText.text = choice.choiceText;
                    }
                }

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
                choiceIndex++;
            }
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

        DisplayCurrentPage();
    }

    private void DisplayCurrentPage()
    {
        if (dialogueText != null) dialogueText.text = "";
        
        bool isLastPage = _currentPageIndex >= _pages.Count - 1;
        bool requiresSTT = DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null;
        int visibleChoices = _activeChoiceButtons.Count;

        // Ensure next button logic
        if (nextButton != null)
        {
            if (!isLastPage)
            {
                // If not last page, ALWAYS show NEXT button so player can paginate
                nextButton.gameObject.SetActive(true);
            }
            else
            {
                // If last page, hide NEXT button if there are choices.
                // Temporarily bypassing STT requirement to allow skipping.
                nextButton.gameObject.SetActive(visibleChoices == 0);
            }
        }

        // If not last page, hide choices so they don't block
        if (choicesGroup != null)
        {
            choicesGroup.SetActive(isLastPage && visibleChoices > 0);
        }

        ShowDialogue(true, _isSkippingAnimation);
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
            if (choicesGroup != null)
            {
                choicesGroup.SetActive(false);
                choicesGroup.transform.localScale = new Vector3(1, 0, 1);
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
            choicesGroup.transform.localScale = new Vector3(1, 0, 1);
        }

        // NOTE: We intentionally do NOT call movementUI.SetActive(true) here.
        // The player is still in dialogue — UI should stay hidden.
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
            if (_currentPageIndex < _pages.Count - 1)
            {
                // Advance to the next page
                _currentPageIndex++;
                DisplayCurrentPage();
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
    }

    private void OnPrevClicked()
    {
        StartCoroutine(ButtonPressAnim(prevButton.transform));
        
        // If we are paginating, maybe go to previous page?
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            DisplayCurrentPage();
            return;
        }

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
            int idx = Mathf.Min(_currentPageIndex, Mathf.Max(0, _translatedPages.Count - 1));
            dialogueText.text = _isTranslatedShowing && _translatedPages.Count > 0 ? _translatedPages[idx] : _pages[_currentPageIndex];
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

        if (!isAlreadyOpen && !skipAnimation)
            yield return StartCoroutine(PopInPanel());
        else
        {
            dialoguePanel.transform.localScale = Vector3.one;
            var cg = dialoguePanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        string textToShow = _pages.Count > 0 ? _pages[_currentPageIndex] : "";
        if (_isTranslatedShowing && _translatedPages.Count > 0) 
        {
            int idx = Mathf.Min(_currentPageIndex, Mathf.Max(0, _translatedPages.Count - 1));
            textToShow = _translatedPages[idx];
        }

        if (typingSpeed > 0 && !skipAnimation)
        {
            if (_typeTextCoroutine != null) StopCoroutine(_typeTextCoroutine);
            _typeTextCoroutine = StartCoroutine(TypeText(textToShow));
            yield return _typeTextCoroutine;
        }
        else
            if (dialogueText != null) dialogueText.text = textToShow;

        // Only show the choices area when there are actual choices to pick from AND we are on the last page
        bool isLastPage = _currentPageIndex >= _pages.Count - 1;
        if (_activeChoiceButtons.Count > 0 && choicesGroup != null && isLastPage)
        {
            choicesGroup.SetActive(true);
            
            // Re-enforce scale BEFORE layout rebuild to prevent "squish"
            choicesGroup.transform.localScale = new Vector3(1, 0, 1);
            
            RectTransform choicesRT = choicesGroup.GetComponent<RectTransform>();
            if (choicesRT != null) LayoutRebuilder.ForceRebuildLayoutImmediate(choicesRT);

            if (curtainDelay > 0 && !skipAnimation) yield return new WaitForSeconds(curtainDelay);
            yield return StartCoroutine(CurtainDrop());
        }
    }

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
    }

    private IEnumerator CurtainDrop()
    {
        if (choicesGroup == null) yield break; // Safety Check

        Vector3 start = new Vector3(1f, 0f, 1f);
        Vector3 end   = Vector3.one;
        choicesGroup.transform.localScale = start;

        float elapsed = 0f;
        while (elapsed < curtainDropDuration)
        {
            if (choicesGroup == null) yield break; // Safety Check mid-loop

            elapsed += Time.unscaledDeltaTime;
            float t     = Mathf.Clamp01(elapsed / curtainDropDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            choicesGroup.transform.localScale = Vector3.Lerp(start, end, eased);
            yield return null;
        }
        if (choicesGroup != null) choicesGroup.transform.localScale = end;
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
            if (btn != null) Destroy(btn);
        _activeChoiceButtons.Clear();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && dialoguePanel != null && dialoguePanel.activeInHierarchy)
        {
            // Navigation
            if (nextButton != null && nextButton.gameObject.activeInHierarchy && nextButton.interactable)
            {
                if (kb.rightArrowKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                    nextButton.onClick.Invoke();
            }
            if (prevButton != null && prevButton.gameObject.activeInHierarchy && prevButton.interactable)
            {
                if (kb.leftArrowKey.wasPressedThisFrame)
                    prevButton.onClick.Invoke();
            }

            // Choices (1, 2, 3, etc.)
            if (_activeChoiceButtons != null && _activeChoiceButtons.Count > 0)
            {
                if (kb.digit1Key.wasPressedThisFrame) _activeChoiceButtons[0].GetComponent<Button>()?.onClick.Invoke();
                if (kb.digit2Key.wasPressedThisFrame && _activeChoiceButtons.Count >= 2) _activeChoiceButtons[1].GetComponent<Button>()?.onClick.Invoke();
                if (kb.digit3Key.wasPressedThisFrame && _activeChoiceButtons.Count >= 3) _activeChoiceButtons[2].GetComponent<Button>()?.onClick.Invoke();
                
                // Allow Enter to also select the first choice if Next isn't active
                if (kb.enterKey.wasPressedThisFrame && (nextButton == null || !nextButton.gameObject.activeInHierarchy))
                {
                    _activeChoiceButtons[0].GetComponent<Button>()?.onClick.Invoke();
                }
            }
        }
#endif
    }

    private List<string> SplitIntoSentences(string text)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        string[] paragraphs = text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var paragraph in paragraphs)
        {
            string p = paragraph.Trim();
            if (string.IsNullOrEmpty(p)) continue;

            int startIndex = 0;
            for (int i = 0; i < p.Length; i++)
            {
                char c = p[i];
                if (c == '.' || c == '!' || c == '?')
                {
                    // Check for ellipsis
                    if (c == '.' && i + 2 < p.Length && p[i+1] == '.' && p[i+2] == '.')
                    {
                        i += 2;
                        continue;
                    }
                    
                    int endIndex = i;
                    while (endIndex + 1 < p.Length && (p[endIndex + 1] == '"' || p[endIndex + 1] == '\'' || p[endIndex + 1] == ')'))
                    {
                        endIndex++;
                    }

                    if (endIndex + 1 >= p.Length || char.IsWhiteSpace(p[endIndex + 1]))
                    {
                        string sentence = p.Substring(startIndex, (endIndex + 1) - startIndex).Trim();
                        if (!string.IsNullOrEmpty(sentence))
                        {
                            result.Add(sentence);
                        }
                        startIndex = endIndex + 1;
                        i = endIndex;
                    }
                }
            }

            if (startIndex < p.Length)
            {
                string remaining = p.Substring(startIndex).Trim();
                if (!string.IsNullOrEmpty(remaining))
                {
                    result.Add(remaining);
                }
            }
        }
        return result;
    }
}
