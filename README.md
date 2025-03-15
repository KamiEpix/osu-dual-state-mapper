# osu-dual-state-mapper
A specialized key/button mapper for Osu! that treats press and release as separate held inputs. Map any key or mouse button to create unique play styles. Only activates in Osu!, features zero-delay processing and easy remapping.

## Why Use This?
Unlike regular key mappers that simply convert one key to another, this mapper:
- Treats every press AND release as a separate input
- Each input triggers and holds your chosen output key
- Only activates when Osu! is your active window
- Works with both keyboard and mouse buttons
- Zero input delay implementation

Example using Space→Z mapping:
1. Press Space = Send and hold Z
2. Release Space = Send and hold a new Z
3. Each new action releases the previous hold

## Download & Install
1. Go to [Releases](../../releases)
2. Download the latest `DualStateMapper.exe`
3. Run it (requires admin privileges)

## Quick Start
1. Launch the mapper
2. Choose your input (any key or mouse button)
3. Choose your output key (like Z or X)
4. Start playing!

Note: To remap keys, focus the console window before pressing R

Commands (when console window is focused):
- R: Remap keys/buttons
- Q: Quit

## Features
- Press and release are separate inputs
- Each input maintains its hold state
- Automatic Osu! window detection
- Keyboard and mouse support
- Simple remapping interface
- No input delay

## Requirements
- Windows OS
- .NET Framework 4.7.2+
- Admin privileges

## Building From Source
1. Clone the repository
2. Open in Visual Studio 2019+
3. Build for Release (x64)
4. Find executable in bin/Release

## License
This project is licensed under GPL-3.0. This means:
- You can modify and distribute this software
- You must keep it open source
- Commercial use is not permitted
- Any modifications must also be GPL-3.0

## Acknowledgments
Created for the Osu! community. Feel free to report issues or suggest improvements!
