using Slic3rPostProcessingUploader.Services.Parsers;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers
{
    [TestClass]
    public class FilamentColorTests
    {
        [DataTestMethod]
        [DataRow("#E72F1D", "Red")]
        [DataRow("#080A0D", "Black")]
        [DataRow("#FFFFFF", "White")]
        [DataRow("#0078BF", "Blue")]
        [DataRow("#A6A9AA", "Grey")]
        [DataRow("#FFFF00", "Yellow")]
        [DataRow("#26A69A", "Teal")]
        [DataRow("#FF8000", "Orange")]
        [DataRow("#8B4513", "Brown")]
        [DataRow("#800080", "Purple")]
        [DataRow("#FF69B4", "Pink")]
        [DataRow("#00AE42", "Green")]
        public void ShouldNameTheNearestBasicColor(string hex, string expected)
        {
            Assert.AreEqual(expected, FilamentColor.Describe(hex));
        }

        [TestMethod]
        public void ShouldAcceptLowercaseHexWithoutHash()
        {
            Assert.AreEqual("Green", FilamentColor.Describe("15a95d"));
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("#FFF")]
        [DataRow("#GGGGGG")]
        [DataRow("red")]
        public void ShouldReturnNullWhenHexIsInvalid(string hex)
        {
            Assert.IsNull(FilamentColor.Describe(hex));
        }
    }
}
