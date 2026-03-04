using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AudioSettingsMenu : MonoBehaviour
{
    public static event Action AudioSettingsChanged;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Value Displays (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private TextMeshProUGUI musicValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("Titles (shown before percentage)")]
    [SerializeField] private string masterTitle = "Master control";
    [SerializeField] private string musicTitle = "Music control";
    [SerializeField] private string sfxTitle = "SFX control";

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private bool listenersAdded = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedAudioSettingsOnLoad()
    {
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        AudioListener.volume = masterVolume;

        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(musicVolume);
            MusicManager.Instance.SetSFXVolume(sfxVolume);

            if (!MusicManager.Instance.IsPlaying())
            {
                MusicManager.Instance.PlayMusic();
            }
            return;
        }

        GameObject applierGo = new GameObject("AudioSettingsApplier");
        DontDestroyOnLoad(applierGo);
        var applier = applierGo.AddComponent<AudioSettingsApplier>();
        applier.Initialize(musicVolume, sfxVolume);
    }

    private class AudioSettingsApplier : MonoBehaviour
    {
        private float musicVolume;
        private float sfxVolume;
        private float startTime;
        private bool applied = false;
        private const float timeout = 5f;

        public void Initialize(float musicVol, float sfxVol)
        {
            musicVolume = musicVol;
            sfxVolume = sfxVol;
            startTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (applied) return;

            if (MusicManager.Instance != null)
            {
                try
                {
                    MusicManager.Instance.SetVolume(musicVolume);
                    MusicManager.Instance.SetSFXVolume(sfxVolume);

                    if (!MusicManager.Instance.IsPlaying())
                    {
                        MusicManager.Instance.PlayMusic();
                    }
                }
                catch (System.Exception)
                {

                }

                applied = true;
                Destroy(gameObject);
                return;
            }

            if (Time.realtimeSinceStartup - startTime > timeout)
            {
                applied = true;
                Destroy(gameObject);
            }
        }
    }

    private void OnEnable()
    {
        LoadSettings();

        if (!listenersAdded)
        {
            if (masterSlider != null)
            {
                masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            listenersAdded = true;
        }
    }

    private void OnDestroy()
    {

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }
    }

    private void LoadSettings()
    {

        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(masterVolume);
        }
        AudioListener.volume = masterVolume;
        UpdateMasterText(masterVolume);

        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(musicVolume);

            if (!MusicManager.Instance.IsPlaying())
            {
                MusicManager.Instance.PlayMusic();
            }
        }
        UpdateMusicText(musicVolume);

        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetSFXVolume(sfxVolume);
        }
        UpdateSFXText(sfxVolume);
    }

    private void OnMasterVolumeChanged(float value)
    {

        AudioListener.volume = value;

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
        PlayerPrefs.Save();

        UpdateMasterText(value);

        AudioSettingsChanged?.Invoke();
    }

    private void OnMusicVolumeChanged(float value)
    {

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(value);

            if (!MusicManager.Instance.IsPlaying())
            {
                MusicManager.Instance.PlayMusic();
            }
        }

        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
        PlayerPrefs.Save();

        UpdateMusicText(value);

        AudioSettingsChanged?.Invoke();
    }

    private void OnSFXVolumeChanged(float value)
    {

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetSFXVolume(value);
        }

        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();

        UpdateSFXText(value);

        AudioSettingsChanged?.Invoke();
    }

    private void UpdateMasterText(float value)
    {
        if (masterValueText != null)
        {
            masterValueText.text = masterTitle + ": " + Mathf.RoundToInt(value * 100f).ToString() + "%";
        }
    }

    private void UpdateMusicText(float value)
    {
        if (musicValueText != null)
        {
            musicValueText.text = musicTitle + ": " + Mathf.RoundToInt(value * 100f).ToString() + "%";
        }
    }

    private void UpdateSFXText(float value)
    {
        if (sfxValueText != null)
        {
            sfxValueText.text = sfxTitle + ": " + Mathf.RoundToInt(value * 100f).ToString() + "%";
        }
    }
}
