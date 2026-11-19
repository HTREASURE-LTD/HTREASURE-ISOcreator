# Changelog

All notable changes to HT ISO Creator. Version 1.0.0 is the first public release. The development builds listed below it were internal. Their internal numbers (1.0.2, 1.0.3) were never released, and 1.0.0 supersedes them.

## 1.0.2: end-user licence agreement

### Added
- **End-user licence agreement** (`LICENSE.txt`): free for personal, non-commercial use; business use needs a separate licence from HTREASURE LTD; governed by the law of England and Wales.
  - The installer shows it, and it must be accepted before installing.
  - It is installed next to the program, included in the portable and documentation packages, and opens from **Help > Licence Agreement** and **About > View licence agreement**.

## 1.0.1: fixed-size window, menu below the banner, website link

### Changed
- **Main window has a fixed size:** it can no longer be resized or maximised. Its position is still remembered; a size or maximised state saved by 1.0.0 is ignored. On a screen too small for the window (high display scaling), it is made to fit and scrolls.
- **Menu below the banner:** File, Tools and Help now sit directly below the HT ISO Creator logo and title, instead of above them.

### Added
- **HTREASURE LTD website:** a [www.htreasure.com](https://www.htreasure.com) link in the main window banner and in the About window. The installer lists it as the publisher's website in Windows' installed apps.

## 1.0.0: first public release (Stage 4)

### Added
- **Help menu:** User Guide, File System Guide, Troubleshooting, Release Notes and Third-Party Notices. They open from the `Documentation` folder installed with the program.
- **About window:** shows the publisher (HTREASURE LTD), the version with its build and configuration (for example "1.0.0 (build 1.0.0.0, Release)"), and the support details (program path, .NET runtime, Windows version) in a tooltip. It states that the program creates data images and does not create bootable images.
- **Window position:** the main window remembers its size, position and maximised state. A saved position that is no longer on any screen is ignored.
- **Screen reader names** for the source, output, volume, file system and target disc fields, the progress bar and the status lines.
- **Windows installer** (`installer\HTISOCreator.iss`, Inno Setup 6):
  - installs for all users or for the current user only;
  - Start Menu shortcut, optional desktop shortcut, uninstall entry;
  - upgrades in place and keeps user settings;
  - checks for .NET Framework 4.6.2;
  - closes a running copy before replacing files.
- **Release build script** (`Tools\Build-Release.ps1`): a clean Release build with warnings treated as errors, tests, the installer, portable, documentation and source packages, and `SHA256SUMS.txt`.
- **Documentation:** README, INSTALLATION, USER_GUIDE, FILESYSTEM_GUIDE, TROUBLESHOOTING, RELEASE_NOTES, TEST_REPORT and THIRD_PARTY_NOTICES.

### Changed
- **Version and publisher:** version 1.0.0 (file version 1.0.0.0); publisher, company and copyright are HTREASURE LTD (previously "HT Software").
- **Data folders:** settings and history are in `%APPDATA%\HTREASURE LTD\HT ISO Creator`, and logs are in `%LOCALAPPDATA%\HTREASURE LTD\HT ISO Creator`. The first start copies settings and history from the development builds' `HT Software` folder, without changing or deleting it. An unfinished operation recorded by a development build is still cleaned up.
- The developer README moved to `docs/DEVELOPMENT_NOTES.md`; `README.md` now describes the product.

### Fixed
- **Read-only ISOs:** replacing an existing **read-only** ISO reported success but left a `.backup` file beside it, because the backup couldn't be deleted. Read-only ISOs are now never replaced; the app says why and suggests a fix, and nothing is written.
- **Reserved file names:** an output file named after a Windows device (`CON.iso`, `NUL.iso`, `COM1.iso`, `LPT1.iso` and so on) was accepted and then failed when the image was written. It is now rejected straight away with an explanation. Found in Stage 4.3 QA.
- **Windows mount errors:** when Windows couldn't mount an image, the verification result showed `#< CLIXML` (raw PowerShell error data) instead of the reason. It now shows Windows' message, for example "The file or directory is corrupted and unreadable." Found in Stage 4.3 QA.
- **UDF verification:** a UDF image whose volume recognition sequence (sector 16) was damaged passed verification when the Windows mount check was off, although Windows refuses to mount such an image. The UDF reader now requires the NSR descriptor and reports the damage. Found in Stage 4.3 QA.

### Removed from scope
- **Bootable images** (El Torito, BIOS, UEFI) are not part of 1.0.0. Nothing advertises them. ISO Information still *detects* a boot record in existing images.

## Development build: Stage 3.3, UDF through Windows IMAPI2 (internal version 1.0.3)

### Why
Joliet stores names of at most 64 characters, so `Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.dll` (70) could only be kept exactly by switching to UDF. DiscUtils can't write UDF. IMAPI2 is built into Windows and could do it. A feasibility test showed it writes UDF images without a disc drive and keeps names up to 127 characters, and Windows mounts the result. That test made images through COM from PowerShell and a small C# test program. Stage 3.3 adds IMAPI2 as a second engine. DiscUtils stays for ISO 9660 and ISO 9660 + Joliet.

### Behaviour changes
- **New formats:** *UDF* and *ISO 9660 + Joliet + UDF* in the main window's new **File system** list (it replaces the Joliet tick box) and in Settings. The default is still ISO 9660 + Joliet.
- **Long DLL names in Strict Mode:** UDF and the hybrid keep names up to 127 characters exactly, so the long MSTest DLL no longer blocks Strict Mode with these formats.
- **Files of 4 GB and larger:** UDF accepts them. The other formats reject them and suggest UDF.
- **Verification and ISO Information read UDF:** with HT ISO Creator's own reader. ISO Information shows the UDF revision and label.

### Engine and services
| File | Change |
|---|---|
| `Models\IsoCreationOptions.cs` | `IsoFileSystemFormat.Udf`, `IsoFileSystemFormat.Iso9660JolietUdf`. |
| `Models\FileSystemFormats.cs` | **New.** Names, list texts, engine, visible-name source, 127-character UDF limit and maximum file size for each format. |
| `Services\ImapiImageBuilder.cs` | **New.** Builds UDF and hybrid images with `IMAPI2FS.MsftFileSystemImage`, using late-bound COM and only documented IMAPI2 members.<br>• **Setup:** media type *disk* (no capacity limit), UDF revision 1.02, and the label from the plan.<br>• **Adding items:** folders and files are added one by one under their planned image paths, with UTC dates.<br>• **File data:** read through a managed `IStream` that opens each source file only when IMAPI2 reads it.<br>• **Errors:** a source error raised inside IMAPI2 (such as a changed file) is rethrown as it was. IMAPI2 errors include their HRESULT.<br>• **Output:** the result image is exposed as a .NET stream, and the COM objects are released when it is disposed. |
| `Services\IsoCreationService.cs` | DiscUtils building moved to `BuildWithDiscUtils`; new `BuildWithImapi`. Both feed the same 1 MB copy loop, so progress, cancellation, the free-space check, temporary file, safe replacement and crash recovery are unchanged. `Prepare` checks that IMAPI2 is available and applies each format's maximum file size. Strict Mode advice now suggests UDF. |
| `Services\IsoSizeCalculator.cs` | Exact size for IMAPI2 formats: the image is laid out with zero-filled placeholder streams and IMAPI2's total size is read (no file data is read). |
| `Services\IsoNameMapper.cs` | UDF mapping: names up to 127 characters are kept, longer ones are shortened (extension kept, `~n` on collision), and control characters become `_`. The Joliet and ISO 9660 rules are unchanged. |
| `Services\UdfImageReader.cs` | **New.** Independent UDF reader (ECMA-167 / OSTA UDF 1.02–2.01, physical partitions).<br>• **Descriptors:** reads the anchor (and requires a second one at the end), the main and reserve volume descriptor sequences, the partition and logical volume descriptors, the file set descriptor, file entries and extended file entries, and file identifiers. Every descriptor tag's identifier, checksum and location is checked.<br>• **Names:** CS0 names (8-bit and UTF-16).<br>• **File data:** short and long allocation descriptors, data embedded in the file entry, and unrecorded extents.<br>• **Problems reported:** duplicate names, loops, depth, and data outside the image. |
| `Services\IsoImageReader.cs` | Recognises UDF-only and UDF bridge images. `Entries` is now the tree Windows shows (UDF, then Joliet, then ISO 9660), and `JolietEntries` and `UdfEntries` are separate. For hybrid images it checks that the UDF view and the ISO 9660 view list the same files and data. `OpenExtent` reads UDF data ranges and embedded data. |
| `Models\IsoImageInfo.cs` | `HasIso9660`, `HasUdf`, `UdfVolumeLabel`, `UdfRevision`, `UdfEntries`, `JolietEntries`, `DisplayLabel`. `IsoImageEntry` gains `DataRanges` and `InlineData`. `Format` recognises the UDF formats. |
| `Services\IsoVerificationService.cs` | New *UDF file system* check. ISO 9660 and Joliet tree checks are skipped when those views don't exist. The label check covers ISO 9660, Joliet and UDF labels. Format names come from `FileSystemFormats`. |
| `Services\CompatibilityService.cs` | Rules and alias notes for each format. New Info finding for hybrid images (IMAPI2's Joliet names over 64 characters and 8.3 ISO 9660 aliases). The 2 GB warning uses each format's file limit. |
| `Helpers\VolumeLabelValidator.cs` | The UDF formats use the 16-character label rule. |
| `Services\FilenameConversionReport.cs`, `Models\HistoryEntry.cs`, `Models\VerificationResult.cs` | Format names (and engine) come from `FileSystemFormats`. |
| `HTISOCreator.csproj` | References `Microsoft.CSharp` (needed for late-bound COM calls). No new NuGet packages. |
| `Properties\AssemblyInfo.cs` | Internal version 1.0.3. |

### User interface
| File | Change |
|---|---|
| `Forms\MainForm(.Designer).cs` | **File system** list (four formats) replaces the Joliet tick box. The size, status line and Filename Compatibility status update when it changes. |
| `Forms\SettingsForm(.Designer).cs` | The default file system list has all four formats. |
| `Forms\CompatibilityForm.cs` | Rules text for each format. Reports name the engine. |
| `Forms\CompletionForm.cs` | *File system* shows the engine, for example "UDF (Windows IMAPI2)". |
| `Forms\IsoInfoForm.cs` | Shows the file systems found, with the UDF revision ("UDF 1.02 only", "ISO 9660 + Joliet + UDF 1.02") and the UDF label. The tree says "UDF names". |

### Tests (153 → 168)
- **New:** `UdfTests` (12) and `UdfUiTests` (3).
- **Changed:** `UiStateTests` uses the new file system list. One Strict Mode advice text is updated.

### Not changed
- **Existing formats:** ISO 9660 and ISO 9660 + Joliet images are made exactly as in 1.0.2.
- **Everything else:** the Strict and Compatibility Mode rules, reports, history, capacity profiles and the .NET Framework 4.6.2 target.

## Development build: Stage 3.2, strict filename preservation (internal version 1.0.2)

### Why
The Stage 3 regression found that two files named `Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.dll` (70 characters) were stored as `...MSTestAdapter.PlatformSe.dll`. Joliet, the long-name format, allows 64 characters per name, and DiscUtils can't write UDF. Programs load DLLs by exact name, so a silent rename can break software on the disc. Stage 3.2 makes every name change a decision the user takes, and checks the result.

### Behaviour changes
- **Strict Mode is the default:** it covers new installations and Stage 3 settings files, which have no mode saved. Sources with any name that can't be stored exactly are refused before anything is written. To create such an image, rename the items or choose Compatibility Mode.
- **Compatibility Mode needs confirmation:** it converts names only after the user ticks a confirmation in the compatibility window. Create ISO is disabled until then.
- **New verification checks:** *File names*, *ISO 9660 directory tree*, *Joliet directory tree* and *Duplicate entries*.
- **More compact main window:** the client area is 650 px instead of 658 px, despite the new section, so it still fits at 150% scaling.

### Engine and services
| File | Change |
|---|---|
| `Models\FilenameMode.cs` | **New.** `Strict` / `Compatibility`. |
| `Models\IsoCreationOptions.cs` | `FilenameMode` (default Strict) and `ConversionsApproved`. |
| `Models\IsoBuildPlan.cs` | `StrictModeError`: the Strict Mode message, also in `Errors`. |
| `Models\AppSettings.cs` | `FilenameMode` (default Strict), saved with the other settings. |
| `Models\HistoryEntry.cs` | `FilenameMode` per attempt. Stored as an optional XML element, so older history files still load, and their entries read as "unknown". |
| `Models\VerificationResult.cs` | `VerificationOptions.FilenameMode`. The result has `NameDifferences` and `NameMismatches`, which also appear in the text report. |
| `Services\IsoCreationService.cs` | `Prepare`: Strict Mode turns name conversions into one error with the first example and advice. It also does a final case-insensitive check that no two source items share an image path. `Create`: refuses plans with converted names unless they are in Compatibility Mode **and** approved. Nothing is written in either case, so an existing ISO is untouched. The filename mode is logged. |
| `Services\CompatibilityService.cs` | New Error finding *Names that cannot be preserved (Strict Mode)*, listing original → proposed name (reason). New Warning *Program files that will be renamed* (DLL, EXE, SYS and similar). Collision text clarified. `CollisionCount`, `IsProgramFile` and `AliasNote` (which explains plain ISO 9660 alias names) added. |
| `Services\IsoNameMapper.cs` | `DuplicateReason` constant. **The conversion rules themselves are unchanged**: the 64-character Joliet limit, extension kept, `~n` collision suffix, case-insensitive collision detection. |
| `Services\FilenameConversionReport.cs` | **New.** TXT and CSV report with date, source, destination, file system, mode, status, totals, affected counts, collision warnings and one row per conversion. CSV is UTF-8 with BOM, RFC 4180 quoting and formula neutralisation. |
| `Services\IsoVerificationService.cs` | New *File names* check: exact, case-sensitive comparison against the plan, judged by Strict, Compatibility or report-only rules. A passing SHA-256 check cannot hide a rename. Also new: separate *ISO 9660 directory tree*, *Joliet directory tree* and *Duplicate entries* checks. |
| `Services\IsoImageReader.cs` | `DuplicateEntryText` constant (same message as before). |
| `Properties\AssemblyInfo.cs` | Internal version 1.0.2. |

### User interface
| File | Change |
|---|---|
| `Forms\MainForm(.Designer).cs` | New **Filename Compatibility** section with these controls:<br>• the Strict and Compatibility Mode options<br>• a preservation status line with the number of affected names and program files<br>• the *Preview conversions...* link (moved here from ISO Information)<br>• an *Export Report...* button<br>The mode is saved immediately. The Create flow passes the mode, records it in the history, gives Strict Mode its own status message, and sets `ConversionsApproved` only when the confirmation was ticked. Spacing was tightened so the window stays within 150% scaling. |
| `Forms\CompatibilityForm(.Designer).cs` | The mode is shown, with a note that alias names are not changes. Original and converted names are shown side by side, and the name columns size themselves to show the full name. A new *Folder* column; collisions and program files are highlighted. New confirmation tick box, which Create needs in Compatibility Mode. *Save Report...* offers TXT or CSV. |
| `Forms\CompletionForm(.Designer).cs` | New *File names* row and a *Name report...* link (TXT or CSV) after creation. |
| `Forms\VerificationForm(.Designer).cs` | New *File names* rule (Strict, Compatibility or report only). Name differences are listed. |
| `Forms\HistoryForm.cs` | *Verify again* uses the mode recorded for that entry. |
| `Helpers\ReportExport.cs` | **New.** Shared TXT/CSV save dialog. |

### Tests (127 → 153)
- **New:** `FilenameModeTests` (16) and `FilenameModeUiTests` (4). They include the original MSTest DLL regression in both modes.
- **Changed:** existing tests that deliberately create images with converted names now say so (Compatibility Mode, approved).
- **Also counted:** the 6 `ShortNameTests` from the ISO 9660 alias fix made after the Stage 3 ZIP.

### Not changed
- **Image writing:** ISO 9660 / Joliet writing, the name conversion rules, capacity profiles, history, crash recovery, safe replacement and the .NET Framework 4.6.2 target.
- **Joliet name limit:** it stays at the standard 64 characters. Longer "Joliet-long" names are non-standard, and DiscUtils images with them were reported as corrupt by Windows in Stage 1.

## Development fix after the Stage 3 ZIP (included from Stage 3.2)
- **Unique plain ISO 9660 alias names:** `Services\IsoShortNames.cs` gives every file and folder a unique, valid alias name in each folder and keeps real extensions. DiscUtils 0.16.13 produced duplicates such as several `HTISOCREATOR.CSP` entries. Used by creation and by the size calculator.

## Development builds: Stages 1 to 3
See docs/DEVELOPMENT_NOTES.md.
