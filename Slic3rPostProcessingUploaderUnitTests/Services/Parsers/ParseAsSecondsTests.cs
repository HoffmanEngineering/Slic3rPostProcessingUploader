using Slic3rPostProcessingUploader.Services.Parsers.OrcaSlicer;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers
{
    /// <summary>
    /// ParseAsSeconds lives in GcodeParserBase and is shared by every slicer parser,
    /// so these tests exercise it through one concrete parser (OrcaParser).
    /// </summary>
    [TestClass]
    public class ParseAsSecondsTests
    {
        private static int? Parse(string input) => new OrcaParser("").ParseAsSeconds(input);

        [DataTestMethod]
        [DataRow("1d 2h 30m 5s", 95405)]
        [DataRow("2d", 172800)]
        [DataRow("1d 5s", 86405)]
        [DataRow("18m 41s", 1121)]
        [DataRow("4h 12m 53s", 15173)]
        [DataRow("1319000", 1319)]
        public void ShouldParseTimeStringsIntoSeconds(string input, int expectedSeconds)
        {
            Assert.AreEqual(expectedSeconds, Parse(input));
        }
    }
}
