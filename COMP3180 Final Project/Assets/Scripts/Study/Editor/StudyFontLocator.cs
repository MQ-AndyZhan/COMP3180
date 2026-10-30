#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;

namespace VNStudy.EditorTools
{
    /// <summary>
    /// Shared font lookup for the scene builder and the build-time coverage check.
    /// English-only projects can use TMP's built-in LiberationSans SDF, or add a
    /// Latin font asset. A font named with "StudySans" wins, so a custom font can
    /// always take priority over the TMP default.
    /// </summary>
    public static class StudyFontLocator
    {
        private static readonly string[] PreferredNameFragments =
        {
            "studysans",
            "liberationsans",
            "roboto",
            "inter",
            "opensans",
            "lato",
            "source sans",
            "source-sans",
            "notosans",
            "notosanssc"
        };

        public static TMP_FontAsset FindStudyFont()
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            TMP_FontAsset firstAny = null;
            TMP_FontAsset firstNonFallback = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset candidate = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (candidate == null)
                {
                    continue;
                }

                if (firstAny == null)
                {
                    firstAny = candidate;
                }

                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (fileName.Contains("fallback"))
                {
                    continue;
                }

                if (firstNonFallback == null)
                {
                    firstNonFallback = candidate;
                }

                for (int i = 0; i < PreferredNameFragments.Length; i++)
                {
                    if (fileName.Contains(PreferredNameFragments[i]))
                    {
                        return candidate;
                    }
                }
            }

            return firstNonFallback != null ? firstNonFallback : firstAny;
        }
    }
}
#endif
