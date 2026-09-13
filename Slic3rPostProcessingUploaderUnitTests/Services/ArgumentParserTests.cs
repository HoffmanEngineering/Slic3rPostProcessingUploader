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
        public void Constructor_WithTemplateAsLastArgument_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["input.gcode", "--template"]));
            Assert.AreEqual("--template requires a path argument", ex.Message);
        }

        [TestMethod]
        public void Constructor_WithEmptyTemplatePath_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--template", "", "input.gcode"]));
            Assert.AreEqual("Note template path cannot be null or empty", ex.Message);
        }

        [TestMethod]
        public void Constructor_WithTemplatePathAsFinalArgument_InputFileIsNull()
        {
            // No G-code path was given at all; the value belongs to --template, so it must not be mistaken for the input file.
            var parser = new ArgumentParser(["--template", "my.txt"]);
            Assert.AreEqual("my.txt", parser.NoteTemplatePath);
            Assert.IsNull(parser.InputFile);
        }

        [TestMethod]
        public void Constructor_WithTemplatePathSameAsInputFile_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--template", "input.gcode", "input.gcode"]));
            Assert.AreEqual("Note template path cannot be the same as the G-code file", ex.Message);
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

        #region Dry Run Flag Tests

        [TestMethod]
        public void Constructor_WithoutDryRunFlag_DryRunIsFalse()
        {
            var parser = new ArgumentParser(["input.gcode"]);
            Assert.IsFalse(parser.DryRun);
        }

        [TestMethod]
        public void Constructor_WithDryRunFlag_SetsDryRun()
        {
            var parser = new ArgumentParser(["--dry-run", "input.gcode"]);
            Assert.IsTrue(parser.DryRun);
            Assert.AreEqual("input.gcode", parser.InputFile);
        }

        [TestMethod]
        public void Constructor_WithDryRunAndTemplate_SetsBoth()
        {
            var parser = new ArgumentParser(["--dry-run", "--template", "my.txt", "input.gcode"]);
            Assert.IsTrue(parser.DryRun);
            Assert.AreEqual("my.txt", parser.NoteTemplatePath);
        }

        [TestMethod]
        public void Constructor_WithDryRunAsLastArgument_InputFileIsNull()
        {
            var parser = new ArgumentParser(["--dry-run"]);
            Assert.IsTrue(parser.DryRun);
            Assert.IsNull(parser.InputFile);
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
        public void Constructor_WithDebugAsLastArgument_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["input.gcode", "--debug"]));
            Assert.AreEqual("--debug requires a path argument", ex.Message);
        }

        [TestMethod]
        public void Constructor_WithDebugPathStartingWithDashes_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--debug", "--invalid", "input.gcode"]));
            Assert.AreEqual("Debug path cannot start with --: --invalid", ex.Message);
        }

        [TestMethod]
        public void Constructor_WithDebugPathAsFinalArgument_InputFileIsNull()
        {
            // This is the --help example run outside a slicer: the path belongs to --debug and there is no G-code file.
            var parser = new ArgumentParser(["--default", "--debug", "C:\\debug\\"]);
            Assert.AreEqual("C:\\debug\\", parser.DebugPath);
            Assert.IsNull(parser.InputFile);
        }

        [TestMethod]
        public void Constructor_WithDebugPathSameAsInputFile_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--debug", "input.gcode", "input.gcode"]));
            Assert.AreEqual("Debug path cannot be the same as input file", ex.Message);
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

        #region Unknown Flag Tests

        [TestMethod]
        public void Constructor_WithMisspelledFullFlag_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--fulll", "input.gcode"]));
            Assert.AreEqual("Unknown option: --fulll", ex.Message);
            Assert.AreEqual("Run with --help to see the available options.", ex.Hint);
        }

        [TestMethod]
        public void Constructor_WithWrongCaseDebugFlag_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--Debug", "input.gcode"]));
            Assert.AreEqual("Unknown option: --Debug", ex.Message);
            Assert.AreEqual("Run with --help to see the available options.", ex.Hint);
        }

        [TestMethod]
        public void Constructor_WithUnknownShortFlag_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["-x", "input.gcode"]));
            Assert.AreEqual("Unknown option: -x", ex.Message);
            Assert.AreEqual("Run with --help to see the available options.", ex.Hint);
        }

        [TestMethod]
        public void Constructor_WithUnknownFlagAsOnlyArgument_ThrowsUserFacingException()
        {
            // A lone unknown flag is not treated as the input file - it's still reported as an unknown option.
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--fulll"]));
            Assert.AreEqual("Unknown option: --fulll", ex.Message);
        }

        [TestMethod]
        public void Constructor_WithUnknownFlagAsLastArgumentAfterKnownFlag_ThrowsUserFacingException()
        {
            var ex = Assert.ThrowsException<UserFacingException>(() => new ArgumentParser(["--default", "--fulll"]));
            Assert.AreEqual("Unknown option: --fulll", ex.Message);
        }

        [TestMethod]
        public void Constructor_WithTemplatePathValue_DoesNotTreatValueAsUnknownFlag()
        {
            // The path passed to --template should not itself be flag-checked, even though it's not the last argument.
            var parser = new ArgumentParser(["--template", "C:\\templates\\custom.txt", "input.gcode"]);
            Assert.AreEqual("C:\\templates\\custom.txt", parser.NoteTemplatePath);
        }

        [TestMethod]
        public void Constructor_WithDebugPathValue_DoesNotTreatValueAsUnknownFlag()
        {
            // The path passed to --debug should not itself be flag-checked, even though it's not the last argument.
            var parser = new ArgumentParser(["--debug", "C:\\debug\\", "input.gcode"]);
            Assert.AreEqual("C:\\debug\\", parser.DebugPath);
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
            Assert.IsTrue(parser.DryRun);
        }

        [TestMethod]
        public void Constructor_WithUninstallAndDryRun_SetsDryRun()
        {
            var parser = new ArgumentParser(["uninstall", "--dry-run"]);
            Assert.AreEqual(AppMode.Uninstall, parser.Mode);
            Assert.IsTrue(parser.DryRun);
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
            Assert.IsFalse(parser.DryRun);
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
