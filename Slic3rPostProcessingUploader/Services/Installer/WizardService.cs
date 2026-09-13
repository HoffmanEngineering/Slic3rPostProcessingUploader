namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal class WizardService
    {
        private readonly IReadOnlyList<ISlicerProfileInstaller> _installers;
        private readonly string _executablePath;

        public WizardService(IReadOnlyList<ISlicerProfileInstaller> installers, string executablePath)
        {
            _installers = installers;
            _executablePath = executablePath;
        }

        public void RunInstall(bool dryRun)
        {
            PrintHeader();
            Console.WriteLine("Scanning for supported slicers...\n");

            var detected = _installers.Where(i => i.IsDetected()).ToList();

            if (detected.Count == 0)
            {
                Console.WriteLine("No supported slicers found on this machine.");
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
                return;
            }

            // Show summary of all slicers
            foreach (var installer in _installers)
            {
                if (!installer.IsDetected())
                {
                    Console.WriteLine($"  {installer.SlicerName,-26} (not detected — skipped)");
                    continue;
                }

                var status = installer.GetInstallStatus(_executablePath);
                string statusText = status.IsInstalled
                    ? $"Already installed | {status.InstalledCount}/{status.ProfileCount} profiles | flags: {status.InstalledFlags}"
                    : $"Not installed | {status.ProfileCount} process profiles found";
                Console.WriteLine($"  Found: {installer.SlicerName,-20} {statusText}");
            }

            Console.WriteLine();

            // Per-slicer prompts
            bool anyInstalled = false;
            foreach (var installer in detected)
            {
                var status = installer.GetInstallStatus(_executablePath);

                Console.WriteLine($"--- {installer.SlicerName} ---");

                if (status.IsInstalled)
                {
                    Console.Write($"Already installed (flags: {status.InstalledFlags}). Reinstall with new flags? [y/N]: ");
                    var answer = Console.ReadLine()?.Trim().ToLower();
                    if (answer != "y") { Console.WriteLine(); continue; }
                }
                else
                {
                    Console.Write($"Install to {installer.SlicerName}? [Y/n]: ");
                    var answer = Console.ReadLine()?.Trim().ToLower();
                    if (answer == "n") { Console.WriteLine(); continue; }
                }

                string flags = PromptForFlags();

                // Install refreshes the path and flags of any override it already owns, so a re-run is enough to change flags.
                var result = installer.Install(_executablePath, flags, dryRun);

                if (dryRun)
                    Console.WriteLine($"  [DRY RUN] Would create/update {result.Created + result.Updated} profiles ({result.Created} new, {result.Updated} updated, {result.Skipped} already up to date).");
                else
                    Console.WriteLine($"  Done: {result.Created} created, {result.Updated} updated, {result.Skipped} skipped.");

                if (result.WithOtherScripts > 0)
                    Console.WriteLine($"  Note: {result.WithOtherScripts} profile(s) had other post-process scripts — ours was appended alongside them.");
                if (result.CoveredByHandMade > 0)
                    Console.WriteLine($"  Note: {result.CoveredByHandMade} profile(s) already run the uploader through a profile you set up by hand — left as-is.");
                if (result.HandMadeRefreshed > 0)
                    Console.WriteLine($"  Note: {result.HandMadeRefreshed} hand-made profile(s) pointed at an old copy of the uploader — updated the path, kept their flags.");
                if (result.Unreadable > 0)
                    Console.WriteLine($"  Warning: {result.Unreadable} profile file(s) could not be read and were skipped.");

                anyInstalled = true;
                Console.WriteLine();
            }

            if (!anyInstalled)
                Console.WriteLine("No changes made.");
            else
                Console.WriteLine("Setup complete!");

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        public void RunUninstall(bool dryRun)
        {
            PrintHeader();
            Console.WriteLine("Uninstall — scanning for installed profiles...\n");

            var detected = _installers.Where(i => i.IsDetected()).ToList();
            bool anyUninstalled = false;

            foreach (var installer in detected)
            {
                var status = installer.GetInstallStatus(_executablePath);
                if (!status.IsInstalled)
                {
                    Console.WriteLine($"  {installer.SlicerName}: not installed — skipped.");
                    continue;
                }

                Console.WriteLine($"--- {installer.SlicerName} ---");
                Console.Write($"Remove from {status.InstalledCount} profile(s)? [Y/n]: ");
                var answer = Console.ReadLine()?.Trim().ToLower();
                if (answer == "n") { Console.WriteLine(); continue; }

                var result = installer.Uninstall(_executablePath, dryRun);

                if (dryRun)
                    Console.WriteLine($"  [DRY RUN] Would remove from {result.RemovedFiles + result.ModifiedFiles} profile(s).");
                else
                    Console.WriteLine($"  Removed from {result.RemovedFiles + result.ModifiedFiles} profile(s). {result.ModifiedFiles} profile(s) had other settings — kept those files.");

                if (result.HandMadeLeft > 0)
                    Console.WriteLine($"  Note: {result.HandMadeLeft} profile(s) you set up by hand still reference the uploader — remove those in the slicer if you no longer want them.");
                if (result.Unreadable > 0)
                    Console.WriteLine($"  Warning: {result.Unreadable} profile file(s) could not be read and were skipped.");

                anyUninstalled = true;
                Console.WriteLine();
            }

            if (!anyUninstalled)
                Console.WriteLine("No changes made.");
            else
                Console.WriteLine("Uninstall complete!");

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        private static string PromptForFlags()
        {
            Console.WriteLine();
            Console.WriteLine("  Note template:");
            Console.WriteLine("    1) Default (recommended)");
            Console.WriteLine("    2) Full");
            Console.Write("  Choice [1]: ");
            var templateChoice = Console.ReadLine()?.Trim();
            bool useFullTemplate = templateChoice == "2";

            Console.Write("  Opt out of telemetry? [y/N]: ");
            var telemetryAnswer = Console.ReadLine()?.Trim().ToLower();
            bool optOutTelemetry = telemetryAnswer == "y";

            Console.Write("  Additional flags (leave blank for none): ");
            var additionalFlags = Console.ReadLine()?.Trim() ?? "";

            Console.WriteLine();
            return BuildFlagsForTesting(useFullTemplate, optOutTelemetry, additionalFlags);
        }

        // internal for testing
        internal static string BuildFlagsForTesting(bool useFullTemplate, bool optOutTelemetry, string additionalFlags)
        {
            var parts = new List<string>();
            parts.Add(useFullTemplate ? "--full" : "--default");
            if (optOutTelemetry) parts.Add("--opt-out-telemetry");
            if (!string.IsNullOrWhiteSpace(additionalFlags)) parts.Add(additionalFlags.Trim());
            return string.Join(" ", parts);
        }

        private static void PrintHeader()
        {
            Console.WriteLine("3D Print Log Uploader - Setup Wizard");
            Console.WriteLine("=====================================");
        }
    }
}
