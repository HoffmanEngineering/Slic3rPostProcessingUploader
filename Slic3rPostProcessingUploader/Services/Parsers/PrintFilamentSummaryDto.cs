namespace Slic3rPostProcessingUploader.Services.Parsers
{
    public class PrintFilamentSummaryDto
    {
        /// <summary>
        /// GUID
        /// </summary>
        public string? Id { get; set; }
        public required FilamentSummary Filament { get; set; }

        public double? AmountMg { get; set; }
        public double? LengthInM { get; set; }
        public double? VolumeMl { get; set; }

        public double? EstimatedAmountMg { get; set; }
        public double? EstimatedLengthInM { get; set; }
        public double? EstimatedVolumeMl { get; set; }

        public PrintFilamentSourceMeasurement Source { get; set; }
        public PrintFilamentSourceMeasurement EstimatedSource { get; set; }

        public required string Notes { get; set; }
    }
}
