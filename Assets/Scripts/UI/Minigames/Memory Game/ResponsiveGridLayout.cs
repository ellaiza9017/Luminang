using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
[ExecuteAlways]
public class ResponsiveGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 4;
    public int columns = 4;

    [Header("Scaling Options")]
    [Tooltip("If true, the cards will always remain perfect squares instead of stretching to fill the rectangle.")]
    public bool forceSquareCards = true;

    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    private void OnEnable()
    {
        UpdateLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateLayout();
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // This makes it update live in the editor while you tweak values!
        UnityEditor.EditorApplication.delayCall += () => {
            if (this != null) UpdateLayout();
        };
    }
    #endif

    public void UpdateLayout()
    {
        if (gridLayout == null) gridLayout = GetComponent<GridLayoutGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (gridLayout == null || rectTransform == null || rows <= 0 || columns <= 0) return;

        // Get the actual width and height of the container
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // Calculate available space by subtracting padding and spacing
        float availableWidth = width - gridLayout.padding.left - gridLayout.padding.right - (gridLayout.spacing.x * (columns - 1));
        float availableHeight = height - gridLayout.padding.top - gridLayout.padding.bottom - (gridLayout.spacing.y * (rows - 1));

        // Calculate the perfect width and height for each cell
        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;

        if (forceSquareCards)
        {
            // Pick the smaller dimension so it doesn't overflow
            float size = Mathf.Min(cellWidth, cellHeight);
            gridLayout.cellSize = new Vector2(size, size);
        }
        else
        {
            // Stretch to fill the exact space
            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        }
    }
}
