using Slic3rPostProcessingUploader.Services;

namespace Slic3rPostProcessingUploaderUnitTests.Services
{
    [TestClass]
    public class ConsoleOutputTests
    {
        private StringWriter _screen = null!;
        private StringWriter _debugFile = null!;

        [TestInitialize]
        public void Setup()
        {
            _screen = new StringWriter();
            _debugFile = new StringWriter();
        }

        private ConsoleOutput Build(bool verbose = false, bool unicode = true, bool withDebugFile = false)
        {
            return new ConsoleOutput(_screen, withDebugFile ? _debugFile : null, verbose, unicode, useColor: false);
        }

        [TestMethod]
        public void Header_PrintsNameAndVersion()
        {
            Build().Header("1.2.3");

            Assert.AreEqual("3D Print Log Uploader v1.2.3" + Environment.NewLine, _screen.ToString());
        }

        [TestMethod]
        public void Step_PrintsCheckmarkLine()
        {
            Build().Step("Parsed benchy.gcode");

            Assert.AreEqual("  ✓ Parsed benchy.gcode" + Environment.NewLine, _screen.ToString());
        }

        [TestMethod]
        public void Info_PrintsArrowLine()
        {
            Build().Info("Opening browser");

            Assert.AreEqual("  → Opening browser" + Environment.NewLine, _screen.ToString());
        }

        [TestMethod]
        public void Raw_PrintsTextWithoutPrefixAndMirrorsToDebugFile()
        {
            Build(withDebugFile: true).Raw("line one\nline two");

            Assert.AreEqual("line one\nline two" + Environment.NewLine, _screen.ToString());
            Assert.AreEqual("line one\nline two" + Environment.NewLine, _debugFile.ToString());
        }

        [TestMethod]
        public void Warn_PrintsBangLine()
        {
            Build().Warn("Slicer not recognized");

            Assert.AreEqual("  ! Slicer not recognized" + Environment.NewLine, _screen.ToString());
        }

        [TestMethod]
        public void Error_PrintsCrossLineAndIndentedHint()
        {
            Build().Error("Could not reach 3dprintlog.com", "Check your internet connection.");

            var expected = "  ✗ Could not reach 3dprintlog.com" + Environment.NewLine
                         + "    Check your internet connection." + Environment.NewLine;
            Assert.AreEqual(expected, _screen.ToString());
        }

        [TestMethod]
        public void Error_WithoutHint_PrintsOnlyMessage()
        {
            Build().Error("Something failed");

            Assert.AreEqual("  ✗ Something failed" + Environment.NewLine, _screen.ToString());
        }

        [TestMethod]
        public void Glyphs_FallBackToAscii_WhenUnicodeDisabled()
        {
            var output = Build(unicode: false);
            output.Step("a");
            output.Info("b");
            output.Error("c");

            var expected = "  [OK] a" + Environment.NewLine
                         + "  -> b" + Environment.NewLine
                         + "  [X] c" + Environment.NewLine;
            Assert.AreEqual(expected, _screen.ToString());
        }

        [TestMethod]
        public void Debug_IsHiddenFromScreen_WhenNotVerbose()
        {
            Build(verbose: false).Debug("raw response");

            Assert.AreEqual(string.Empty, _screen.ToString());
        }

        [TestMethod]
        public void Debug_PrintsToScreen_WhenVerbose()
        {
            Build(verbose: true).Debug("raw response");

            Assert.AreEqual("    raw response" + Environment.NewLine, _screen.ToString());
        }

        [TestMethod]
        public void DebugFile_ReceivesEveryLine_IncludingHiddenDebug()
        {
            var output = Build(verbose: false, withDebugFile: true);
            output.Step("Parsed");
            output.Debug("raw response");

            Assert.AreEqual("  ✓ Parsed" + Environment.NewLine, _screen.ToString());
            Assert.AreEqual("  ✓ Parsed" + Environment.NewLine + "    raw response" + Environment.NewLine, _debugFile.ToString());
        }

        [TestMethod]
        public void ReportException_UserFacing_PrintsMessageAndHint_NoStackTraceOnScreen()
        {
            var output = Build(withDebugFile: true);

            output.ReportException(new UserFacingException("Could not read the G-code file", "Check the file path."));

            var expected = "  ✗ Could not read the G-code file" + Environment.NewLine
                         + "    Check the file path." + Environment.NewLine;
            Assert.AreEqual(expected, _screen.ToString());
            StringAssert.Contains(_debugFile.ToString(), "UserFacingException");
        }

        [TestMethod]
        public void ReportException_Unexpected_PrintsGenericMessageWithDebugHint()
        {
            var output = Build(withDebugFile: true);

            output.ReportException(new InvalidOperationException("boom"));

            var expected = "  ✗ Something went wrong: boom" + Environment.NewLine
                         + "    Run again with --debug <folder> and report the issue at https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/issues" + Environment.NewLine;
            Assert.AreEqual(expected, _screen.ToString());
            StringAssert.Contains(_debugFile.ToString(), "InvalidOperationException");
        }

        [TestMethod]
        public void ReportException_Verbose_PrintsStackTraceOnScreen()
        {
            var output = Build(verbose: true);

            output.ReportException(new InvalidOperationException("boom"));

            StringAssert.Contains(_screen.ToString(), "InvalidOperationException");
        }
    }
}
