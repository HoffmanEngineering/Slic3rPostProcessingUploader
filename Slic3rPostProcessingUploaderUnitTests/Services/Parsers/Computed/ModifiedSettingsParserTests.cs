using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.Computed
{
    /// <summary>
    /// Orca serialises list settings (inherits_group, different_settings_to_system) as ';'-separated entries with C-style
    /// quoting. Entries are positional (process; filaments…; printer), so ParseEntries keeps empties, while ParseKeys
    /// flattens the change list into a key set.
    /// </summary>
    [TestClass]
    public class ModifiedSettingsParserTests
    {
        [TestMethod]
        public void ParseEntriesShouldKeepEmptyPositions()
        {
            CollectionAssert.AreEqual(new[] { "post_process", "", "" }, ModifiedSettingsParser.ParseEntries("post_process;;").ToList());
        }

        [TestMethod]
        public void ParseEntriesShouldUnquoteEntriesWithSpaces()
        {
            CollectionAssert.AreEqual(
                new[] { "0.16 High Quality @U1", "", "" },
                ModifiedSettingsParser.ParseEntries("\"0.16 High Quality @U1\";;").ToList());
        }

        [TestMethod]
        public void ParseEntriesShouldKeepSemicolonsInsideQuotes()
        {
            CollectionAssert.AreEqual(new[] { "a;b", "c" }, ModifiedSettingsParser.ParseEntries("\"a;b\";c").ToList());
        }

        [TestMethod]
        public void ParseEntriesShouldUnescapeQuotesAndBackslashes()
        {
            CollectionAssert.AreEqual(new[] { "say \"hi\" \\ bye" }, ModifiedSettingsParser.ParseEntries("\"say \\\"hi\\\" \\\\ bye\"").ToList());
        }

        [TestMethod]
        public void ParseEntriesShouldRunAnUnmatchedQuoteToTheEnd()
        {
            CollectionAssert.AreEqual(new[] { "a;b" }, ModifiedSettingsParser.ParseEntries("\"a;b").ToList());
        }

        [TestMethod]
        public void ParseEntriesShouldTrimEntries()
        {
            CollectionAssert.AreEqual(new[] { "a", "b" }, ModifiedSettingsParser.ParseEntries(" a ; b ").ToList());
        }

        [TestMethod]
        public void ParseEntriesShouldReturnNothingForAnEmptyValue()
        {
            Assert.AreEqual(0, ModifiedSettingsParser.ParseEntries("").Count);
        }

        [TestMethod]
        public void ParseKeysShouldDropEmptyEntries()
        {
            CollectionAssert.AreEqual(
                new[] { "enable_support", "wall_loops" },
                ModifiedSettingsParser.ParseKeys("enable_support;wall_loops;;;;;;").ToList());
        }

        [TestMethod]
        public void ParseKeysShouldSplitQuotedInnerLists()
        {
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, ModifiedSettingsParser.ParseKeys("\"a;b\";c").ToList());
        }

        [TestMethod]
        public void ParseKeysShouldDedupeKeys()
        {
            CollectionAssert.AreEqual(
                new[] { "nozzle_temperature", "printable_area" },
                ModifiedSettingsParser.ParseKeys("nozzle_temperature;nozzle_temperature;printable_area").ToList());
        }

        [TestMethod]
        public void ParseKeysShouldDropPostProcessBecauseTheUploaderPutItThere()
        {
            CollectionAssert.AreEqual(new[] { "wall_loops" }, ModifiedSettingsParser.ParseKeys("post_process;wall_loops").ToList());
        }
    }
}
