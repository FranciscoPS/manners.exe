using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsMenu : MonoBehaviour
{
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

    private void OnEnable()
    {
        LoadSettings();
        
        // Agregar listeners solo una vez
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
        // Remover listeners
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
        // Cargar volumen maestro
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(masterVolume);
        }
        AudioListener.volume = masterVolume;
        UpdateMasterText(masterVolume);

        // Cargar volumen de música
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
        }
        UpdateMusicText(musicVolume);

        // Cargar volumen de SFX
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
        }
        UpdateSFXText(sfxVolume);
    }

    private void OnMasterVolumeChanged(float value)
    {
        // AudioListener.volume controla el volumen global de todo el juego
        AudioListener.volume = value;
        
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
        PlayerPrefs.Save();

        UpdateMasterText(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        // Solo aplicar a MusicManager si existe (en la escena de juego)
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(value);
        }
        
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
        PlayerPrefs.Save();

        UpdateMusicText(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        // Solo aplicar a MusicManager si existe (en la escena de juego)
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetSFXVolume(value);
        }
        
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();

        UpdateSFXText(value);
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
