#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VNStudy;

namespace VNStudy.EditorTools
{
    /// <summary>
    /// Builds the complete StudyScene described in the design document:
    /// one Canvas, three normal panels, one ErrorPanel, five fixed choice slots,
    /// fixed 1920x1080 reference layout, and all controller references wired.
    ///
    /// Re-running the command replaces the scene. Story JSON assets are bound
    /// automatically when Assets/Dialogues/story_A.json etc. exist.
    /// </summary>
    public static class StudySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/StudyScene.unity";
        private const float RefWidth = 1920f;
        private const float RefHeight = 1080f;

        [MenuItem("Tools/Visual Novel Study/Build StudyScene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!Application.isBatchMode
                && File.Exists(ScenePath)
                && !EditorUtility.DisplayDialog(
                    "StudyScene 已存在",
                    "将用新生成的场景覆盖现有 StudyScene.unity。继续？",
                    "覆盖",
                    "取消"))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("StudyScene");

            CreateCamera(root.transform);

            GameObject controllerObject = new GameObject("StudyController");
            controllerObject.transform.SetParent(root.transform, false);
            StudyController controller = controllerObject.AddComponent<StudyController>();

            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(root.transform, false);
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            eventSystem.sendNavigationEvents = false;

            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            TMP_FontAsset font = StudyFontLocator.FindStudyFont();
            if (font == null)
            {
                Debug.LogWarning(
                    "[StudySceneBuilder] 未找到 TMP 字体资产。请使用 TMP 自带的 LiberationSans SDF，或创建一个拉丁字体资产，" +
                    "否则运行时会进入配置错误页。");
            }

            // ----------------------------------------------------------------
            // MenuPanel
            // ----------------------------------------------------------------
            RectTransform menu = CreatePanel("MenuPanel", canvas.transform, Hex("#151A24"));

            CreateText(
                "TitleText", menu, 480f, 260f, 960f, 80f,
                "Reading Study", 48f, TextAlignmentOptions.Center, Color.white, font);

            CreateText(
                "InstructionText", menu, 480f, 370f, 960f, 120f,
                "Please choose the version in the order given by the researcher. Click Continue to read; click one option when a response appears.",
                28f, TextAlignmentOptions.Center, Color.white, font);

            SingleClickButton[] versionButtons = new SingleClickButton[3];
            TMP_Text[] versionLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                string label = ((char)('A' + i)).ToString();
                TMP_Text text;
                versionButtons[i] = CreateButton(
                    "VersionButton_" + label,
                    menu,
                    540f + i * 300f,
                    570f,
                    240f,
                    88f,
                    label,
                    32f,
                    font,
                    8f, 8f, 4f, 4f,
                    TextAlignmentOptions.Center,
                    out text);
                versionLabels[i] = text;
            }

            // ----------------------------------------------------------------
            // GamePanel (reading + choices)
            // ----------------------------------------------------------------
            RectTransform game = CreateFullStretch("GamePanel", canvas.transform);

            CreateFullStretchImage("BackgroundImage", game, Hex("#20252D"));
            RectTransform playerPortrait = CreateImage(
                "PlayerPortrait", game, 120f, 96f, 600f, 920f, Hex("#2E3B4E"), true);
            RectTransform friendPortrait = CreateImage(
                "FriendPortrait", game, 1200f, 96f, 600f, 920f, Hex("#2E3B4E"), true);

            Color dialoguePanelColor = Hex("#151A24");
            dialoguePanelColor.a = 235f / 255f;
            RectTransform dialoguePanel = CreateImage(
                "DialoguePanel", game, 96f, 600f, 1728f, 384f, dialoguePanelColor, false);

            TMP_Text speakerNameText = CreateText(
                "SpeakerNameText", dialoguePanel, 48f, 24f, 1200f, 48f,
                string.Empty, 32f, TextAlignmentOptions.Left, Color.white, font);

            TMP_Text dialogueText = CreateText(
                "DialogueText", dialoguePanel, 48f, 86f, 1632f, 192f,
                string.Empty, 36f, TextAlignmentOptions.TopLeft, Hex("#F5F5F5"), font);

            TMP_Text continueLabel;
            SingleClickButton continueButton = CreateButton(
                "ContinueButton",
                dialoguePanel,
                1384f, 312f,
                296f, 56f,
                "Continue",
                28f,
                font,
                8f, 8f, 4f, 4f,
                TextAlignmentOptions.Center,
                out continueLabel);

            RectTransform choicesPanel = CreateRect(
                "ChoicesPanel", game, 780f, 72f, 1044f, 504f);

            SingleClickButton[] choiceSlots = new SingleClickButton[5];
            TMP_Text[] choiceLabels = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
            {
                TMP_Text text;
                choiceSlots[i] = CreateButton(
                    "ChoiceButton_" + (i + 1),
                    choicesPanel,
                    18f,
                    18f + i * 96f,
                    1008f,
                    84f,
                    "Option " + (i + 1),
                    28f,
                    font,
                    24f, 24f, 8f, 8f,
                    TextAlignmentOptions.Left,
                    out text);
                choiceLabels[i] = text;
            }

            // ----------------------------------------------------------------
            // CompletePanel and ErrorPanel
            // ----------------------------------------------------------------
            RectTransform complete = CreatePanel("CompletePanel", canvas.transform, Hex("#151A24"));
            TMP_Text completeTitle = CreateText(
                "CompleteTitleText", complete, 480f, 300f, 960f, 80f,
                "Version A complete", 44f, TextAlignmentOptions.Center, Color.white, font);
            TMP_Text completeInstruction = CreateText(
                "CompleteInstructionText", complete, 480f, 420f, 960f, 120f,
                "Please complete the corresponding questionnaire. When finished, follow the researcher's instructions to return to the start screen.",
                28f, TextAlignmentOptions.Center, Color.white, font);

            TMP_Text completeReturnLabel;
            SingleClickButton completeReturn = CreateButton(
                "ReturnButton", complete, 720f, 720f, 480f, 80f,
                "Return to start", 28f, font,
                8f, 8f, 4f, 4f, TextAlignmentOptions.Center, out completeReturnLabel);

            RectTransform error = CreatePanel("ErrorPanel", canvas.transform, Hex("#151A24"));
            TMP_Text errorMessage = CreateText(
                "ErrorMessageText", error, 360f, 360f, 1200f, 240f,
                "The version could not start. Please tell the researcher.", 28f, TextAlignmentOptions.Center, Color.white, font);

            TMP_Text errorReturnLabel;
            SingleClickButton errorReturn = CreateButton(
                "ReturnButton", error, 720f, 720f, 480f, 80f,
                "Return to start", 28f, font,
                8f, 8f, 4f, 4f, TextAlignmentOptions.Center, out errorReturnLabel);

            // ----------------------------------------------------------------
            // Wire the controller
            // ----------------------------------------------------------------
            VersionConfig[] configs = new VersionConfig[3];
            for (int i = 0; i < 3; i++)
            {
                string label = ((char)('A' + i)).ToString();
                configs[i] = new VersionConfig
                {
                    label = label,
                    expectedChoiceCount = i == 0 ? 2 : (i == 1 ? 3 : 5)
                };
                TryAssignStory(configs[i], "story_" + label);
            }

            controller.versionConfigs = configs;
            controller.menuPanel = menu.gameObject;
            controller.gamePanel = game.gameObject;
            controller.completePanel = complete.gameObject;
            controller.errorPanel = error.gameObject;

            controller.versionButtons = versionButtons;
            controller.versionButtonLabels = versionLabels;

            controller.speakerNameText = speakerNameText;
            controller.dialogueText = dialogueText;
            controller.continueButton = continueButton;
            controller.canvasScaler = scaler;

            controller.playerPortrait = playerPortrait.GetComponent<Image>();
            controller.friendPortrait = friendPortrait.GetComponent<Image>();
            controller.nonSpeakerPortraitValue = 0.3f;

            controller.choicesPanel = choicesPanel.gameObject;
            controller.choiceSlots = choiceSlots;
            controller.choiceLabels = choiceLabels;

            controller.completeTitleText = completeTitle;
            controller.completeInstructionText = completeInstruction;
            controller.completeReturnButton = completeReturn;

            controller.errorMessageText = errorMessage;
            controller.errorReturnButton = errorReturn;

            controller.studyFont = font;
            controller.playerDisplayName = "You";
            controller.friendDisplayName = "Classmate";
            controller.expectedDecisionPointCount = 4;
            controller.maxControlOutputs = 64;
            controller.maxDialogueLines = 4;
            controller.maxChoiceLines = 2;

            menu.gameObject.SetActive(true);
            game.gameObject.SetActive(false);
            complete.gameObject.SetActive(false);
            error.gameObject.SetActive(false);

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[StudySceneBuilder] 保存场景失败：" + ScenePath);
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            ApplyPlayerSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = controllerObject;

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "StudyScene 已生成",
                    "场景已保存到 " + ScenePath + "。\n\n" +
                    "下一步：\n" +
                    "1. 在 StudyController 的 Version Configs 中确认 A/B/C 已绑定 story_A/B/C.json。\n" +
                    "2. 给 BackgroundImage、PlayerPortrait、FriendPortrait 换上正式素材。\n" +
                    "3. 确认 studyFont 是拉丁字体资产（TMP 自带的 LiberationSans SDF 即可；也可换成命名含 StudySans 的字体资产）。",
                    "好");
            }
        }

        // --------------------------------------------------------------------
        // Scene object helpers
        // --------------------------------------------------------------------

        private static void CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = RefHeight / 200f;
            camera.cullingMask = 0;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static RectTransform CreateFullStretch(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateFullStretch(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateFullStretchImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateFullStretch(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateImage(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color color,
            bool preserveAspect)
        {
            RectTransform rect = CreateRect(name, parent, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            string content,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            TMP_FontAsset font)
        {
            RectTransform rect = CreateRect(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            if (font != null)
            {
                text.font = font;
            }

            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.richText = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 0f;
            text.characterSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = Vector4.zero;
            text.raycastTarget = false;
            return text;
        }

        private static SingleClickButton CreateButton(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            string labelContent,
            float fontSize,
            TMP_FontAsset font,
            float padLeft,
            float padRight,
            float padTop,
            float padBottom,
            TextAlignmentOptions alignment,
            out TMP_Text label)
        {
            RectTransform rect = CreateRect(name, parent, x, y, width, height);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Hex("#273142");
            image.raycastTarget = true;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(padLeft, padBottom);
            labelRect.offsetMax = new Vector2(-padRight, -padTop);

            TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.text = labelContent;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.richText = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 0f;
            text.characterSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = Vector4.zero;
            text.raycastTarget = false;

            SingleClickButton button = rect.gameObject.AddComponent<SingleClickButton>();
            button.targetImage = image;
            button.label = text;

            label = text;
            return button;
        }

        // --------------------------------------------------------------------
        // Assets and project settings
        // --------------------------------------------------------------------

        private static void TryAssignStory(VersionConfig config, string assetName)
        {
            string[] guids = AssetDatabase.FindAssets("t:TextAsset " + assetName);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset != null && asset.name == assetName)
                {
                    config.inkJson = asset;
                    return;
                }
            }

            Debug.LogWarning(
                $"[StudySceneBuilder] 未找到 Assets/Dialogues/{assetName}.json；" +
                $"请在 StudyController 的 Version Configs 中手动绑定版本 {config.label}。");
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.defaultScreenWidth = DisplaySetup.Width;
            PlayerSettings.defaultScreenHeight = DisplaySetup.Height;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.runInBackground = true;
        }

        private static Color Hex(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.white;
        }
    }
}
#endif
