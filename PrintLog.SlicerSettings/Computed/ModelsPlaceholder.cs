using System.Globalization;

namespace PrintLog.SlicerSettings.Computed
{
    /// <summary>
    /// {{models}}: the objects on the plate and the size each was printed at, so a print can be recreated at the same
    /// scale later. One line per distinct (name, size), with the number of copies:
    /// <code>
    /// 3DBenchy.drc  ×16   60.0 × 31.0 × 48.0 mm
    /// Cube          ×2    27.0 × 27.0 × 26.9 mm
    /// </code>
    /// Sizes are outer dimensions: the extrusion centreline box plus one outer-wall line width in X and Y.
    /// </summary>
    internal static class ModelsPlaceholder
    {
        public static readonly ComputedPlaceholder Instance = new("models", Render);

        private sealed record Size(string Name, double Width, double Depth, double Height);

        private sealed class Group(Size size)
        {
            public Size Size { get; } = size;
            public int Count { get; set; }
        }

        private static string Render(ComputedContext context)
        {
            IReadOnlyList<ObjectBounds> bounds;
            try
            {
                using var stream = context.OpenFullGcode();
                bounds = ObjectBoundsScanner.Scan(stream, context.DebugLog);
            }
            catch (Exception e)
            {
                context.DebugLog($"Could not measure the objects in the G-code: {e.Message}");
                return string.Empty;
            }

            if (bounds.Count == 0)
            {
                return string.Empty;
            }

            double lineWidth = LineWidth(context.Settings);
            var groups = new List<Group>();
            foreach (var box in bounds)
            {
                var size = new Size(box.Name, Round(box.MaxX - box.MinX + lineWidth), Round(box.MaxY - box.MinY + lineWidth), Round(box.MaxZ));
                var group = groups.FirstOrDefault(g => g.Size == size);
                if (group == null)
                {
                    groups.Add(group = new Group(size));
                }

                group.Count++;
            }

            int nameWidth = groups.Max(g => g.Size.Name.Length);
            int countWidth = groups.Max(g => g.Count.ToString(CultureInfo.InvariantCulture).Length);
            var lines = groups.Select(g =>
                $"  {g.Size.Name.PadRight(nameWidth)}  ×{g.Count.ToString(CultureInfo.InvariantCulture).PadRight(countWidth)}   {Mm(g.Size.Width)} × {Mm(g.Size.Depth)} × {Mm(g.Size.Height)} mm\n");

            // The value carries its own heading and ends with a newline, so a template can put the placeholder on a
            // line of its own and the whole section disappears when there are no objects.
            return "Models:\n" + string.Concat(lines);
        }

        /// <summary>
        /// The outer wall's line width the way Orca resolves it (PrintRegion::flow, Flow::new_from_config_width):
        /// outer_wall_line_width, or line_width when that is 0; a percentage is of the (first) nozzle diameter, as Orca
        /// 2.5's system profiles write it ("100%"); and when both are 0, Orca's automatic width, 1.125 × nozzle diameter.
        /// Without a nozzle diameter a percentage cannot be resolved, so an absolute line_width is the fallback and,
        /// failing that, the centreline box is shown as-is.
        /// </summary>
        private static double LineWidth(GcodeSettings settings)
        {
            var nozzle = FirstNumber(settings.Get("nozzle_diameter"));
            var width = Resolve(settings.Get("outer_wall_line_width"), nozzle);
            if (width is null or 0)
            {
                width = Resolve(settings.Get("line_width"), nozzle);
            }

            if (width == 0)
            {
                width = nozzle is > 0 ? 1.125 * nozzle.Value : null;
            }

            return width is > 0 && double.IsFinite(width.Value) ? width.Value : 0;
        }

        /// <summary>
        /// A "mm or %" value: the width in mm, 0 when it is 0 (meaning "use the default"), or null when it is missing or
        /// cannot be resolved.
        /// </summary>
        private static double? Resolve(string value, double? nozzle)
        {
            value = value.Trim();
            bool percent = value.EndsWith('%');
            var number = FirstNumber(percent ? value[..^1] : value);
            if (number is null or < 0)
            {
                return null;
            }

            if (number == 0)
            {
                return 0;
            }

            if (!percent)
            {
                return number;
            }

            return nozzle is > 0 ? number.Value / 100 * nozzle.Value : null;
        }

        /// <summary>The first entry of a value such as "0.4" or "0.4,0.6", or null when it is not a finite number.</summary>
        private static double? FirstNumber(string value)
        {
            int comma = value.IndexOf(',');
            var first = (comma >= 0 ? value[..comma] : value).Trim();
            return double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)
                ? number
                : null;
        }

        private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

        private static string Mm(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
