# HT ISO Creator 1.1.0 — Troubleshooting

Most messages explain the problem and what to do. Error messages also include a short **reference** (for example `8c03145c`). Search for it in the log (**Tools > View Today's Log**) to see the technical details. Logs are kept for 30 days in `%LOCALAPPDATA%\HTREASURE LTD\HT ISO Creator\Logs`.

## Creating an image
| Message or symptom | Cause and solution |
|---|---|
| **"Strict Mode: N file or folder name(s) cannot be stored exactly…"** | Some names don't fit the chosen file system. The Filename Compatibility window lists them. Choose **UDF** for names of up to 127 characters, rename the items, or use **Compatibility Mode** to allow the listed conversions. |
| **Create ISO is disabled in the Filename Compatibility window** | In Compatibility Mode you must first tick *"I have reviewed all N name conversion(s)…"*. If there are errors, they must be fixed first. |
| **"… cannot store files of 4 GB or larger"** | Only **UDF** stores files of 4 GB and larger. Choose *UDF* in **File system**. |
| **"Cannot read … the file is in use by another program"** | Another program has the file open for writing (for example Visual Studio's `.vs` folder, a database or a virtual machine disk). Close that program, or leave the folder out of the source, then click **Refresh**. Files are never copied while they might be changing. |
| **"There is not enough free space on the output drive"** | The exact image size is shown. Free some space, or choose another drive with **Save As...**. Nothing has been written. |
| **"The existing ISO file is read-only…"** | Read-only ISOs are never replaced. Clear *Read-only* in the file's Properties, or choose another name. |
| **"'CON' is a name Windows reserves for a device…"** | `CON`, `PRN`, `AUX`, `NUL`, `COM0`–`COM9` and `LPT0`–`LPT9` can't be used as file names, even with `.iso` added. Choose another name. |
| **Cancel takes a few seconds with UDF** | Windows lays out the whole UDF image before anything is written, and that step can't be interrupted (about 3 seconds per 1,000 files). Cancelling still removes everything once it takes effect. |
| **"The ISO file cannot be saved inside the source folder…"** | The image would include itself. Save it somewhere else. |
| **"The volume name may only contain…" / "…at most 16 characters"** | Use A–Z, 0–9 and `_` only, and no more than 16 characters (32 with *ISO 9660 only*). |
| **"A source file changed while the image was being created"** | Something modified a file during creation. The partial image was removed. Close the program that is changing it and try again. |
| **"The UDF formats use the Windows IMAPI2 component, which is not available"** | The IMAPI2 component of Windows is missing or disabled (rare; some reduced Windows editions). Use *ISO 9660 + Joliet*, or restore the component (Windows Update or `sfc /scannow`). |
| **"Windows IMAPI2 could not lay out the image: … (HRESULT 0x…)"** | Windows rejected the layout. Note the HRESULT and the reference from the log. Trying *ISO 9660 + Joliet* or *UDF* instead of the hybrid often helps. |
| **The window is larger than my screen** | The main window has a fixed size. At high display scaling on a small screen it is made to fit the screen and gets scroll bars, so every control can still be reached. It remembers its position. |
| **Creation was interrupted (crash or power loss)** | On the next start HT ISO Creator removes the unfinished file, restores an ISO it was replacing, and tells you what it did. |

## Verification
| Message | Cause and solution |
|---|---|
| **File names: Failed — "… differ from the originals, which Strict Mode does not allow"** | The image contains converted names (for example one made in Compatibility Mode). This is correct if the conversions were intended: verify with *File names: Approved conversions (Compatibility)*. |
| **File contents (SHA-256): Failed** | A file's content in the image differs from the source. Usually the source changed after the image was made. Otherwise, create the image again. |
| **Missing from image / In the image but not in the source** | The source folder changed since the image was made, or the wrong folder or image was selected. |
| **Windows mount: Failed** | Windows couldn't mount the image. Policy may block mounting (common on managed PCs), the image may be open in another program, or it may be on a network drive that can't be mounted. The other checks are still valid. |
| **"… is not an ISO 9660 image or a UDF image"** | The file isn't a disc image (for example a renamed ZIP), or it is badly damaged. |
| **"UDF: … which this reader does not support"** | Images from other tools sometimes use UDF features outside UDF 1.02–2.01 physical partitions. The image may still be valid; mount it in Windows to check it. |

## Security Advisory History
| Message | Cause and solution |
|---|---|
| **"Offline history: snapshot bundled with this release…"** | Normal. The advisories shown come from the release you installed, so they are historical, not a current assessment. Newer advisories appear when you install a newer release. |
| **"The advisory service is not configured for this product."** | HTREASURE LTD has no online advisory service for HT ISO Creator yet, so **Check for Advisories** can't fetch newer data. Nothing is wrong with your installation. |
| **"No verified advisory data is available…" / "could not be verified"** | The bundled advisory snapshot is missing, unsigned or altered, so it isn't shown. The status is then *Unknown*. Reinstall HT ISO Creator from a file whose SHA-256 matches `SHA256SUMS.txt`. |
| **"Development build: the bundled advisory snapshot is not signed."** | Shown only by internal test builds, never by a release. |

## Source folders
- **Network folders** (`\\server\share\…`) work, but are only as fast and reliable as the network. Avoid shares that other people are changing at the same time.
- **OneDrive folders:** files that are *online-only* are downloaded as they are read, so creation may be slower and needs an internet connection. To avoid that, right-click the folder and choose *Always keep on this device* first.
- **Long paths (over 260 characters)** need Windows long path support. It is switched on by the policy *Enable Win32 long paths*, or by `LongPathsEnabled=1` under `HKLM\SYSTEM\CurrentControlSet\Control\FileSystem`.
- **Read-only files and folders** are read normally. They are never modified.

## Installation
| Symptom | Solution |
|---|---|
| **"HT ISO Creator needs Microsoft .NET Framework 4.6.2 or later"** | Install .NET Framework 4.8 from Windows Update or Microsoft, then run the installer again. |
| **"Windows protected your PC" (SmartScreen)** | The program and installer of version 1.1.0 are not digitally signed. Check the file against `SHA256SUMS.txt` (see [INSTALLATION.md](INSTALLATION.md)), then choose **More info > Run anyway**. |
| **"Setup has detected that HT ISO Creator is currently running"** | Close every HT ISO Creator window, then click **OK**. A silent installation stops with exit code 1 in this case; close the program and run it again. |
| **"This program does not support the version of Windows your computer is running"** | HT ISO Creator needs Windows 7 SP1, 8.1, 10 or 11. On Windows 7, install Service Pack 1; the original Windows 8 needs the free update to Windows 8.1. |
| **Windows mount check is unavailable (Windows 7)** | Windows 7 can't mount ISO images. Use the other verification checks; they work the same. |
| **Settings are gone after reinstalling** | Settings are kept on uninstall unless their folder was deleted: `%APPDATA%\HTREASURE LTD\HT ISO Creator`. |

## Getting help
Please include:
- the version (**Help > About HT ISO Creator**; hovering over the version shows the program path, .NET and Windows details);
- the reference from the message;
- the log file for that day.
