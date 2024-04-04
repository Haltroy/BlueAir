using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BlueAir.ViewModels;
using BlueAir.Views;

namespace BlueAir;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Properties.Resources.Culture = CultureInfo.CurrentCulture;
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                }.WithArguments(desktop.Args ?? Array.Empty<string>());
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void SetAccent(Color color)
    {
        Resources["SystemAccentColor"] = color;
        Resources["SystemAccentColorDark1"] = color;
        Resources["SystemAccentColorDark2"] = ShiftBrightness(color, 20);
        Resources["SystemAccentColorDark3"] = ShiftBrightness(color, 40);
        Resources["SystemAccentColorLight1"] = color;
        Resources["SystemAccentColorLight2"] = ShiftBrightness(color, 20);
        Resources["SystemAccentColorLight3"] = ShiftBrightness(color, 40);
    }

    private static Color ShiftBrightness(Color c, int value, bool shiftAlpha = false)
    {
        return new Color(
            shiftAlpha
                ? !IsTransparencyHigh(c)
                    ? (byte)AddIfNeeded(c.A, value, byte.MaxValue)
                    : (byte)SubtractIfNeeded(c.A, value)
                : c.A,
            !IsBright(c) ? (byte)AddIfNeeded(c.R, value, byte.MaxValue) : (byte)SubtractIfNeeded(c.R, value),
            !IsBright(c) ? (byte)AddIfNeeded(c.G, value, byte.MaxValue) : (byte)SubtractIfNeeded(c.G, value),
            !IsBright(c) ? (byte)AddIfNeeded(c.B, value, byte.MaxValue) : (byte)SubtractIfNeeded(c.B, value));
    }

    private static int Brightness(Color c)
    {
        return (int)Math.Sqrt(
            c.R * c.R * .241 +
            c.G * c.G * .691 +
            c.B * c.B * .068);
    }

    private static bool IsTransparencyHigh(Color c)
    {
        return c.A < 130;
    }

    private static bool IsBright(Color c)
    {
        return Brightness(c) > 130;
    }

    private static int SubtractIfNeeded(int number, int subtract, int limit = 0)
    {
        return limit == 0 ? number > subtract ? number - subtract : number :
            number - subtract < limit ? number : number - subtract;
    }

    private static int AddIfNeeded(int number, int add, int limit = int.MaxValue)
    {
        return number + add > limit ? number : number + add;
    }
}