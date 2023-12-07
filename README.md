# BlueAir

BlueAir is a program that batch downloads files and allows processing them with commands.

## Availability

BlueAir is available to these platforms with these options

| Platform  | CLI    | GUI | Notes                                                                         |
|-----------|--------|-----|-------------------------------------------------------------------------------|
| Windows   | Yes    | Yes |                                                                               |
| Linux\*   | Yes    | Yes |                                                                               |
| macOS     | Yes    | Yes | Commands are disabled.                                                        |
| Android   | No\*\* | Yes | Commands are disabled.                                                        |
| iOS       | No     | No  | No planned support on iOS.                                                    |
| BSD\*\*\* | No     | No  | No plans but if .NET SDK is installed it is possible to build it from source. |

\*: All linux distributions (either with GNU toolkit or not). Currently Avalonia requires X11 for GUI, if on Wayland
please install XWayland to your system.

\*\*: Termux is supported as it is a live Linux environment.

\*\*\*: See [.NET on FreeBSD wiki](https://wiki.freebsd.org/.NET).

## Get BlueAir

Head to [`Download BlueAir page on haltroy.com`](https://haltroy.com/blueair) to download it.

## Build

BlueAir requires .NET SDK 8.0 or newer.

Clone this repository:

````sh
git clone https://github.com/haltroy/BlueAir.git
````

To build the Standard library, open up a terminal in BlueAir.Standard folder and
execute `dotnet build`. Built files should be in bin folder.

To build the CLI, open up a terminal in BlueAir.CLI folder and execute `dotnet build`. To run it, you can simply run the
BlueAir.CLI executable in the bin folder or with `dotnet run`

To build for desktop, open up a terminal in BlueAir.Desktop folder and execute `dotnet build`. To run it, you can simply
run the BlueAir.Desktop executable in the bin folder or with `dotnet run`. If you are building it for macOS,
use `dotnet workload restore` (requires administrative privileges) before using the `dotnet builld`.

To build for Android, open up a terminal in BlueAir.Android folder and execute `dotnet workload restore` (requires
administrative privileges) then `dotnet build`.
