using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class SettingsManager : MonoBehaviour
{
    [Header("Dependencies (Will auto-find if left empty!)")]
    public AudioManager audioManager;
    public HapticManager hapticManager;
    public InputHandler inputHandler;

    public enum ExpandDirection { Left, Right, Up, Down }
    [Header("Menu Configuration")]
    public ExpandDirection expandDirection = ExpandDirection.Left;

    [Header("Main Gear Button")]
    public RectTransform gearBtnTransform;

    [Header("The Sub-Buttons")]
    public RectTransform musicBtn;
    public RectTransform sfxBtn;
    public RectTransform hapticBtn;
    public RectTransform batteryBtn;

    [Header("Button Images")]
    public Image musicImg;
    public Image sfxImg;
    public Image hapticImg;
    public Image batteryImg;

    [Header("Sprite Icons")]
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;
    public Sprite sfxOnSprite;
    public Sprite sfxOffSprite;
    public Sprite hapticOnSprite;
    public Sprite hapticOffSprite;
    public Sprite batteryOnSprite;
    public Sprite batteryOffSprite;

    [Header("Animation Settings")]
    public float spacing = 150f;
    public float moveSpeed = 15f;
    public float rotateSpeed = 10f;
    public float gearRotateAngle = -180f;

    private bool isExpanded = false;
    private bool isMusicOn;
    private bool isSFXOn;
    private bool isHapticsOn;
    private bool isBatterySaverOn;

    private Vector2 gearOriginAnchored;
    private Vector2 musicTarget;
    private Vector2 sfxTarget;
    private Vector2 hapticTarget;
    private Vector2 batteryTarget;
    private Quaternion gearTargetRot;

    void Start()
    {
        if (audioManager == null) audioManager = FindFirstObjectByType<AudioManager>();
        if (hapticManager == null) hapticManager = FindFirstObjectByType<HapticManager>();
        if (inputHandler == null) inputHandler = FindFirstObjectByType<InputHandler>();

        isMusicOn = PlayerPrefs.GetInt("Setting_Music", 1) == 1;
        isSFXOn = PlayerPrefs.GetInt("Setting_SFX", 1) == 1;
        isHapticsOn = PlayerPrefs.GetInt("Setting_Haptics", 1) == 1;
        isBatterySaverOn = PlayerPrefs.GetInt("Setting_Battery", 0) == 1;

        DisableButtonTint(musicBtn);
        DisableButtonTint(sfxBtn);
        DisableButtonTint(hapticBtn);
        if (batteryBtn != null) DisableButtonTint(batteryBtn);

        ApplySettings();
        UpdateVisuals();

        if (gearBtnTransform != null)
        {
            gearOriginAnchored = gearBtnTransform.anchoredPosition;

            musicTarget = gearOriginAnchored;
            sfxTarget = gearOriginAnchored;
            hapticTarget = gearOriginAnchored;
            batteryTarget = gearOriginAnchored;
            gearTargetRot = gearBtnTransform.rotation;

            if (musicBtn != null) musicBtn.anchoredPosition = gearOriginAnchored;
            if (sfxBtn != null) sfxBtn.anchoredPosition = gearOriginAnchored;
            if (hapticBtn != null) hapticBtn.anchoredPosition = gearOriginAnchored;
            if (batteryBtn != null) batteryBtn.anchoredPosition = gearOriginAnchored;

            if (musicBtn != null) musicBtn.localScale = Vector3.zero;
            if (sfxBtn != null) sfxBtn.localScale = Vector3.zero;
            if (hapticBtn != null) hapticBtn.localScale = Vector3.zero;
            if (batteryBtn != null) batteryBtn.localScale = Vector3.zero;
        }
    }

    void Update()
    {
        if (isExpanded && inputHandler != null)
        {
            if (inputHandler.IsTouching())
            {
                bool isTouchingUI = false;
                if (EventSystem.current != null)
                {
                    if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    {
                        isTouchingUI = true;
                    }
                    else if (EventSystem.current.IsPointerOverGameObject())
                    {
                        isTouchingUI = true;
                    }
                }

                if (!isTouchingUI)
                {
                    SetMenuState(false);
                }
            }
        }

        if (gearBtnTransform != null)
        {
            float dt = Time.deltaTime;

            if (musicBtn != null) musicBtn.anchoredPosition = Vector2.Lerp(musicBtn.anchoredPosition, musicTarget, dt * moveSpeed);
            if (sfxBtn != null) sfxBtn.anchoredPosition = Vector2.Lerp(sfxBtn.anchoredPosition, sfxTarget, dt * moveSpeed);
            if (hapticBtn != null) hapticBtn.anchoredPosition = Vector2.Lerp(hapticBtn.anchoredPosition, hapticTarget, dt * moveSpeed);
            if (batteryBtn != null) batteryBtn.anchoredPosition = Vector2.Lerp(batteryBtn.anchoredPosition, batteryTarget, dt * moveSpeed);

            gearBtnTransform.rotation = Quaternion.Lerp(gearBtnTransform.rotation, gearTargetRot, dt * rotateSpeed);

            float targetScale = isExpanded ? 1f : 0f;
            if (!isExpanded && musicBtn != null && Vector2.Distance(musicBtn.anchoredPosition, gearOriginAnchored) < 10f) targetScale = 0f;
            else if (isExpanded) targetScale = 1f;

            if (musicBtn != null) musicBtn.localScale = Vector3.Lerp(musicBtn.localScale, Vector3.one * targetScale, dt * moveSpeed);
            if (sfxBtn != null) sfxBtn.localScale = Vector3.Lerp(sfxBtn.localScale, Vector3.one * targetScale, dt * moveSpeed);
            if (hapticBtn != null) hapticBtn.localScale = Vector3.Lerp(hapticBtn.localScale, Vector3.one * targetScale, dt * moveSpeed);
            if (batteryBtn != null) batteryBtn.localScale = Vector3.Lerp(batteryBtn.localScale, Vector3.one * targetScale, dt * moveSpeed);
        }
    }

    public void ToggleMenu()
    {
        SetMenuState(!isExpanded);
    }

    void SetMenuState(bool open)
    {
        isExpanded = open;

        Vector2 dir = Vector2.left;
        switch (expandDirection)
        {
            case ExpandDirection.Left: dir = Vector2.left; break;
            case ExpandDirection.Right: dir = Vector2.right; break;
            case ExpandDirection.Up: dir = Vector2.up; break;
            case ExpandDirection.Down: dir = Vector2.down; break;
        }

        if (isExpanded)
        {
            int index = 1;
            if (musicBtn != null) { musicTarget = gearOriginAnchored + (dir * (spacing * index)); index++; }
            if (sfxBtn != null) { sfxTarget = gearOriginAnchored + (dir * (spacing * index)); index++; }
            if (hapticBtn != null) { hapticTarget = gearOriginAnchored + (dir * (spacing * index)); index++; }
            if (batteryBtn != null) { batteryTarget = gearOriginAnchored + (dir * (spacing * index)); index++; }

            gearTargetRot = Quaternion.Euler(0, 0, gearRotateAngle);
        }
        else
        {
            musicTarget = gearOriginAnchored;
            sfxTarget = gearOriginAnchored;
            hapticTarget = gearOriginAnchored;
            batteryTarget = gearOriginAnchored;

            gearTargetRot = Quaternion.Euler(0, 0, 0);
        }
    }

    void DisableButtonTint(RectTransform btnTransform)
    {
        if (btnTransform == null) return;
        Button btn = btnTransform.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
        }
    }

    IEnumerator PunchScale(RectTransform target)
    {
        if (target == null) yield break;
        float t = 0f;
        while (t < 0.1f) { t += Time.deltaTime; target.localScale = Vector3.one * Mathf.Lerp(1f, 1.2f, t / 0.1f); yield return null; }
        t = 0f;
        while (t < 0.1f) { t += Time.deltaTime; target.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t / 0.1f); yield return null; }
        target.localScale = Vector3.one;
    }

    public void ToggleMusic() { isMusicOn = !isMusicOn; if (musicBtn != null) StartCoroutine(PunchScale(musicBtn)); SaveAndApply(); }
    public void ToggleSFX() { isSFXOn = !isSFXOn; if (sfxBtn != null) StartCoroutine(PunchScale(sfxBtn)); SaveAndApply(); }
    public void ToggleHaptics() { isHapticsOn = !isHapticsOn; if (hapticBtn != null) StartCoroutine(PunchScale(hapticBtn)); SaveAndApply(); }
    public void ToggleBatterySaver() { isBatterySaverOn = !isBatterySaverOn; if (batteryBtn != null) StartCoroutine(PunchScale(batteryBtn)); SaveAndApply(); }

    private void SaveAndApply()
    {
        PlayerPrefs.SetInt("Setting_Music", isMusicOn ? 1 : 0);
        PlayerPrefs.SetInt("Setting_SFX", isSFXOn ? 1 : 0);
        PlayerPrefs.SetInt("Setting_Haptics", isHapticsOn ? 1 : 0);
        PlayerPrefs.SetInt("Setting_Battery", isBatterySaverOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplySettings();
        UpdateVisuals();
    }

    private void ApplySettings()
    {
        if (audioManager) audioManager.SetMusicEnabled(isMusicOn);
        if (audioManager) audioManager.SetSFXEnabled(isSFXOn);
        if (hapticManager) hapticManager.SetHapticsEnabled(isHapticsOn);

        Application.targetFrameRate = isBatterySaverOn ? 60 : 120;
    }

    private void UpdateVisuals()
    {
        if (musicImg) musicImg.sprite = isMusicOn ? musicOnSprite : musicOffSprite;
        if (sfxImg) sfxImg.sprite = isSFXOn ? sfxOnSprite : sfxOffSprite;
        if (hapticImg) hapticImg.sprite = isHapticsOn ? hapticOnSprite : hapticOffSprite;
        if (batteryImg) batteryImg.sprite = isBatterySaverOn ? batteryOnSprite : batteryOffSprite;

        if (musicImg) musicImg.color = Color.white;
        if (sfxImg) sfxImg.color = Color.white;
        if (hapticImg) hapticImg.color = Color.white;
        if (batteryImg) batteryImg.color = Color.white;
    }
}