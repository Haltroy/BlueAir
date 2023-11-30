using System;
using Avalonia.Controls;

namespace BlueAir.Views;

public partial class MainWindow : Window
{
    private string[] Arguments = Array.Empty<string>();

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow WithArguments(string[] args)
    {
        Arguments = args;
        return this;
    }

    private void Init(object? sender, EventArgs e)
    {
        Content = new MainView().WithArguments(Arguments);
    }
}