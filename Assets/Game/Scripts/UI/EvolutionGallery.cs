using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class EvolutionGallery : MonoBehaviour
{
    [Header("Dependencies")]
    public Spawner spawner;
    public CurrencyManager currencyManager;

    [Header("UI Setup")]
    public Transform container;
    public GameObject iconTemplate;

    public void InitializeGallery()
    {
        if (spawner == null || container == null || iconTemplate == null) return;

        if (currencyManager == null) currencyManager = FindFirstObjectByType<CurrencyManager>();

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child.gameObject != iconTemplate) Destroy(child.gameObject);
        }

        iconTemplate.SetActive(false);

        for (int i = 0; i < spawner.fellaPrefabs.Length; i++)
        {
            GameObject prefab = spawner.fellaPrefabs[i];
            if (prefab == null) continue;

            Sprite displaySprite = spawner.GetSkinSprite(i);

            if (displaySprite == null)
            {
                SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                if (sr != null) displaySprite = sr.sprite;
            }

            if (displaySprite != null)
            {
                CreateIcon(displaySprite, i);
            }
        }
    }

    void CreateIcon(Sprite sprite, int tierIndex)
    {
        GameObject newIconObj = Instantiate(iconTemplate, container);
        newIconObj.SetActive(true);
        newIconObj.name = "Tier_" + tierIndex;

        Image img = GetImageFromSlot(newIconObj.transform);

        if (img != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;

            if (CollectionManager.IsUnlocked(tierIndex))
                img.color = Color.white;
            else
                img.color = Color.black;
        }

        TextMeshProUGUI valText = null;
        Transform txtTrans = newIconObj.transform.Find("RewardText");

        if (txtTrans != null)
        {
            valText = txtTrans.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            valText = newIconObj.transform.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (valText != null)
        {
            int value = 0;
            if (currencyManager != null) value = currencyManager.GetRewardForTier(tierIndex);

            if (value > 0)
            {
                valText.gameObject.SetActive(true);
                valText.text = "+" + value;
                valText.color = Color.yellow;
            }
            else
            {
                valText.gameObject.SetActive(false);
            }
        }
    }

    public void UnlockTierVisual(int tierIndex)
    {
        Transform targetSlot = container.Find("Tier_" + tierIndex);
        if (targetSlot != null)
        {
            Image img = GetImageFromSlot(targetSlot);
            if (img != null)
            {
                img.color = Color.white;
                StartCoroutine(PopIcon(img.transform));
            }
        }
    }

    Image GetImageFromSlot(Transform slot)
    {
        Image img = slot.GetComponent<Image>();
        if (img == null || img.gameObject == iconTemplate)
        {
            Transform childIcon = slot.Find("Icon");
            if (childIcon != null) img = childIcon.GetComponent<Image>();
        }
        return img;
    }

    IEnumerator PopIcon(Transform target)
    {
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float scale = 1f + Mathf.Sin(t * 10f) * 0.3f;
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        target.localScale = Vector3.one;
    }
}