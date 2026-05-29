using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GalleryController : MonoBehaviour
{
    [Header("Dependencies")]
    public PackManager packManager;

    [Header("UI Setup")]
    public GameObject galleryPanel;
    public CanvasGroup panelCanvasGroup;
    public Transform contentContainer;
    public ScrollRect galleryScrollView;

    [Header("Templates")]
    public GameObject headerTemplate;
    public GameObject gridTemplate;
    public GameObject iconTemplate;

    [Header("Menu Cleanup")]
    public GameObject[] menuElementsToHide;

    [Header("Animation")]
    public float fadeDuration = 0.3f;
    public float itemStaggerDelay = 0.02f;

    private bool isPanelOpen = false;

    void Start()
    {
        if (packManager == null) packManager = FindFirstObjectByType<PackManager>();

        if (galleryPanel != null)
        {
            galleryPanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (headerTemplate != null) headerTemplate.SetActive(false);
        if (gridTemplate != null) gridTemplate.SetActive(false);
        if (iconTemplate != null) iconTemplate.SetActive(false);
    }

    void Update()
    {
        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseGallery();
        }
    }

    public void OpenGallery()
    {
        StopAllCoroutines();

        galleryPanel.SetActive(true);
        isPanelOpen = true;
        ToggleMenuHUD(false);

        ClearList();
        GenerateGallery();

        if (galleryScrollView != null)
        {
            galleryScrollView.verticalNormalizedPosition = 1f;
        }

        StartCoroutine(AnimatePanelOpen());
    }

    public void CloseGallery()
    {
        if (!isPanelOpen) return;

        StopAllCoroutines();
        isPanelOpen = false;
        StartCoroutine(AnimatePanelClose());
    }

    void ClearList()
    {
        foreach (Transform child in contentContainer)
        {
            if (child.gameObject != headerTemplate &&
                child.gameObject != gridTemplate &&
                child.gameObject != iconTemplate)
            {
                Destroy(child.gameObject);
            }
        }
    }

    void GenerateGallery()
    {
        if (packManager == null) return;

        foreach (var pack in packManager.availablePacks)
        {
            GameObject header = Instantiate(headerTemplate, contentContainer);
            header.SetActive(true);
            header.transform.localScale = Vector3.zero;
            header.GetComponentInChildren<TextMeshProUGUI>().text = pack.packName;

            GameObject grid = Instantiate(gridTemplate, contentContainer);
            grid.SetActive(true);

            if (pack.fellaPrefabs != null)
            {
                for (int i = 0; i < pack.fellaPrefabs.Length; i++)
                {
                    GameObject prefab = pack.fellaPrefabs[i];
                    if (prefab != null)
                    {
                        Sprite s = prefab.GetComponent<SpriteRenderer>().sprite;

                        AudioClip clip = null;
                        if (pack.mergeSounds != null && i < pack.mergeSounds.Length)
                        {
                            clip = pack.mergeSounds[i];
                        }

                        CreateIcon(s, pack.packID, i, grid.transform, clip);
                    }
                }
            }
        }
    }

    void CreateIcon(Sprite sprite, string packID, int tierIndex, Transform parentGrid, AudioClip soundClip)
    {
        GameObject newSlot = Instantiate(iconTemplate, parentGrid);
        newSlot.SetActive(true);
        newSlot.name = $"{packID}_Tier_{tierIndex}";
        newSlot.transform.localScale = Vector3.zero;

        Image img = GetImageFromSlot(newSlot.transform);

        Button btn = newSlot.GetComponent<Button>();
        if (btn == null) btn = newSlot.AddComponent<Button>();

        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();

        if (img != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;

            if (CollectionManager.IsContentUnlocked(packID, tierIndex))
            {
                img.color = Color.white;
                btn.interactable = true;

                if (soundClip != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        PlayFellaSound(soundClip);
                        StartCoroutine(PunchIcon(newSlot.transform));
                    });
                }
            }
            else
            {
                img.color = Color.black;
                btn.interactable = false;
            }
        }
    }

    public void PlayFellaSound(AudioClip clip)
    {
        if (clip == null) return;

        if (PlayerPrefs.GetInt("Setting_SFX", 1) == 0) return;

        try
        {
            GameObject go = new GameObject("Gallery_Audio");
            go.transform.SetParent(this.transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.volume = 1f;

            float baseDuration = clip.length;
            float extraTime = 0.5f;

            AudioManager realManager = AudioManager.Instance;

            if (realManager == null || realManager.availableVoiceEffects == null || realManager.availableVoiceEffects.Length == 0)
            {
                AudioManager[] allManagers = Resources.FindObjectsOfTypeAll<AudioManager>();
                foreach (var m in allManagers)
                {
                    if (m.availableVoiceEffects != null && m.availableVoiceEffects.Length > 0)
                    {
                        realManager = m;
                        break;
                    }
                }
            }

            if (realManager != null)
            {
                source.volume = realManager.memeVolume > 0.05f ? realManager.memeVolume : 1f;

                string equippedVoiceID = PlayerPrefs.GetString("Shop_Equipped_voice", "voice_normal");
                if (string.IsNullOrEmpty(equippedVoiceID)) equippedVoiceID = "voice_normal";

                AudioManager.VoiceEffectProfile activeProfile = null;

                if (realManager.availableVoiceEffects != null)
                {
                    foreach (var profile in realManager.availableVoiceEffects)
                    {
                        if (profile != null && profile.itemID == equippedVoiceID)
                        {
                            activeProfile = profile;
                            break;
                        }
                    }
                }

                if (activeProfile != null)
                {
                    source.pitch = activeProfile.pitch;

                    if (activeProfile.useDynamicPitch)
                    {
                        DynamicPitchShifter shifter = go.AddComponent<DynamicPitchShifter>();
                        shifter.source = source;
                        shifter.startPitch = activeProfile.pitch;
                        shifter.endPitch = activeProfile.endPitch;
                        shifter.duration = clip.length;

                        float avgPitch = (activeProfile.pitch + activeProfile.endPitch) / 2f;
                        if (avgPitch <= 0.1f) avgPitch = 1f;
                        baseDuration = clip.length / avgPitch;
                    }
                    else
                    {
                        if (activeProfile.pitch <= 0.1f) activeProfile.pitch = 1f;
                        baseDuration = clip.length / activeProfile.pitch;
                    }

                    if (activeProfile.useEcho)
                    {
                        AudioEchoFilter echo = go.AddComponent<AudioEchoFilter>();
                        echo.delay = activeProfile.echoDelay;
                        echo.decayRatio = activeProfile.echoDecay;
                        extraTime = 2.0f;
                    }
                    if (activeProfile.useDistortion)
                    {
                        AudioDistortionFilter dist = go.AddComponent<AudioDistortionFilter>();
                        dist.distortionLevel = activeProfile.distortionLevel;
                    }
                    if (activeProfile.useHighPass)
                    {
                        AudioHighPassFilter hp = go.AddComponent<AudioHighPassFilter>();
                        hp.cutoffFrequency = activeProfile.highPassCutoff;
                    }
                    if (activeProfile.useLowPass)
                    {
                        AudioLowPassFilter lp = go.AddComponent<AudioLowPassFilter>();
                        lp.cutoffFrequency = activeProfile.lowPassCutoff;
                    }
                    if (activeProfile.useChorus)
                    {
                        AudioChorusFilter chorus = go.AddComponent<AudioChorusFilter>();
                        chorus.rate = activeProfile.chorusRate;
                        chorus.depth = activeProfile.chorusDepth;
                    }
                }
                else
                {
                    source.pitch = Random.Range(0.95f, 1.05f);
                }
            }
            else
            {
                source.pitch = Random.Range(0.95f, 1.05f);
            }

            source.Play();
            Destroy(go, baseDuration + extraTime + 0.1f);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error playing Gallery Audio: " + e.Message);
        }
    }

    IEnumerator PunchIcon(Transform target)
    {
        if (target == null) yield break;

        float t = 0f;
        while (t < 0.1f)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, 1.15f, t / 0.1f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        t = 0f;
        while (t < 0.15f)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.15f, 1f, t / 0.15f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        if (target != null) target.localScale = Vector3.one;
    }

    IEnumerator AnimatePanelOpen()
    {
        if (panelCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / fadeDuration; panelCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t); yield return null; }
            panelCanvasGroup.alpha = 1f;
        }
        StartCoroutine(AnimateIconsIn());
    }

    IEnumerator AnimateIconsIn()
    {
        foreach (Transform child in contentContainer)
        {
            if (child == null) continue;
            if (child.GetComponent<TextMeshProUGUI>() != null) StartCoroutine(PopInIcon(child));
        }

        foreach (Transform child in contentContainer)
        {
            if (child == null) continue;
            if (child.GetComponent<GridLayoutGroup>() != null)
            {
                foreach (Transform icon in child)
                {
                    if (icon == null) continue;
                    StartCoroutine(PopInIcon(icon));
                    yield return new WaitForSeconds(itemStaggerDelay);
                }
            }
        }
    }

    IEnumerator PopInIcon(Transform target)
    {
        if (target == null) yield break;

        float t = 0;
        while (t < 1f)
        {
            if (target == null) yield break;
            t += Time.deltaTime * 8f;
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f) + (Mathf.Sin(t * Mathf.PI) * 0.2f);
            if (t >= 1f) scale = 1f;
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    IEnumerator AnimatePanelClose()
    {
        if (panelCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / fadeDuration; panelCanvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t); yield return null; }
            panelCanvasGroup.alpha = 0f;
        }

        ClearList();

        galleryPanel.SetActive(false);
        ToggleMenuHUD(true);
    }

    Image GetImageFromSlot(Transform slot)
    {
        Transform childIcon = slot.Find("Icon");
        if (childIcon != null) return childIcon.GetComponent<Image>();
        return slot.GetComponent<Image>();
    }

    void ToggleMenuHUD(bool show)
    {
        if (menuElementsToHide != null)
        {
            foreach (GameObject go in menuElementsToHide)
            {
                if (go != null)
                {
                    if (show && VIPManager.Instance != null && VIPManager.IsVIP())
                    {
                        if (VIPManager.Instance.IsThisTheVIPButton(go))
                        {
                            go.SetActive(false);
                            continue;
                        }
                    }

                    if (show && PlayerPrefs.GetInt("HasClickedRateUs", 0) == 1)
                    {
                        if (go.name == "btn_RateUs")
                        {
                            go.SetActive(false);
                            continue;
                        }
                    }

                    go.SetActive(show);
                }
            }
        }
    }
}