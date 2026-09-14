using Slic3rPostProcessingUploader.Services.Installer;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer
{
    [TestClass]
    public class WizardServiceTests
    {
        [TestMethod]
        public void BuildFlags_DefaultTemplate_ReturnsDefaultFlag()
        {
            var flags = WizardService.BuildFlags(useFullTemplate: false, optOutTelemetry: false, additionalFlags: "");
            Assert.AreEqual("--default", flags);
        }

        [TestMethod]
        public void BuildFlags_FullTemplate_ReturnsFullFlag()
        {
            var flags = WizardService.BuildFlags(useFullTemplate: true, optOutTelemetry: false, additionalFlags: "");
            Assert.AreEqual("--full", flags);
        }

        [TestMethod]
        public void BuildFlags_WithTelemetryOptOut_AppendsTelemetryFlag()
        {
            var flags = WizardService.BuildFlags(useFullTemplate: false, optOutTelemetry: true, additionalFlags: "");
            Assert.AreEqual("--default --opt-out-telemetry", flags);
        }

        [TestMethod]
        public void BuildFlags_WithAdditionalFlags_AppendsThemLast()
        {
            var flags = WizardService.BuildFlags(useFullTemplate: true, optOutTelemetry: false, additionalFlags: "--local-dev");
            Assert.AreEqual("--full --local-dev", flags);
        }

        [TestMethod]
        public void BuildFlags_WithAllOptions_CombinesCorrectly()
        {
            var flags = WizardService.BuildFlags(useFullTemplate: true, optOutTelemetry: true, additionalFlags: "--local-dev");
            Assert.AreEqual("--full --opt-out-telemetry --local-dev", flags);
        }
    }
}
