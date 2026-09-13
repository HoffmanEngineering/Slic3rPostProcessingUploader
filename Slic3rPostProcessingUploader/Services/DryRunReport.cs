using Slic3rPostProcessingUploader.Services.Parsers;
using System.Globalization;
using System.Text;

namespace Slic3rPostProcessingUploader.Services;

/// <summary>
/// Console report for <c>--dry-run</c>: the key parsed fields, the rendered note, and the DTO JSON that would have
/// been uploaded. Lets template authors check their output without hitting the API or opening a browser.
/// </summary>
internal static class DryRunReport
{
    public const string DtoFileName = "slic3r-dto.json";

    public static void Write(ConsoleOutput output, CuraSettingDto dto, string? debugPath)
    {
        CuraSettings settings = dto.settings;

        output.Info("Dry run: nothing was uploaded and no browser was opened");
        output.Info($"Slicer: {dto.Slicer} {dto.CuraVersion}");
        output.Info($"Print name: {settings.print_name ?? "(none)"}");
        output.Info($"File name: {settings.file_name ?? "(none)"}");
        output.Info($"Estimated print time: {FormatDuration(settings.estimated_print_time_seconds)} ({settings.estimated_print_time_seconds} s)");
        output.Info($"Material used: {(settings.material_used_mg.HasValue ? FormatGrams(settings.material_used_mg.Value) : "not found")}");
        output.Info($"Filaments: {DescribeFilaments(settings.filamentUsage)}");
        output.Info($"Thumbnail: {DescribeThumbnail(settings.Snapshot)}");

        output.Raw("");
        output.Raw("Rendered note:");
        output.Raw("----------------------------------------");
        output.Raw(settings.note ?? "");
        output.Raw("----------------------------------------");
        output.Raw("");

        if (string.IsNullOrEmpty(debugPath))
        {
            output.Raw("DTO JSON:");
            output.Raw(dto.ToJSON());
        }
        else
        {
            output.Info($"DTO JSON written to {Path.Combine(debugPath, DtoFileName)}");
        }
    }

    private static string DescribeFilaments(List<PrintFilamentSummaryDto>? filaments)
    {
        if (filaments == null || filaments.Count == 0)
        {
            return "none";
        }

        var parts = filaments.Select(f =>
        {
            string label = string.IsNullOrWhiteSpace(f.Notes) ? f.Filament?.DisplayName ?? "Unknown" : f.Notes;
            var amounts = new List<string>();
            double? length = f.LengthInM ?? f.EstimatedLengthInM;
            double? mg = f.AmountMg ?? f.EstimatedAmountMg;
            if (length.HasValue)
            {
                amounts.Add(length.Value.ToString("0.##", CultureInfo.InvariantCulture) + " m");
            }
            if (mg.HasValue)
            {
                amounts.Add(FormatGrams(mg.Value));
            }

            return amounts.Count == 0 ? label : $"{label}: {string.Join(", ", amounts)}";
        });

        return $"{filaments.Count} ({string.Join("; ", parts)})";
    }

    private static string DescribeThumbnail(string? snapshot)
    {
        if (string.IsNullOrEmpty(snapshot))
        {
            return "not found";
        }

        // Base64 encodes 3 bytes per 4 characters; close enough for a size hint without decoding.
        double approxKb = snapshot.Length * 3.0 / 4.0 / 1024.0;
        return $"found ({snapshot.Length} base64 chars, ~{approxKb.ToString("0.#", CultureInfo.InvariantCulture)} KB)";
    }

    private static string FormatGrams(double milligrams) =>
        (milligrams / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + " g";

    private static string FormatDuration(int totalSeconds)
    {
        var time = TimeSpan.FromSeconds(totalSeconds);
        var sb = new StringBuilder();
        if (time.Days > 0) sb.Append(time.Days).Append("d ");
        if (time.Hours > 0) sb.Append(time.Hours).Append("h ");
        if (time.Minutes > 0) sb.Append(time.Minutes).Append("m ");
        if (time.Seconds > 0 || sb.Length == 0) sb.Append(time.Seconds).Append('s');
        return sb.ToString().TrimEnd();
    }
}
