using Slic3rPostProcessingUploader.Services.Parsers;

namespace Slic3rPostProcessingUploader.Services;

/// <summary>
/// The bits of the upload that come from the environment rather than the G-code: which file name to report,
/// the print title derived from it, and the URL the browser is sent to afterwards.
/// </summary>
internal static class PrintMetadata
{
    /// <summary>
    /// Fills in <c>file_name</c>, <c>print_name</c> and <c>PluginVersion</c> on the parsed DTO.
    /// </summary>
    /// <param name="inputFile">The G-code path the slicer passed on the command line.</param>
    /// <param name="outputName">
    /// The value of <c>SLIC3R_PP_OUTPUT_NAME</c>, if the slicer set it. PrusaSlicer-family slicers post-process a
    /// temporary file, so this is the only place the final file name is available.
    /// </param>
    public static void Apply(CuraSettingDto dto, string inputFile, string? outputName, string pluginVersion)
    {
        dto.settings.file_name = Path.GetFileName(outputName ?? inputFile);
        dto.settings.print_name = new TitleService().GetTitle(Path.GetFileNameWithoutExtension(dto.settings.file_name));
        dto.PluginVersion = pluginVersion;
    }

    public static string BuildPrintUrl(string newPrintUrl, CuraSettingDto dto, string settingId) =>
        $"{newPrintUrl}?cura_version={dto.CuraVersion}&plugin_version={dto.PluginVersion}&settingId={settingId}";
}
