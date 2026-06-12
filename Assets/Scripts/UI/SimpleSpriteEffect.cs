using UnityEngine;

public class SimpleSpriteEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.55f;
    [SerializeField] private float startScale = 0.65f;
    [SerializeField] private float endScale = 1.45f;

    private SpriteRenderer spriteRenderer;
    private float elapsed;

    public void Configure(float effectLifetime)
    {
        lifetime = Mathf.Max(0.05f, effectLifetime);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, lifetime));
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);

        if (spriteRenderer)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - t;
            spriteRenderer.color = color;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
