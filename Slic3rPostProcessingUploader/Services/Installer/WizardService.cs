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

            // Each status is a scan of the slicer's config tree, so take it once and reuse it for the prompts.
            var detected = _installers
                .Where(i => i.IsDetected())
                .Select(i => (Installer: i, Status: i.GetInstallStatus(_executablePath)))
                .ToList();

            if (detected.Count == 0)
            {
                Console.WriteLine("No supported slicers found on this machine.");
                PauseBeforeExit();
                return;
            }

            foreach (var installer in _installers)
            {
                var found = detected.FirstOrDefault(d => d.Installer == installer);
                if (found.Installer == null)
                {
                    Console.WriteLine($"  {installer.SlicerName,-26} (not detected — skipped)");
                    continue;
                }

                string statusText = found.Status.IsInstalled
                    ? $"Already installed | {found.Status.InstalledCount}/{found.Status.ProfileCount} profiles | flags: {found.Status.InstalledFlags}"
                    : $"Not installed | {found.Status.ProfileCount} process profiles found";
                Console.WriteLine($"  Found: {installer.SlicerName,-20} {statusText}");
            }

            Console.WriteLine();

            var installedTo = new List<string>();
            foreach (var (installer, status) in detected)
            {
                Console.WriteLine($"--- {installer.SlicerName} ---");

                // Vendor process profiles only exist once a printer has been added in the slicer; without any there
                // is nothing to install into, and asking would just end in "0 created".
                if (status.ProfileCount == 0)
                {
                    Console.WriteLine($"No process profiles found. Add your printer(s) in {installer.SlicerName} first, then run this again.\n");
                    continue;
                }

                if (status.IsInstalled)
                {
                    Console.Write($"Already installed (flags: {status.InstalledFlags}). Reinstall with new flags? [y/N]: ");
                    if (ReadAnswer() != "y") { Console.WriteLine(); continue; }
                }
                else
                {
                    Console.Write($"Install to {installer.SlicerName}? [Y/n]: ");
                    if (ReadAnswer() == "n") { Console.WriteLine(); continue; }
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

                if (!dryRun) installedTo.Add(installer.SlicerName);
                Console.WriteLine();
            }

            if (installedTo.Count > 0)
            {
                Console.WriteLine("Setup complete!");
                Console.WriteLine($"Restart {string.Join(" / ", installedTo)} and choose a process preset ending in \" - 3DPrintLog\" to have each export logged.");
            }
            else if (dryRun && detected.Any(d => d.Status.ProfileCount > 0))
                Console.WriteLine("Dry run — no files were written.");
            else
                Console.WriteLine("No changes made.");

            PauseBeforeExit();
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
                if (ReadAnswer() == "n") { Console.WriteLine(); continue; }

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

            PauseBeforeExit();
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
            bool optOutTelemetry = ReadAnswer() == "y";

            Console.Write("  Additional flags (leave blank for none): ");
            var additionalFlags = Console.ReadLine()?.Trim() ?? "";

            Console.WriteLine();
            return BuildFlags(useFullTemplate, optOutTelemetry, additionalFlags);
        }

        private static string ReadAnswer() => Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";

        internal static string BuildFlags(bool useFullTemplate, bool optOutTelemetry, string additionalFlags)
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

        /// <summary>
        /// Double-clicking the executable opens a console that closes on exit, so give the user a chance to read
        /// the summary. When input is piped (scripts, tests) ReadKey would throw, so exit straight away instead.
        /// </summary>
        private static void PauseBeforeExit()
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
                return;

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey(intercept: true);
        }
    }
}
