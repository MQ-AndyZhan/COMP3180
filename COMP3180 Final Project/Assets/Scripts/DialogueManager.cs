using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;

public class InkDialogueManager : MonoBehaviour
{
    [Header("Ink Configuration")]
    [SerializeField] private TextAsset inkJSONAsset;
    [SerializeField] private string sceneToLoadAtEnd;
    private Story story;
    private bool storyHasEnded;

    [Header("UI References")]
    [SerializeField] private GenerateDialogues storyText; 
    [SerializeField] private Transform UIDialogueChoiceContainer; // Container for the choice buttons
    [SerializeField] private Button UIDialogueChoicePrefab; // Prefab for the choice buttons

    [SerializeField] private Button continueBtn;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private GameObject pausePanel;
    private TMP_Text continueButtonText;
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = new PlayerInput();
        playerInput.PauseMenu.Newaction.performed += OnPausePressed;
        playerInput.PauseMenu.Enable();
    }

    void OnDestroy()
    {
        playerInput.PauseMenu.Newaction.performed -= OnPausePressed;
        playerInput.PauseMenu.Disable();
        playerInput.Dispose();
    }

    private void OnPausePressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Debug.Log("Pause button pressed");
        if (pausePanel.activeSelf)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            pausePanel.SetActive(true);
        }
    }

    void Start()
    {
        continueButtonText = continueBtn.GetComponentInChildren<TMP_Text>();
        SetContinueButtonText("Continue");

        // 1. Initialize the story
        story = new Story(inkJSONAsset.text);
        
        // 2. Start displaying the story
        RefreshView();
        continueBtn.gameObject.SetActive(false); // Hide the continue button initially
        pausePanel.SetActive(false); // Ensure the pause panel is hidden at the start
    }

    // This handles advancing the text and creating buttons
    public void RefreshView()
    {
        if (storyHasEnded)
        {
            LoadSceneAtEnd();
            return;
        }

        // Clear out any old choice buttons from the previous turn
        RemoveExistingButtons();

        // Read and display one line so the player controls when the story advances.
        string text = story.canContinue ? story.Continue() : string.Empty;
        UpdateSpeakerText();

        if (story.currentChoices.Count > 0)
        {
            continueBtn.gameObject.SetActive(false); // Hide the continue button when choices are available
            storyText.TypeSentence(text, DisplayChoices); // Show choices after the typewriter effect finishes
        }
        else
        {
            if (story.canContinue)
            {
                SetContinueButtonText("skip");
                storyText.TypeSentence(text, () => SetContinueButtonText("Continue")); // Start typing the text with the typewriter effect
                continueBtn.gameObject.SetActive(true);
            }
            else
            {
                storyHasEnded = true;
                SetContinueButtonText("skip");
                storyText.TypeSentence(text, () => SetContinueButtonText("Continue"));
                continueBtn.gameObject.SetActive(true);
            }
        }
    }

    private void SetContinueButtonText(string text)
    {
        if (continueButtonText != null)
        {
            continueButtonText.text = text;
        }
    }

    private void LoadSceneAtEnd()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoadAtEnd))
        {
            Debug.LogError("No scene has been assigned for the end of the Ink story.");
            return;
        }

        SceneManager.LoadScene(sceneToLoadAtEnd);
    }

    void UpdateSpeakerText()
    {
        if (speakerText == null)
        {
            return;
        }

        speakerText.text = string.Empty;

        foreach (string tag in story.currentTags)
        {
            const string speakerPrefix = "Speaker:";
            string trimmedTag = tag.Trim();

            if (trimmedTag.StartsWith("#Speaker:"))
            {
                speakerText.text = trimmedTag.Substring("#Speaker:".Length).Trim() + ":";
                return;
            }

            if (trimmedTag.StartsWith(speakerPrefix))
            {
                speakerText.text = trimmedTag.Substring(speakerPrefix.Length).Trim() + ":";
                return;
            }
        }
    }

    void DisplayChoices()
    {
        int yPos = 300;
        // Loop through every choice Ink gives us
        for (int i = 0; i < story.currentChoices.Count; i++)
        {
            Choice choice = story.currentChoices[i];

            // Instantiate a new button prefab inside our container
            Button choiceButton = Instantiate(UIDialogueChoicePrefab, UIDialogueChoiceContainer);
            choiceButton.transform.localPosition = new Vector3(0, yPos, 0);
            yPos -= 200; // Adjust this value to control the vertical spacing between buttons

            // Set the button's text to match the Ink choice
            TextMeshProUGUI buttonText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = choice.text;

            // Capture the current index for the listener lambda function
            int choiceIndex = i;

            // Tell the button what to do when clicked
            choiceButton.onClick.AddListener(() => OnClickChoiceButton(choiceIndex));
        }
    }

    void OnClickChoiceButton(int choiceIndex)
    {
        // Tell Ink which choice the player picked
        story.ChooseChoiceIndex(choiceIndex);

        // Continue the story layout
        RefreshView();
    }

    void RemoveExistingButtons()
    {
        foreach (Transform child in UIDialogueChoiceContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void returnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void closePausePanel()
    {
        pausePanel.SetActive(false);
    }
}


