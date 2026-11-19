# HT ISO Creator 1.0.1 — Installation

## Requirements
| | |
|---|---|
| Windows | Windows 10 or Windows 11 (64-bit or 32-bit) |
| .NET Framework | **4.6.2 or later.** Windows 10 version 1607 and later and Windows 11 already include it (Windows 11 has 4.8). |
| UDF formats | The Windows IMAPI2 file system component, which is part of Windows 10 and 11. |
| Disk space | About 1 MB for the program, plus room for the images you create. |
| Administrator rights | Needed only to install for all users. "Install for me only" and the portable version don't need them. |

## Option 1: Installer (recommended)
1. Run `HTISOCreator_Setup_v1.0.1.exe`.
2. Choose **Install for all users** (needs administrator rights; installs to `C:\Program Files\HT ISO Creator`) or **Install for me only** (no administrator rights; installs to `%LOCALAPPDATA%\Programs\HT ISO Creator`).
3. Read the **licence agreement** and accept it to continue. HT ISO Creator is free for personal, non-commercial use; see `LICENSE.txt`.
4. Follow the wizard. You can change the folder, and optionally create a desktop shortcut.
5. HT ISO Creator appears in the Start menu.

The installer:
- adds an entry to **Settings > Apps > Installed apps** so you can uninstall it;
- installs no services, adds nothing to Windows startup, and includes no advertising or other software;
- stops with a clear message if .NET Framework 4.6.2 or later is missing.

### Silent installation (for administrators)
```
HTISOCreator_Setup_v1.0.1.exe /VERYSILENT /SUPPRESSMSGBOXES /ALLUSERS
HTISOCreator_Setup_v1.0.1.exe /VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER
```
A silent installation accepts the licence agreement in `LICENSE.txt` on the user's behalf; only use it where that is appropriate. Add `/TASKS="desktopicon"` for a desktop shortcut, and `/DIR="D:\Tools\HT ISO Creator"` for another folder.

### Upgrading
Run the newer installer. It finds the existing installation and replaces the program files in the same folder, keeping your settings and history. If HT ISO Creator is open, setup asks you to close it first, then continues when you click OK. A silent installation stops with exit code 1 instead of replacing files that are in use.

### Uninstalling
Use **Settings > Apps > Installed apps > HT ISO Creator > Uninstall**. The program files, shortcuts and uninstall entry are removed.

Your settings and history are **kept**, so a later reinstall remembers them. To remove them too, delete these folders:
- `%APPDATA%\HTREASURE LTD\HT ISO Creator` (settings, history)
- `%LOCALAPPDATA%\HTREASURE LTD\HT ISO Creator` (logs)

## Option 2: Portable version
1. Extract `HTISOCreator_Portable_v1.0.1.zip` to any folder you can write to, for example `D:\Tools`.
2. Run `HTISOCreator.exe`.

Nothing is installed and no administrator rights are needed. Keep all the files together: `HTISOCreator.exe`, `HTISOCreator.exe.config` and the three `DiscUtils.*.dll` files. Settings and history are stored in the same per-user folders as the installed version. To remove the portable version, delete its folder.

If Windows shows "Windows protected your PC" (SmartScreen) for a downloaded file, check it against `SHA256SUMS.txt` (see below). Then choose **More info > Run anyway** only if the checksum matches. Version 1.0.1 is not digitally signed.

## Checking a download
Each release includes `SHA256SUMS.txt`. In PowerShell, run the following and compare the result with the line for that file:
```
Get-FileHash .\HTISOCreator_Setup_v1.0.1.exe -Algorithm SHA256
```

## Upgrading from a development build (Stages 1–3)
Development builds stored their data in `%APPDATA%\HT Software\HT ISO Creator`. Version 1.0.0 and later copy your settings and history from there the first time it starts. The old folder is not changed or deleted.
