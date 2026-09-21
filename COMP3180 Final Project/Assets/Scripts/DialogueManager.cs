using UnityEngine;
using UnityEngine.UI;
using TMPro; // Remove if using standard Unity Text
using Ink.Runtime;

public class InkDialogueManager : MonoBehaviour
{
    [Header("Ink Configuration")]
    [SerializeField] private TextAsset inkJSONAsset;
    private Story story;

    [Header("UI References")]
    [SerializeField] private GenerateDialogues storyText; // Use Text if not using TMP
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;

    [SerializeField] private Button continueBtn;

    void Start()
    {
        // 1. Initialize the story
        story = new Story(inkJSONAsset.text);
        
        // 2. Start displaying the story
        RefreshView();
        continueBtn.gameObject.SetActive(false); // Hide the continue button initially
    }

    // This handles advancing the text and creating buttons
    public void RefreshView()
    {
        // Clear out any old choice buttons from the previous turn
        RemoveExistingButtons();

        // Read and display one line so the player controls when the story advances.
        string text = story.canContinue ? story.Continue() : string.Empty;

        storyText.TypeSentence(text); // Start typing the text with the typewriter effect

        // If there are choices, display buttons for them
        if (story.currentChoices.Count > 0)
        {
            continueBtn.gameObject.SetActive(false); // Hide the continue button when choices are available
            DisplayChoices();
        }
        else if (story.canContinue)
        {
            // DisplayContinueButton();
            continueBtn.gameObject.SetActive(true);
        }
    }

    // void DisplayContinueButton()
    // {
    //     Button continueButton = Instantiate(buttonPrefab, buttonContainer);
    //     TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
    //     buttonText.text = "Continue";
    //     continueButton.onClick.AddListener(RefreshView);
    // }

    void DisplayChoices()
    {
        int yPos = 200;
        // Loop through every choice Ink gives us
        for (int i = 0; i < story.currentChoices.Count; i++)
        {
            Choice choice = story.currentChoices[i];

            // Instantiate a new button prefab inside our container
            Button choiceButton = Instantiate(buttonPrefab, buttonContainer);
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
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }
}


