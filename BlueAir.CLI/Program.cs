using System.Diagnostics;
using System.Reflection;
using System.Text;
using BlueAir.CLI.Resources;

namespace BlueAir.CLI;

public static class Program
{
    public static void Main(string[] args)
    {
        // Disable commands if running on these platforms
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() ||
            OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsWatchOS())
            BlueAir.DisableCommands = true;
        List<string> files_and_folders = new();
        var verbose = false;
        var autoSave = false;
        var inputIsXml = false;
        var xTimer = false;
        var timer = false;

        for (var i = 0; i < args.Length;)
            switch (args[i].ToLowerInvariant())
            {
                case "--verbose":
                    verbose = true;
                    break;

                case "--auto-save":
                    autoSave = true;
                    break;

                case "--version":
                    Console.WriteLine(Translations.VersionInfo
                        .Replace("{VER}", GetVersion(), StringComparison.InvariantCultureIgnoreCase)
                        .Replace("{APP_NAME}", "BlueAir CLI", StringComparison.InvariantCultureIgnoreCase));
                    return;

                case "--timer":
                    timer = true;
                    break;

                case "--x-timer":
                    timer = true;
                    xTimer = true;
                    break;

                case "--help":
                    Console.WriteLine(Translations.UsageText.Replace("{app_command}", "blueair-cli",
                        StringComparison.InvariantCultureIgnoreCase));
                    Console.WriteLine(Translations.UsageOptionsTitle);
                    Console.WriteLine(
                        $@"--verbose                    {Translations.UsageVerbose}");
                    Console.WriteLine(
                        $@"--timer                      {Translations.UsageTimer}");
                    Console.WriteLine(
                        $@"--x-timer                    {Translations.UsageXTimer}");
                    Console.WriteLine(
                        $@"--auto-save                  {Translations.UsageAutoSave}");
                    Console.WriteLine(
                        $@"--input-xml                  {Translations.UsageInputXML}");
                    Console.WriteLine(
                        $@"--version                    {Translations.UsageVersion}");
                    Console.WriteLine(
                        $@"--help                       {Translations.UsageHelp}");
                    return;

                case "--input-xml":
                    inputIsXml = true;
                    break;

                default:
                    files_and_folders.Add(args[i]);
                    break;
            }

        var stopwatch = new Stopwatch();

        if (files_and_folders.Count < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Translations.TooFewArguments);
            return;
        }

        var inputFile = files_and_folders[0];
        var mainFolder = files_and_folders[1];
        var outputFile = string.Empty;

        if (files_and_folders.Count > 2) outputFile = files_and_folders[2];
        if (autoSave) outputFile = string.IsNullOrWhiteSpace(outputFile) ? inputFile : outputFile;

        if (verbose) Console.WriteLine(Translations.ReadingInputFile);

        if (timer) stopwatch.Start();

        BlueAir.Init();
        var download = new DownloadInfo(inputFile, inputIsXml);

        download.OnProgressChanged += (percentage, item) =>
        {
            var itemName = item switch
            {
                DownloadFile file => string.IsNullOrWhiteSpace(file.FileName) ? file.Link : file.FileName,
                DownloadFolder folder => folder.Name,
                _ => "???"
            };
            Console.WriteLine($@"[{(int)(percentage * 100)}%] ""{itemName}""");
        };

        Console.CancelKeyPress += (_, _) =>
        {
            if (verbose) Console.WriteLine(Translations.CancelRequested);
            using var stream = new FileStream(outputFile, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(download.ToXml());
        };
        if (verbose)
            Console.WriteLine(
                Translations.StartProgress.Replace("{APP_NAME}", "BlueAir",
                    StringComparison.InvariantCultureIgnoreCase) +
                Environment.NewLine +
                $@"   {Translations.StartInputFile}: {inputFile}" +
                Environment.NewLine +
                $@"   {Translations.StartOutputFile}: {mainFolder}" +
                Environment.NewLine +
                $@"   {Translations.StartAutoSaveFile}: {outputFile}");
        download.Start(mainFolder);

        if (timer) stopwatch.Stop();

        if (timer)
        {
            if (xTimer)
                Console.WriteLine(stopwatch.Elapsed.ToString("G"));
            else
                Console.WriteLine(Translations.DownloadTook
                    .Replace("{d}", stopwatch.Elapsed.Days + "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{h}", stopwatch.Elapsed.Hours + "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{m}", stopwatch.Elapsed.Minutes + "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{s}", stopwatch.Elapsed.Seconds + "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{ms}", stopwatch.Elapsed.Milliseconds + "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{us}", stopwatch.Elapsed.Microseconds + "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{ns}", stopwatch.Elapsed.Nanoseconds + "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{t}", stopwatch.Elapsed.Ticks + "", StringComparison.InvariantCultureIgnoreCase));
        }
    }

    private static string GetVersion()
    {
        return "v"
               + (
                   Assembly.GetExecutingAssembly() is { } ass
                   && ass.GetName() is { } name
                   && name.Version != null
                       ? "" + (name.Version.Major > 0 ? name.Version.Major : "") +
                         (name.Version.Minor > 0 ? "." + name.Version.Minor : "") +
                         (name.Version.Build > 0 ? "." + name.Version.Build : "") +
                         (name.Version.Revision > 0 ? "." + name.Version.Revision : "")
                       : "?"
               );
    }
}