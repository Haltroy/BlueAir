# BlueAir

BlueAir is a program that batch downloads files and allows processing them with commands.

## Completion

BlueAir is currently not finished. Here are the missing pieces:

- [ ] Standard Code
    - [ ] Read & save settings
    - [ ] Agents
    - [ ] Download process
- [X] CLI Interface
- [ ] GUI
    - [ ] Translations
    - [ ] Load from file
- [X] Desktop Port
- [X] Android Port

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

\*: All linux distributions (either with GNU toolkit or not).

\*\*: Termux is supported as it is a live Linux environment.

\*\*\*: Mostly FreeBSD and clones.