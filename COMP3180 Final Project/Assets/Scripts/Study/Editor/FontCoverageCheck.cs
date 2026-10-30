#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VNStudy.EditorTools
{
    /// <summary>
    /// Build-time protection for the font asset:
    /// collect every non-whitespace character used by the compiled story_*.json
    /// assets, then verify that the TMP font can render all of them. Dynamic font
    /// assets have missing glyphs added to the atlas; static font assets are
    /// checked with HasCharacters. Either mode is supported.
    ///
    /// It also provides a menu item that writes the collected character set to
    /// Assets/Art/Font/study_charset.txt for Font Asset Creator.
    /// </summary>
    public class FontCoverageCheck : IPreprocessBuildWithReport
    {
        private const string CharsetFolder = "Assets/Art/Font";
        private const string CharsetPath = CharsetFolder + "/study_charset.txt";

        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            TMP_FontAsset font = StudyFontLocator.FindStudyFont();
            if (font == null)
            {
                throw new BuildFailedException(
                    "未找到 TMP 字体资产。请使用 TMP 自带的 LiberationSans SDF，或创建一个拉丁字体资产。");
            }

            string charset = CollectStoryCharacters(out int characterCount);
            if (characterCount == 0)
            {
                Debug.LogWarning("[FontCheck] 未找到 story_*.json 或其中没有可检查的字符，跳过缺字检查。");
                return;
            }

            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                if (!font.TryAddCharacters(charset, out string missing))
                {
                    if (!string.IsNullOrEmpty(missing))
                    {
                        throw new BuildFailedException(
                            $"字体缺字 {missing.Length} 个：{missing}");
                    }

                    throw new BuildFailedException(
                        $"字体 {font.name} 无法把剧本字符写入动态图集。");
                }

                EditorUtility.SetDirty(font);
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (!font.HasCharacters(charset, out List<char> missing))
                {
                    string missingText = missing != null ? new string(missing.ToArray()) : string.Empty;
                    throw new BuildFailedException(
                        string.IsNullOrEmpty(missingText)
                            ? $"字体 {font.name} 缺少剧本字符，但无法列出具体字符。"
                            : $"字体缺字 {missing.Count} 个：{missingText}");
                }
            }

            Debug.Log($"[FontCheck] 字体 {font.name}：字符 {characterCount} 个，缺字 0。");
        }

        [MenuItem("Tools/Visual Novel Study/Export Story Charset (study_charset.txt)")]
        public static void ExportCharset()
        {
            string charset = CollectStoryCharacters(out int characterCount);
            if (characterCount == 0)
            {
                Debug.LogWarning("[FontCheck] 未找到 story_*.json 或其中没有可检查的字符。");
                return;
            }

            Directory.CreateDirectory(CharsetFolder);
            File.WriteAllText(CharsetPath, charset, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(CharsetPath);

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CharsetPath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"[FontCheck] 已导出 {characterCount} 个字符到 {CharsetPath}。");
        }

        private static string CollectStoryCharacters(out int characterCount)
        {
            HashSet<char> characters = new HashSet<char>();

            string[] guids = AssetDatabase.FindAssets("t:TextAsset story_");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null)
                {
                    continue;
                }

                string text = asset.text;
                for (int i = 0; i < text.Length; i++)
                {
                    char character = text[i];
                    if (character == '\uFEFF' || char.IsWhiteSpace(character) || char.IsControl(character))
                    {
                        continue;
                    }

                    characters.Add(character);
                }
            }

            List<char> sorted = new List<char>(characters);
            sorted.Sort();

            characterCount = sorted.Count;
            return new string(sorted.ToArray());
        }
    }
}
#endif
