namespace Slic3rPostProcessingUploader.Services
{
    /// <summary>
    /// Parses the arguments provided to the program
    /// </summary>
    internal class ArgumentParser
    {
        public string? InputFile { get; private set; }
        public bool UseDefaultNoteTemplate { get; private set; }
        public bool UseFullNoteTemplate { get; private set; }

        public string? NoteTemplatePath { get; private set; }

        public bool UseLocalDev { get; private set; }

        public string? DebugPath { get; private set; }

        public bool DisableTelemetry { get; private set; }

        /// <summary>
        /// Post-process mode: parse and print the rendered note and DTO without uploading or opening a browser.
        /// Install/uninstall mode: report the profile changes that would be made without writing any files.
        /// </summary>
        public bool DryRun { get; private set; }

        public bool DisplayHelp { get; private set; }

        public bool DisplayVersion { get; private set; }

        public AppMode Mode { get; private set; }

        public ArgumentParser(string[] args) {
            this.UseDefaultNoteTemplate = true;
            this.UseFullNoteTemplate = false;
            this.DisableTelemetry = false;
            this.DisplayHelp = false;

            // Detect install/uninstall sub-commands first
            if (args.Length == 0)
            {
                this.Mode = AppMode.Wizard;
                return;
            }

            if (args[0] == "install")
            {
                this.Mode = AppMode.Install;
                this.DryRun = args.Contains("--dry-run");
                return;
            }

            if (args[0] == "uninstall")
            {
                this.Mode = AppMode.Uninstall;
                this.DryRun = args.Contains("--dry-run");
                return;
            }

            this.Mode = AppMode.PostProcess;

            // InputFile is the last argument, but only if it's not a flag
            var lastArg = args.LastOrDefault();
            this.InputFile = lastArg != null && !lastArg.StartsWith("--") && lastArg != "-h" ? lastArg : null;

            // The last argument is the G-code path (when it's not a flag) and is exempt from flag checking.
            // It's still flag-checked when it looks like a flag, so a lone unknown flag is still reported.
            int lastIndex = args.Length - 1;
            bool lastArgIsInputFile = this.InputFile != null && lastIndex >= 0 && args[lastIndex] == this.InputFile;

            // Check for if the user wants a default, full, or custom note template
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--default")
                {
                    this.UseDefaultNoteTemplate = true;
                    this.UseFullNoteTemplate = false;
                }
                else if (args[i] == "--full")
                {
                    this.UseDefaultNoteTemplate = false;
                    this.UseFullNoteTemplate = true;
                }
                else if (args[i] == "--template")
                {
                    this.UseDefaultNoteTemplate = false;
                    this.UseFullNoteTemplate = false;

                    if (i + 1 >= args.Length)
                    {
                        throw new UserFacingException(
                            "--template requires a path argument",
                            "Pass the path to your custom note template, e.g. --template C:\\templates\\custom.txt");
                    }

                    this.NoteTemplatePath = args[i + 1];
                    i++; // The path value belongs to --template; skip it so it isn't checked as a flag.
                    ClearInputFileIfConsumed(i, args.Length);

                    if (string.IsNullOrEmpty(this.NoteTemplatePath))
                    {
                        throw new UserFacingException(
                            "Note template path cannot be null or empty",
                            "Pass the path to your custom note template, e.g. --template C:\\templates\\custom.txt");
                    }

                    if (this.NoteTemplatePath == this.InputFile)
                    {
                        throw new UserFacingException(
                            "Note template path cannot be the same as the G-code file",
                            "Pass a different path for --template than the G-code file being processed.");
                    }
                }
                else if (args[i] == "--local-dev")
                {
                    this.UseLocalDev = true;
                }
                else if (args[i] == "--debug")
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new UserFacingException(
                            "--debug requires a path argument",
                            "Pass the path to save debug output to, e.g. --debug C:\\debug\\");
                    }

                    this.DebugPath = args[i + 1];
                    i++; // The path value belongs to --debug; skip it so it isn't checked as a flag.
                    ClearInputFileIfConsumed(i, args.Length);

                    if (string.IsNullOrEmpty(this.DebugPath))
                    {
                        throw new UserFacingException(
                            "Debug path cannot be null or empty",
                            "Pass the path to save debug output to, e.g. --debug C:\\debug\\");
                    }

                    if (this.DebugPath == this.InputFile)
                    {
                        throw new UserFacingException(
                            "Debug path cannot be the same as input file",
                            "Pass a different path for --debug than the G-code file being processed.");
                    }

                    if (this.DebugPath.StartsWith("--"))
                    {
                        throw new UserFacingException(
                            $"Debug path cannot start with --: {this.DebugPath}",
                            "Pass the path to save debug output to, e.g. --debug C:\\debug\\");
                    }
                }
                else if (args[i] == "--opt-out-telemetry")
                {
                    this.DisableTelemetry = true;
                }
                else if (args[i] == "--dry-run")
                {
                    this.DryRun = true;
                }
                else if (args[i] == "--help" || args[i] == "-h")
                {
                    this.DisplayHelp = true;
                }
                else if (args[i] == "--version" || args[i] == "-v")
                {
                    this.DisplayVersion = true;
                }
                else if (i == lastIndex && lastArgIsInputFile)
                {
                    // The last argument is the G-code path, not a flag; nothing to validate.
                }
                else if (args[i].StartsWith("--") || args[i].StartsWith("-"))
                {
                    throw new UserFacingException(
                        $"Unknown option: {args[i]}",
                        "Run with --help to see the available options.");
                }
            }
        }


        /// <summary>
        /// The input file is provisionally the last argument, but when an option consumes that argument as its value
        /// (e.g. <c>--debug C:\debug\</c> with no G-code path after it) there is no input file at all. Clearing it here
        /// lets the caller report "no G-code file was given" instead of a misleading "path cannot be the same as input file".
        /// </summary>
        private void ClearInputFileIfConsumed(int valueIndex, int argCount)
        {
            if (valueIndex == argCount - 1)
            {
                this.InputFile = null;
            }
        }

        public void DisplayHelpDocs()
        {
            Console.WriteLine("Welcome to the 3D Print Log uploader for Slic3r-based slicers (OrcaSlicer, BambuSlicer, PrusaSlicer, Etc)");
            Console.WriteLine("This program will parse the gcode file and open up https://www.3dprintlog.com with the print details filled out.");
            Console.WriteLine("Create a free account at https://www.3dprintlog.com to use.");
            Console.WriteLine();
            Console.WriteLine("Usage: In the Slicer's 'Post-Processing Scripts' section, add the path to this file");
            Console.WriteLine("Slic3rPostProcessingUploader.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("--help, -h: Display this help message. No settings will be uploaded if help is displayed.");
            Console.WriteLine("--version, -v: Display the version number.");
            Console.WriteLine("--local-dev: Use the local development environment");
            Console.WriteLine("--debug <path>: Save debug information to the specified path");
            Console.WriteLine("--dry-run: Parse the G-code and print the rendered note, key parsed fields, and the DTO JSON to the console without uploading to 3dprintlog.com or opening a browser. Useful for checking a custom --template.");
            Console.WriteLine("--opt-out-telemetry: Disable telemetry tracking. To help improve the plugin, we track slicer and plugin versions, as well as log errors that are thrown. No personal data is collected.");
            Console.WriteLine();
            Console.WriteLine("Note Template Options:");
            Console.WriteLine("  --default: Use the default note template, which contains a curated list of general settings. Preferred by most users. The Default template is used if no other note template option is given.");
            Console.WriteLine("  --full: Use the full note template, which lists most of the settings available in the slicers");
            Console.WriteLine("  --template <path>: Use a custom note template. Absolute paths work better. See README for more details on syntax");
            Console.WriteLine();
            Console.WriteLine("Example: Slic3rPostProcessingUploader --default --debug C:\\debug\\");
            Console.WriteLine("Example: Slic3rPostProcessingUploader --dry-run --template C:\\templates\\my.txt C:\\prints\\benchy.gcode");
            Console.WriteLine();
        }
    }

}
