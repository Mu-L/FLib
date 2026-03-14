// =================================================={By Qcbf|qcbf@qq.com|2024-08-05}==================================================

using System.Text.RegularExpressions;

namespace FLib.Unity.Editor
{
    public class MarkdownUtility
    {
        public static string ToHtml(string md)
        {
            // Nested Headers in Lists
            md = Regex.Replace(md, @"^(\s*)\- (#{1,6})\s+(.*?)\s*$", m =>
            {
                var indent = m.Groups[1].Value;
                var header = m.Groups[2].Value;
                var content = m.Groups[3].Value;
                var size = 30 - header.Length * 5; // Scale size down with header level
                return $"{new string(' ', 4 * indent.Length)}• <size={size}>{content}</size>";
            }, RegexOptions.Multiline);

            // Headers
            md = Regex.Replace(md, @"^(#{1,6})\s+(.*?)\s*$", m =>
            {
                var header = m.Groups[1].Value;
                var content = m.Groups[2].Value;
                var size = 30 - header.Length * 5; // Example to scale size down with header level
                return $"<size={size}>{content}</size>";
            }, RegexOptions.Multiline);

            // Lists
            md = Regex.Replace(md, @"^( *)\- (.*?)$", m => new string(' ', 4 * m.Groups[1].Value.Length) + "• " + m.Groups[2].Value, RegexOptions.Multiline);

            // Bold and Italic
            md = Regex.Replace(md, @"(\*\*|__)(.*?)\1|\*(.*?)\*|_(.*?)_", m =>
            {
                if (m.Groups[1].Success) // Bold ** or __
                    return $"<b>{m.Groups[2].Value}</b>";
                if (m.Groups[3].Success) // Italic *
                    return $"<i>{m.Groups[3].Value}</i>";
                // Italic _
                return $"<i>{m.Groups[4].Value}</i>";
            }, RegexOptions.Singleline);

            // Inline code
            md = Regex.Replace(md, "`(.*?)`", "<color=#cccccc><i>$1</i></color>", RegexOptions.Multiline);

            // Links
            md = Regex.Replace(md, @"\[(.*?)\]\((.*?)\)", "<link=$2>$1</link>", RegexOptions.Multiline);

            // Blockquotes
            md = Regex.Replace(md, @"^\>(.*?)$", "<color=grey>$1</color>", RegexOptions.Multiline);

            // Horizontal rules
            md = Regex.Replace(md, "^-{3,}$", "<line-height=0.5>\n</line-height>", RegexOptions.Multiline);

            return md;
        }
    }
}
