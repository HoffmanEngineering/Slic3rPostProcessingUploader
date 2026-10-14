namespace PrintLog.SlicerSettings
{
    /// <summary>
    /// Finds embedded thumbnail blocks in one forward pass. It returns exactly what the regex
    /// <c>thumbnail(?:_JPG)? begin (\d+)x(\d+)[\sa-zA-Z\d]*([\S\s]*?); thumbnail end</c> (IgnoreCase) matched, but in
    /// linear time: the regex rescanned to the end of the text from every <c>thumbnail begin</c> that had no end tag
    /// after it, which made a run of unterminated blocks quadratic (43 s for 1 MB).
    /// </summary>
    internal static class ThumbnailScanner
    {
        internal readonly record struct Block(string Width, string Height, string Payload);

        private const string Thumbnail = "thumbnail";
        private const string JpgSuffix = "_JPG";
        private const string Begin = " begin ";
        private const string End = "; thumbnail end";

        public static List<Block> Find(string text)
        {
            var blocks = new List<Block>();
            int position = 0;

            while (true)
            {
                int start = text.IndexOf(Thumbnail, position, StringComparison.OrdinalIgnoreCase);
                if (start < 0)
                {
                    return blocks;
                }

                if (!TryReadOpening(text, start + Thumbnail.Length, out var width, out var height, out int payloadStart))
                {
                    position = start + 1;
                    continue;
                }

                // The regex's lazy payload stops at the first end tag after the opening. When there is none, no later
                // opening can have one either, so the scan is over (this is where the regex went quadratic).
                int end = text.IndexOf(End, payloadStart, StringComparison.OrdinalIgnoreCase);
                if (end < 0)
                {
                    return blocks;
                }

                blocks.Add(new Block(width, height, text[payloadStart..end]));
                position = end + End.Length;
            }
        }

        /// <summary>
        /// Matches <c>(?:_JPG)? begin (\d+)x(\d+)[\sa-zA-Z\d]*</c> at <paramref name="index"/>. The trailing run can never
        /// contain the <c>;</c> that starts the end tag, so the regex never had a reason to give any of it back.
        /// </summary>
        private static bool TryReadOpening(string text, int index, out string width, out string height, out int payloadStart)
        {
            width = height = string.Empty;
            payloadStart = 0;

            // The regex tries "_JPG begin " first and then " begin "; they cannot both fit at the same index.
            if (At(text, index, JpgSuffix) && At(text, index + JpgSuffix.Length, Begin))
            {
                index += JpgSuffix.Length + Begin.Length;
            }
            else if (At(text, index, Begin))
            {
                index += Begin.Length;
            }
            else
            {
                return false;
            }

            int widthEnd = SkipDigits(text, index);
            if (widthEnd == index || widthEnd >= text.Length || char.ToLowerInvariant(text[widthEnd]) != 'x')
            {
                return false;
            }

            int heightEnd = SkipDigits(text, widthEnd + 1);
            if (heightEnd == widthEnd + 1)
            {
                return false;
            }

            width = text[index..widthEnd];
            height = text[(widthEnd + 1)..heightEnd];

            payloadStart = heightEnd;
            while (payloadStart < text.Length && IsRunChar(text[payloadStart]))
            {
                payloadStart++;
            }

            return true;
        }

        private static bool At(string text, int index, string value) =>
            index + value.Length <= text.Length && text.AsSpan(index, value.Length).Equals(value, StringComparison.OrdinalIgnoreCase);

        // Regex \d is any Unicode decimal digit, which char.IsDigit also means.
        private static int SkipDigits(string text, int index)
        {
            while (index < text.Length && char.IsDigit(text[index]))
            {
                index++;
            }

            return index;
        }

        // [\sa-zA-Z\d]
        private static bool IsRunChar(char c) => char.IsWhiteSpace(c) || char.IsAsciiLetter(c) || char.IsDigit(c);
    }
}
