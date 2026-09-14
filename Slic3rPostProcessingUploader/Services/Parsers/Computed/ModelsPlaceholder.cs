using System.Globalization;

namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
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
        /// Orca's outer_wall_line_width may be a percentage of the nozzle diameter ("105%"); only an absolute value is
        /// usable, so the generic line_width is the fallback and, failing that, the centreline box is shown as-is.
        /// </summary>
        private static double LineWidth(GcodeSettings settings)
        {
            foreach (var key in new[] { "outer_wall_line_width", "line_width" })
            {
                if (double.TryParse(settings.Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
                    && double.IsFinite(width) && width > 0)
                {
                    return width;
                }
            }

            return 0;
        }

        private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

        private static string Mm(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
