# HT ISO Creator 1.0.1 — User Guide

HT ISO Creator makes **data ISO images** from a folder on your computer and checks that each image is correct. An ISO image is a single file that holds a complete copy of the folder. You can open it in Windows, burn it to a disc, or keep it as an archive.

> **Data images only.** HT ISO Creator does not add boot functionality. An image it creates won't start a computer, even if the source folder contains operating-system setup files.

## 1. Main window at a glance

| Section | What it is for |
|---|---|
| **Source** | The folder to put into the image, with the number of files and subfolders, the source size, the exact size of the ISO, the file system and the free space at the destination. |
| **ISO Information** | The **Volume Name** (the disc label), the **File system** and the **Target disc** size. |
| **Filename Compatibility** | **Strict Mode** or **Compatibility Mode**, a line saying whether every name will be kept exactly, *Preview conversions...* and *Export Report...*. |
| **Output Destination** | The **ISO File** to create, and the verification options. |
| **Progress** | Progress, the current file, the amount written, elapsed and remaining time, and speed. |

## 2. Select a source folder
Do one of the following:
- Click **Browse...** next to *Source Folder*, or choose **File > Select Source Folder...** (Ctrl+O).
- Drag a folder from File Explorer onto the window.
- Type or paste the folder path into *Source Folder*.

The folder is analysed straight away. **Refresh** (Ctrl+R) analyses it again if its contents change. HT ISO Creator only ever reads the source folder; it never changes, renames or deletes anything in it.

**Links and junctions are not followed.** Folder shortcuts that point elsewhere (symbolic links and junctions) are skipped and listed as warnings, so the image can't accidentally contain the same data twice or a whole other drive.

## 3. Choose a file system
Pick one in **File system**. [FILESYSTEM_GUIDE.md](FILESYSTEM_GUIDE.md) explains the choice in detail.

| File system | Choose it when |
|---|---|
| **ISO 9660 + Joliet (64-character names)** (default) | General data discs. Readable almost everywhere. |
| **UDF (127-character names, files over 4 GB)** | Names longer than 64 characters (for example long DLL names), or files of 4 GB or more. |
| **ISO 9660 + Joliet + UDF (hybrid)** | The UDF benefits for names, plus extra names for older readers. Files must be under 4 GB. |
| **ISO 9660 only (8.3-style names)** | Very old systems only. Almost every name changes. |

You can set the default file system in **Tools > Settings**.

## 4. Select a target disc capacity
**Target disc** compares the exact image size with the capacity of a CD (650 or 700 MB), a DVD (4.7 GB or Dual Layer 8.5 GB), a Blu-ray disc (25 GB), or a **Custom size** in MB. The line beside it shows how full the disc would be, or warns that the image is too large.

The target disc never changes the image. "Fits" also doesn't guarantee that a particular burner or blank disc will accept it. **No capacity limit** turns the comparison off.

## 5. Configure the volume label
**Volume Name** is the label Windows shows when the image is opened. It can have up to 16 characters (up to 32 with *ISO 9660 only*), using the letters A–Z, the digits 0–9 and the underscore. HT ISO Creator suggests a label from the folder name, and any problem is shown under the box.

## 6. Strict Mode and Compatibility Mode
Each file system has limits on names, such as 64 characters for Joliet, 127 for UDF, and characters like `;` that Joliet doesn't allow. The **Filename Compatibility** section decides what happens when a name doesn't fit.

### Strict Mode — Preserve Original Filenames (recommended, default)
Every file and folder must keep its exact name. If any name would change, **no image is created**:
- The **Filename Compatibility** window lists each affected item with its original name, the name it would get, and the reason.
- Nothing is written, and an existing ISO with the same name stays exactly as it was.
- To go ahead, rename the item in the source folder, choose a file system that can store the name (UDF keeps names up to 127 characters), or switch to Compatibility Mode.

Use Strict Mode for software, drivers and anything else that finds its files by exact name.

### Compatibility Mode — Allow Filename Conversion
Names that don't fit are converted: shortened with the extension kept, or with unsupported characters replaced by `_`. If two converted names would clash, a number is added (`name~1.ext`). Before anything is written:
1. The Filename Compatibility window opens and lists every conversion side by side: **Original name**, **Name in image**, **Reason**, **Type** and **Folder**. DLLs, programs and clashing names are highlighted.
2. **Create ISO stays disabled** until you tick *"I have reviewed all N name conversion(s) and accept them."* The tick box also says how many of the renamed files are DLLs or program files, which may stop working.

The mode you choose is remembered.

## 7. Preview filename conversions
Before creating anything, you can check what would happen. Click **Preview conversions...** in the Filename Compatibility section, or choose **Tools > Filename Compatibility...** (Ctrl+K). The window shows:
- **Findings:** errors, warnings and information, such as empty folders, zero-byte files, files over 2 GB, deep folders, and links that will be skipped.
- **Affected items** for the selected finding.
- **Name conversion preview:** every name that would change. You can filter the list.

Nothing is written during a preview.

## 8. Create an ISO
1. Check the **ISO File** path, or click **Save As...** (Ctrl+S) to choose one.
2. Click **Create ISO**, press **F5**, or choose **File > Create ISO**.
3. If the file already exists, you're asked whether to replace it. The existing file is kept until the new image is complete. A **read-only** ISO is never replaced.
4. If there are problems or name conversions, the Filename Compatibility window opens first.
5. The image is written while the Progress section shows how far it has got. **Cancel** or **Esc** stops at any time; the unfinished file is removed and an existing ISO is left unchanged. With the UDF formats, cancelling takes effect once Windows has finished laying out the image, which takes about 3 seconds per 1,000 files.
6. When it finishes, a summary shows the file, size, file system, verification result and file names.

Your free disk space is checked before writing, using the exact image size. If HT ISO Creator is closed unexpectedly while writing, the unfinished file is removed the next time it starts.

## 9. Verify an ISO
**Automatic verification.** With **Verify the image after creating it** ticked, each new image is checked straight away:

| Check | What it confirms |
|---|---|
| Structure | The image's internal records (ISO 9660, Joliet and UDF) are complete and consistent, and every file's data lies inside the image. |
| File names | Every name matches the original (Strict Mode) or the conversions you approved (Compatibility Mode), letter case included. |
| Files, folders and sizes | Every file and folder is present, with nothing extra, and every size matches. |
| **SHA-256** (if ticked) | Every file's content is read back from the image and compared with the original. |

**Checking an existing image.** Choose **Tools > Verify ISO...** (Ctrl+Shift+V) and select the ISO:
- **Without a source folder,** only the structure is checked.
- **With the source folder,** paths, sizes and names are compared as well. **File names** sets the rule: *Must match originals (Strict)*, *Approved conversions (Compatibility)* or *Report differences only*.
- **Compare file contents with SHA-256** reads every file again.
- **Also mount with Windows** mounts the image with Windows' own driver and compares what Windows lists.

**Results.** Every check is shown as **Passed**, **Failed**, **Warning** or **Skipped**. A skipped check is never counted as passed. The verification level is always stated: *Structural verification only*, *Source verification (paths and sizes)* or *Full source-content verification (paths, sizes and SHA-256)*. **Save Report...** saves the result as text.

## 10. Mount an ISO in Windows
Windows 10 and 11 can open ISO files without extra software. Double-click the ISO in File Explorer, or right-click it and choose **Mount**. The image appears as a new drive. To finish, right-click that drive in *This PC* and choose **Eject**.

HT ISO Creator's mount check (above) does this for you and ejects the drive afterwards.

## 11. Export reports
| Report | Where |
|---|---|
| Filename conversion report (TXT or CSV) | **Export Report...** in the main window; **Save Report...** in the Filename Compatibility window; **Name report...** in the completion summary |
| Verification report (TXT) | **Save Report...** in the Verify ISO window, or **View Report** in the completion summary |
| Creation history (CSV) | **Tools > Creation History...** (Ctrl+H) > *Export* |

The conversion report includes the date, source folder, destination ISO, file system, mode and status, plus each original and converted path with the reason and any clashes. CSV files are saved as UTF-8, so Excel shows non-English names correctly.

## 12. Interpret warnings and errors
- **Error:** the image can't be created until it is resolved. Examples: a file of 4 GB or more with a file system other than UDF, an unreadable or locked file, an invalid volume name or output path, not enough disk space, or a name change in Strict Mode. Nothing is written.
- **Warning:** the image can be created, but read the warning first. Examples: names that will change in Compatibility Mode, renamed program files, files over 2 GB (some old systems can't read them), deep folders with *ISO 9660 only*, or an image larger than the target disc.
- **Information:** for your awareness only. Examples: empty folders and zero-byte files (both are included), or the extra name views in hybrid images.

Error messages give a short **reference** such as `8c03145c`. It identifies the operation in the log file (**Tools > View Today's Log**), which you can quote when asking for support. [TROUBLESHOOTING.md](TROUBLESHOOTING.md) explains common messages.

## 13. Other tools
- **ISO Information** (Ctrl+I): shows what an existing image actually records — labels, file systems (including the UDF version), dates, boot record presence (detected only), and the directory tree.
- **Creation History** (Ctrl+H): every attempt with its result and verification. You can search, sort, verify again, open the folder or export the list.
- **Settings** (Ctrl+,): default output folder, file system, volume name, target disc, verification, remembering the last folders, completion notification and theme (System, Light or Dark).

## 14. Keyboard shortcuts
| Keys | Action |
|---|---|
| Ctrl+O | Select source folder |
| Ctrl+S | Choose output file |
| Ctrl+R | Refresh source analysis |
| F5 | Create ISO |
| Esc | Cancel creation or verification |
| Ctrl+Shift+V | Verify ISO |
| Ctrl+I | ISO information |
| Ctrl+K | Filename compatibility |
| Ctrl+H | Creation history |
| Ctrl+, | Settings |
| F1 | Keyboard shortcuts |

Every control can be reached with Tab. Underlined letters work with Alt; for example, Alt+M selects Compatibility Mode.
