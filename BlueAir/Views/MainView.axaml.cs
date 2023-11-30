using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;

namespace BlueAir.Views;

public partial class MainView : UserControl
{
    public static readonly Color BrandingColor = Color.Parse("#87c4ff");

    private readonly string CurrentFile = string.Empty;

    private string[]? Arguments;

    private DownloadInfo? CurrentInfo;

    private bool IsLoadedFromFile;

    public MainView()
    {
        InitializeComponent();

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() ||
            OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsWatchOS())
            BlueAir.DisableCommands = true;
    }

    public MainView WithArguments(string[] args)
    {
        Arguments = args;
        return this;
    }

    private void Navigate(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: string link }) return;
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = link
        });
    }

    private void UseSystemThemeChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is null) return;
        if (Application.Current is null) return;
        var systemDefault = BlueAir.FindSetting("use-system-default", true);
        var useDark = BlueAir.FindSetting("use-dark-theme", false);
        systemDefault.Value = UseSystemTheme.IsChecked is false;
        if (systemDefault.Value is true)
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
        else
            Application.Current.RequestedThemeVariant = useDark.Value is true ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void DarkCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is null) return;
        if (Application.Current is null) return;
        var systemDefault = BlueAir.FindSetting("use-system-default", true);
        var useDark = BlueAir.FindSetting("use-dark-theme", false);
        useDark.Value = LightDarkMode.IsChecked is true;
        if (systemDefault.Value is true)
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
        else
            Application.Current.RequestedThemeVariant = useDark.Value is true ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void UseSystemColorCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is null) return;
        if (Application.Current is not App app) return;
        var systemDefault = BlueAir.FindSetting("use-system-default-color", true);
        var color = BlueAir.FindSetting("color", BrandingColor);
        systemDefault.Value = UseSystemColor.IsChecked is false;
        if (systemDefault.Value is true)
            app.SetAccent(BrandingColor);
        else
            app.SetAccent(color.Value is uint s ? Color.FromUInt32(s) : BrandingColor);
    }

    private void AccentColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (sender is null) return;
        if (AccentColor is null) return;
        if (Application.Current is not App app) return;
        var systemDefault = BlueAir.FindSetting("use-system-default-color", true);
        var color = BlueAir.FindSetting("color", BrandingColor);
        color.Value = AccentColor.Color.ToUInt32();
        if (systemDefault.Value is true)
            app.SetAccent(BrandingColor);
        else
            app.SetAccent(color.Value is uint s ? Color.FromUInt32(s) : BrandingColor);
    }

    private void LoadSettings()
    {
        BlueAir.Init();
        if (Application.Current is not App app) return;
        var systemDefault = BlueAir.FindSetting("use-system-default", true);
        var useDark = BlueAir.FindSetting("use-dark-theme", false);
        var systemDefaultColor = BlueAir.FindSetting("use-system-default-color", true);
        var color = BlueAir.FindSetting("color", BrandingColor);

        UseSystemTheme.IsChecked = systemDefault.Value is false;
        LightDarkMode.IsChecked = useDark.Value is true;
        UseSystemColor.IsChecked = systemDefaultColor.Value is false;
        AccentColor.Color = color.Value is uint ac ? Color.FromUInt32(ac) : BrandingColor;

        if (systemDefault.Value is true)
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;

        else
            Application.Current.RequestedThemeVariant = useDark.Value is true ? ThemeVariant.Dark : ThemeVariant.Light;

        if (systemDefault.Value is true)
            app.SetAccent(BrandingColor);
        else
            app.SetAccent(color.Value is uint s ? Color.FromUInt32(s) : BrandingColor);

        if (BlueAir.Agents.Length > 0)
            foreach (var agent in BlueAir.Agents)
            {
                // TODO: All agents to settings
            }

        ReloadAgentsComboBox();

        if (BlueAir.CustomFolders != null && BlueAir.CustomFolders.Length > 0)
            foreach (var env_var in BlueAir.CustomFolders)
            {
                // TODO: Custom folder thing to settings
            }
    }

    private void ReloadAgentsComboBox()
    {
        var selected = DownloadAgents.SelectedIndex;
        DownloadAgents.Items.Clear();
        if (BlueAir.WorkingAgents.Length > 0)
            foreach (var agent in BlueAir.WorkingAgents)
                DownloadAgents.Items.Add(new ComboBoxItem { Tag = agent, Content = agent.Name });
        DownloadAgents.SelectedIndex = selected;
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

    private void OnInit(object? sender, EventArgs e)
    {
        LoadSettings();

        if (Arguments.Length > 0) LoadFile(Arguments[0]);
    }

    private void LoadFile(string path)
    {
        IsLoadedFromFile = true;
        // TODO
    }

    private async void LoadClicked(object? sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            if (Parent is not TopLevel topLevel) return;

            if (!topLevel.StorageProvider.CanOpen) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open a BlueAir file...",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("BlueAir")
                        { Patterns = "*.baf|*.xml".Split('|') },
                    FilePickerFileTypes.All
                }
            });

            if (files.Count > 0) await Dispatcher.UIThread.InvokeAsync(() => LoadFile(files[0].Path.AbsolutePath));
        });
    }

    private async void SaveClicked(object? sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            if (CurrentInfo is null) return;
            if (IsLoadedFromFile || !string.IsNullOrWhiteSpace(CurrentFile))
            {
                await using var stream = new FileStream(CurrentFile,
                    File.Exists(CurrentFile) ? FileMode.Truncate : FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                await using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(CurrentInfo.ToXml());
            }
            else
            {
                SaveAsClicked(sender, e);
            }
        });
    }

    private async void SaveAsClicked(object? sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            if (Parent is not TopLevel topLevel) return;
            if (CurrentInfo is null) return;

            if (!topLevel.StorageProvider.CanSave) return;

            var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save a BlueAir file...",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("BlueAir")
                        { Patterns = "*.baf|*.xml".Split('|'), MimeTypes = "application/blueair".Split('|') },
                    FilePickerFileTypes.All
                }
            });

            if (files != null)
            {
                IsLoadedFromFile = true;
                await using var stream = new FileStream(files.Path.AbsolutePath,
                    File.Exists(files.Path.AbsolutePath) ? FileMode.Truncate : FileMode.CreateNew, FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                await using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(CurrentInfo.ToXml());
            }
        });
    }

    private async void StartStopClicked(object? sender, RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            Dispatcher.UIThread.InvokeAsync(() => StartStop.IsEnabled = false);
            if (CurrentInfo is null || string.IsNullOrWhiteSpace(TargetFolder.Text)) return;
            CurrentInfo.OnProgressChanged += CurrentInfoProgressChanged;
            CurrentInfo.Start(TargetFolder.Text);
            CurrentInfo.OnProgressChanged -= CurrentInfoProgressChanged;
            Dispatcher.UIThread.InvokeAsync(() => StartStop.IsEnabled = true);
        });
    }

    private void CurrentInfoProgressChanged(float percentage, DownloadObject item)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var itemName = item switch
            {
                DownloadFile file => string.IsNullOrWhiteSpace(file.FileName) ? file.Link : file.FileName,
                DownloadFolder folder => folder.Name,
                _ => "???"
            };
            Logs.Text += $"""
                          [{(int)(percentage * 100)}%] "{itemName}"
                          """ + Environment.NewLine;
        });
    }

    private async void TargetFolderPicker(object? sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            if (Parent is not TopLevel topLevel) return;
            if (!topLevel.StorageProvider.CanPickFolder) return;

            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Pick a target folder...",
                SuggestedStartLocation = !string.IsNullOrWhiteSpace(TargetFolder.Text)
                    ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(TargetFolder.Text)
                    : null,
                AllowMultiple = false
            });

            if (folder.Count <= 0) return;
            TargetFolder.Text = folder[0].Path.AbsolutePath;
        });
    }

    private void DownloadAgentChanged(object? sender, SelectionChangedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        if (DownloadAgents.SelectedItem is ComboBoxItem { Tag: DownloadAgent agent })
            CurrentInfo.Downloader = agent;
    }

    private void RunCommandsBeforeAfterChanged(object? sender, RoutedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        CurrentInfo.RunCommands = RunCommandsBeforeAfter.IsChecked is true;
    }

    private void BeforeCommandChanged(object? sender, TextChangedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        CurrentInfo.CommandToExecuteBefore.Command = BeforeCommand.Text;
    }

    private void BeforeCommandRequirementChanged(object? sender, RoutedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        CurrentInfo.CommandToExecuteBefore.CanFail = BeforeCommandRequire.IsChecked is false;
    }

    private void AfterCommandChanged(object? sender, TextChangedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        CurrentInfo.CommandToExecuteAfter.Command = AfterCommand.Text;
    }

    private void AfterCommandRequirementChanged(object? sender, RoutedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        CurrentInfo.CommandToExecuteAfter.CanFail = AfterCommandRequire.IsChecked is false;
    }

    private void NewFolder(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void NewFile(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void RemoveSelected(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void NameChanged(object? sender, TextChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void URLChanged(object? sender, TextChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ItemTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void AgentRemoveSelected(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void AgentInstallFromFile(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}