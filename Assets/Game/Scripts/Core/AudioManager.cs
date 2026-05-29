using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class VoiceEffectProfile
    {
        public string itemID;
        public string note = "Effect Name";

        [Header("Pitch & Speed")]
        [Range(0.1f, 3f)] public float pitch = 1f;

        [Header("Dynamic Pitch (Low Battery / Shrinking)")]
        public bool useDynamicPitch;
        [Tooltip("The pitch it will smoothly transition to by the end of the sound")]
        [Range(0.1f, 3f)] public float endPitch = 0.5f;

        [Header("Filters")]
        public bool useEcho;
        [Range(10, 5000)] public float echoDelay = 500f;
        [Range(0f, 1f)] public float echoDecay = 0.5f;

        public bool useDistortion;
        [Range(0f, 1f)] public float distortionLevel = 0.5f;

        public bool useHighPass;
        [Tooltip("Cuts out deep bass (Telephone/Radio effect)")]
        [Range(10f, 22000f)] public float highPassCutoff = 1000f;

        public bool useLowPass;
        [Tooltip("Cuts out sharp treble (Muffled/Mask effect)")]
        [Range(10f, 22000f)] public float lowPassCutoff = 4000f;

        public bool useChorus;
        [Tooltip("Gives a metallic, vibrating robot sound")]
        [Range(0f, 20f)] public float chorusRate = 15f;
        [Range(0f, 1f)] public float chorusDepth = 0.5f;
    }

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.3f;

    [Header("Sound Effect Volumes")]
    [Range(0f, 1f)] public float dropVolume = 1.0f;
    [Range(0f, 1f)] public float popVolume = 1.0f;
    [Range(0f, 1f)] public float buttonVolume = 1.0f;
    [Range(0f, 1f)] public float memeVolume = 1.0f;
    [Range(0f, 1f)] public float gameOverVolume = 1.0f;
    [Range(0f, 1f)] public float coinVolume = 1.0f;

    [Header("Clips")]
    public AudioClip dropClip;
    public AudioClip gameOverClip;
    public AudioClip buttonClickClip;
    public AudioClip coinClip;
    public AudioClip menuOpenClip;
    public AudioClip[] genericPopClips;
    public AudioClip[] mergeClips;

    [Header("Settings")]
    public float smartGroupWindow = 0.5f;
    public float maxChainWindow = 1.5f;

    [Header("Shop Voice Effects")]
    [Tooltip("Add all your unlockable voice effects here!")]
    public VoiceEffectProfile[] availableVoiceEffects;

    private int highestPendingTier = -1;
    private float groupTimer = 0f;
    private float chainDuration = 0f;
    private bool isMergePending = false;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private bool isMusicOn = true;
    private bool isSFXOn = true;

    public static AudioManager Instance;

    void Awake()
    {
        Instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        LoadVolumeSettings();
    }

    void Start() { PlayMusic(); }

    void LoadVolumeSettings()
    {
        isMusicOn = PlayerPrefs.GetInt("Setting_Music", 1) == 1;
        isSFXOn = PlayerPrefs.GetInt("Setting_SFX", 1) == 1;
    }

    void Update()
    {
        if (isMergePending)
        {
            groupTimer -= Time.deltaTime;
            chainDuration += Time.deltaTime;

            if (groupTimer <= 0f || chainDuration >= maxChainWindow)
            {
                PlaySpecificSound(highestPendingTier);
                isMergePending = false;
                highestPendingTier = -1;
                chainDuration = 0f;
            }
        }
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        isMusicOn = isEnabled;
        if (musicSource != null) musicSource.mute = !isMusicOn;
    }

    public void SetSFXEnabled(bool isEnabled) { isSFXOn = isEnabled; }

    public void PlayMusic()
    {
        if (musicSource != null && musicSource.isPlaying) return;
        if (backgroundMusic != null)
        {
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.playOnAwake = true;
            musicSource.mute = !isMusicOn;
            musicSource.Play();
        }
    }

    public void PlayDrop() { if (isSFXOn && dropClip != null) sfxSource.PlayOneShot(dropClip, dropVolume); }
    public void PlayButtonClick() { if (isSFXOn && buttonClickClip != null) sfxSource.PlayOneShot(buttonClickClip, buttonVolume); }
    public void PlayMenuSound() { if (isSFXOn && menuOpenClip != null) sfxSource.PlayOneShot(menuOpenClip, buttonVolume); }
    public void PlayCoinSound() { if (isSFXOn && coinClip != null) sfxSource.PlayOneShot(coinClip, coinVolume); }

    public void PlayGenericPop()
    {
        if (!isSFXOn || genericPopClips == null || genericPopClips.Length == 0) return;
        AudioClip clip = genericPopClips[Random.Range(0, genericPopClips.Length)];
        if (clip != null)
        {
            GameObject go = new GameObject("Audio_Pop");
            go.transform.position = Camera.main.transform.position;
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = popVolume;
            source.pitch = Random.Range(0.9f, 1.1f);
            source.spatialBlend = 0f;
            source.Play();
            Destroy(go, clip.length);
        }
    }

    public void PlayMergeGrouped(int tierIndex)
    {
        if (!isSFXOn) return;
        if (tierIndex > highestPendingTier) highestPendingTier = tierIndex;
        groupTimer = smartGroupWindow;
        isMergePending = true;
    }

    private void PlaySpecificSound(int tierIndex)
    {
        if (!isSFXOn) return;
        if (mergeClips != null && tierIndex < mergeClips.Length)
        {
            AudioClip clip = mergeClips[tierIndex];
            if (clip != null)
            {
                GameObject go = new GameObject("Audio_Meme_" + tierIndex);
                go.transform.position = Camera.main.transform.position;
                AudioSource source = go.AddComponent<AudioSource>();
                source.clip = clip;
                source.volume = memeVolume;

                string equippedVoiceID = PlayerPrefs.GetString("Shop_Equipped_voice", "voice_normal");
                if (equippedVoiceID == "") equippedVoiceID = "voice_normal";

                VoiceEffectProfile activeProfile = null;
                foreach (var profile in availableVoiceEffects)
                {
                    if (profile.itemID == equippedVoiceID) { activeProfile = profile; break; }
                }

                float baseDuration = clip.length;
                float extraTime = 0.5f;

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
                        baseDuration = clip.length / avgPitch;
                    }
                    else
                    {
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

                source.spatialBlend = 0f;
                source.Play();

                Destroy(go, baseDuration + extraTime);
            }
        }
    }

    public void PlayGameOver()
    {
        if (musicSource != null) musicSource.Stop();
        if (!isSFXOn || gameOverClip == null) return;
        sfxSource.PlayOneShot(gameOverClip, gameOverVolume);
    }
}


public class DynamicPitchShifter : MonoBehaviour
{
    public AudioSource source;
    public float startPitch;
    public float endPitch;
    public float duration;

    private float timer = 0f;

    void Update()
    {
        if (source == null) return;

        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / duration);
        source.pitch = Mathf.Lerp(startPitch, endPitch, progress);
    }
}