namespace PrintLog.SlicerSettings
{
    public class MaterialDensityGramsPerCubicCm
    {
        public double PLA { get; set; }
        public double ABS { get; set; }
        public double PETG { get; set; }
        public double Nylon { get; set; }
    }

    public static class MaterialDensities
    {
        public static MaterialDensityGramsPerCubicCm Materials = new MaterialDensityGramsPerCubicCm
        {
            PLA = 1.24,
            ABS = 1.04,
            PETG = 1.23,
            Nylon = 1.06,
        };
    }
}
