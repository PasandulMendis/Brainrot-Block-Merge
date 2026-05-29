using UnityEngine;
using TMPro;

public class CosmeticManager : MonoBehaviour
{
    [System.Serializable]
    public class CosmeticItem
    {
        public string itemID;
        public Sprite realSprite;

        [Tooltip("ONLY FOR BUCKETS: The cropped front bar to create the 3D illusion!")]
        public Sprite topBarSprite;
    }

    [System.Serializable]
    public class LaserCosmeticItem
    {
        public string itemID;
        public Sprite laserSprite;
        public Color laserColor = new Color(1, 1, 1, 0.5f);
        public float width = 0.05f;

        [Header("Effects")]
        [Tooltip("Type text here (like '67') to override the sprite!")]
        public string textPattern;

        [Tooltip("Optional: Drag a TextMeshPro Font Asset here!")]
        public TMP_FontAsset customFont;

        public bool useTiling;
        public float scrollSpeed;
        public bool isRainbow;
        public bool isPulsing;
    }

    [Header("Background Setup")]
    public SpriteRenderer backgroundRenderer;
    public CosmeticItem[] availableBackgrounds;

    [Header("Bucket Setup")]
    public SpriteRenderer bucketFrontRenderer;
    [Tooltip("The cropped front bar renderer on a higher sorting layer")]
    public SpriteRenderer bucketTopBarRenderer;
    public CosmeticItem[] availableBuckets;

    [Header("Laser Setup")]
    public LaserCosmeticItem[] availableLasers;

    void Start()
    {
        ApplyCosmetics();
    }

    void ApplyCosmetics()
    {
        string equippedBG = PlayerPrefs.GetString("Shop_Equipped_bg", "");
        if (!string.IsNullOrEmpty(equippedBG) && backgroundRenderer != null)
        {
            foreach (CosmeticItem bg in availableBackgrounds)
            {
                if (bg.itemID == equippedBG) { backgroundRenderer.sprite = bg.realSprite; break; }
            }
        }

        string equippedBucket = PlayerPrefs.GetString("Shop_Equipped_bucket", "");
        if (!string.IsNullOrEmpty(equippedBucket) && bucketFrontRenderer != null)
        {
            foreach (CosmeticItem bucket in availableBuckets)
            {
                if (bucket.itemID == equippedBucket)
                {
                    bucketFrontRenderer.sprite = bucket.realSprite;

                    if (bucketTopBarRenderer != null)
                    {
                        bucketTopBarRenderer.sprite = bucket.topBarSprite;
                    }
                    break;
                }
            }
        }

        ApplyLasers();
    }

    void ApplyLasers()
    {
        Spawner spawner = GetComponent<Spawner>();
        if (spawner == null || spawner.guideLight == null) return;

        string equippedLaser = PlayerPrefs.GetString("Shop_Equipped_laser", "laser_default");
        if (equippedLaser == "") equippedLaser = "laser_default";

        foreach (LaserCosmeticItem laser in availableLasers)
        {
            if (laser.itemID == equippedLaser)
            {
                LaserEffect effect = spawner.guideLight.GetComponent<LaserEffect>();
                if (effect == null) effect = spawner.guideLight.gameObject.AddComponent<LaserEffect>();

                effect.Setup(laser.laserSprite, laser.laserColor, laser.width, laser.useTiling, laser.isRainbow, laser.isPulsing, laser.scrollSpeed, laser.textPattern, laser.customFont);
                break;
            }
        }
    }
}