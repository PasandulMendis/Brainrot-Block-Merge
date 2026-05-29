using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LevelUpUI : MonoBehaviour
{
    [Header("In-Game HUD Elements")]
    public TextMeshProUGUI hudLevelText;
    public TextMeshProUGUI hudGoalDescText;
    
    [Tooltip("Drag your old perfectly rounded Filled Image here!")]
    public Image hudProgressBarFill;

    [Header("The Level Up Toast Popup")]
    public RectTransform toastContainer;
    public CanvasGroup toastCanvasGroup;
    public TextMeshProUGUI toastLevelUpText; 
    public TextMeshProUGUI toastRewardText;  
    public ParticleSystem confettiParticles;

    [Header("Audio")]
    public AudioClip levelUpSound;
    private AudioSource audioSource;

    private Vector2 toastHiddenPos = new Vector2(0, 300);
    private Vector2 toastVisiblePos = new Vector2(0, -150);

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (toastContainer != null)
        {
            toastContainer.anchoredPosition = toastHiddenPos;
            toastCanvasGroup.alpha = 0f;
        }
    }

    public void UpdateLiveProgress(string goalDesc, float progressPercent, int currentLevel)
    {
        if (hudLevelText != null) hudLevelText.text = "LEVEL " + currentLevel;
        if (hudGoalDescText != null) hudGoalDescText.text = goalDesc;
        
        StopCoroutine("SmoothFillBar");
        StartCoroutine(SmoothFillBar(progressPercent));

        if (progressPercent > 0f)
        {
            StopCoroutine("FlashBar");
            StartCoroutine(FlashBar());
        }
    }

    IEnumerator SmoothFillBar(float targetFill)
    {
        if (hudProgressBarFill == null) yield break;

        float currentFill = hudProgressBarFill.fillAmount;
        
        if (targetFill < currentFill) 
        {
            hudProgressBarFill.fillAmount = 0;
            currentFill = 0;
        }

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            hudProgressBarFill.fillAmount = Mathf.Lerp(currentFill, targetFill, t);
            yield return null;
        }
        
        hudProgressBarFill.fillAmount = targetFill;
    }

    IEnumerator FlashBar()
    {
        if (hudProgressBarFill == null) yield break;

        Color original = hudProgressBarFill.color;
        Color flashColor = Color.white;
        float t = 0;

        while (t < 0.15f)
        {
            t += Time.deltaTime;
            hudProgressBarFill.color = Color.Lerp(original, flashColor, t / 0.15f);
            yield return null;
        }
        
        t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            hudProgressBarFill.color = Color.Lerp(flashColor, original, t / 0.2f);
            yield return null;
        }
        hudProgressBarFill.color = original;
    }

    public void ShowLevelUp(int oldLevel, int newLevel, int coinReward)
    {
        StopCoroutine("ToastRoutine");
        StartCoroutine(ToastRoutine(newLevel, coinReward));
    }

    IEnumerator ToastRoutine(int newLevel, int coinReward)
    {
        if (toastLevelUpText != null) toastLevelUpText.text = "LEVEL " + newLevel + "!";
        if (toastRewardText != null) toastRewardText.text = "+" + coinReward;

        if (audioSource != null && levelUpSound != null) audioSource.PlayOneShot(levelUpSound);
        if (confettiParticles != null) confettiParticles.Play();

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            float ease = 1f - Mathf.Pow(1f - t, 3f); 
            
            toastCanvasGroup.alpha = Mathf.Lerp(0f, 1f, ease);
            toastContainer.anchoredPosition = Vector2.Lerp(toastHiddenPos, toastVisiblePos, ease);
            
            float scalePop = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            toastContainer.localScale = Vector3.one * scalePop;

            yield return null;
        }

        toastContainer.localScale = Vector3.one;
        toastCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(2.0f);

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            float ease = t * t * t; 
            
            toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, ease);
            toastContainer.anchoredPosition = Vector2.Lerp(toastVisiblePos, toastHiddenPos, ease);
            yield return null;
        }
    }
}