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

    // Asegura que al cargar la escena siempre se apliquen los ajustes guardados,
    // aunque la UI esté desactivada o MusicManager aún no exista.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedAudioSettingsOnLoad()
    {
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        AudioListener.volume = masterVolume;

        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);

        // Si el MusicManager ya existe, aplicamos inmediatamente y nos aseguramos de que la música arranque.
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

        // Si no existe aún, crear un applier temporal que reintente hasta que MusicManager exista
        GameObject applierGo = new GameObject("AudioSettingsApplier");
        DontDestroyOnLoad(applierGo);
        var applier = applierGo.AddComponent<AudioSettingsApplier>();
        applier.Initialize(musicVolume, sfxVolume);
    }

    // Componente temporal que intenta aplicar los volúmenes hasta que MusicManager esté disponible.
    private class AudioSettingsApplier : MonoBehaviour
    {
        private float musicVolume;
        private float sfxVolume;
        private float startTime;
        private bool applied = false;
        private const float timeout = 5f; // segundos máximo para reintentar

        public void Initialize(float musicVol, float sfxVol)
        {
            musicVolume = musicVol;
            sfxVolume = sfxVol;
            startTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (applied) return;

            // Intentar aplicar si MusicManager ya existe
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
                    // Silenciar errores para no romper el flujo; volveremos a intentar.
                }

                applied = true;
                Destroy(gameObject);
                return;
            }

            // Timeout por si nunca aparece MusicManager: destruir el applier tras tiempo para no acumular objetos
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
        // Aplicar al MusicManager para sincronizar estado si ya existe
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(musicVolume);

            // Asegurar que la música suene si no está sonando
            if (!MusicManager.Instance.IsPlaying())
            {
                MusicManager.Instance.PlayMusic();
            }
        }
        UpdateMusicText(musicVolume);

        // Cargar volumen de SFX
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
        }
        // Aplicar al MusicManager para sincronizar SFX si ya existe
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetSFXVolume(sfxVolume);
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

        AudioSettingsChanged?.Invoke();
    }

    private void OnMusicVolumeChanged(float value)
    {
        // Solo aplicar a MusicManager si existe (en la escena de juego)
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(value);

            // Si el usuario sube el volumen manualmente desde 0 y la pista está parada,
            // asegurar que empiece a sonar.
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
        // Solo aplicar a MusicManager si existe (en la escena de juego)
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
