using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingPopupSystem : MonoBehaviour
{
    private static FloatingPopupSystem instance;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 0.75f;
    [SerializeField] private Vector2 defaultUiOffset = new Vector2(0f, 30f);
    [SerializeField] private Vector3 defaultWorldOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector2 defaultTravel = new Vector2(0f, 20f);

    public static FloatingPopupSystem Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("FloatingPopupSystem");
                instance = go.AddComponent<FloatingPopupSystem>();
                DontDestroyOnLoad(go);
            }

            return instance;
        }
    }

    public void ShowUiPopup(TextMeshProUGUI source, string text, Color color, float? duration = null, Vector2? offset = null)
    {
        if (!source || !source.transform.parent) return;

        var popupObject = new GameObject($"{source.name}_Popup");
        popupObject.transform.SetParent(source.transform.parent, false);

        var popupTransform = popupObject.AddComponent<RectTransform>();
        popupTransform.anchorMin = source.rectTransform.anchorMin;
        popupTransform.anchorMax = source.rectTransform.anchorMax;
        popupTransform.pivot = source.rectTransform.pivot;
        popupTransform.anchoredPosition = source.rectTransform.anchoredPosition + (offset ?? defaultUiOffset);
        popupTransform.sizeDelta = source.rectTransform.sizeDelta;

        var popup = popupObject.AddComponent<TextMeshProUGUI>();
        popup.font = source.font;
        popup.fontSharedMaterial = source.fontSharedMaterial;
        popup.fontSize = source.fontSize * 0.8f;
        popup.alignment = TextAlignmentOptions.Center;
        popup.raycastTarget = false;
        popup.text = text;
        popup.color = color;

        StartCoroutine(AnimateUiPopup(popup, duration ?? defaultDuration));
    }

    public void ShowWorldPopup(Vector3 worldPosition, string text, Color color, Camera targetCamera, float? duration = null, Vector3? offset = null)
    {
        if (targetCamera == null) return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (!canvas) return;

        var popupObject = new GameObject("WorldPopup");
        popupObject.transform.SetParent(canvas.transform, false);

        var popupTransform = popupObject.AddComponent<RectTransform>();
        popupTransform.sizeDelta = new Vector2(120f, 36f);

        var popup = popupObject.AddComponent<TextMeshProUGUI>();
        popup.fontSize = 24f;
        popup.alignment = TextAlignmentOptions.Center;
        popup.raycastTarget = false;
        popup.text = text;
        popup.color = color;

        StartCoroutine(AnimateWorldPopup(popup, worldPosition + (offset ?? defaultWorldOffset), targetCamera, duration ?? defaultDuration));
    }

    private IEnumerator AnimateUiPopup(TextMeshProUGUI popup, float duration)
    {
        if (!popup) yield break;

        var rect = popup.rectTransform;
        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + defaultTravel;
        Color startColor = popup.color;
        float elapsed = 0f;

        while (elapsed < duration && popup)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            popup.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        if (popup) Destroy(popup.gameObject);
    }

    private IEnumerator AnimateWorldPopup(TextMeshProUGUI popup, Vector3 worldPosition, Camera targetCamera, float duration)
    {
        if (!popup) yield break;

        Color startColor = popup.color;
        Vector3 endWorld = worldPosition + new Vector3(0f, 0.35f, 0f);
        float elapsed = 0f;

        while (elapsed < duration && popup)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 currentWorld = Vector3.Lerp(worldPosition, endWorld, t);
            popup.rectTransform.position = RectTransformUtility.WorldToScreenPoint(targetCamera, currentWorld);
            popup.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        if (popup) Destroy(popup.gameObject);
    }
}
