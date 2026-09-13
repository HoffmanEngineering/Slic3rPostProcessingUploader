using System.Globalization;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    /// <summary>
    /// Turns a slicer filament colour (e.g. "#E72F1D") into a basic colour name (e.g. "Red") so multi-colour
    /// prints can be matched to the right spool without decoding hex values.
    /// </summary>
    internal static class FilamentColor
    {
        /// <summary>
        /// Returns a basic colour name for a 6-digit hex colour, or null if the input is not a valid hex colour.
        /// </summary>
        public static string? Describe(string? hex)
        {
            if (!TryParseRgb(hex, out var r, out var g, out var b))
            {
                return null;
            }

            var (hue, saturation, value) = ToHsv(r, g, b);

            if (value < 0.15)
            {
                return "Black";
            }

            if (saturation < 0.12)
            {
                return value > 0.85 ? "White" : "Grey";
            }

            if (hue < 15 || hue >= 345)
            {
                return "Red";
            }

            if (hue < 45)
            {
                return value < 0.6 ? "Brown" : "Orange";
            }

            if (hue < 70)
            {
                return "Yellow";
            }

            if (hue < 165)
            {
                return "Green";
            }

            if (hue < 195)
            {
                return "Teal";
            }

            if (hue < 255)
            {
                return "Blue";
            }

            if (hue < 290)
            {
                return "Purple";
            }

            return value < 0.65 ? "Purple" : "Pink";
        }

        private static bool TryParseRgb(string? hex, out int r, out int g, out int b)
        {
            r = g = b = 0;

            var trimmed = hex?.Trim().TrimStart('#');
            if (trimmed is null || trimmed.Length != 6)
            {
                return false;
            }

            if (!int.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                return false;
            }

            r = (rgb >> 16) & 0xFF;
            g = (rgb >> 8) & 0xFF;
            b = rgb & 0xFF;
            return true;
        }

        /// <summary>
        /// Hue in degrees [0, 360), saturation and value in [0, 1].
        /// </summary>
        private static (double hue, double saturation, double value) ToHsv(int r, int g, int b)
        {
            double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
            double max = Math.Max(rf, Math.Max(gf, bf));
            double min = Math.Min(rf, Math.Min(gf, bf));
            double delta = max - min;

            double hue = 0;
            if (delta > 0)
            {
                if (max == rf)
                {
                    hue = 60 * (((gf - bf) / delta) % 6);
                }
                else if (max == gf)
                {
                    hue = 60 * ((bf - rf) / delta + 2);
                }
                else
                {
                    hue = 60 * ((rf - gf) / delta + 4);
                }

                if (hue < 0)
                {
                    hue += 360;
                }
            }

            double saturation = max == 0 ? 0 : delta / max;
            return (hue, saturation, max);
        }
    }
}
