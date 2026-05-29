using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SoftUIGlow : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Drag your blurry white square sprite here!")]
    public Sprite blurSprite;

    [Tooltip("Use Black for a Shadow. Use Yellow/Cyan for a Glow. Alpha controls Intensity.")]
    public Color effectColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Sizing")]
    [Tooltip("How much larger than the panel should the glow expand?")]
    public float spreadSize = 40f;
    [Tooltip("Shift the shadow (e.g., X=0, Y=-10 for a standard drop shadow)")]
    public Vector2 offset = Vector2.zero;

    [Header("Animation (Optional)")]
    [Tooltip("Makes the glow breathe in and out! Great for rare rewards.")]
    public bool animatePulse = false;
    public float pulseSpeed = 3f;
    [Range(0f, 1f)] public float pulseMinAlpha = 0.3f;
    [Range(0f, 1f)] public float pulseMaxAlpha = 1.0f;

    private GameObject shadowObj;
    private Image shadowImage;
    private RectTransform shadowRect;
    private RectTransform myRect;
    private CanvasGroup parentCanvasGroup;
    private float baseAlpha;
    private bool isInitialized = false;

    void Awake()
    {
        InitializeShadow();
    }

    void InitializeShadow()
    {
        if (isInitialized) return;

        myRect = GetComponent<RectTransform>();
        parentCanvasGroup = GetComponent<CanvasGroup>();
        baseAlpha = effectColor.a;

        shadowObj = new GameObject(gameObject.name + "_GlowShadow");
        shadowObj.transform.SetParent(transform.parent, false);
        shadowObj.transform.SetSiblingIndex(transform.GetSiblingIndex());

        shadowRect = shadowObj.AddComponent<RectTransform>();
        shadowRect.anchorMin = myRect.anchorMin;
        shadowRect.anchorMax = myRect.anchorMax;
        shadowRect.pivot = myRect.pivot;

        shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.sprite = blurSprite;
        shadowImage.color = effectColor;
        shadowImage.type = Image.Type.Sliced;
        shadowImage.raycastTarget = false;

        UpdateShadowSize();

        shadowObj.SetActive(gameObject.activeInHierarchy);
        isInitialized = true;
    }

    void OnEnable()
    {
        if (!isInitialized) InitializeShadow();
        if (shadowObj != null) shadowObj.SetActive(true);
    }

    void OnDisable()
    {
        if (shadowObj != null) shadowObj.SetActive(false);
    }

    void Update()
    {
        if (shadowImage == null || shadowObj == null) return;

        if (shadowObj.transform.GetSiblingIndex() > transform.GetSiblingIndex())
        {
            shadowObj.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }

        float currentFadeAlpha = (parentCanvasGroup != null) ? parentCanvasGroup.alpha : 1f;

        float pulseMultiplier = 1f;
        if (animatePulse)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            pulseMultiplier = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);
        }

        Color newColor = effectColor;
        newColor.a = baseAlpha * pulseMultiplier * currentFadeAlpha;
        shadowImage.color = newColor;
    }

    void OnRectTransformDimensionsChange()
    {
        if (shadowRect != null && myRect != null)
        {
            UpdateShadowSize();
        }
    }

    void UpdateShadowSize()
    {
        if (shadowRect == null || myRect == null) return;

        shadowRect.anchoredPosition = myRect.anchoredPosition + offset;
        shadowRect.sizeDelta = myRect.sizeDelta + new Vector2(spreadSize, spreadSize);
        shadowRect.localScale = myRect.localScale;
        shadowRect.rotation = myRect.rotation;
    }

    void OnDestroy()
    {
        if (shadowObj != null) Destroy(shadowObj);
    }
}