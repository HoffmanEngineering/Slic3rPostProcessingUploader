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

        public bool DisplayHelp { get; private set; }

        public bool DisplayVersion { get; private set; }

        public AppMode Mode { get; private set; }
        public bool IsDryRun { get; private set; }

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
                this.IsDryRun = args.Contains("--dry-run");
                return;
            }

            if (args[0] == "uninstall")
            {
                this.Mode = AppMode.Uninstall;
                this.IsDryRun = args.Contains("--dry-run");
                return;
            }

            this.Mode = AppMode.PostProcess;

            // InputFile is the last argument, but only if it's not a flag
            var lastArg = args.LastOrDefault();
            this.InputFile = lastArg != null && !lastArg.StartsWith("--") && lastArg != "-h" ? lastArg : null;

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
                        throw new ArgumentException("--template requires a path argument");
                    }

                    this.NoteTemplatePath = args[i + 1];

                    if (string.IsNullOrEmpty(this.NoteTemplatePath))
                    {
                        throw new ArgumentNullException("Note template path cannot be null or empty");
                    }

                    if (this.NoteTemplatePath == this.InputFile)
                    {
                        throw new ArgumentException("Note template path cannot be null or empty");
                    }
                }

                if (args[i] == "--local-dev")
                {
                    this.UseLocalDev = true;
                }

                if (args[i] == "--debug")
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("--debug requires a path argument");
                    }

                    this.DebugPath = args[i + 1];

                    if (this.DebugPath == this.InputFile)
                    {
                        throw new ArgumentException("Debug path cannot be the same as input file");
                    }

                    if (string.IsNullOrEmpty(this.DebugPath))
                    {
                        throw new ArgumentNullException("Debug path cannot be null or empty");
                    }

                    if(this.DebugPath.StartsWith("--"))
                    {
                        throw new ArgumentException("Debug path cannot start with --" + this.DebugPath);
                    }
                }

                if (args[i] == "--opt-out-telemetry")
                {
                    this.DisableTelemetry = true;
                }

                if (args[i] == "--help" ||  args[i] == "-h")
                {
                    this.DisplayHelp = true;
                }

                if (args[i] == "--version" || args[i] == "-v")
                {
                    this.DisplayVersion = true;
                }
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
            Console.WriteLine("--opt-out-telemetry: Disable telemetry tracking. To help improve the plugin, we track slicer and plugin versions, as well as log errors that are thrown. No personal data is collected.");
            Console.WriteLine();
            Console.WriteLine("Note Template Options:");
            Console.WriteLine("  --default: Use the default note template, which contains a curated list of general settings. Preferred by most users. The Default template is used if no other note template option is given.");
            Console.WriteLine("  --full: Use the full note template, which lists most of the settings available in the slicers");
            Console.WriteLine("  --template <path>: Use a custom note template. Absolute paths work better. See README for more details on syntax");
            Console.WriteLine();
            Console.WriteLine("Example: Slic3rPostProcessingUploader --default --debug C:\\debug\\");
            Console.WriteLine();
        }
    }

}
