using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VNStudy
{
    /// <summary>
    /// Applies and verifies the fixed 1920x1080 window required by the study
    /// protocol. The resolution is applied before the first scene loads; the
    /// assertion runs two frames later, once SetResolution and Canvas layout have
    /// taken effect. In the Editor the assertion is skipped because the Game view
    /// owns its own size.
    /// </summary>
    public static class DisplaySetup
    {
        public const int Width = 1920;
        public const int Height = 1080;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
#if !UNITY_EDITOR
            Application.runInBackground = true;

            bool exactSystemSize =
                Display.main.systemWidth == Width &&
                Display.main.systemHeight == Height;

            Screen.SetResolution(
                Width,
                Height,
                exactSystemSize ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
        }

        public static IEnumerator Verify(CanvasScaler scaler, Action<string> fail)
        {
            yield return null;
            yield return null;

#if UNITY_EDITOR
            Debug.Log(
                $"[DisplaySetup] Editor 模式跳过窗口断言（当前 Game 视图 {Screen.width}×{Screen.height}）。" +
                "正式构建会要求客户区 1920×1080 且 Canvas scaleFactor = 1.000。");
            yield break;
#else
            if (Display.main.systemWidth < Width || Display.main.systemHeight < Height)
            {
                fail?.Invoke(
                    $"Display settings mismatch (screen {Display.main.systemWidth}×{Display.main.systemHeight}). Please tell the researcher.");
                yield break;
            }

            if (Screen.width != Width || Screen.height != Height)
            {
                fail?.Invoke(
                    $"Display settings mismatch (current {Screen.width}×{Screen.height}). Please tell the researcher.");
                yield break;
            }

            float scaleFactor = scaler != null ? scaler.scaleFactor : 0f;
            if (Mathf.Abs(scaleFactor - 1f) > 0.001f)
            {
                fail?.Invoke(
                    $"Display settings mismatch (Canvas scaleFactor {scaleFactor:0.###}). Please tell the researcher.");
            }
#endif
        }
    }
}
