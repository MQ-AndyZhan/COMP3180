using System.Collections.Generic;

namespace VNStudy
{
    public enum Speaker
    {
        Player,
        Friend,
        Narrator
    }

    /// <summary>
    /// Metadata copied from one visible Ink unit's tags. A null ChoicePointId means
    /// the unit is ordinary narrative rather than a decision question.
    /// </summary>
    public sealed class StudyLineMeta
    {
        public Speaker Speaker;
        public string LineId;
        public string ChoicePointId;
        public bool IsEnding;
    }

    /// <summary>
    /// Parses and validates the tag contract from the design document:
    /// # speaker:player|friend|narrator # line:... [# choice_point:cpNN] [# end:common]
    /// </summary>
    public static class StudyTagParser
    {
        public static bool TryParse(IReadOnlyList<string> tags, out StudyLineMeta meta, out string error)
        {
            meta = null;
            error = null;

            if (tags == null || tags.Count == 0)
            {
                error = "缺少 tag：每个可见单元都需要 # speaker:... # line:...";
                return false;
            }

            Speaker? speaker = null;
            string lineId = null;
            string choicePointId = null;
            string endValue = null;

            for (int i = 0; i < tags.Count; i++)
            {
                string raw = tags[i];
                if (raw == null)
                {
                    continue;
                }

                string tag = raw.Trim();
                if (tag.Length == 0)
                {
                    continue;
                }

                int colon = tag.IndexOf(':');
                if (colon <= 0)
                {
                    error = $"tag 格式错误：'{tag}'（应为 key:value）";
                    return false;
                }

                string key = tag.Substring(0, colon).Trim().ToLowerInvariant();
                string value = tag.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "speaker":
                        if (speaker.HasValue)
                        {
                            error = "同一单元出现两个 speaker tag";
                            return false;
                        }

                        Speaker parsedSpeaker;
                        if (!TryParseSpeaker(value, out parsedSpeaker))
                        {
                            error = $"未知 speaker：'{value}'（只允许 player / friend / narrator）";
                            return false;
                        }

                        speaker = parsedSpeaker;
                        break;

                    case "line":
                        if (lineId != null)
                        {
                            error = "同一单元出现两个 line tag";
                            return false;
                        }

                        if (value.Length == 0)
                        {
                            error = "line tag 的值不能为空";
                            return false;
                        }

                        lineId = value;
                        break;

                    case "choice_point":
                        if (choicePointId != null)
                        {
                            error = "同一单元出现两个 choice_point tag";
                            return false;
                        }

                        if (value.Length == 0)
                        {
                            error = "choice_point tag 的值不能为空";
                            return false;
                        }

                        choicePointId = value.ToLowerInvariant();
                        break;

                    case "end":
                        if (endValue != null)
                        {
                            error = "同一单元出现两个 end tag";
                            return false;
                        }

                        if (value.Length == 0)
                        {
                            error = "end tag 的值不能为空";
                            return false;
                        }

                        endValue = value.ToLowerInvariant();
                        break;

                    default:
                        error = $"未知 tag：'{key}'";
                        return false;
                }
            }

            if (!speaker.HasValue)
            {
                error = "缺少 speaker tag";
                return false;
            }

            if (lineId == null)
            {
                error = "缺少 line tag";
                return false;
            }

            if (choicePointId != null && endValue != null)
            {
                error = "choice_point 与 end 不能出现在同一单元";
                return false;
            }

            if (endValue != null && endValue != "common")
            {
                error = $"end 只允许 common，实际为 '{endValue}'";
                return false;
            }

            meta = new StudyLineMeta
            {
                Speaker = speaker.Value,
                LineId = lineId,
                ChoicePointId = choicePointId,
                IsEnding = endValue == "common"
            };
            return true;
        }

        private static bool TryParseSpeaker(string value, out Speaker speaker)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "player":
                    speaker = Speaker.Player;
                    return true;
                case "friend":
                    speaker = Speaker.Friend;
                    return true;
                case "narrator":
                    speaker = Speaker.Narrator;
                    return true;
                default:
                    speaker = Speaker.Narrator;
                    return false;
            }
        }
    }
}
