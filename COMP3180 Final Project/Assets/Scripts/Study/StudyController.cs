using System;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VNStudy
{
    public enum StudyState
    {
        Menu,
        Loading,
        Advancing,
        Reading,
        Choosing,
        Complete,
        Error
    }

    /// <summary>
    /// The single controller for the study prototype. It owns the Ink Story, the
    /// state machine, tag validation, choice validation, and the three normal
    /// screens (menu, reading, complete) plus the blocking error screen.
    ///
    /// One accepted input is processed per frame. Every callback captures the
    /// viewToken of the screen it was created for, so a stale callback is ignored.
    /// </summary>
    public class StudyController : MonoBehaviour
    {
        private const string StartupFailureText = "The version could not start. Please tell the researcher.";
        private const string RuntimeFailureText = "This run has stopped. Please tell the researcher.";
        private const string SetupFailureText = "The study program is not configured correctly. Please tell the researcher.";
        private const string CompleteInstruction = "Please complete the corresponding questionnaire. When finished, follow the researcher's instructions to return to the start screen.";

        [Header("Version Configurations (A / B / C)")]
        public VersionConfig[] versionConfigs = new VersionConfig[3];

        [Header("Panels")]
        public GameObject menuPanel;
        public GameObject gamePanel;
        public GameObject completePanel;
        public GameObject errorPanel;

        [Header("Menu")]
        public SingleClickButton[] versionButtons = new SingleClickButton[3];
        public TMP_Text[] versionButtonLabels = new TMP_Text[3];

        [Header("Reading")]
        public TMP_Text speakerNameText;
        public TMP_Text dialogueText;
        public SingleClickButton continueButton;
        public CanvasScaler canvasScaler;

        [Header("Choices")]
        public GameObject choicesPanel;
        public SingleClickButton[] choiceSlots = new SingleClickButton[5];
        public TMP_Text[] choiceLabels = new TMP_Text[5];

        [Header("Complete")]
        public TMP_Text completeTitleText;
        public TMP_Text completeInstructionText;
        public SingleClickButton completeReturnButton;

        [Header("Error")]
        public TMP_Text errorMessageText;
        public SingleClickButton errorReturnButton;

        [Header("Study Settings")]
        public TMP_FontAsset studyFont;
        public string playerDisplayName = "You";
        public string friendDisplayName = "Classmate";

        [Tooltip("正式构建固定为 4；联调单点示例时可临时改为 1。")]
        public int expectedDecisionPointCount = 4;

        [Tooltip("每次操作最多处理的连续空输出数量。")]
        public int maxControlOutputs = 64;

        public int maxDialogueLines = 4;
        public int maxChoiceLines = 2;

        private readonly List<Choice> displayedChoices = new List<Choice>();
        private readonly HashSet<string> seenLineIds = new HashSet<string>();

        private Story story;
        private VersionConfig activeConfig;
        private StudyState state = StudyState.Menu;

        private string currentLineId = string.Empty;
        private string currentChoicePointId = string.Empty;
        private int completedChoicePoints;
        private bool currentLineIsEnding;
        private bool expectChoiceResponse;

        private int viewToken;
        private int lastAcceptedFrame = -1;
        private int controlOutputsThisOperation;

        private bool runtimeError;
        private string runtimeErrorMessage = string.Empty;
        private bool displayLocked;
        private bool setupValid;

        private Action[] menuCallbacks;
        private Action[] choiceCallbacks;
        private Action continueCallback;
        private Action returnCallback;

        private void Awake()
        {
            EnsureCallbackArrays();
            setupValid = ValidateSetup();

            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = false;
            }

            if (!setupValid)
            {
                ShowSetupError();
                return;
            }

            ApplyStudyFont();
            ConfigureStudyText();
            SetContinueLabel();
            HideAllPanels();
            ShowMenu();
        }

        private void Start()
        {
            if (!setupValid)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = false;
            }

            StartCoroutine(DisplaySetup.Verify(canvasScaler, OnDisplayCheckFailed));
        }

        // ------------------------------------------------------------------
        // Menu
        // ------------------------------------------------------------------

        private void ShowMenu()
        {
            state = StudyState.Menu;

            HideChoicesAndClear();
            HideContinue();
            UnbindReturnButton();
            UnbindMenuButtons();

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = string.Empty;
                speakerNameText.gameObject.SetActive(true);
            }

            if (completeTitleText != null)
            {
                completeTitleText.text = string.Empty;
            }

            if (errorMessageText != null)
            {
                errorMessageText.text = string.Empty;
            }

            if (completeReturnButton != null)
            {
                completeReturnButton.SetInteractable(false);
            }

            if (errorReturnButton != null)
            {
                errorReturnButton.SetInteractable(false);
            }

            for (int i = 0; i < versionButtons.Length; i++)
            {
                if (versionButtons[i] != null)
                {
                    versionButtons[i].gameObject.SetActive(true);
                }

                if (versionButtonLabels != null && i < versionButtonLabels.Length && versionButtonLabels[i] != null)
                {
                    versionButtonLabels[i].text = GetConfiguredLabel(i);
                }
            }

            SetOnlyPanel(menuPanel);

            int token = IssueInputToken();
            BindMenuButtons(token);
            SetVersionButtonsInteractable(true);
            ClearSelection();
        }

        private void OnVersionClicked(int configIndex, int token)
        {
            if (!AcceptInput(StudyState.Menu, token))
            {
                return;
            }

            lastAcceptedFrame = Time.frameCount;
            StartVersion(configIndex);
        }

        private void StartVersion(int configIndex)
        {
            state = StudyState.Loading;
            InvalidateInput();
            UnbindMenuButtons();
            SetVersionButtonsInteractable(false);

            VersionConfig config;
            string configError;
            if (!TryGetConfig(configIndex, out config, out configError))
            {
                FailCurrentRun("Invalid version configuration: " + configError, StartupFailureText);
                return;
            }

            CleanupRun();
            activeConfig = config;

            try
            {
                story = new Story(config.inkJson.text);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Study] 无法创建 Story（版本 {config.label}）：{exception}");
                FailCurrentRun("Story construction failed: " + exception.Message, StartupFailureText);
                return;
            }

            story.onError += HandleInkError;

            HideChoicesAndClear();
            HideContinue();

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = string.Empty;
                speakerNameText.gameObject.SetActive(true);
            }

            SetOnlyPanel(gamePanel);

            state = StudyState.Advancing;
            ReadNextVisibleOrBoundary(StartupFailureText);
        }

        // ------------------------------------------------------------------
        // Reading and advancing
        // ------------------------------------------------------------------

        private void OnContinueClicked(int token)
        {
            if (!AcceptInput(StudyState.Reading, token))
            {
                return;
            }

            lastAcceptedFrame = Time.frameCount;
            state = StudyState.Advancing;
            InvalidateInput();

            if (currentLineIsEnding)
            {
                ConfirmEnding();
                return;
            }

            ReadNextVisibleOrBoundary(RuntimeFailureText);
        }

        private void ReadNextVisibleOrBoundary(string failureText)
        {
            controlOutputsThisOperation = 0;

            if (story == null)
            {
                FailCurrentRun("Story is null while reading.", failureText);
                return;
            }

            while (true)
            {
                if (runtimeError)
                {
                    FailCurrentRun("Ink runtime error: " + runtimeErrorMessage, failureText);
                    return;
                }

                if (!story.canContinue)
                {
                    FailCurrentRun(
                        $"Story stopped without a displayable unit (choices={story.currentChoices.Count}).",
                        failureText);
                    return;
                }

                string rawText = story.Continue();
                string[] tags = CopyCurrentTags();

                if (runtimeError)
                {
                    FailCurrentRun("Ink runtime error: " + runtimeErrorMessage, failureText);
                    return;
                }

                string text = TrimTrailingNewlines(rawText);
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (!ConsumeControlOutput())
                    {
                        FailCurrentRun($"Control output limit ({maxControlOutputs}) exceeded.", failureText);
                        return;
                    }

                    continue;
                }

                StudyLineMeta meta;
                string tagError;
                if (!TryParseAndValidateTags(tags, out meta, out tagError))
                {
                    FailCurrentRun(tagError, failureText);
                    return;
                }

                if (expectChoiceResponse && meta.ChoicePointId != null)
                {
                    FailCurrentRun("选择之后的局部分支不得立刻出现新的决策点。", failureText);
                    return;
                }

                if (!RenderUnit(text, meta, failureText))
                {
                    return;
                }

                expectChoiceResponse = false;
                currentLineIsEnding = meta.IsEnding;

                if (meta.ChoicePointId != null)
                {
                    currentChoicePointId = meta.ChoicePointId;
                    ResolveQuestionBoundary(failureText);
                    return;
                }

                state = StudyState.Reading;
                ShowContinueForReading();
                return;
            }
        }

        // ------------------------------------------------------------------
        // Choices
        // ------------------------------------------------------------------

        private void ResolveQuestionBoundary(string failureText)
        {
            if (story == null)
            {
                FailCurrentRun("Story is null at a question boundary.", failureText);
                return;
            }

            while (story.canContinue)
            {
                string rawText = story.Continue();
                CopyCurrentTags();

                if (runtimeError)
                {
                    FailCurrentRun("Ink runtime error: " + runtimeErrorMessage, failureText);
                    return;
                }

                string text = TrimTrailingNewlines(rawText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    FailCurrentRun(
                        $"{currentChoicePointId} 之后出现非空正文（应为纯控制输出）：\"{Snippet(text)}\"",
                        failureText);
                    return;
                }

                if (!ConsumeControlOutput())
                {
                    FailCurrentRun($"Control output limit ({maxControlOutputs}) exceeded after {currentChoicePointId}.", failureText);
                    return;
                }
            }

            List<Choice> choices = story.currentChoices;

            if (choices == null || choices.Count == 0)
            {
                FailCurrentRun($"{currentChoicePointId} 没有 currentChoices。", failureText);
                return;
            }

            if (activeConfig == null)
            {
                FailCurrentRun("activeConfig 在提问处为空。", failureText);
                return;
            }

            if (choices.Count != activeConfig.expectedChoiceCount)
            {
                FailCurrentRun(
                    $"{currentChoicePointId} 的选项数 {choices.Count} 与配置 {activeConfig.expectedChoiceCount} 不一致。",
                    failureText);
                return;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i] == null || string.IsNullOrWhiteSpace(choices[i].text))
                {
                    FailCurrentRun($"{currentChoicePointId} 的第 {i + 1} 个选项文案为空。", failureText);
                    return;
                }
            }

            displayedChoices.Clear();
            displayedChoices.AddRange(choices);
            ShowChoices(failureText);
        }

        private void ShowChoices(string failureText)
        {
            if (choicesPanel == null || choiceSlots == null || choiceLabels == null)
            {
                FailCurrentRun("选项 UI 引用缺失。", failureText);
                return;
            }

            UnbindChoiceCallbacks();

            for (int i = 0; i < choiceSlots.Length; i++)
            {
                if (choiceSlots[i] != null)
                {
                    choiceSlots[i].SetInteractable(false);
                    choiceSlots[i].gameObject.SetActive(false);
                }

                if (i < choiceLabels.Length && choiceLabels[i] != null)
                {
                    choiceLabels[i].text = string.Empty;
                }
            }

            choicesPanel.SetActive(true);
            int token = IssueInputToken();

            int usedSlots = Mathf.Min(displayedChoices.Count, choiceSlots.Length);
            for (int i = 0; i < usedSlots; i++)
            {
                SingleClickButton slot = choiceSlots[i];
                TMP_Text label = i < choiceLabels.Length ? choiceLabels[i] : null;

                if (slot == null || label == null)
                {
                    FailCurrentRun($"选项槽位 {i + 1} 的引用缺失。", failureText);
                    return;
                }

                label.text = displayedChoices[i].text;
                slot.gameObject.SetActive(true);

                if (!Fits(label, maxChoiceLines))
                {
                    FailCurrentRun(
                        $"{currentChoicePointId} 的第 {i + 1} 个选项超过 {maxChoiceLines} 行或超出按钮高度。",
                        failureText);
                    return;
                }

                slot.SetInteractable(true);

                int choiceIndex = displayedChoices[i].index;
                Action callback = () => OnChoiceClicked(choiceIndex, token);
                choiceCallbacks[i] = callback;
                slot.Clicked += callback;
            }

            HideContinue();
            state = StudyState.Choosing;
        }

        private void OnChoiceClicked(int choiceIndex, int token)
        {
            if (!AcceptInput(StudyState.Choosing, token))
            {
                return;
            }

            if (!IsChoiceInCurrentSnapshot(choiceIndex))
            {
                return;
            }

            lastAcceptedFrame = Time.frameCount;
            state = StudyState.Advancing;
            InvalidateInput();

            try
            {
                story.ChooseChoiceIndex(choiceIndex);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Study] ChooseChoiceIndex({choiceIndex}) 失败：{exception}");
                FailCurrentRun("ChooseChoiceIndex failed: " + exception.Message, RuntimeFailureText);
                return;
            }

            if (runtimeError)
            {
                FailCurrentRun("Ink runtime error: " + runtimeErrorMessage, RuntimeFailureText);
                return;
            }

            completedChoicePoints++;
            expectChoiceResponse = true;
            HideChoicesAndClear();
            ReadNextVisibleOrBoundary(RuntimeFailureText);
        }

        private bool IsChoiceInCurrentSnapshot(int choiceIndex)
        {
            for (int i = 0; i < displayedChoices.Count; i++)
            {
                if (displayedChoices[i] != null && displayedChoices[i].index == choiceIndex)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------------
        // Ending
        // ------------------------------------------------------------------

        private void ConfirmEnding()
        {
            if (!currentLineIsEnding)
            {
                FailCurrentRun("ConfirmEnding called on a non-ending unit.", RuntimeFailureText);
                return;
            }

            if (completedChoicePoints != expectedDecisionPointCount)
            {
                FailCurrentRun(
                    $"结局在 {completedChoicePoints}/{expectedDecisionPointCount} 个决策点之后出现。",
                    RuntimeFailureText);
                return;
            }

            if (story == null)
            {
                FailCurrentRun("Story is null at ending confirmation.", RuntimeFailureText);
                return;
            }

            controlOutputsThisOperation = 0;

            while (story.canContinue)
            {
                string rawText = story.Continue();
                CopyCurrentTags();

                if (runtimeError)
                {
                    FailCurrentRun("Ink runtime error: " + runtimeErrorMessage, RuntimeFailureText);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(TrimTrailingNewlines(rawText)))
                {
                    FailCurrentRun("end:common 之后仍有正文。", RuntimeFailureText);
                    return;
                }

                if (!ConsumeControlOutput())
                {
                    FailCurrentRun($"Control output limit ({maxControlOutputs}) exceeded at ending.", RuntimeFailureText);
                    return;
                }
            }

            if (story.currentChoices.Count > 0)
            {
                FailCurrentRun("end:common 之后仍有选项。", RuntimeFailureText);
                return;
            }

            ShowComplete(activeConfig != null ? activeConfig.label : string.Empty);
        }

        private void ShowComplete(string label)
        {
            state = StudyState.Complete;
            InvalidateInput();
            HideChoicesAndClear();
            HideContinue();

            if (completeTitleText != null)
            {
                completeTitleText.text = $"Version {label} complete";
            }

            if (completeInstructionText != null)
            {
                completeInstructionText.text = CompleteInstruction;
            }

            SetOnlyPanel(completePanel);

            UnbindReturnButton();

            if (completeReturnButton != null)
            {
                int token = IssueInputToken();
                returnCallback = () => ReturnToMenu(token);
                completeReturnButton.Clicked += returnCallback;
                completeReturnButton.SetInteractable(true);
            }

            ClearSelection();
        }

        // ------------------------------------------------------------------
        // Error and reset
        // ------------------------------------------------------------------

        private void FailCurrentRun(string reason, string playerMessage)
        {
            if (state == StudyState.Error)
            {
                return;
            }

            Debug.LogError(
                $"[Study] {reason} | version={activeConfig?.label ?? "-"} | line={currentLineId} | state={state}");

            state = StudyState.Error;
            InvalidateInput();
            HideChoicesAndClear();
            HideContinue();

            if (errorMessageText != null)
            {
                errorMessageText.text = string.IsNullOrEmpty(playerMessage) ? RuntimeFailureText : playerMessage;
            }

            SetOnlyPanel(errorPanel);
            UnbindReturnButton();

            if (errorReturnButton != null)
            {
                if (displayLocked)
                {
                    errorReturnButton.SetInteractable(false);
                }
                else
                {
                    int token = IssueInputToken();
                    returnCallback = () => ReturnToMenu(token);
                    errorReturnButton.Clicked += returnCallback;
                    errorReturnButton.SetInteractable(true);
                }
            }

            ClearSelection();
        }

        private void OnDisplayCheckFailed(string message)
        {
            displayLocked = true;
            FailCurrentRun("Display check failed: " + message, message);
        }

        private void ReturnToMenu(int token)
        {
            if (token != viewToken || Time.frameCount == lastAcceptedFrame)
            {
                return;
            }

            if (state != StudyState.Complete && state != StudyState.Error)
            {
                return;
            }

            lastAcceptedFrame = Time.frameCount;
            state = StudyState.Loading;
            InvalidateInput();
            UnbindReturnButton();
            CleanupRun();
            ShowMenu();
        }

        private void CleanupRun()
        {
            HideChoicesAndClear();
            HideContinue();
            UnbindReturnButton();

            if (story != null)
            {
                story.onError -= HandleInkError;
                story = null;
            }

            activeConfig = null;
            currentLineId = string.Empty;
            currentChoicePointId = string.Empty;
            completedChoicePoints = 0;
            currentLineIsEnding = false;
            expectChoiceResponse = false;
            runtimeError = false;
            runtimeErrorMessage = string.Empty;
            controlOutputsThisOperation = 0;
            seenLineIds.Clear();

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = string.Empty;
                speakerNameText.gameObject.SetActive(true);
            }
        }

        private void ShowSetupError()
        {
            Debug.LogError("[Study] 场景引用或研究配置不完整，无法启动。请按上面的 Console 错误逐项修复。");

            state = StudyState.Error;
            displayLocked = true;
            SetOnlyPanel(errorPanel);

            if (errorMessageText != null)
            {
                errorMessageText.text = SetupFailureText;
            }

            if (errorReturnButton != null)
            {
                errorReturnButton.SetInteractable(false);
            }
        }

        // ------------------------------------------------------------------
        // Input helpers
        // ------------------------------------------------------------------

        private void ShowContinueForReading()
        {
            if (continueButton == null)
            {
                FailCurrentRun("继续按钮引用缺失。", RuntimeFailureText);
                return;
            }

            if (continueCallback != null)
            {
                continueButton.Clicked -= continueCallback;
            }

            int token = IssueInputToken();
            continueCallback = () => OnContinueClicked(token);
            continueButton.Clicked += continueCallback;

            continueButton.gameObject.SetActive(true);
            continueButton.SetInteractable(true);
        }

        private void HideContinue()
        {
            if (continueButton == null)
            {
                continueCallback = null;
                return;
            }

            if (continueCallback != null)
            {
                continueButton.Clicked -= continueCallback;
                continueCallback = null;
            }

            continueButton.SetInteractable(false);
            continueButton.gameObject.SetActive(false);
        }

        private void HideChoicesAndClear()
        {
            UnbindChoiceCallbacks();

            if (choiceSlots != null)
            {
                for (int i = 0; i < choiceSlots.Length; i++)
                {
                    if (choiceSlots[i] == null)
                    {
                        continue;
                    }

                    choiceSlots[i].SetInteractable(false);
                    choiceSlots[i].gameObject.SetActive(false);
                }
            }

            if (choiceLabels != null)
            {
                for (int i = 0; i < choiceLabels.Length; i++)
                {
                    if (choiceLabels[i] != null)
                    {
                        choiceLabels[i].text = string.Empty;
                    }
                }
            }

            if (choicesPanel != null)
            {
                choicesPanel.SetActive(false);
            }

            displayedChoices.Clear();
            currentChoicePointId = string.Empty;
        }

        private void BindMenuButtons(int token)
        {
            if (versionButtons == null)
            {
                return;
            }

            for (int i = 0; i < versionButtons.Length; i++)
            {
                SingleClickButton button = versionButtons[i];
                if (button == null)
                {
                    continue;
                }

                int configIndex = i;
                Action callback = () => OnVersionClicked(configIndex, token);
                menuCallbacks[i] = callback;
                button.Clicked += callback;
            }
        }

        private void UnbindMenuButtons()
        {
            if (versionButtons == null || menuCallbacks == null)
            {
                return;
            }

            for (int i = 0; i < versionButtons.Length && i < menuCallbacks.Length; i++)
            {
                if (versionButtons[i] != null && menuCallbacks[i] != null)
                {
                    versionButtons[i].Clicked -= menuCallbacks[i];
                }

                menuCallbacks[i] = null;
            }
        }

        private void UnbindChoiceCallbacks()
        {
            if (choiceSlots == null || choiceCallbacks == null)
            {
                return;
            }

            for (int i = 0; i < choiceSlots.Length && i < choiceCallbacks.Length; i++)
            {
                if (choiceSlots[i] != null && choiceCallbacks[i] != null)
                {
                    choiceSlots[i].Clicked -= choiceCallbacks[i];
                }

                choiceCallbacks[i] = null;
            }
        }

        private void UnbindReturnButton()
        {
            if (returnCallback == null)
            {
                return;
            }

            if (completeReturnButton != null)
            {
                completeReturnButton.Clicked -= returnCallback;
            }

            if (errorReturnButton != null)
            {
                errorReturnButton.Clicked -= returnCallback;
            }

            returnCallback = null;
        }

        private void SetVersionButtonsInteractable(bool value)
        {
            if (versionButtons == null)
            {
                return;
            }

            for (int i = 0; i < versionButtons.Length; i++)
            {
                if (versionButtons[i] != null)
                {
                    versionButtons[i].SetInteractable(value);
                }
            }
        }

        private bool AcceptInput(StudyState expectedState, int token)
        {
            return state == expectedState && token == viewToken && Time.frameCount != lastAcceptedFrame;
        }

        private void InvalidateInput()
        {
            viewToken++;

            if (continueButton != null)
            {
                continueButton.SetInteractable(false);
            }

            if (choiceSlots != null)
            {
                for (int i = 0; i < choiceSlots.Length; i++)
                {
                    if (choiceSlots[i] != null)
                    {
                        choiceSlots[i].SetInteractable(false);
                    }
                }
            }

            SetVersionButtonsInteractable(false);
        }

        private int IssueInputToken()
        {
            viewToken++;
            return viewToken;
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        // ------------------------------------------------------------------
        // Ink helpers
        // ------------------------------------------------------------------

        private string[] CopyCurrentTags()
        {
            if (story == null)
            {
                return Array.Empty<string>();
            }

            List<string> tags = story.currentTags;
            if (tags == null || tags.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[tags.Count];
            for (int i = 0; i < tags.Count; i++)
            {
                copy[i] = tags[i];
            }

            return copy;
        }

        private bool TryParseAndValidateTags(string[] tags, out StudyLineMeta meta, out string error)
        {
            if (!StudyTagParser.TryParse(tags, out meta, out error))
            {
                return false;
            }

            if (meta.ChoicePointId != null)
            {
                int nextDecisionPoint = completedChoicePoints + 1;

                if (nextDecisionPoint > expectedDecisionPointCount)
                {
                    error = $"出现多余的决策点 {meta.ChoicePointId}（预期共 {expectedDecisionPointCount} 个）。";
                    return false;
                }

                string expectedId = "cp" + nextDecisionPoint.ToString("00");
                if (!string.Equals(meta.ChoicePointId, expectedId, StringComparison.Ordinal))
                {
                    error = $"决策点顺序错误：预期 {expectedId}，实际 {meta.ChoicePointId}。";
                    return false;
                }
            }

            if (meta.IsEnding && completedChoicePoints != expectedDecisionPointCount)
            {
                error = $"共同结局出现在 {completedChoicePoints}/{expectedDecisionPointCount} 个决策点之后。";
                return false;
            }

            if (!seenLineIds.Add(meta.LineId))
            {
                error = $"line ID 重复：{meta.LineId}。";
                return false;
            }

            return true;
        }

        private bool RenderUnit(string text, StudyLineMeta meta, string failureText)
        {
            if (dialogueText == null)
            {
                FailCurrentRun("正文 TMP_Text 引用缺失。", failureText);
                return false;
            }

            dialogueText.text = text;
            if (!Fits(dialogueText, maxDialogueLines))
            {
                FailCurrentRun(
                    $"正文超过 {maxDialogueLines} 行或超出正文框高度（line {meta.LineId}）。",
                    failureText);
                return false;
            }

            string displayName = SpeakerCatalog.GetDisplayName(
                meta.Speaker,
                playerDisplayName,
                friendDisplayName);

            if (speakerNameText != null)
            {
                bool hasName = !string.IsNullOrEmpty(displayName);
                speakerNameText.gameObject.SetActive(true);
                speakerNameText.text = displayName;

                if (hasName && !Fits(speakerNameText, 1))
                {
                    FailCurrentRun($"姓名超过 1 行（line {meta.LineId}）。", failureText);
                    return false;
                }

                if (!hasName)
                {
                    speakerNameText.gameObject.SetActive(false);
                }
            }

            currentLineId = meta.LineId;
            return true;
        }

        private static bool Fits(TMP_Text label, int maxLines)
        {
            if (label == null)
            {
                return false;
            }

            label.ForceMeshUpdate();

            if (label.textInfo == null)
            {
                return false;
            }

            return label.textInfo.lineCount <= maxLines
                   && label.preferredHeight <= label.rectTransform.rect.height + 0.5f;
        }

        private void HandleInkError(string message, Ink.ErrorType type)
        {
            if (type == Ink.ErrorType.Error)
            {
                runtimeError = true;
                runtimeErrorMessage = message;
            }
            else
            {
                Debug.LogWarning($"[Ink {type}] {message}");
            }
        }

        private bool ConsumeControlOutput()
        {
            controlOutputsThisOperation++;
            return controlOutputsThisOperation <= Mathf.Max(1, maxControlOutputs);
        }

        private static string TrimTrailingNewlines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int end = text.Length;
            while (end > 0 && (text[end - 1] == '\n' || text[end - 1] == '\r'))
            {
                end--;
            }

            return text.Substring(0, end);
        }

        private static string Snippet(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            const int limit = 40;
            return text.Length <= limit ? text : text.Substring(0, limit) + "…";
        }

        // ------------------------------------------------------------------
        // Setup, validation, and configuration
        // ------------------------------------------------------------------

        private bool TryGetConfig(int index, out VersionConfig config, out string error)
        {
            config = null;
            error = null;

            if (versionConfigs == null || versionConfigs.Length != 3)
            {
                error = "versionConfigs 必须包含 A/B/C 三项。";
                return false;
            }

            if (index < 0 || index >= versionConfigs.Length)
            {
                error = $"版本索引越界：{index}。";
                return false;
            }

            HashSet<string> labels = new HashSet<string>();
            HashSet<int> assets = new HashSet<int>();

            for (int i = 0; i < versionConfigs.Length; i++)
            {
                VersionConfig candidate = versionConfigs[i];
                if (candidate == null)
                {
                    error = $"versionConfigs[{i}] 为空。";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(candidate.label))
                {
                    error = $"versionConfigs[{i}].label 为空。";
                    return false;
                }

                if (candidate.inkJson == null)
                {
                    error = $"版本 {candidate.label} 没有绑定编译后的 Ink JSON。";
                    return false;
                }

                int maxSlots = choiceSlots != null ? choiceSlots.Length : 5;
                if (candidate.expectedChoiceCount < 1 || candidate.expectedChoiceCount > maxSlots)
                {
                    error = $"版本 {candidate.label} 的 expectedChoiceCount 必须在 1..{maxSlots}。";
                    return false;
                }

                if (!labels.Add(candidate.label))
                {
                    error = $"版本标签重复：{candidate.label}。";
                    return false;
                }

                if (!assets.Add(candidate.inkJson.GetInstanceID()))
                {
                    error = "三个版本不能共用同一个 Ink JSON 资产。";
                    return false;
                }
            }

            config = versionConfigs[index];
            return true;
        }

        private string GetConfiguredLabel(int index)
        {
            if (versionConfigs != null
                && index >= 0
                && index < versionConfigs.Length
                && versionConfigs[index] != null
                && !string.IsNullOrWhiteSpace(versionConfigs[index].label))
            {
                return versionConfigs[index].label;
            }

            return index >= 0 && index < 3 ? ((char)('A' + index)).ToString() : "?";
        }

        private void ApplyStudyFont()
        {
            if (studyFont == null)
            {
                return;
            }

            Transform root = transform.root;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = studyFont;
                texts[i].enableAutoSizing = false;
            }

            SingleClickButton[] buttons = root.GetComponentsInChildren<SingleClickButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].RefreshVisual();
            }
        }

        private void ConfigureStudyText()
        {
            ConfigureLabel(dialogueText, TextAlignmentOptions.TopLeft);
            ConfigureLabel(speakerNameText, TextAlignmentOptions.Left);

            if (choiceLabels == null)
            {
                return;
            }

            for (int i = 0; i < choiceLabels.Length; i++)
            {
                ConfigureLabel(choiceLabels[i], TextAlignmentOptions.Left);
            }
        }

        private static void ConfigureLabel(TMP_Text label, TextAlignmentOptions alignment)
        {
            if (label == null)
            {
                return;
            }

            label.enableAutoSizing = false;
            label.richText = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.lineSpacing = 0f;
            label.characterSpacing = 0f;
            label.paragraphSpacing = 0f;
            label.alignment = alignment;
            label.margin = Vector4.zero;
        }

        private void SetContinueLabel()
        {
            if (continueButton == null)
            {
                return;
            }

            TMP_Text label = continueButton.label != null
                ? continueButton.label
                : continueButton.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                label.text = "Continue";
            }
        }

        private void SetOnlyPanel(GameObject panel)
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(panel == menuPanel);
            }

            if (gamePanel != null)
            {
                gamePanel.SetActive(panel == gamePanel);
            }

            if (completePanel != null)
            {
                completePanel.SetActive(panel == completePanel);
            }

            if (errorPanel != null)
            {
                errorPanel.SetActive(panel == errorPanel);
            }
        }

        private void HideAllPanels()
        {
            SetOnlyPanel(null);
        }

        private bool ValidateSetup()
        {
            bool valid = true;

            valid &= Require(menuPanel, nameof(menuPanel));
            valid &= Require(gamePanel, nameof(gamePanel));
            valid &= Require(completePanel, nameof(completePanel));
            valid &= Require(errorPanel, nameof(errorPanel));
            valid &= Require(speakerNameText, nameof(speakerNameText));
            valid &= Require(dialogueText, nameof(dialogueText));
            valid &= Require(continueButton, nameof(continueButton));
            valid &= Require(canvasScaler, nameof(canvasScaler));
            valid &= Require(choicesPanel, nameof(choicesPanel));
            valid &= Require(completeTitleText, nameof(completeTitleText));
            valid &= Require(completeReturnButton, nameof(completeReturnButton));
            valid &= Require(errorMessageText, nameof(errorMessageText));
            valid &= Require(errorReturnButton, nameof(errorReturnButton));
            valid &= Require(studyFont, nameof(studyFont));

            valid &= RequireArray(versionButtons, 3, nameof(versionButtons));
            valid &= RequireArray(versionButtonLabels, 3, nameof(versionButtonLabels));
            valid &= RequireArray(choiceSlots, 5, nameof(choiceSlots));
            valid &= RequireArray(choiceLabels, 5, nameof(choiceLabels));

            if (versionConfigs == null || versionConfigs.Length != 3)
            {
                Debug.LogError("[Study] versionConfigs 必须正好包含 3 项（A/B/C）。");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(playerDisplayName))
            {
                Debug.LogError("[Study] playerDisplayName 不能为空。");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(friendDisplayName))
            {
                Debug.LogError("[Study] friendDisplayName 不能为空。");
                valid = false;
            }

            if (expectedDecisionPointCount < 1)
            {
                Debug.LogError("[Study] expectedDecisionPointCount 必须 >= 1。");
                valid = false;
            }

            if (maxControlOutputs < 1 || maxDialogueLines < 1 || maxChoiceLines < 1)
            {
                Debug.LogError("[Study] maxControlOutputs / maxDialogueLines / maxChoiceLines 必须 >= 1。");
                valid = false;
            }

            return valid;
        }

        private static bool Require(UnityEngine.Object value, string fieldName)
        {
            if (value != null)
            {
                return true;
            }

            Debug.LogError($"[Study] 引用未绑定：{fieldName}");
            return false;
        }

        private bool RequireArray(SingleClickButton[] array, int expectedLength, string fieldName)
        {
            if (array == null || array.Length != expectedLength)
            {
                Debug.LogError($"[Study] {fieldName} 必须正好包含 {expectedLength} 项。");
                return false;
            }

            bool valid = true;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    Debug.LogError($"[Study] {fieldName}[{i}] 未绑定。");
                    valid = false;
                }
            }

            return valid;
        }

        private bool RequireArray(TMP_Text[] array, int expectedLength, string fieldName)
        {
            if (array == null || array.Length != expectedLength)
            {
                Debug.LogError($"[Study] {fieldName} 必须正好包含 {expectedLength} 项。");
                return false;
            }

            bool valid = true;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    Debug.LogError($"[Study] {fieldName}[{i}] 未绑定。");
                    valid = false;
                }
            }

            return valid;
        }

        private void EnsureCallbackArrays()
        {
            int menuCount = versionButtons != null ? versionButtons.Length : 0;
            menuCallbacks = new Action[menuCount];

            int choiceCount = choiceSlots != null ? choiceSlots.Length : 0;
            choiceCallbacks = new Action[choiceCount];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxControlOutputs < 1)
            {
                maxControlOutputs = 1;
            }

            if (maxDialogueLines < 1)
            {
                maxDialogueLines = 1;
            }

            if (maxChoiceLines < 1)
            {
                maxChoiceLines = 1;
            }

            if (expectedDecisionPointCount < 1)
            {
                expectedDecisionPointCount = 1;
            }
        }
#endif
    }
}
