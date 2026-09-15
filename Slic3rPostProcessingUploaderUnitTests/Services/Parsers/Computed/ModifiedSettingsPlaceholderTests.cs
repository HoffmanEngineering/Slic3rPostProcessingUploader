using Slic3rPostProcessingUploader.Services.Parsers;
using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.Computed
{
    [TestClass]
    public class ModifiedSettingsPlaceholderTests
    {
        private static string Render(string gcode)
        {
            var settings = GcodeSettings.Parse(gcode, "=");
            return ModifiedSettingsPlaceholder.Instance.Render(new ComputedContext(settings, ParseOptions.InMemory(gcode)));
        }

        [TestMethod]
        public void ShouldListEachChangedKeyWithItsValueUnderItsOwnHeading()
        {
            var note = Render(
                "; different_settings_to_system = sparse_infill_density;wall_loops;;\n" +
                "; inherits_group = \"0.16 High Quality @U1\";;\n" +
                "; sparse_infill_density = 8%\n" +
                "; wall_loops = 3\n");

            // The value carries its heading and a trailing newline so the template drops the section when empty.
            Assert.AreEqual("Profile Changes:\n  Changed from \"0.16 High Quality @U1\": sparse_infill_density = 8%, wall_loops = 3\n", note);
        }

        [TestMethod]
        public void ShouldReportEveryChangeAsUnsavedWhenAllPresetsAreSystemPresets()
        {
            // Nothing can be saved into a system preset, so with no user preset anywhere the split is certain.
            var note = Render(
                "; different_settings_to_system = wall_loops\n" +
                "; inherits_group = ;;\n" +
                "; wall_loops = 3\n");

            Assert.AreEqual("Profile Changes:\n  Changed from profile:\n    Unsaved changes: wall_loops = 3\n", note);
        }

        [TestMethod]
        public void ShouldShowTheFlatListWhenInheritsGroupIsMissing()
        {
            var note = Render("; different_settings_to_system = wall_loops\n; wall_loops = 3\n");

            Assert.AreEqual("Profile Changes:\n  Changed from profile: wall_loops = 3\n", note);
        }

        [TestMethod]
        public void ShouldMarkKeysOrcaDoesNotEchoIntoTheGcode()
        {
            var note = Render("; different_settings_to_system = some_hidden_key\n; inherits_group = \"P\"\n");

            Assert.AreEqual("Profile Changes:\n  Changed from \"P\": some_hidden_key = (not in G-code)\n", note);
        }

        [TestMethod]
        public void ShouldRenderNothingWhenOnlyPostProcessChanged()
        {
            Assert.AreEqual(string.Empty, Render("; different_settings_to_system = post_process;;\n; inherits_group = \"P\";;\n"));
        }

        [TestMethod]
        public void ShouldRenderNothingWhenTheSettingIsAbsent()
        {
            Assert.AreEqual(string.Empty, Render("; layer_height = 0.2\n"));
        }

        // ---- Level 2: saved vs unsaved, using the user's preset files on disk ----

        private string root = null!;

        [TestInitialize]
        public void CreateRoot()
        {
            root = Path.Combine(Path.GetTempPath(), "uploader-modified-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TestCleanup]
        public void DeleteRoot()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private void WritePreset(string type, string name, string json)
        {
            var dir = Path.Combine(root, "user", "default", type);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, name + ".json"), json);
        }

        private string RenderWithRoots(string gcode, params string[] roots)
        {
            var settings = GcodeSettings.Parse(gcode, "=");
            var options = new ParseOptions(() => Stream.Null, _ => { }, () => roots);
            return ModifiedSettingsPlaceholder.Instance.Render(new ComputedContext(settings, options));
        }

        private const string TwoChangesGcode =
            "; different_settings_to_system = sparse_infill_density;wall_loops;nozzle_temperature;;\n" +
            "; inherits_group = \"0.16 High Quality @U1\";\"Generic PLA\";\n" +
            "; print_settings_id = 0.16 High Quality @U1 - 3DPrintLog\n" +
            "; filament_settings_id = \"My PLA\"\n" +
            "; printer_settings_id = U1 0.4 nozzle\n" +
            "; sparse_infill_density = 8%\n" +
            "; wall_loops = 3\n" +
            "; nozzle_temperature = 215\n";

        [TestMethod]
        public void ShouldListOnlyTheUnsavedChangesWhenThePresetFilesAreFound()
        {
            WritePreset("process", "0.16 High Quality @U1 - 3DPrintLog", """{"sparse_infill_density": "8%"}""");
            WritePreset("filament", "My PLA", """{"nozzle_temperature": ["215"]}""");

            var note = RenderWithRoots(TwoChangesGcode, root);

            Assert.AreEqual(
                "Profile Changes:\n" +
                "  Changed from \"0.16 High Quality @U1\":\n" +
                "    Unsaved changes: wall_loops = 3\n",
                note);
        }

        [TestMethod]
        public void ShouldOmitTheSectionWhenEverythingIsSaved()
        {
            WritePreset("process", "0.16 High Quality @U1 - 3DPrintLog", """{"sparse_infill_density": "8%", "wall_loops": "3"}""");
            WritePreset("filament", "My PLA", """{"nozzle_temperature": ["215"]}""");

            var note = RenderWithRoots(TwoChangesGcode, root);

            Assert.AreEqual(string.Empty, note);
        }

        [TestMethod]
        public void ShouldTreatASavedValueThatDiffersFromTheGcodeAsUnsaved()
        {
            WritePreset("process", "0.16 High Quality @U1 - 3DPrintLog", """{"sparse_infill_density": "15%", "wall_loops": "3"}""");
            WritePreset("filament", "My PLA", """{"nozzle_temperature": ["215"]}""");

            var note = RenderWithRoots(TwoChangesGcode, root);

            StringAssert.Contains(note, "    Unsaved changes: sparse_infill_density = 8%");
        }

        [TestMethod]
        public void ShouldFallBackToTheFlatListWhenAPresetFileIsMissing()
        {
            WritePreset("process", "0.16 High Quality @U1 - 3DPrintLog", """{"sparse_infill_density": "8%"}""");

            var note = RenderWithRoots(TwoChangesGcode, root);

            Assert.AreEqual("Profile Changes:\n  Changed from \"0.16 High Quality @U1\": sparse_infill_density = 8%, wall_loops = 3, nozzle_temperature = 215\n", note);
        }

        [TestMethod]
        public void ShouldFallBackToTheFlatListWhenTheConfigRootsCannotBeResolved()
        {
            var settings = GcodeSettings.Parse(TwoChangesGcode, "=");
            var log = new List<string>();
            var options = new ParseOptions(() => Stream.Null, log.Add, () => throw new InvalidOperationException("no home"));

            var note = ModifiedSettingsPlaceholder.Instance.Render(new ComputedContext(settings, options));

            StringAssert.StartsWith(note, "Profile Changes:\n  Changed from \"0.16 High Quality @U1\": sparse_infill_density");
            Assert.IsTrue(log.Any(l => l.Contains("no home")), string.Join("\n", log));
        }

        [TestMethod]
        public void ShouldNotTouchTheDiskWhenNothingChanged()
        {
            var settings = GcodeSettings.Parse("; different_settings_to_system = post_process;;\n", "=");
            var options = new ParseOptions(() => Stream.Null, _ => { }, () => throw new InvalidOperationException("must not be called"));

            Assert.AreEqual(string.Empty, ModifiedSettingsPlaceholder.Instance.Render(new ComputedContext(settings, options)));
        }
    }
}
