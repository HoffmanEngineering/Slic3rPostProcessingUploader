namespace PrintLog.SlicerSettings
{
    internal sealed class MaterialDensityGramsPerCubicCm
    {
        public double PLA { get; init; }
        public double ABS { get; init; }
        public double PETG { get; init; }
        public double Nylon { get; init; }
    }

    /// <summary>
    /// Last-resort densities for G-code that carries no <c>filament_density</c>.
    /// </summary>
    internal static class MaterialDensities
    {
        public static readonly MaterialDensityGramsPerCubicCm Materials = new()
        {
            PLA = 1.24,
            ABS = 1.04,
            PETG = 1.23,
            Nylon = 1.06,
        };
    }
}
