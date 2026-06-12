using UnityEngine;

public class EnemyHealthBarView : MonoBehaviour
{
    private static Sprite cachedWhiteSprite;

    [Header("Appearance")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Vector2 size = new Vector2(0.55f, 0.06f);
    [SerializeField] private Color healthyFillColor = Color.red;
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.18f, 0.25f, 0.85f);
    [SerializeField] private Color lowHealthFillColor = new Color(0.95f, 0.28f, 0.24f, 1f);
    [SerializeField] [Range(0.05f, 1f)] private float lowHealthThreshold = 0.3f;
    [SerializeField] private int sortingOrder = 25;

    private Enemy owner;
    private Transform fillTransform;

    // Added: store the fill's original scale and any initial positional offset so UpdateFill preserves height and custom offsets.
    private Vector3 fillOriginalScale = Vector3.one;
    private Vector3 fillPositionOffset = Vector3.zero;

    public void Initialise(Enemy ownerEnemy)
    {
        owner = ownerEnemy;
        ResolveRenderers();
        ApplyAppearance();
        ApplyWorldSpaceCompensation();

        fillTransform = fillRenderer ? fillRenderer.transform : null;
        if (fillTransform)
        {
            // preserve any small designer offset and capture the original scale set in ApplyAppearance
            fillTransform.localPosition = new Vector3(-0.02f, 0f, 0f);
            fillPositionOffset = fillTransform.localPosition;
            fillOriginalScale = fillTransform.localScale;
        }

        if (owner)
        {
            owner.OnHealthChanged += UpdateFill;
            owner.OnDied += HandleOwnerDeath;
        }
    }

    public void HandleOwnerDeath()
    {
        if (owner)
        {
            owner.OnHealthChanged -= UpdateFill;
            owner.OnDied -= HandleOwnerDeath;
            owner = null;
        }

        if (this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (owner)
        {
            owner.OnHealthChanged -= UpdateFill;
            owner.OnDied -= HandleOwnerDeath;
        }
    }

    private void UpdateFill(float normalised)
    {
        if (!fillTransform)
        {
            return;
        }

        float clamped = Mathf.Clamp01(normalised);

        // Scale X proportionally to the original width, keep original Y (height).
        float newScaleX = Mathf.Max(0.0001f, fillOriginalScale.x * clamped);
        fillTransform.localScale = new Vector3(newScaleX, fillOriginalScale.y, 1f);

        // Move the fill so it clips from the left correctly and preserve any small designer offset.
        float xOffsetFromScaling = (newScaleX - fillOriginalScale.x) * 0.5f;
        fillTransform.localPosition = new Vector3(xOffsetFromScaling + fillPositionOffset.x, fillPositionOffset.y, fillPositionOffset.z);

        if (fillRenderer)
        {
            fillRenderer.color = clamped <= lowHealthThreshold ? lowHealthFillColor : healthyFillColor;
        }
    }

    private void ResolveRenderers()
    {
        backgroundRenderer = backgroundRenderer ? backgroundRenderer : ResolveRenderer("BarBackground", 0);
        fillRenderer = fillRenderer ? fillRenderer : ResolveRenderer("BarFill", 1);
    }

    private SpriteRenderer ResolveRenderer(string childName, int orderOffset)
    {
        Transform child = transform.Find(childName);
        SpriteRenderer renderer = child ? child.GetComponent<SpriteRenderer>() : null;
        if (renderer)
        {
            return renderer;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(transform, false);
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;

        renderer = childObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder + orderOffset;
        return renderer;
    }

    private void ApplyAppearance()
    {
        Vector2 resolvedSize = new Vector2(
            Mathf.Max(0.1f, size.x),
            Mathf.Max(0.02f, size.y));

        if (backgroundRenderer)
        {
            backgroundRenderer.sprite = backgroundSprite ? backgroundSprite : CreateWhiteSprite();
            backgroundRenderer.color = backgroundColor;
            backgroundRenderer.sortingOrder = sortingOrder;
            backgroundRenderer.transform.localPosition = Vector3.zero;
            backgroundRenderer.transform.localScale = new Vector3(resolvedSize.x, resolvedSize.y, 1f);
        }

        if (fillRenderer)
        {
            fillRenderer.sprite = fillSprite ? fillSprite : CreateWhiteSprite();
            fillRenderer.color = healthyFillColor;
            fillRenderer.sortingOrder = sortingOrder + 1;
            // set the intended base width/height here; UpdateFill will use these as the baseline
            fillRenderer.transform.localScale = new Vector3(
                Mathf.Max(0.05f, resolvedSize.x - 0.04f),
                Mathf.Max(0.03f, resolvedSize.y - 0.02f),
                1f);
        }
    }

    private static Sprite CreateWhiteSprite()
    {
        if (cachedWhiteSprite)
        {
            return cachedWhiteSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "HealthBarWhiteSprite";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        cachedWhiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        cachedWhiteSprite.name = "HealthBarWhiteSprite";
        return cachedWhiteSprite;
    }

    private void ApplyWorldSpaceCompensation()
    {
        if (!owner)
        {
            return;
        }

        Vector3 lossyScale = owner.transform.lossyScale;
        transform.localScale = new Vector3(
            SafeInverse(lossyScale.x),
            SafeInverse(lossyScale.y),
            1f);
    }

    private void LateUpdate()
    {
        ApplyWorldSpaceCompensation();
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) <= 0.0001f ? 1f : 1f / value;
    }
}
