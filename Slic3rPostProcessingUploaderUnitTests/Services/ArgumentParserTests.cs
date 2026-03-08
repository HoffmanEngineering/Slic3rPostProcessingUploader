using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers;
using Snapshooter.MSTest;
using System.IO;

namespace Slic3rPostProcessingUploaderUnitTests.Services
{
    [TestClass]
    public class ArgumentParserTests
    {
        [TestMethod]
        public void ShouldDisplayHelpDocs()
        {
            var parser = new ArgumentParser([]);

            string allConsoleOutput;

            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);

                parser.DisplayHelpDocs();

                allConsoleOutput = sw.ToString();
            }

            Assert.IsNotNull(allConsoleOutput);

            Snapshot.Match(allConsoleOutput);
        }

        #region InputFile Tests

        [TestMethod]
        public void Constructor_WithNoArguments_InputFileIsNull()
        {
            var parser = new ArgumentParser([]);
            Assert.IsNull(parser.InputFile);
        }

        [TestMethod]
        public void Constructor_WithOnlyFlags_InputFileIsNull()
        {
            var parser = new ArgumentParser(["--default", "--opt-out-telemetry"]);
            Assert.IsNull(parser.InputFile);
        }

        [TestMethod]
        public void Constructor_WithInputFile_SetsInputFile()
        {
            var parser = new ArgumentParser(["--default", "myfile.gcode"]);
            Assert.AreEqual("myfile.gcode", parser.InputFile);
        }

        [TestMethod]
        public void Constructor_WithHelpFlag_InputFileIsNull()
        {
            var parser = new ArgumentParser(["--help"]);
            Assert.IsNull(parser.InputFile);
        }

        [TestMethod]
        public void Constructor_WithShortHelpFlag_InputFileIsNull()
        {
            var parser = new ArgumentParser(["-h"]);
            Assert.IsNull(parser.InputFile);
        }

        #endregion

        #region Template Flag Tests

        [TestMethod]
        public void Constructor_WithTemplateAndPath_SetsTemplatePath()
        {
            var parser = new ArgumentParser(["--template", "C:\\templates\\custom.txt", "input.gcode"]);
            Assert.AreEqual("C:\\templates\\custom.txt", parser.NoteTemplatePath);
            Assert.IsFalse(parser.UseDefaultNoteTemplate);
            Assert.IsFalse(parser.UseFullNoteTemplate);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithTemplateAsLastArgument_ThrowsException()
        {
            new ArgumentParser(["input.gcode", "--template"]);
        }

        [TestMethod]
        public void Constructor_WithDefaultFlag_UsesDefaultTemplate()
        {
            var parser = new ArgumentParser(["--default", "input.gcode"]);
            Assert.IsTrue(parser.UseDefaultNoteTemplate);
            Assert.IsFalse(parser.UseFullNoteTemplate);
        }

        [TestMethod]
        public void Constructor_WithFullFlag_UsesFullTemplate()
        {
            var parser = new ArgumentParser(["--full", "input.gcode"]);
            Assert.IsFalse(parser.UseDefaultNoteTemplate);
            Assert.IsTrue(parser.UseFullNoteTemplate);
        }

        #endregion

        #region Debug Flag Tests

        [TestMethod]
        public void Constructor_WithDebugAndPath_SetsDebugPath()
        {
            var parser = new ArgumentParser(["--debug", "C:\\debug\\", "input.gcode"]);
            Assert.AreEqual("C:\\debug\\", parser.DebugPath);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithDebugAsLastArgument_ThrowsException()
        {
            new ArgumentParser(["input.gcode", "--debug"]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithDebugPathStartingWithDashes_ThrowsException()
        {
            new ArgumentParser(["--debug", "--invalid", "input.gcode"]);
        }

        #endregion

        #region Other Flags Tests

        [TestMethod]
        public void Constructor_WithLocalDevFlag_SetsLocalDev()
        {
            var parser = new ArgumentParser(["--local-dev", "input.gcode"]);
            Assert.IsTrue(parser.UseLocalDev);
        }

        [TestMethod]
        public void Constructor_WithOptOutTelemetryFlag_DisablesTelemetry()
        {
            var parser = new ArgumentParser(["--opt-out-telemetry", "input.gcode"]);
            Assert.IsTrue(parser.DisableTelemetry);
        }

        [TestMethod]
        public void Constructor_WithHelpFlag_SetsDisplayHelp()
        {
            var parser = new ArgumentParser(["--help"]);
            Assert.IsTrue(parser.DisplayHelp);
        }

        [TestMethod]
        public void Constructor_WithShortHelpFlag_SetsDisplayHelp()
        {
            var parser = new ArgumentParser(["-h"]);
            Assert.IsTrue(parser.DisplayHelp);
        }

        [TestMethod]
        public void Constructor_WithVersionFlag_SetsDisplayVersion()
        {
            var parser = new ArgumentParser(["--version"]);
            Assert.IsTrue(parser.DisplayVersion);
        }

        [TestMethod]
        public void Constructor_WithShortVersionFlag_SetsDisplayVersion()
        {
            var parser = new ArgumentParser(["-v"]);
            Assert.IsTrue(parser.DisplayVersion);
        }

        [TestMethod]
        public void Constructor_WithMultipleFlags_SetsAllFlags()
        {
            var parser = new ArgumentParser(["--full", "--local-dev", "--opt-out-telemetry", "input.gcode"]);
            Assert.IsFalse(parser.UseDefaultNoteTemplate);
            Assert.IsTrue(parser.UseFullNoteTemplate);
            Assert.IsTrue(parser.UseLocalDev);
            Assert.IsTrue(parser.DisableTelemetry);
            Assert.AreEqual("input.gcode", parser.InputFile);
        }

        #endregion

        #region Default Values Tests

        [TestMethod]
        public void Constructor_WithNoFlags_HasCorrectDefaults()
        {
            var parser = new ArgumentParser(["input.gcode"]);
            Assert.IsTrue(parser.UseDefaultNoteTemplate);
            Assert.IsFalse(parser.UseFullNoteTemplate);
            Assert.IsFalse(parser.UseLocalDev);
            Assert.IsFalse(parser.DisableTelemetry);
            Assert.IsFalse(parser.DisplayHelp);
            Assert.IsNull(parser.NoteTemplatePath);
            Assert.IsNull(parser.DebugPath);
        }

        #endregion

        #region AppMode Tests

        [TestMethod]
        public void Constructor_WithNoArguments_SetsWizardMode()
        {
            var parser = new ArgumentParser([]);
            Assert.AreEqual(AppMode.Wizard, parser.Mode);
        }

        [TestMethod]
        public void Constructor_WithInstallSubcommand_SetsInstallMode()
        {
            var parser = new ArgumentParser(["install"]);
            Assert.AreEqual(AppMode.Install, parser.Mode);
        }

        [TestMethod]
        public void Constructor_WithUninstallSubcommand_SetsUninstallMode()
        {
            var parser = new ArgumentParser(["uninstall"]);
            Assert.AreEqual(AppMode.Uninstall, parser.Mode);
        }

        [TestMethod]
        public void Constructor_WithInstallAndDryRun_SetsDryRun()
        {
            var parser = new ArgumentParser(["install", "--dry-run"]);
            Assert.AreEqual(AppMode.Install, parser.Mode);
            Assert.IsTrue(parser.IsDryRun);
        }

        [TestMethod]
        public void Constructor_WithUninstallAndDryRun_SetsDryRun()
        {
            var parser = new ArgumentParser(["uninstall", "--dry-run"]);
            Assert.AreEqual(AppMode.Uninstall, parser.Mode);
            Assert.IsTrue(parser.IsDryRun);
        }

        [TestMethod]
        public void Constructor_WithGcodeFile_SetsPostProcessMode()
        {
            var parser = new ArgumentParser(["--full", "myfile.gcode"]);
            Assert.AreEqual(AppMode.PostProcess, parser.Mode);
        }

        [TestMethod]
        public void Constructor_WithInstallMode_IsDryRunDefaultsFalse()
        {
            var parser = new ArgumentParser(["install"]);
            Assert.IsFalse(parser.IsDryRun);
        }

        [TestMethod]
        public void Constructor_WithInstallAndExtraArgs_InputFileRemainsNull()
        {
            var parser = new ArgumentParser(["install", "myfile.gcode", "--full"]);
            Assert.AreEqual(AppMode.Install, parser.Mode);
            Assert.IsNull(parser.InputFile);
        }

        #endregion
    }
}
