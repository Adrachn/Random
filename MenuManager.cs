using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//ToDo: save to JSON file instead?
public class MenuManager : MonoBehaviour {
    [SerializeField]private GameObject backgroundPanel;
    [SerializeField]private GameObject fadePanel;
    [SerializeField]private GameObject inGameMenuHolder;
    [SerializeField]private GameObject optionsMenuHolder;
    [SerializeField]private GameObject loadLevelsMenuHolder;
    [SerializeField]private GameObject levelsScrollbar;
    [SerializeField]private Text levelText;
    [SerializeField]private Slider volumeSlider;
    [SerializeField]private Slider musicVolumeSlider;
    [SerializeField]private Toggle vSyncToggle;
    [SerializeField]private Toggle lightsToggle;
    [SerializeField]private Toggle fullscreenToggle;
    [SerializeField]private Dropdown resolutionDropdown;
    [SerializeField]private GameObject topButton;
    private Resolution[] resolutions;
    private bool hasReloaded;
    public float fadeTimer = 1.5f;
   

    private static MenuManager instance;
    public static MenuManager Instance {
        get {
            if (instance == null) {
                instance = FindObjectOfType(typeof(MenuManager)) as MenuManager;
            }
            return instance;
        }
    }

    private void OnEnable() {
       //Start listening to event
       SceneManager.sceneLoaded += OnLevelFinishedLoading;
       GetComponentInChildren<EventSystem>().SetSelectedGameObject(topButton); 
    }

    void Awake() {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(this.gameObject);
        if (SceneManager.GetActiveScene().buildIndex != 0) {
            DontDestroyOnLoad(this.gameObject);
        }
        if (Screen.fullScreen) {
            fullscreenToggle.isOn = true;
        }
        else
        fullscreenToggle.isOn = false;
        //all available resolutions for the screen
        resolutions = Screen.resolutions;
        //Start-level of audio. 1 if nothing saved
        volumeSlider.value = PlayerPrefs.GetFloat("savedVolume", 1.0f);
        AudioListener.volume = PlayerPrefs.GetFloat("savedVolume", 1.0f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("savedMusicVolume", 1.0f);
		GameManager.Instance.GetComponent<AudioSource>().volume = PlayerPrefs.GetFloat("savedMusicVolume", 1.0f);
        QualitySettings.vSyncCount = 0;

        if (PlayerPrefs.GetInt("savedVsync") == 1) {
            vSyncToggle.isOn = true;
        }
        else
            vSyncToggle.isOn = false;
        //volumeSlider.onValueChanged.AddListener(delegate { OnVolumeChange(); }); //ist för behöva länka slider/toggle till funktion
        
        foreach (Resolution resolution in resolutions) {
            resolutionDropdown.options.Add(new Dropdown.OptionData(resolution.ToString()));
        }
    }

    private void Update() {
      
        if (EventSystem.current.IsPointerOverGameObject()) {
            EventSystem.current.SetSelectedGameObject(null);
        }
        if (Input.GetButtonUp("Reload"))  // Doesn't fit here
            ReloadScene();
        if (Input.GetButtonUp("Escape")) {
            if (!inGameMenuHolder.activeSelf)
                InGameMenu();
            else 
				GoBack ();
        }
        ShowCurrentLevel();
    }
   
    public void OnLightsChange() {
		if(SceneManager.GetActiveScene().buildIndex != 0 && SceneManager.GetActiveScene().buildIndex != 1 && SceneManager.GetActiveScene().buildIndex != 25)
			Camera.main.GetComponent<Light2D.LightingSystem> ().enabled = lightsToggle.isOn;
    }

    public void OnMasterVolumeChange() {
        //Volume changed real time when slider moves
        AudioListener.volume = volumeSlider.value;
    }

    public void OnMusicVolumeChange() {
		GameManager.Instance.GetComponent<AudioSource>().volume = musicVolumeSlider.value;
    }

    public void OnVSyncChange() {
        if (vSyncToggle.isOn)
        QualitySettings.vSyncCount = 1; 
        else
            QualitySettings.vSyncCount = 0;
    }

    public void OnFullScreen() {
        Screen.fullScreen = fullscreenToggle.isOn;
    }

    public void OnResolutionChange() {
       Screen.SetResolution(resolutions[resolutionDropdown.value].width, resolutions[resolutionDropdown.value].height, Screen.fullScreen);
    }
 
    public void InGameMenu() {
        Time.timeScale = 0;
        if (backgroundPanel != null) {
        backgroundPanel.SetActive(true);
        }
        inGameMenuHolder.SetActive(true);
        optionsMenuHolder.SetActive(false);
        loadLevelsMenuHolder.SetActive(false);
        GetComponentInChildren<EventSystem>().SetSelectedGameObject(topButton); 
    }

    public void StartMenu() {
        Time.timeScale = 1;
        inGameMenuHolder.SetActive(true);
        optionsMenuHolder.SetActive(false);
        loadLevelsMenuHolder.SetActive(false);
        GetComponentInChildren<EventSystem>().SetSelectedGameObject(topButton); 
    }

    public void OptionsMenu() {
        inGameMenuHolder.SetActive(false);
        optionsMenuHolder.SetActive(true);
       resolutionDropdown.Select();                                               
    }

    public void LoadLevelsMenu() {
        loadLevelsMenuHolder.SetActive(true);
        inGameMenuHolder.SetActive(false);
        GetComponentInChildren<EventSystem>().SetSelectedGameObject(levelsScrollbar);
    }

    public void QuitGame() {
      //Enable Are you sure you want to quit-window?
        Application.Quit();
    }

    public void GoBack() { //remove
        if (backgroundPanel != null) {
            backgroundPanel.SetActive(false);
            inGameMenuHolder.SetActive(false);
        }
        if (SceneManager.GetActiveScene().buildIndex != 0)
            optionsMenuHolder.SetActive(false);
        if (SceneManager.GetActiveScene().buildIndex == 1)
            Time.timeScale = 1;
    }

    public void GoBackFromOptions() {
        //Get previously saved values
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("savedVsync");
        GameManager.Instance.GetComponent<AudioSource>().volume = PlayerPrefs.GetFloat("savedMusicVolume");
        AudioListener.volume = PlayerPrefs.GetFloat("savedVolume");
        //resolution
        volumeSlider.value = PlayerPrefs.GetFloat("savedVolume");
        musicVolumeSlider.value = PlayerPrefs.GetFloat("savedMusicVolume");
        if (PlayerPrefs.GetInt("savedVsync") == 1) {
            vSyncToggle.isOn = true;
        }
        else vSyncToggle.isOn = false;
        if (Screen.fullScreen) {
            fullscreenToggle.isOn = true;
        }
        else
            fullscreenToggle.isOn = false;

        if (backgroundPanel != null) { 
        backgroundPanel.SetActive(false);
            Time.timeScale = 1;
        }   
        optionsMenuHolder.SetActive(false);
        inGameMenuHolder.SetActive(true);
    }

   public void Apply() {
        //Save and set Volume
        PlayerPrefs.SetFloat("savedVolume", volumeSlider.value);
        //Save and set Music volume
        PlayerPrefs.SetFloat("savedMusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetInt("savedResolution", resolutionDropdown.value);

       if(vSyncToggle.isOn == true) {
            PlayerPrefs.SetInt("savedVsync", 1);
       }
       else
            PlayerPrefs.SetInt("savedVsync", 0);

       if (fullscreenToggle.isOn == true) {
            PlayerPrefs.SetInt("savedFullscreen", 1);
       }
       else
            PlayerPrefs.SetInt("savedFullscreen", 0);
   }

    public void ReloadScene() {
        inGameMenuHolder.SetActive(false);
        if (backgroundPanel.activeSelf == true) {
            backgroundPanel.SetActive(false);
        }
      
		GameManager.Instance.inSpiritWorld = false;
        if (!hasReloaded) { 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            hasReloaded = true;
        }
        Time.timeScale = 1;
    }

    public void LoadNextScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        GameManager.Instance.ClearedLevel(SceneManager.GetActiveScene().buildIndex-1);
        GameManager.Instance.inSpiritWorld = false;
        Time.timeScale = 1;
    }

    public void ToMainMenu() {
        Time.timeScale = 1;
        inGameMenuHolder.SetActive(false);
        backgroundPanel.SetActive(false);
        SceneManager.LoadScene(0);
        Destroy(gameObject);
    }

    public void LoadScene(int level) {
        inGameMenuHolder.SetActive(false);
        loadLevelsMenuHolder.SetActive(false);
        if (backgroundPanel != null) {
            backgroundPanel.SetActive(false);
        }
        GameManager.Instance.inSpiritWorld = false;
        SceneManager.LoadScene(level);
        Time.timeScale = 1;
    }

    //Shows current level
    void ShowCurrentLevel() {
        if(levelText != null) {
            if(SceneManager.GetActiveScene().buildIndex < 12) { 
             levelText.text = "Level: " + (SceneManager.GetActiveScene().buildIndex-1);
            }
            if (SceneManager.GetActiveScene().buildIndex == 12)
                levelText.text = "The Jaguar";
            if (SceneManager.GetActiveScene().buildIndex > 12) {
            levelText.text = "Level: " + (SceneManager.GetActiveScene().buildIndex -3);
            }
            if (SceneManager.GetActiveScene().buildIndex == 23) {
                levelText.text = "Donaldo";
            }
       }
    }
   
    public IEnumerator Fade(float fadeTimer) {
		AudioManager.Instans.FinishLevel ();
        for (float i = 0; i < 1; i += 0.1f) {
            fadePanel.GetComponent<CanvasGroup>().alpha += i * Time.deltaTime;
        }
        yield return new WaitForSeconds(fadeTimer);
        LoadNextScene();
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode) {
        hasReloaded = false;
        while (fadePanel.GetComponent<CanvasGroup>().alpha > 0) {
            fadePanel.GetComponent<CanvasGroup>().alpha -= Time.deltaTime*0.1f;
        }
		OnLightsChange ();
    }

    private void OnDisable() {
        //Stop listening to event
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }
}

