using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsMenu : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

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

        // Cargar volumen de música
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
        }

        // Cargar volumen de SFX
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        // AudioListener.volume controla el volumen global de todo el juego
        AudioListener.volume = value;
        
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
        PlayerPrefs.Save();
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
    }
}
