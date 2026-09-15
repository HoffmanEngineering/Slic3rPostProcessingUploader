using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.Computed
{
    /// <summary>
    /// The locator finds the user's own preset files (user/&lt;account&gt;/{process,filament,machine}/&lt;name&gt;.json) under
    /// one or more Orca-family config roots and reports the values saved in them, so a changed setting can be told
    /// apart from an unsaved plater edit. It gives up (returns null) rather than guess.
    /// </summary>
    [TestClass]
    public class UserPresetLocatorTests
    {
        private string root = null!;
        private readonly List<string> log = [];

        [TestInitialize]
        public void CreateRoot()
        {
            root = Path.Combine(Path.GetTempPath(), "uploader-locator-" + Guid.NewGuid().ToString("N"));
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

        private void WritePreset(string account, string type, string name, string json, string? configRoot = null)
        {
            var dir = Path.Combine(configRoot ?? root, "user", account, type);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, name + ".json"), json);
        }

        private UserPresetLocator Locator(params string[] roots) => new(roots.Length == 0 ? [root] : roots, log.Add);

        private static PresetIdentity User(string type, string name) => new(type, name, IsUserPreset: true);
        private static PresetIdentity System(string type, string name) => new(type, name, IsUserPreset: false);

        [TestMethod]
        public void ShouldReadSavedValuesFromTheProcessPreset()
        {
            WritePreset("default", "process", "Fast - 3DPrintLog", """{"sparse_infill_density": "8%", "wall_loops": "3"}""");

            var saved = Locator().FindSavedValues([User("process", "Fast - 3DPrintLog")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("sparse_infill_density", "8%"));
            Assert.IsTrue(saved.IsSaved("wall_loops", "3"));
            Assert.IsFalse(saved.IsSaved("wall_loops", "4"));
            Assert.IsFalse(saved.IsSaved("enable_support", "1"));
        }

        [TestMethod]
        public void ShouldCombineProcessFilamentAndMachinePresets()
        {
            WritePreset("default", "process", "P", """{"wall_loops": "3"}""");
            WritePreset("default", "filament", "F", """{"nozzle_temperature": ["215"]}""");
            WritePreset("default", "machine", "M", """{"printable_area": ["0x0", "250x0", "250x210", "0x210"]}""");

            var saved = Locator().FindSavedValues([User("process", "P"), User("filament", "F"), User("machine", "M")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("wall_loops", "3"));
            Assert.IsTrue(saved.IsSaved("nozzle_temperature", "215"));
            // Orca writes numeric/point vectors with , in the G-code config block.
            Assert.IsTrue(saved.IsSaved("printable_area", "0x0,250x0,250x210,0x210"));
        }

        [TestMethod]
        public void ShouldNotLookForAFileForASystemPreset()
        {
            WritePreset("default", "process", "P", """{"wall_loops": "3"}""");

            var saved = Locator().FindSavedValues([User("process", "P"), System("filament", "Generic PLA")]);

            Assert.IsNotNull(saved);
            Assert.IsFalse(saved.IsSaved("nozzle_temperature", "215"));
        }

        [TestMethod]
        public void ShouldGiveUpWhenAUserPresetHasNoFile()
        {
            WritePreset("default", "process", "P", """{"wall_loops": "3"}""");

            Assert.IsNull(Locator().FindSavedValues([User("process", "P"), User("filament", "Missing")]));
            Assert.IsTrue(log.Any(l => l.Contains("Missing")), string.Join("\n", log));
        }

        [TestMethod]
        public void ShouldGiveUpWhenTheSameNameExistsUnderTwoAccounts()
        {
            WritePreset("default", "process", "P", """{"wall_loops": "3"}""");
            WritePreset("12345", "process", "P", """{"wall_loops": "4"}""");

            Assert.IsNull(Locator().FindSavedValues([User("process", "P")]));
        }

        [TestMethod]
        public void ShouldGiveUpWhenTheSameNameExistsUnderTwoRoots()
        {
            var other = Path.Combine(root, "other-root");
            WritePreset("default", "process", "P", """{"wall_loops": "3"}""");
            WritePreset("default", "process", "P", """{"wall_loops": "4"}""", other);

            Assert.IsNull(Locator(root, other).FindSavedValues([User("process", "P")]));
        }

        [TestMethod]
        public void ShouldSkipRootsThatDoNotExist()
        {
            WritePreset("default", "process", "P", """{"wall_loops": "3"}""");

            var saved = Locator(Path.Combine(root, "nope"), root).FindSavedValues([User("process", "P")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("wall_loops", "3"));
        }

        [TestMethod]
        public void ShouldSkipARootWhoseUserEntryIsNotADirectory()
        {
            var broken = Path.Combine(root, "broken");
            Directory.CreateDirectory(broken);
            File.WriteAllText(Path.Combine(broken, "user"), "not a directory");
            WritePreset("default", "process", "P", """{"wall_loops": "3"}""");

            var saved = Locator(broken, root).FindSavedValues([User("process", "P")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("wall_loops", "3"));
        }

        [TestMethod]
        public void ShouldGiveUpOnMalformedJson()
        {
            WritePreset("default", "process", "P", "{ not json");

            Assert.IsNull(Locator().FindSavedValues([User("process", "P")]));
        }

        [TestMethod]
        public void ShouldTakeTheFirstOfDuplicateJsonKeys()
        {
            WritePreset("default", "process", "P", """{"wall_loops": "3", "wall_loops": "4"}""");

            var saved = Locator().FindSavedValues([User("process", "P")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("wall_loops", "3"));
            Assert.IsFalse(saved.IsSaved("wall_loops", "4"));
        }

        [TestMethod]
        public void ShouldCompareNumbersAndBooleansAsWritten()
        {
            WritePreset("default", "process", "P", """{"wall_loops": 3, "enable_support": true, "nested": {"a": 1}}""");

            var saved = Locator().FindSavedValues([User("process", "P")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("wall_loops", "3"));
            Assert.IsTrue(saved.IsSaved("enable_support", "true"));
            Assert.IsFalse(saved.IsSaved("nested", "{\"a\": 1}"));
        }

        [TestMethod]
        public void ShouldTrimWhitespaceWhenComparing()
        {
            WritePreset("default", "process", "P", """{"wall_loops": " 3 "}""");

            var saved = Locator().FindSavedValues([User("process", "P")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("wall_loops", "3"));
        }

        [TestMethod]
        public void ShouldMatchArraysAgainstEitherOfOrcasListSeparators()
        {
            // Numeric vectors are ','-joined in the G-code (nozzle_temperature = 215,215); string vectors ';'-joined.
            WritePreset("default", "filament", "F", """{"nozzle_temperature": ["215", "215"], "filament_type": ["PLA", "PETG"]}""");

            var saved = Locator().FindSavedValues([User("filament", "F")]);

            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.IsSaved("nozzle_temperature", "215,215"));
            Assert.IsTrue(saved.IsSaved("filament_type", "PLA;PETG"));
            Assert.IsFalse(saved.IsSaved("nozzle_temperature", "215,220"));
        }

        [DataTestMethod]
        [DataRow("P", true)]
        [DataRow("0.16 High Quality @U1 (0.4 nozzle) - 3DPrintLog", true)]
        [DataRow("", false)]
        [DataRow(".", false)]
        [DataRow("..", false)]
        [DataRow("../P", false)]
        [DataRow(@"..\P", false)]
        [DataRow("sub/P", false)]
        [DataRow(@"sub\P", false)]
        [DataRow(@"C:\P", false)]
        [DataRow("P\0", false)]
        public void ShouldOnlyAcceptPlainFileNamesAsPresetNames(string name, bool expected)
        {
            Assert.AreEqual(expected, UserPresetLocator.IsSafePresetName(name));
        }
    }
}
