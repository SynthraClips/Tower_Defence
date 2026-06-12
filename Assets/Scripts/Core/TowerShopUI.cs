using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TowerShopUI : MonoBehaviour
{
    [System.Serializable]
    public struct TowerShopEntry
    {
        public string key;
        public Tower prefab;
        public Color placeholderColour;
    }

    [Header("References")]
    public TowerPlacer placer;
    public Tower ballistaPrefab;
    public Tower cannonPrefab;
    public Tower magicPrefab;
    public Tower airPrefab;
    public Tower catapultPrefab;

    [Header("Bottom Button Bar")]
    [Tooltip("Creates missing tower buttons using the existing BuyBallista/BuyCannon button as a template. No runtime text labels are created.")]
    [SerializeField] private bool autoCreateMissingTowerButtons = true;

    [Tooltip("Keeps tower buttons and the speed button on one row. This only moves UI buttons, not gameplay objects.")]
    [SerializeField] private bool alignTowerAndSpeedButtons = true;

    [SerializeField] private RectTransform toolbarParent;
    [SerializeField] private float buttonSpacing = 54f;
    [SerializeField] private float rowYOffset = -44f;
    [SerializeField] private Vector2 iconSize = new Vector2(38f, 38f);
    [SerializeField] private TowerShopEntry[] extraTowerEntries;
    [SerializeField] private bool warnWhenShopIconMissing = true;

    private readonly List<Button> managedButtons = new List<Button>();

    private void Awake()
    {
        if (!placer)
        {
            placer = FindAnyObjectByType<TowerPlacer>();
        }
    }

    private void Start()
    {
        EnsureTowerPlacer();
        BuildOrRefreshBottomButtons();
    }

    public void BuyBallista() { BuyLightAttack(); }
    public void BuyCannon() { BuyHeavyAttack(); }

    public void BuyLightAttack() { BuyTower(ballistaPrefab); }
    public void BuyHeavyAttack() { BuyTower(cannonPrefab); }
    public void BuyMagicAttack() { BuyTower(magicPrefab); }
    public void BuyAirAttack() { BuyTower(airPrefab); }
    public void BuyCatapultAttack() { BuyTower(catapultPrefab); }

    public void BuyTower(Tower towerPrefab)
    {
        if (!towerPrefab)
        {
            Debug.LogWarning("[TowerShopUI] No tower prefab assigned for this shop button.", this);
            return;
        }

        if (!placer)
        {
            EnsureTowerPlacer();
        }

        if (!placer)
        {
            Debug.LogWarning("[TowerShopUI] No TowerPlacer found in the scene.", this);
            return;
        }

        placer.BeginPlacement(towerPrefab);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) BuyLightAttack();
        if (Keyboard.current.digit2Key.wasPressedThisFrame) BuyHeavyAttack();
        if (Keyboard.current.digit3Key.wasPressedThisFrame) BuyMagicAttack();
        if (Keyboard.current.digit4Key.wasPressedThisFrame) BuyAirAttack();
        if (Keyboard.current.digit5Key.wasPressedThisFrame) BuyCatapultAttack();
    }

    private void BuildOrRefreshBottomButtons()
    {
        List<TowerShopEntry> entries = BuildEntryList();
        if (entries.Count == 0)
        {
            return;
        }

        RectTransform parent = ResolveToolbarParent();
        if (!parent)
        {
            Debug.LogWarning("[TowerShopUI] Could not find a toolbar parent.", this);
            return;
        }

        Button templateButton = FindButtonByName("BuyBallista") ?? FindButtonByName("BuyCannon");
        RectTransform templateRect = templateButton ? templateButton.transform as RectTransform : null;

        managedButtons.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            TowerShopEntry entry = entries[i];
            Button button = FindButtonByName(entry.key);
            bool created = false;

            if (!button && autoCreateMissingTowerButtons && templateRect)
            {
                button = CreateButton(parent, entry.key, templateRect);
                created = true;
            }
            else if (!button && autoCreateMissingTowerButtons)
            {
                button = CreateDefaultButton(parent, entry.key, i);
                created = true;
            }

            if (!button)
            {
                continue;
            }

            button.transform.SetParent(parent, false);
            ConfigureButton(button, entry, ResolveBuyAction(entry.key), created);
            managedButtons.Add(button);
        }

        if (alignTowerAndSpeedButtons)
        {
            AlignBottomRow(parent, managedButtons);
        }
    }

    private List<TowerShopEntry> BuildEntryList()
    {
        var entries = new List<TowerShopEntry>();
        AddEntry(entries, "BuyBallista", ballistaPrefab, new Color(0.55f, 0.36f, 0.14f, 0.95f));
        AddEntry(entries, "BuyCannon", cannonPrefab, new Color(0.28f, 0.28f, 0.30f, 0.95f));
        AddEntry(entries, "BuyMagic", magicPrefab, new Color(0.34f, 0.20f, 0.58f, 0.95f));
        AddEntry(entries, "BuyAir", airPrefab, new Color(0.20f, 0.48f, 0.74f, 0.95f));
        AddEntry(entries, "BuyCatapult", catapultPrefab, new Color(0.46f, 0.30f, 0.16f, 0.95f));

        if (extraTowerEntries != null)
        {
            foreach (TowerShopEntry extra in extraTowerEntries)
            {
                if (extra.prefab)
                {
                    entries.Add(extra);
                }
            }
        }

        return entries;
    }

    private static void AddEntry(List<TowerShopEntry> entries, string key, Tower prefab, Color colour)
    {
        if (!prefab)
        {
            return;
        }

        entries.Add(new TowerShopEntry
        {
            key = key,
            prefab = prefab,
            placeholderColour = colour
        });
    }

    private RectTransform ResolveToolbarParent()
    {
        if (toolbarParent)
        {
            return toolbarParent;
        }

        Canvas canvas = FindPreferredCanvas();
        if (!canvas)
        {
            return null;
        }

        toolbarParent = EnsureRuntimeToolbar(canvas.transform as RectTransform);
        return toolbarParent;
    }

    private Canvas FindPreferredCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            if (!canvas)
            {
                continue;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.overrideSorting)
            {
                return canvas;
            }
        }

        return FindAnyObjectByType<Canvas>();
    }

    private Button CreateButton(RectTransform parent, string key, RectTransform templateRect)
    {
        var buttonObject = new GameObject(key, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.transform as RectTransform;
        CopyRectTransformSettings(templateRect, rect);

        return buttonObject.GetComponent<Button>();
    }

    private Button CreateDefaultButton(RectTransform parent, string key, int index)
    {
        var buttonObject = new GameObject(key, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(52f, 52f);
        rect.anchoredPosition = new Vector2((-buttonSpacing * 1.5f) + buttonSpacing * index, 0f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        return buttonObject.GetComponent<Button>();
    }

    private RectTransform EnsureRuntimeToolbar(RectTransform canvasRect)
    {
        if (!canvasRect)
        {
            return null;
        }

        Transform existingBar = canvasRect.Find("TowerShopBar");
        RectTransform barRect = existingBar ? existingBar as RectTransform : null;
        if (!barRect)
        {
            GameObject barObject = new GameObject("TowerShopBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(canvasRect, false);
            barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = new Vector2(0f, 42f);
            barRect.sizeDelta = new Vector2(430f, 72f);

            Image barImage = barObject.GetComponent<Image>();
            barImage.color = new Color(0.05f, 0.1f, 0.16f, 0.86f);
            barImage.raycastTarget = false;
        }

        return barRect;
    }

    private void ConfigureButton(Button button, TowerShopEntry entry, UnityAction action, bool buttonWasCreated)
    {
        if (!button)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (!image)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        // Keep existing scene button sprites intact. Only brand-new buttons get simple placeholder panels.
        if (buttonWasCreated || image.sprite == null || ResolveShopIcon(entry.prefab))
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = entry.placeholderColour.a > 0f ? entry.placeholderColour : new Color(0.18f, 0.23f, 0.32f, 0.95f);
        }

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        ConfigureShopIcon(button.transform as RectTransform, entry.prefab);
    }

    private void AlignBottomRow(RectTransform parent, List<Button> towerButtons)
    {
        if (!parent || towerButtons.Count == 0)
        {
            return;
        }

        bool runtimeToolbar = parent.name == "TowerShopBar";
        Button templateButton = runtimeToolbar ? null : FindButtonByName("BuyBallista") ?? FindButtonByName("BuyCannon");
        RectTransform templateRect = templateButton ? templateButton.transform as RectTransform : null;

        float x = runtimeToolbar
            ? (-buttonSpacing * (towerButtons.Count - 1)) * 0.5f
            : templateRect ? templateRect.anchoredPosition.x : (-buttonSpacing * (towerButtons.Count - 1)) * 0.5f;
        float y = runtimeToolbar
            ? 0f
            : templateRect ? templateRect.anchoredPosition.y + rowYOffset : 0f;

        int index = 0;
        foreach (Button button in towerButtons)
        {
            if (!button || !(button.transform is RectTransform rect))
            {
                continue;
            }

            if (templateRect)
            {
                rect.anchorMin = templateRect.anchorMin;
                rect.anchorMax = templateRect.anchorMax;
                rect.pivot = templateRect.pivot;
                rect.sizeDelta = templateRect.sizeDelta;
                rect.localScale = templateRect.localScale;
                rect.localRotation = templateRect.localRotation;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(52f, 52f);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            rect.anchoredPosition = new Vector2(x + buttonSpacing * index, y);
            index++;
        }

        Button speedButton = FindButtonByName("SpeedControl");
        if (speedButton && speedButton.transform.parent == parent && speedButton.transform is RectTransform speedRect)
        {
            if (templateRect)
            {
                speedRect.anchorMin = templateRect.anchorMin;
                speedRect.anchorMax = templateRect.anchorMax;
                speedRect.pivot = templateRect.pivot;
                speedRect.sizeDelta = templateRect.sizeDelta;
                speedRect.localScale = templateRect.localScale;
                speedRect.localRotation = templateRect.localRotation;
            }
            else
            {
                speedRect.anchorMin = new Vector2(0.5f, 0.5f);
                speedRect.anchorMax = new Vector2(0.5f, 0.5f);
                speedRect.pivot = new Vector2(0.5f, 0.5f);
                speedRect.sizeDelta = new Vector2(52f, 52f);
                speedRect.localScale = Vector3.one;
                speedRect.localRotation = Quaternion.identity;
            }

            speedRect.anchoredPosition = new Vector2(x + buttonSpacing * index, y);
        }
    }

    private UnityAction ResolveBuyAction(string key)
    {
        return key switch
        {
            "BuyBallista" => BuyLightAttack,
            "BuyCannon" => BuyHeavyAttack,
            "BuyMagic" => BuyMagicAttack,
            "BuyAir" => BuyAirAttack,
            "BuyCatapult" => BuyCatapultAttack,
            _ => null,
        };
    }

    private static void CopyRectTransformSettings(RectTransform source, RectTransform target)
    {
        if (!source || !target)
        {
            return;
        }

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
        target.anchoredPosition = source.anchoredPosition;
    }

    private void ConfigureShopIcon(RectTransform buttonRect, Tower towerPrefab)
    {
        if (!buttonRect)
        {
            return;
        }

        Transform existingIconTransform = buttonRect.Find("ShopIcon");
        Image iconImage = existingIconTransform ? existingIconTransform.GetComponent<Image>() : null;
        if (!iconImage)
        {
            GameObject iconObject = new GameObject("ShopIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(buttonRect, false);
            iconImage = iconObject.GetComponent<Image>();
        }

        RectTransform iconRect = iconImage.transform as RectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = iconSize;
        iconRect.anchoredPosition = Vector2.zero;
        Vector3 parentScale = buttonRect.localScale;
        iconRect.localScale = new Vector3(
            SafeInverse(parentScale.x),
            SafeInverse(parentScale.y),
            1f);

        Sprite iconSprite = ResolveShopIcon(towerPrefab);
        iconImage.sprite = iconSprite;
        iconImage.preserveAspect = true;
        iconImage.color = iconSprite ? Color.white : new Color(1f, 1f, 1f, 0f);
        iconImage.raycastTarget = false;

        if (!iconSprite && towerPrefab && warnWhenShopIconMissing)
        {
            Debug.LogWarning($"[TowerShopUI] Missing shop icon for tower '{towerPrefab.towerDisplayName}'.", towerPrefab);
        }
    }

    private static Sprite ResolveShopIcon(Tower towerPrefab)
    {
        if (!towerPrefab)
        {
            return null;
        }

        if (towerPrefab.shopIcon)
        {
            return towerPrefab.shopIcon;
        }

        string fallbackName = towerPrefab.towerArchetype switch
        {
            TowerArchetype.Heavy => "shop_icon_cannon",
            TowerArchetype.Magic => "shop_icon_magic",
            TowerArchetype.Air => "shop_icon_air",
            _ => "shop_icon_ballista",
        };

        return Resources.Load<Sprite>($"UI/{fallbackName}");
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) <= 0.0001f ? 1f : 1f / value;
    }

    private Button FindButtonByName(string buttonName)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
        {
            if (button && button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private void EnsureTowerPlacer()
    {
        if (placer)
        {
            return;
        }

        placer = FindAnyObjectByType<TowerPlacer>();
        if (placer)
        {
            return;
        }

        GameObject placerObject = new GameObject("RuntimeTowerPlacer");
        placer = placerObject.AddComponent<TowerPlacer>();
        placer.cam = Camera.main;
        placer.build = BuildManager.Instance;
        Debug.LogWarning("[TowerShopUI] TowerPlacer was missing, so a runtime fallback TowerPlacer was created.", this);
    }
}
