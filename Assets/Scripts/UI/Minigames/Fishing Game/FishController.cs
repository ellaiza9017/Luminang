using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FishController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Sprite[] frames;
    public float framesPerSecond = 12f;
    public bool loop = true;
    
    [Header("Movement Settings")]
    public float minSwimSpeed = 30f;
    public float maxSwimSpeed = 80f;
    public bool autoFlip = true;
    public bool spriteFacesLeft = false;

    [Header("Spacing Settings")]
    public float personalSpace = 100f;
    
    [Header("Drawn Swim Area")]
    public Collider2D swimAreaCollider;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private int currentFrame;
    private float timer;

    private Vector3 targetPosition;
    private float currentSpeed;

    private bool isFirstMove = true;
    private float initialVisualDirection;

    [HideInInspector] public string assignedWord = "";
    [HideInInspector] public string assignedId = "";
    [HideInInspector] public Sprite iconSprite; 
    [HideInInspector] public Vector3 spawnPosition; // saved at start so we can snap it back
    [HideInInspector] public Vector3 spawnScale;    // saved at start so we can restore its correct facing direction
    
    // NEW: Variable to freeze the fish when caught
    [HideInInspector] public bool isCaught = false;

    public static List<FishController> allFishes = new List<FishController>();

    void OnEnable()
    {
        allFishes.Add(this);
    }

    void OnDisable()
    {
        allFishes.Remove(this);
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        
        if (frames != null && frames.Length > 0)
        {
            iconSprite = frames[0];
            SetSprite(frames[0]);
        }

        spawnPosition = transform.position; // Remember where this fish started!
        spawnScale = transform.localScale;  // Remember its original scale (including mirror!)
        initialVisualDirection = Mathf.Sign(transform.localScale.x);
        targetPosition = transform.position;
        PickNewTargetPosition();
    }

    void Update()
    {
        AnimateSprite();
        Swim();
    }

    void AnimateSprite()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        float timePerFrame = 1f / framesPerSecond;

        if (timer >= timePerFrame)
        {
            timer -= timePerFrame;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                currentFrame = loop ? 0 : frames.Length - 1;
            }

            SetSprite(frames[currentFrame]);
        }
    }

    void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        if (uiImage != null) uiImage.sprite = sprite;
    }

    void Swim()
    {
        // Don't swim away if we are being reeled in!
        if (swimAreaCollider == null || isCaught) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            PickNewTargetPosition();
        }
    }

    void PickNewTargetPosition()
    {
        if (swimAreaCollider == null) return;
        Bounds bounds = swimAreaCollider.bounds;
        bool foundValidPoint = false;
        int attempts = 0;
        float desiredMovementDirection = 0f;
        
        if (isFirstMove)
        {
            desiredMovementDirection = initialVisualDirection;
            if (spriteFacesLeft) desiredMovementDirection *= -1f;
        }

        while (!foundValidPoint && attempts < 50)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            if (desiredMovementDirection > 0 && randomX < transform.position.x) { attempts++; continue; }
            if (desiredMovementDirection < 0 && randomX > transform.position.x) { attempts++; continue; }

            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            Vector2 randomPoint = new Vector2(randomX, randomY);

            if (swimAreaCollider.OverlapPoint(randomPoint))
            {
                bool tooCrowded = false;
                foreach (var otherFish in allFishes)
                {
                    if (otherFish != this)
                    {
                        if (Vector2.Distance(randomPoint, otherFish.transform.position) < personalSpace ||
                            Vector2.Distance(randomPoint, otherFish.targetPosition) < personalSpace)
                        {
                            tooCrowded = true;
                            break;
                        }
                    }
                }
                if (!tooCrowded)
                {
                    targetPosition = new Vector3(randomX, randomY, transform.position.z);
                    foundValidPoint = true;
                }
            }
            attempts++;
        }

        if (!foundValidPoint && isFirstMove)
        {
            isFirstMove = false;
            PickNewTargetPosition(); 
            return;
        }
        isFirstMove = false;

        if (!foundValidPoint)
        {
            for (int i = 0; i < 30; i++)
            {
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                Vector2 randomPoint = new Vector2(randomX, randomY);
                if (swimAreaCollider.OverlapPoint(randomPoint))
                {
                    targetPosition = new Vector3(randomX, randomY, transform.position.z);
                    foundValidPoint = true;
                    break;
                }
            }
        }

        if (!foundValidPoint)
        {
            targetPosition = transform.position;
            return;
        }
        
        currentSpeed = Random.Range(minSwimSpeed, maxSwimSpeed);

        if (autoFlip)
        {
            float direction = targetPosition.x > transform.position.x ? 1f : -1f;
            if (spriteFacesLeft) direction *= -1f;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }
    }

    // Called externally by FishTouchHandler when this fish is tapped on mobile/desktop
    public void OnFishTapped()
    {
        if (isCaught) return;
        HandleClick();
    }

    void HandleClick()
    {
        bool alreadySelected = FishingSequenceManager.Instance != null &&
                               FishingSequenceManager.Instance.currentlySelectedFish == this;

        if (alreadySelected)
        {
            // Clicking the same fish again = deselect it
            if (FishTooltip.Instance != null) FishTooltip.Instance.HideTooltip();
            FishingSequenceManager.Instance.DeselectFish();
        }
        else
        {
            // Select this fish
            if (FishTooltip.Instance != null && !string.IsNullOrEmpty(assignedWord))
                FishTooltip.Instance.ShowTooltip(this, assignedWord);

            if (FishingSequenceManager.Instance != null)
                FishingSequenceManager.Instance.SelectFish(this);
        }
    }
}
