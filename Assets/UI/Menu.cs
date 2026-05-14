using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _mainPage;
    private VisualElement _settingsPage;

    private Button _playBtn;
    private Button _settingsBtn;
    private Button _quitBtn;
    private Button _backBtn;
    private Slider _volumeSlider;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null) return;

        var root = _doc.rootVisualElement;

        _mainPage = root.Q<VisualElement>("main-page");
        _settingsPage = root.Q<VisualElement>("settings-page");

        // Main Page
        _playBtn = root.Q<Button>("play-btn");
        _settingsBtn = root.Q<Button>("settings-btn");
        _quitBtn = root.Q<Button>("quit-btn");

        // Settings Page
        _backBtn = root.Q<Button>("back-btn");
        _volumeSlider = root.Q<Slider>("volume-slider");

        if (_playBtn != null)
        {
            _playBtn.text = "PLAY";
            _playBtn.clicked += OnPlay;
        }

        if (_settingsBtn != null) _settingsBtn.clicked += ShowSettings;
        if (_quitBtn != null) _quitBtn.clicked += OnQuit;
        if (_backBtn != null) _backBtn.clicked += ShowMain;

        // Volume slider — maps 0-100 UI range to 0.0-1.0 AudioListener.volume
        VolumeSettings.Load();
        if (_volumeSlider != null)
        {
            _volumeSlider.value = VolumeSettings.GetSavedVolume() * 100f;
            _volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
        }

        ShowMain();
    }

    private void OnDisable()
    {
        if (_playBtn != null) _playBtn.clicked -= OnPlay;
        if (_settingsBtn != null) _settingsBtn.clicked -= ShowSettings;
        if (_quitBtn != null) _quitBtn.clicked -= OnQuit;
        if (_backBtn != null) _backBtn.clicked -= ShowMain;

        if (_volumeSlider != null)
            _volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        VolumeSettings.Volume = evt.newValue / 100f;
    }

    private void OnPlay()
    {
        PlayClick();
        SceneManager.LoadScene("Level 1");
    }

    private void ShowSettings()
    {
        PlayClick();
        if (_mainPage != null) _mainPage.AddToClassList("hidden");
        if (_settingsPage != null) _settingsPage.RemoveFromClassList("hidden");
    }

    private void ShowMain()
    {
        PlayClick();
        if (_mainPage != null) _mainPage.RemoveFromClassList("hidden");
        if (_settingsPage != null) _settingsPage.AddToClassList("hidden");
    }

    private void OnQuit()
    {
        PlayClick();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private static void PlayClick()
    {
        if (MainMenuController.Instance != null)
            MainMenuController.Instance.PlayClickSound();
    }
}
