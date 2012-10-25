# OpenThings is a keyboard-powered app launcher for Windows. 

----------

## Background

I used the excellent [Launchy](http://www.launchy.net) for a long time, but it started to crash a lot and development seems to have stalled, so I began looking for an alternative. As it turned out, all the alternatives were either way more than I needed, or looked so horrible I didn't want anything to do with them.

So I built this during a couple of lunch hours. OpenThings lets you... well, open things. There's no support, no real testing (apart from the fact I use it every day) and it's probably got some really dodgy code in it—but it does basically work.

----------

## System Requirements

Windows 7 or Server 2008 with the .NET Framework 4 installed—it might well work on previous versions, but I haven't even tried them out).

----------

## Usage

Put a file named paths.txt in the same folder as the executable, then add the paths you'd like to search for apps and shortcuts to it, one per line:

    C:\ProgramData\Microsoft\Windows\Start Menu
    C:\Users\YOURUSERNAME\AppData\Roaming\Microsoft\Windows\Start Menu

The example above will search both the All Users start menu and your user start menu. Launch the app, and it will recursively find all the shortcuts (.lnk) and .exe files within each folder you specified.

Then just hit `ALT-SPACE`* and type a few characters which from the name of the app you're looking for. If the first (selected) item in the list is the one you want, just hit `ENTER` to launch it. If not, use the up and down arrows to select the one you do want, then hit `ENTER`.

----------

## License

This utility is licensed under the [GNU GPL 3.0](http://opensource.org/licenses/gpl-3.0.html).

----------

*I know this is a Windows system shortcut, but I never used it and it's the same one I used with Launchy. If you don't like it, fork the code and put in your own preferred shortcut(s).


