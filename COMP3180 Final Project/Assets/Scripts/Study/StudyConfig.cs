using System;
using UnityEngine;

namespace VNStudy
{
    /// <summary>
    /// One experimental version: the player-visible label, the expected number of
    /// choices per decision point, and the compiled Ink JSON asset.
    /// </summary>
    [Serializable]
    public class VersionConfig
    {
        [Tooltip("玩家可见的版本标签，例如 A / B / C。界面只显示这个值。")]
        public string label = "A";

        [Tooltip("该版本每个决策点应出现的选项数量（A=2, B=3, C=5）。")]
        public int expectedChoiceCount = 2;

        [Tooltip("Ink Unity Integration 编译生成的 JSON TextAsset，不是 .ink 源文件。")]
        public TextAsset inkJson;
    }

    /// <summary>
    /// The speaker tag values are fixed by the design document. Display names are
    /// shared by all three versions and come from the controller's inspector fields.
    /// </summary>
    public static class SpeakerCatalog
    {
        public static string GetDisplayName(Speaker speaker, string playerName, string friendName)
        {
            switch (speaker)
            {
                case Speaker.Player:
                    return playerName ?? string.Empty;
                case Speaker.Friend:
                    return friendName ?? string.Empty;
                default:
                    return string.Empty;
            }
        }
    }
}
