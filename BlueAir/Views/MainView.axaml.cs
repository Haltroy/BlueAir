using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using BlueAir.Properties;
using FluentAvalonia.UI.Controls;
using ColorChangedEventArgs = Avalonia.Controls.ColorChangedEventArgs;

namespace BlueAir.Views;

public partial class MainView : UserControl
{
    private static readonly Color BrandingColor = Color.Parse("#87c4ff");

    private string[]? Arguments;

    private string CurrentFile = string.Empty;

    private DownloadInfo? CurrentInfo;

    private bool IsLoadedFromFile;

    public MainView()
    {
        InitializeComponent();

        Version.Text = GetVersion();

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
        var systemDefault = BlueAir.FindSetting("use-system-default", "true");
        var useDark = BlueAir.FindSetting("use-dark-theme", "false");
        systemDefault.Value = UseSystemTheme.IsChecked is false ? "true" : "false";
        if (systemDefault.Value.ToLowerInvariant() == "true")
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
        else
            Application.Current.RequestedThemeVariant =
                useDark.Value.ToLowerInvariant() == "true" ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void DarkCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is null) return;
        if (Application.Current is null) return;
        var systemDefault = BlueAir.FindSetting("use-system-default", "true");
        var useDark = BlueAir.FindSetting("use-dark-theme", "false");
        useDark.Value = LightDarkMode.IsChecked is true ? "true" : "false";
        if (systemDefault.Value.ToLowerInvariant() == "true")
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
        else
            Application.Current.RequestedThemeVariant =
                useDark.Value.ToLowerInvariant() == "true" ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void UseSystemColorCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is null) return;
        if (Application.Current is not App app) return;
        var systemDefault = BlueAir.FindSetting("use-system-default-color", "true");
        var color = BlueAir.FindSetting("color", BrandingColor.ToString());
        systemDefault.Value = UseSystemColor.IsChecked is false ? "true" : "false";
        if (systemDefault.Value.ToLowerInvariant() == "true")
            app.SetAccent(BrandingColor);
        else
            app.SetAccent(Color.TryParse(color.Value, out var c) ? c : BrandingColor);
    }

    private void AccentColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (sender is null) return;
        if (AccentColor is null) return;
        if (Application.Current is not App app) return;
        var systemDefault = BlueAir.FindSetting("use-system-default-color", "true");
        var color = BlueAir.FindSetting("color", BrandingColor.ToString());
        color.Value = AccentColor.Color.ToString();
        if (systemDefault.Value.ToLowerInvariant() == "true")
            app.SetAccent(BrandingColor);
        else
            app.SetAccent(Color.TryParse(color.Value, out var c) ? c : BrandingColor);
    }

    private void LoadSettings()
    {
        BlueAir.Init();
        if (Application.Current is not App app) return;
        var systemDefault = BlueAir.FindSetting("use-system-default", "true");
        var useDark = BlueAir.FindSetting("use-dark-theme", "false");
        var systemDefaultColor = BlueAir.FindSetting("use-system-default-color", "true");
        var color = BlueAir.FindSetting("color", BrandingColor.ToString());

        UseSystemTheme.IsChecked = systemDefault.Value.ToLowerInvariant() == "false";
        LightDarkMode.IsChecked = useDark.Value.ToLowerInvariant() == "true";
        UseSystemColor.IsChecked = systemDefaultColor.Value.ToLowerInvariant() == "false";
        AccentColor.Color = Color.TryParse(color.Value, out var ac) ? ac : BrandingColor;

        if (systemDefault.Value.ToLowerInvariant() == "true")
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;

        else
            Application.Current.RequestedThemeVariant =
                useDark.Value.ToLowerInvariant() == "true" ? ThemeVariant.Dark : ThemeVariant.Light;

        if (systemDefault.Value.ToLowerInvariant() == "true")
            app.SetAccent(BrandingColor);
        else
            app.SetAccent(Color.TryParse(color.Value, out var c) ? c : BrandingColor);

        if (BlueAir.Agents.Length > 0)
            foreach (var agent in BlueAir.Agents)
                AgentsList.Items.Add(new TreeViewItem { Header = agent.Name, Tag = agent });

        ReloadAgentsComboBox();

        if (BlueAir.CustomFolders != null && BlueAir.CustomFolders.Length > 0)
            foreach (var env_var in BlueAir.CustomFolders)
            {
                DockPanel dockPanel = new() { LastChildFill = true };
                TextBlock varName = new()
                    { VerticalAlignment = VerticalAlignment.Center, Text = env_var.SpecialFolder + ":" };
                DockPanel.SetDock(varName, Dock.Left);
                dockPanel.Children.Add(varName);
                var browse = new Button { Content = "..." };
                DockPanel.SetDock(browse, Dock.Right);
                dockPanel.Children.Add(browse);
                var varValue = new TextBox { Text = env_var.GetPath };
                varValue.TextChanged += (_, _) => env_var.NewPath = varValue.Text;
                dockPanel.Children.Add(varValue);
                browse.Click += async (_, _) =>
                {
                    await Task.Run(async () =>
                    {
                        if (Parent is not TopLevel topLevel) return;
                        if (!topLevel.StorageProvider.CanPickFolder) return;

                        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                        {
                            Title = Properties.Resources.PickAFolder,
                            SuggestedStartLocation = !string.IsNullOrWhiteSpace(TargetFolder.Text)
                                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(TargetFolder.Text)
                                : null,
                            AllowMultiple = false
                        });

                        if (folder.Count <= 0) return;
                        varValue.Text = folder[0].Path.AbsolutePath;
                        env_var.NewPath = folder[0].Path.AbsolutePath;
                    });
                };
                VariableList.Children.Add(dockPanel);
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

        if (Arguments is not null && Arguments.Length > 0) LoadFile(Arguments[0]);
    }

    private void LoadFile(string path)
    {
        IsLoadedFromFile = true;
        CurrentFile = path;
        XmlDocument doc = new();
        using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            doc.Load(stream);
        }

        if (doc.DocumentElement is null) return;
        CurrentInfo = new DownloadInfo(doc.DocumentElement);
        RunCommandsBeforeAfter.IsChecked = CurrentInfo.RunCommands;
        BeforeCommand.Text = CurrentInfo.CommandToExecuteBefore.Command;
        BeforeCommandRequire.IsChecked = !CurrentInfo.CommandToExecuteBefore.CanFail;
        AfterCommand.Text = CurrentInfo.CommandToExecuteAfter.Command;
        AfterCommandRequire.IsChecked = !CurrentInfo.CommandToExecuteAfter.CanFail;

        for (var i = 0; i < DownloadAgents.Items.Count; i++)
            if (DownloadAgents.Items[i] is ComboBoxItem { Tag: DownloadAgent agent } && agent == CurrentInfo.Downloader)
            {
                DownloadAgents.SelectedIndex = i;
                break;
            }

        foreach (var item in CurrentInfo.Downloads)
            MainFolder.Items.Add(GenerateFilesAndFolders(item));
    }

    private TreeViewItem GenerateFilesAndFolders(DownloadObject item)
    {
        switch (item)
        {
            case DownloadFile file:
                var tv_item_file = new TreeViewItem { Header = file.FileName, Tag = file };
                file.AssociatedObject = tv_item_file;
                return tv_item_file;

            case DownloadFolder folder:
                var tv_item_folder = new TreeViewItem { Header = folder.Name, Tag = folder };
                folder.AssociatedObject = tv_item_folder;
                foreach (var sub_item in folder.Content) tv_item_folder.Items.Add(GenerateFilesAndFolders(sub_item));

                return tv_item_folder;
        }

        return new TreeViewItem { Header = "???", Tag = item };
    }

    private async void LoadClicked(object? sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            if (Parent is not TopLevel topLevel) return;

            if (!topLevel.StorageProvider.CanOpen) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Properties.Resources.OpenAFileTitle.Replace("{app_name}", "BlueAir",
                    StringComparison.OrdinalIgnoreCase),
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
                Title = Properties.Resources.SaveAFile.Replace("{app_name}", "BlueAir",
                    StringComparison.OrdinalIgnoreCase),
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("BlueAir")
                        { Patterns = "*.baf|*.xml".Split('|') },
                    FilePickerFileTypes.All
                }
            });

            if (files != null)
            {
                IsLoadedFromFile = true;
                CurrentFile = files.Path.AbsolutePath;
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
            CurrentInfo.OnFinishedItem += CurrentInfoOnFinishedItem;
            CurrentInfo.OnConsoleOutput += CurrentInfoOnConsoleOutput;
            CurrentInfo.Start(TargetFolder.Text);
            CurrentInfo.OnConsoleOutput -= CurrentInfoOnConsoleOutput;
            CurrentInfo.OnFinishedItem -= CurrentInfoOnFinishedItem;
            CurrentInfo.OnProgressChanged -= CurrentInfoProgressChanged;
            Dispatcher.UIThread.InvokeAsync(() => StartStop.IsEnabled = true);
        });
    }

    private void CurrentInfoOnConsoleOutput(string output)
    {
        Dispatcher.UIThread.InvokeAsync(() => Logs.Text += output + Environment.NewLine);
    }

    private void CurrentInfoOnFinishedItem(DownloadObject item)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (AutoRemoveItems.IsChecked is not true || CurrentInfo is null ||
                item.AssociatedObject is not TreeViewItem tv_item) return;
            if (item.Parent is null)
            {
                CurrentInfo.Downloads.Remove(item);
                MainFolder.Items.Remove(tv_item);
            }
            else
            {
                if (item.Parent.AssociatedObject is TreeViewItem folderParent)
                    folderParent.Items.Remove(tv_item);
                item.Parent.Content.Remove(item);
            }
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
                Title = Properties.Resources.PickATargetFolder,
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
        CurrentInfo ??= new DownloadInfo();
        if (ItemTreeView.SelectedItem is TreeViewItem item)
        {
            switch (item.Tag)
            {
                case DownloadFolder folder:
                    var folderInsideFolder = new DownloadFolder { Name = FileName.Text, Parent = folder };
                    var fifAssoc = new TreeViewItem { Header = FileName.Text, Tag = folderInsideFolder };
                    folderInsideFolder.AssociatedObject = fifAssoc;
                    folder.Content.Add(folderInsideFolder);
                    item.Items.Add(fifAssoc);
                    break;
                case DownloadFile file:
                    var parent = file.Parent;
                    var folderInsideFileParent = new DownloadFolder { Name = FileName.Text, Parent = parent };
                    var fifeAssoc = new TreeViewItem { Header = FileName.Text, Tag = folderInsideFileParent };
                    folderInsideFileParent.AssociatedObject = fifeAssoc;
                    parent.Content.Add(folderInsideFileParent);
                    if (parent.AssociatedObject is TreeViewItem parentItem)
                        parentItem.Items.Add(fifeAssoc);
                    break;
                case "MainFolder":
                    var folderInRoot1 = new DownloadFolder { Name = FileName.Text };
                    var fir1Assoc = new TreeViewItem { Header = FileName.Text, Tag = folderInRoot1 };
                    folderInRoot1.AssociatedObject = fir1Assoc;
                    CurrentInfo.Downloads.Add(folderInRoot1);
                    MainFolder.Items.Add(fir1Assoc);
                    break;
            }

            return;
        }

        var folderInRoot = new DownloadFolder { Name = FileName.Text };
        var firAssoc = new TreeViewItem { Header = FileName.Text, Tag = folderInRoot };
        folderInRoot.AssociatedObject = firAssoc;
        CurrentInfo.Downloads.Add(folderInRoot);
        MainFolder.Items.Add(firAssoc);
    }

    private void NewFile(object? sender, RoutedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        if (ItemTreeView.SelectedItem is TreeViewItem item)
        {
            switch (item.Tag)
            {
                case DownloadFolder folder:
                    var fileInsideFolder = new DownloadFile
                        { FileName = FileName.Text, Link = FileUrl.Text, Parent = folder };
                    var fifAssoc = new TreeViewItem { Header = FileName.Text, Tag = fileInsideFolder };
                    fileInsideFolder.AssociatedObject = fifAssoc;
                    folder.Content.Add(fileInsideFolder);
                    item.Items.Add(fifAssoc);
                    break;
                case DownloadFile file:
                    var parent = file.Parent;
                    var fileInsideFileParent = new DownloadFile
                        { FileName = FileName.Text, Link = FileUrl.Text, Parent = parent };
                    var fifeAssoc = new TreeViewItem { Header = FileName.Text, Tag = fileInsideFileParent };
                    fileInsideFileParent.AssociatedObject = fifeAssoc;
                    parent.Content.Add(fileInsideFileParent);
                    if (parent.AssociatedObject is TreeViewItem parentItem)
                        parentItem.Items.Add(fifeAssoc);
                    break;
                case "MainFolder":
                    var fileInRoot1 = new DownloadFile { FileName = FileName.Text, Link = FileUrl.Text };
                    var fir1Assoc = new TreeViewItem { Header = FileName.Text, Tag = fileInRoot1 };
                    fileInRoot1.AssociatedObject = fir1Assoc;
                    CurrentInfo.Downloads.Add(fileInRoot1);
                    MainFolder.Items.Add(fir1Assoc);
                    break;
            }

            return;
        }

        var fileInRoot = new DownloadFile { FileName = FileName.Text, Link = FileUrl.Text };
        var firAssoc = new TreeViewItem { Header = FileName.Text, Tag = fileInRoot };
        fileInRoot.AssociatedObject = firAssoc;
        CurrentInfo.Downloads.Add(fileInRoot);
        MainFolder.Items.Add(firAssoc);
    }

    private void RemoveSelected(object? sender, RoutedEventArgs e)
    {
        CurrentInfo ??= new DownloadInfo();
        if (ItemTreeView.SelectedItem is not TreeViewItem { Tag: DownloadObject download } item) return;
        if (download.Parent is null)
        {
            CurrentInfo.Downloads.Remove(download);
            MainFolder.Items.Remove(item);
        }
        else
        {
            if (download.Parent.AssociatedObject is TreeViewItem folderParent)
                folderParent.Items.Remove(item);
            download.Parent.Content.Remove(download);
        }
    }

    private void NameChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Control { IsEnabled: true } ||
            ItemTreeView.SelectedItem is not TreeViewItem { Tag: DownloadObject download }) return;
        switch (download)
        {
            case DownloadFile file:
                file.FileName = FileName.Text;
                break;
            case DownloadFolder folder:
                folder.Name = FileName.Text;
                break;
        }
    }

    private void URLChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is Control { IsEnabled: true } &&
            ItemTreeView.SelectedItem is TreeViewItem { Tag: DownloadFile file })
            file.Link = FileUrl.Text;
    }

    private void ItemTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        FileName.IsEnabled = false;
        FileUrl.IsEnabled = false;
        if (ItemTreeView.SelectedItem is TreeViewItem { Tag: DownloadObject download })
        {
            FileName.Text = download switch
            {
                DownloadFile file => file.FileName,
                DownloadFolder folder => folder.Name,
                _ => FileName.Text
            };

            FileUrl.Text = download is DownloadFile _file ? _file.Link : string.Empty;
        }

        FileName.IsEnabled = true;
        FileUrl.IsEnabled = true;
    }

    private void AgentRemoveSelected(object? sender, RoutedEventArgs e)
    {
        if (AgentsList.SelectedItem is TreeViewItem { Tag: DownloadAgent agent } selectedItem)
        {
            if (agent.Exists())
            {
                var found = DownloadAgents.Items.Where(it => it is ComboBoxItem item && item.Tag == agent).ToArray();
                foreach (var it in found) DownloadAgents.Items.Remove(it);
            }

            AgentsList.Items.Remove(selectedItem);
            BlueAir.UninstallAgent(agent);
        }
    }

    private async void AgentInstallFromFile(object? sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            if (Parent is not TopLevel topLevel) return;

            if (!topLevel.StorageProvider.CanOpen) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Properties.Resources.PickAnAgentfile.Replace("{app_name}", "BlueAir",
                    StringComparison.OrdinalIgnoreCase),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Properties.Resources.AgentFileDesc.Replace("{app_name}", "BlueAir",
                            StringComparison.OrdinalIgnoreCase))
                        { Patterns = "*.bada|*.xml".Split('|') },
                    FilePickerFileTypes.All
                }
            });

            if (files.Count > 0)
            {
                var agent = BlueAir.InstallAgent(files[0].Path.AbsolutePath);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AgentsList.Items.Add(new TreeViewItem { Header = agent.Name, Tag = agent });
                    if (agent.Exists())
                        DownloadAgents.Items.Add(new ComboBoxItem { Content = agent.Name, Tag = agent });
                });
            }
        });
    }

    private async void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        await Task.Run(async () =>
        {
            if (Parent is not TopLevel topLevel) return;

            if (!topLevel.StorageProvider.CanSave) return;

            var lobster = BlueAir.FindSetting("lobster", "false");

            if (lobster.Value.ToLowerInvariant() == "true") return;

            var lobsterFile = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "BlueAir",
                SuggestedFileName = "LOBSTER",
                FileTypeChoices = new[] { FilePickerFileTypes.All },
                ShowOverwritePrompt = true
            });

            lobster.Value = "true";

            if (lobsterFile is null) return;
            await using var stream = await lobsterFile.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteLineAsync(Lobster.Code);


            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Content is TabView tabView)
                    tabView.SelectedIndex = 0;
            });
        });
    }
}