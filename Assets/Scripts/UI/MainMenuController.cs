using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach to the MainMenu scene's controller GameObject (built by
// MainMenuSetup.CreateMainMenuScene). Singleplayer writes the chosen
// AIDifficulty to GameSetup and loads PlayScene; Multiplayer is a disabled
// placeholder button since multiplayer isn't implemented yet.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Button       singleplayerButton;
    [SerializeField] private Button       multiplayerButton;
    [SerializeField] private string       playSceneName = "PlayScene";

    private void Awake()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string> { "Easy", "Medium", "Hard" });
            difficultyDropdown.value = (int)AIDifficulty.Medium;
            difficultyDropdown.RefreshShownValue();
        }

        singleplayerButton?.onClick.AddListener(OnSingleplayerClicked);

        // Kept visible but non-interactive (rather than hidden) so it reads as
        // a planned feature, not a missing button.
        if (multiplayerButton != null)
            multiplayerButton.interactable = false;
    }

    private void OnSingleplayerClicked()
    {
        GameSetup.Mode       = GameMode.Singleplayer;
        GameSetup.Difficulty = difficultyDropdown != null ? (AIDifficulty)difficultyDropdown.value : AIDifficulty.Medium;
        SceneManager.LoadScene(playSceneName);
    }
}
