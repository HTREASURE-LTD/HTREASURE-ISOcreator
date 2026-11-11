# HT ISO Creator — Development Notes (Stages 1–3.3)

> Design notes from the development stages that led to version 1.0.0: how each feature works, why, and what was tested at each stage. Version numbers 1.0.2 and 1.0.3 below were internal development builds; the first public release is **1.0.0** (see README.md, RELEASE_NOTES.md and TEST_REPORT.md).

HT ISO Creator is a Windows Forms desktop app that makes standard ISO 9660 / Joliet and UDF image files from a folder. It can also verify, inspect and keep a history of the images it makes. The images are real ISO images, not renamed ZIP files.

| | |
|---|---|
| Language / UI | C#, Windows Forms |
| Framework | .NET Framework 4.6.2 (AnyCPU, runs as 64-bit on 64-bit Windows) |
| ISO writers | [DiscUtils.Iso9660 0.16.13](https://github.com/DiscUtils/DiscUtils), MIT licence (see `THIRD-PARTY-NOTICES.txt`), for ISO 9660 and ISO 9660 + Joliet. **Windows IMAPI2** (`IMAPI2FS.MsftFileSystemImage`, part of Windows, nothing to install or redistribute) for UDF and ISO 9660 + Joliet + UDF. |
| Image reader for verification | HT ISO Creator's own readers: `IsoImageReader` (ISO 9660 / Joliet) and `UdfImageReader` (UDF). Neither shares code with DiscUtils or IMAPI2. |
| Supported OS | Windows 10 and Windows 11 |

## Building in Visual Studio (exact steps)
1. Install **Visual Studio 2022** (17.x) or later with the **.NET desktop development** workload. You don't need the .NET Framework 4.6.2 targeting pack, because the project references `Microsoft.NETFramework.ReferenceAssemblies`.
2. Extract the source ZIP to a short folder such as `C:\Dev\`, so the solution is at `C:\Dev\HTISOCreator\HTISOCreator.sln`. At about 200 characters the .NET Framework test host can't discover tests. The app itself builds fine from long paths.
3. Open `HTISOCreator.sln`. The first build restores the NuGet packages (DiscUtils.Iso9660 / Core / Streams 0.16.13, MSTest 3.6.1, Microsoft.NET.Test.Sdk 17.11.1, the reference assemblies). You need internet access for this. If it doesn't happen automatically, right-click the solution and choose **Restore NuGet Packages**.
4. Pick **Release** and run **Build > Build Solution** (Ctrl+Shift+B). You should get 0 errors and 0 warnings.
5. Run the app with **Debug > Start Without Debugging** (Ctrl+F5). The output is `HTISOCreator\bin\Release\net462\HTISOCreator.exe`.
6. To run the tests, choose **Test > Run All Tests**. Test Explorer lists **168 tests**, and all of them should pass. They take about a minute and create temporary files under `%TEMP%\HTISOCreator.Tests`. One test writes a 4 GB UDF image, so about 9 GB of free space on the temporary drive is needed while it runs; the files are deleted afterwards.

Command line (.NET SDK 6 or later): `dotnet build HTISOCreator.sln -c Release`, then `dotnet test HTISOCreator.sln -c Release`.

## What Stage 3.3 adds: UDF through Windows IMAPI2
The **File system** list in the main window (and in Settings) now offers four formats:

| File system | Engine | Names readers show | Largest file | Use it for |
|---|---|---|---|---|
| ISO 9660 + Joliet (default) | DiscUtils | Joliet, up to 64 characters | under 4 GB | General data discs, as before |
| **UDF** | Windows IMAPI2 | UDF, up to **127** characters, every character Windows allows | **no 4 GB limit** (a 4.5 GB file was tested) | Software with long DLL names, large files, modern Windows/macOS/Linux |
| **ISO 9660 + Joliet + UDF** (hybrid) | Windows IMAPI2 | UDF, up to 127 characters | under 4 GB (the ISO 9660 view stores 32-bit sizes) | The same software, with extra name views for older readers |
| ISO 9660 only | DiscUtils | ISO 9660, 8.3-style upper case | under 4 GB | Very old systems |

**Why IMAPI2:**
- **No extra software:** it is part of Windows, so there is nothing to install or bundle.
- **No disc drive needed:** it writes image files directly.
- **Same safeguards:** its output is copied to disk in 1 MB blocks, like DiscUtils images, so progress, cancellation, the free-space check, safe replacement of an existing ISO and crash recovery all work as before.
- **Planned names only:** each file is added under the name in HT ISO Creator's plan. IMAPI2 never picks a UDF name itself. Names over 127 characters are handled by the app, so Strict Mode refuses them, and Compatibility Mode shortens them only after you confirm.

**Results with the long DLL name:** with **UDF** or the **hybrid**, `Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.dll` (70 characters) is kept exactly in Strict Mode. The real project copy below has 597 files and 157 folders, and every name was preserved.

**Independent UDF verification:** IMAPI2's images are read back by `UdfImageReader`, written for this from ECMA-167 and the OSTA UDF specification. I first tried the DiscUtils UDF reader. It garbled Unicode names and joined up deeply nested paths, so it wasn't usable for verification. The new reader checks:
- **Descriptors:** the anchor descriptors and their backup copies, every volume, file set and file entry descriptor (identifier, checksum and recorded location), and every file identifier.
- **File data:** where each file's data lies.

For hybrid images it also checks that the UDF view and the ISO 9660 view list the same files and data. Verification adds a **UDF file system** check, and SHA-256 hashing reads files through their UDF data extents.

**Elsewhere in the app:**
- **ISO Information:** shows "UDF 1.02 only" or "ISO 9660 + Joliet + UDF 1.02" and the UDF volume label.
- **Completion dialog and reports:** name the engine.
- **History:** records the format.

**IMAPI2 settings used:** UDF revision **1.02**, the most widely readable. There is no capacity limit (IMAPI2's "disk" media type). File and folder dates are your source last-modified dates, recorded in UTC and confirmed by the UDF reader.

## What Stage 3.2 added: strict filename preservation
Changes are listed file by file in [CHANGELOG.md](CHANGELOG.md).

### Why names were changed (root cause)
The image is written by DiscUtils 0.16.13, which can write only ISO 9660 and Joliet; it has **no UDF writer**. Two limits follow (Stage 3.3 adds UDF, which removes the first for names up to 127 characters):

- **Joliet** (the names Windows, macOS, Linux and 7-Zip show) allows at most **64 Unicode characters** per name and no `* / : ; ? \`. The Joliet specification sets this limit, not DiscUtils. `Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.dll` has 70 characters, so `IsoNameMapper` had to shorten it to `...MSTestAdapter.PlatformSe.dll`, keeping the `.dll` extension. Before Stage 3.2 the app showed this in the conversion preview and then created the image anyway once you clicked Create.
- **Plain ISO 9660** (only used by very old readers) allows `A-Z 0-9 _`, at most 30 characters. Every image also contains these **alias names**. They are always shortened. They aren't a loss of the original names, because the Joliet names are stored alongside them.

**Can the 64-character limit be raised safely?** No. Some tools write "long Joliet" names of up to 103 characters, but that breaks the Joliet specification and gives non-standard images. In Stage 1 testing, DiscUtils images with Joliet names over 64 characters were reported as corrupt by Windows. Names over 64 characters can only be kept exactly in UDF, which needs a different ISO engine. Stage 3.2 therefore doesn't raise the limit. It **never lets a name change without your decision**.

Files changed for this: `Services\IsoCreationService.cs`, `Services\CompatibilityService.cs`, `Services\IsoNameMapper.cs`, `Services\IsoVerificationService.cs`, `Services\IsoImageReader.cs`, the new `Services\FilenameConversionReport.cs`, `Models\*` (filename mode, options, plan, settings, history, verification), and the main, compatibility, completion, verification and history windows. All of them are listed in CHANGELOG.md.

### Strict Mode — Preserve Original Filenames (default)
- **Check before writing:** before anything is written, every file and folder name is checked against the selected file system. The check looks for truncation, unsupported characters, case-only collisions and duplicates after conversion.
- **Blocked creation:** if any name would differ in the image, creation is **blocked**. The Filename Compatibility window lists the error *Names that cannot be preserved (Strict Mode)*. For each name it shows the original name, the proposed conversion, the reason and the folder. You can save the list as TXT or CSV.
- **Nothing changes on disk:** nothing is written, the source folder isn't touched, and an existing ISO at the output path stays exactly as it was. The attempt is recorded in the history as *Failed* with the Strict Mode message.
- **"ISO 9660 only":** with Joliet off, the visible names are the plain ISO 9660 names, so almost every name changes. The message suggests turning Joliet on.
- **Default:** Strict Mode is the default for new installations, and for Stage 3 settings files, which have no mode saved. Your other settings are kept.

### Compatibility Mode — Allow Filename Conversion
- **Review first:** the compatibility window always opens before creation when any name would change. It shows the original and converted names side by side, with the reason and folder. DLLs, executables and collisions are highlighted.
- **Explicit confirmation:** **Create ISO stays disabled** until you tick *"I have reviewed all N name conversion(s) and accept them…"*. The tick box says how many of the renamed files are DLL or program files that may stop working.
- **Enforced by the engine:** `IsoCreationService.Create` refuses to write a plan with converted names unless the plan is in Compatibility Mode **and** was approved. Names are never renamed silently.
- **Collisions:** names that would collide after conversion get a numbered suffix (`name~1.ext`), so no two source items ever share a path in the image. A final check stops creation if two items would still end up at the same path, ignoring case.
- **Saving the mode:** the selected mode is saved in the settings immediately and recorded in each history entry.

### Filename Compatibility section (main window)
This section has:
- **Mode choice:** the Strict and Compatibility Mode options.
- **Preservation status:** for example *"Every file and folder name (728) will be preserved exactly."*, or *"2 name(s) cannot be preserved (2 program file(s)). Creation is blocked."*
- **Preview conversions...:** the full compatibility check, without writing anything.
- **Export Report...:** saves the conversion report.

### Filename conversion report (TXT and CSV)
The report is available:
- **Before creation:** from **Export Report...**, and from **Save Report...** in the compatibility window.
- **After creation:** from **Name report...** in the completion dialog.

It contains:
- **Header:** date and time, source folder, destination ISO, file system, filename mode, and status (*Blocked (Strict Mode)*, *Awaiting confirmation*, *Approved by user* or *Applied in image*).
- **Totals:** files and folders, files and folders affected, and the number of collision warnings.
- **One row per changed name:** original relative path, destination relative path, reason, status and collision flag.

The CSV is UTF-8 with a BOM so Excel shows Unicode names. Fields are quoted per RFC 4180, and cells starting with `= + - @` are prefixed with `'` so they can't run as formulas.

### Verification of names
Verification now adds these checks:
- **ISO 9660 directory tree** and **Joliet directory tree**, checked separately.
- **Duplicate entries.**
- **File names:** the image's names are compared **exactly, letter case included**, with the expected names.
  - **Strict:** every name must equal the original. The check **fails** if any name was changed, even when every file's SHA-256 matches.
  - **Compatibility:** names must match the approved conversion plan exactly.
  - **No rule:** differences are reported as a **warning** and listed in the report.

Verification right after creation uses the mode of the creation. The **Verify ISO** window has a *File names* rule. Its default is the mode recorded in the history for that ISO; older entries only report differences.

## What Stage 3 added
| Area | What it does |
|---|---|
| **ISO verification** (`IsoVerificationService`, **Tools > Verify ISO**, Ctrl+Shift+V) | Opens any ISO and checks its volume descriptors and both directory trees. It reads the volume label and every directory entry. If you give it the source folder, it also compares every path and size and lists files that are **missing**, **extra**, **a different size** or **different in content**. You can add a **SHA-256** comparison of every file, which also gives the SHA-256 of the whole image, and an optional check by **mounting the image with Windows**. It shows progress, can be cancelled, and can **save a text report**. |
| **Verification levels** | The result always says which level ran: *Structural verification only* (no source folder), *Source verification (paths and sizes)*, or *Full source-content verification (paths, sizes and SHA-256)*. Checks that didn't run are shown as **Skipped**, never as Passed. Writing an image successfully is never described as verification. |
| **Verify after creation** | The main window has **Verify the image after creating it** and **Include SHA-256 content check**. Verification uses the same name mapping as creation, so the source isn't scanned again. If verification fails, the completion dialog says **"ISO image created, but verification FAILED."** and is shown even if completion notifications are turned off. Cancelling verification keeps the finished image, which is recorded as not verified. |
| **Creation history** (**Tools > Creation History**, Ctrl+H) | Records every attempt (succeeded, failed or cancelled): date, source, ISO path, volume label, format, size, duration, result, verification status and a log reference. You can search, filter by result or verification, sort by any column, view details, open the folder, verify the ISO again, copy the path, export to CSV, delete entries or clear the history (each delete asks first). History is stored in `%APPDATA%\HT Software\HT ISO Creator\history.xml` and keeps the newest 1,000 entries. Saves are atomic, and a damaged file is kept aside as `history.xml.damaged-*` rather than lost. |
| **Disc capacity profiles** | The **Target disc** list offers CD 650 MB, CD 700 MB, DVD 4.7 GB, DVD Dual Layer 8.5 GB, Blu-ray 25 GB, a custom size in MB (1 MB = 1,048,576 bytes) or no limit. The window shows the image size against the chosen capacity and warns when it's too large. A profile **doesn't change the ISO or its file system**, and fitting **doesn't guarantee** that a particular burner or blank disc will accept the image. The capacities use standard user-data sector counts: 333,000, 360,000, 2,295,104, 4,173,824 and 12,219,392 sectors of 2,048 bytes. |
| **ISO information viewer** (**Tools > ISO Information**, Ctrl+I) | Shows the file name, file size, declared volume size, ISO 9660 and Joliet labels, the detected file systems, whether an El Torito boot record is present, the number of files and folders, the volume creation and modification dates *as recorded in the image*, the application identifier, and a directory tree (the first 20,000 items). Anything the image doesn't record is shown as **Not recorded** rather than guessed. |
| **Filename compatibility analysis** (**Tools > Filename Compatibility**, Ctrl+K, or *Preview conversions...*) | Before anything is written, it reports unsupported characters, names that are too long, duplicates after conversion, folder depth over 8 levels, ISO 9660 paths over 255 characters, files of 4 GB or more (errors), files over 2 GB, skipped links, empty items and disc capacity. It shows a **preview of every name conversion** with the reason, which you can filter and save as a report. This window appears automatically before creation whenever names will change or there are warnings or errors. Creation is blocked while there are errors. The name conversion rules from Stages 1–2 are unchanged. |
| **File dates** | Files and folders now keep their source last-modified dates inside the image. Before, DiscUtils stored the build time for everything. Tests check this with the independent reader. |

## Reliability and recovery
- **Disk space is checked before writing**, using the exact image size, and again just before writing in case space was used up in the meantime.
- **Replacing an existing ISO** (from Stage 2): the old file is renamed to a backup, the new image is moved in, and only then is the backup deleted. If something fails, the backup is restored.
- **Crash recovery:** while an image is being written or replaced, its temporary and backup paths are recorded in `%LOCALAPPDATA%\HT Software\HT ISO Creator\pending-operations.txt`. On the next start, an interrupted partial file is deleted, and an original ISO left as a backup is put back (or removed, if the new image had already replaced it). The app tells you what it did.
- **Source files that change** during creation, **locked files**, **access errors** and **cancellation** are all handled as in Stages 1–2. The partial file is removed, and the app never reports success.
- **Verification errors**, such as an unreadable file, a missing source folder or a damaged image, are reported as **FAILED** checks with details, not as exceptions.
- **Logging:** every operation gets a short **reference** (for example `8c03145c`). It appears on each log line, in error messages and in the history. Logs include the OS, .NET version and HRESULTs. They're stored in `%LOCALAPPDATA%\HT Software\HT ISO Creator\Logs` and deleted after 30 days. **Tools > View Today's Log** opens the current log.

## Keyboard shortcuts
Ctrl+O source folder · Ctrl+S output file · Ctrl+R refresh analysis · F5 create · Esc cancel creation or verification · Ctrl+Shift+V verify ISO · Ctrl+I ISO information · Ctrl+K filename compatibility · Ctrl+H history · Ctrl+, settings · F1 shortcut list.

## Settings (Tools > Settings, Ctrl+,)
Stage 2 settings plus: **target disc** (with a custom size), **verify each new image automatically**, **include SHA-256**, and **also mount with Windows when verifying**. The target disc, verification choices and **filename mode** made in the main window are also remembered.

## Known limitations
| Limitation | Notes |
|---|---|
| UDF needs IMAPI2 | The UDF formats use the IMAPI2 file system component of Windows (present on Windows 10 and 11). If it is missing or disabled, those formats report a clear error and ISO 9660 + Joliet still works. |
| UDF revision | Images are UDF **1.02**. Newer revisions (2.01, 2.50 with metadata partitions) aren't offered. The UDF reader supports 1.02–2.01 physical partitions, and reports metadata, virtual or sparable partitions as unsupported rather than misreading them. |
| Hybrid Joliet names | In ISO 9660 + Joliet + UDF images, IMAPI2 writes its own Joliet names of up to about 110 characters (standard Joliet allows 64) and 8.3-style ISO 9660 alias names (`MICROS~1.DLL`). Windows, macOS and Linux show the UDF names. Very old Joliet-only readers may not read names over 64 characters. The app doesn't control these alias views, so Strict Mode and verification check the UDF names. The app does check that all three views list the same files and data. |
| Names over 64 / 127 characters | ISO 9660 + Joliet keeps names up to 64 characters exactly, and UDF up to 127. Longer names are refused in Strict Mode, or shortened after you confirm in Compatibility Mode. Raising the Joliet limit isn't possible within the standard. |
| Files of 4 GB or larger | Only the **UDF** format can store them. ISO 9660, Joliet and the hybrid use 32-bit sizes, so the app reports such files as errors and suggests UDF. |
| Bootable images | Not offered in 1.0.0 (a Stage 4 decision). The information viewer *detects* a boot record in existing images. |
| Independent readers | Both support 2,048-byte logical blocks. The ISO reader supports single-extent files; the UDF reader supports short and long allocation descriptors, embedded data, and up to 64 folder levels. These cover everything this app creates. Other tools' images that use features outside this (multi-extent ISO files, UDF extended or continued allocation descriptors) are reported rather than misread. |
| Windows mount check | Uses PowerShell's `Mount-DiskImage`. It briefly assigns a drive letter and always dismounts afterwards; an image you had already mounted is left mounted. It's optional and off by default. It can fail where policy blocks mounting, and it then reports **FAILED** with Windows' message. |
| Volume dates | DiscUtils writes the volume creation and modification dates without a time-zone offset, so the viewer shows them "as recorded". They may differ from your local time by your UTC offset. |
| Plain ISO 9660 names | In "ISO 9660 + Joliet" images, Windows and most tools show the Joliet names. The plain ISO 9660 names are shortened, upper-case names made by `IsoShortNames`: unique in each folder, real extension kept (up to 8 characters), at most 30 characters, for example `MICROSOFT_TESTPLATFORM_CO.DLL`. DiscUtils 0.16.13's own generator made duplicates (several files called `HTISOCREATOR.CSP`) and dropped extensions. That is fixed by setting DiscUtils's internal short-name field, which `DiscUtilsShortNameField_IsAvailable` checks. |
| Plain ISO 9660 record order | DiscUtils writes directory records in the order of the Joliet names, so where long names were shortened the plain ISO 9660 records may not be in strict alphabetical order. Windows, Linux and 7-Zip read the records one by one and are not affected. This was already the case before the name fix. |
| History | Keeps the newest 1,000 entries. The ISO Information tree shows up to 20,000 items. |

## Testing
### Automated: 168 MSTest tests (`Tests\HTISOCreator.Tests`)
| Spec item | Tests |
|---|---|
| Stage 1 and 2 (all kept) | `IsoCreationServiceTests`, `ValidationTests`, `SettingsServiceTests`, `DropValidatorTests`, `SourceAnalysisTests`, `OutputReplacementTests`, `UiStateTests` (72 tests) |
| Valid ISO verification | `ValidIso_FullContentVerificationPasses` (also checks the image SHA-256 against a separate calculation), `ValidIso_WithoutSource_IsOnlyStructural`, `ValidIso_PathsAndSizesWithoutHashes_IsReportedAsSuch`, `Iso9660OnlyImage_VerifiesAgainstSourceUsingNameMapping`, `VerificationAgainstPlan_PassesRightAfterCreation`, `IndependentReader_AgreesWithDiscUtilsReader` |
| Invalid ISO rejection | `NonIsoFile_IsRejected`, `ZipRenamedToIso_IsRejected`, `TruncatedIso_IsRejected`, `CorruptedDirectoryRecords_AreRejected`, `MissingIsoFile_FailsWithoutThrowing`, `WrongLabelOrFormat_FailsWhenExpected` |
| Missing source files | `MissingAndExtraFiles_AreReported`, `MissingSourceFolder_FailsComparison` |
| Modified file contents | `ModifiedSize_IsReported`, `ModifiedContentWithSameSize_IsOnlyCaughtBySha256`, `VerificationForm_ShowsFailuresForModifiedSource` |
| Filename compatibility | `CompatibilityTests` (7 tests: unsupported characters, long names, duplicates after conversion, depth, long paths, size limits, empty items) |
| Plain ISO 9660 names | `ShortNameTests` (6 tests: unique and valid names in every folder, extensions kept, valid names unchanged, ISO 9660-only images, size calculation matches, DiscUtils field present) |
| **Stage 3.3: UDF and hybrid (IMAPI2)** | `UdfTests` (12): `ImapiIsAvailableOnThisWindows`, `Udf_StrictModeKeepsMsTestDllAndLongNamesExactly` (MSTest DLL, 124-character name, Unicode, `;`, letter case, 12 levels, empty items; verified from the plan and from the source in Strict Mode), `Udf_RecordsSourceFileAndFolderDates`, `Hybrid_UdfNamesExact_AndAllThreeViewsAgree`, `NamesOver127_StrictRejects_CompatibilityShortensWithoutCollisions`, `FilesOf4GBOrMore_OnlyUdfAccepts_AndImageReadsBack` (writes and reads back a 4 GB + 4 KB file), `SizeCalculation_MatchesImapiImages`, `Cancellation_LeavesNoFiles`, `SourceChangedBeforeWriting_FailsWithTheRealReason`, `DamagedUdfImage_IsRejectedOrReported`, `ModifiedSourceContent_IsCaughtInUdfImage`, `UdfFormatsReportEngineAndRules`. `UdfUiTests` (3): `MainForm_UdfKeepsMsTestDllInStrictMode_CreatesAndVerifies` (real Create button), `IsoInfoForm_DescribesUdfAndHybridImages`, `SettingsForm_OffersAllFourFileSystems` |
| **Stage 3.2: Strict Mode** | `Defaults_AreStrictMode`, `NormalNames_StrictModeCreatesAndVerifiesExactNames`, `MsTestDllRegression_StrictModeRejectsBeforeWritingAnything` (the original 70-character MSTest DLL in Debug and Release), `StrictModeRejection_KeepsExistingIsoUnchanged`, `UnsupportedCharacters_StrictModeRejectsWithReason`, `UnicodeNames_JolietPreservesExactly_Iso9660OnlyRejectsInStrictMode`, `EmptyFoldersZeroByteFilesAndDeepFolders_StrictModeCreatesAndVerifies` (12 levels), `MainForm_StrictModeRejectsAndKeepsExistingIso` (real Create button) |
| **Stage 3.2: Compatibility Mode** | `CompatibilityMode_RefusesToWriteUnapprovedConversions`, `MsTestDllRegression_CompatibilityModeCreatesApprovedPlanAndVerifiesIt` (also: Strict re-verification fails although SHA-256 passes), `DifferentLongNamesWithSamePrefix_GetUniqueCollisionSafeNames`, `CaseOnlyDifferences_AreTreatedAsCollisions`, `MainForm_CompatibilityModeRequiresConfirmationThenCreatesAndVerifies`, `CompatibilityForm_CancelInCompatibilityModeCreatesNothing` |
| **Stage 3.2: reports, settings, UI** | `ConversionReport_TextAndCsvContainEveryRequiredField`, `ConversionReport_StrictModeStatusAndNoChanges`, `CsvEscaping_QuotesAndNeutralisesFormulas`, `FilenameMode_IsSavedAndOldSettingsFilesDefaultToStrict`, `History_RecordsFilenameModeAndReadsOlderEntries`, `MainForm_ShowsModeAndPreservationStatus_AndSavesTheMode`, `MainForm_LayoutHasNoOverlapsWhenScaled` (100/125/150%) |
| Identical ISO 9660 aliases | `ShortNameTests` (see above) |
| Capacity calculations | `CapacityTests` (sector-exact profiles, fits/exceeds/unknown/no limit, warning rather than error), `MainForm_ShowsCapacityStatusAndRemembersProfile` |
| Creation history persistence | `HistoryTests` (round trip, update/remove/clear, verification recording, 1,000-entry cap, damaged-file recovery, CSV escaping), `HistoryForm_SearchesFiltersAndSorts` |
| Verification cancellation | `Verification_CanBeCancelled` (also checks no file handles are left open), `VerificationForm_VerifiesAndCanBeCancelled` |
| Safe output replacement | `OutputReplacementTests` (Stage 2), `SuccessfulAndCancelledCreations_LeaveNothingPending` |
| Error recovery | `RecoveryTests` (interrupted temp file removed, backup restored, backup removed once replaced), `InsufficientSpace_IsReportedBeforeWriting`, `SpaceRunningOutBeforeWriting_FailsCleanly`, `DamagedHistoryFile_IsKeptAsideAndHistoryRecovers` |
| Integrated workflow | `MainForm_CreatesVerifiesAndRecordsHistory` (Create button, then auto-verify with SHA-256, then a history entry), `MainForm_CompatibilityCheckPreviewsWithoutWriting`, `SourceTimestamps_ArePreservedInTheImage`, `IsoInfoForm_ShowsRecordedInformation`, `CompatibilityForm_BlocksCreationWhenThereAreErrors` |

### Manual tests run during development (Windows 11 Pro 26300, 64-bit, 100% scaling)
1. **Real exe, create + auto-verify + Windows mount.** Folder: 6 files and 14 folders, including Unicode names, 10 levels of nesting, empty folders and a 40 MB random file. Settings: SHA-256 on, Windows mount on, CD 650 MB target. The completion dialog showed "Verification: Passed – paths, sizes and SHA-256 match the source", and the main window showed "Fits: 6.2% of CD 650 MB". The history entry was recorded with its log reference. The image was dismounted afterwards. `Tools\Verify-Iso.ps1`, which uses Windows' own mount, independently confirmed all 6 files and 14 folders match by SHA-256.
2. **Verify ISO, ISO Information and Creation History windows**, opened from their shortcuts in both light and dark themes. After one byte of a source file was changed, the Verify window reported *FAILED – Different content: docs\guide.txt* with SHA-256, while paths and sizes still matched.
3. **Crash recovery.** The app was killed while writing a 1.5 GB image. The `.partial` file and its pending record were left behind. On restart the app reported what it tidied up, deleted the partial file and cleared the record.

4. **Stage 3.2 regression on a real project copy** (this solution with its `bin`/`obj` folders: 591 files, 137 folders, including both 70-character MSTest DLLs). These results are from this build, not from your 595-file run, which had slightly different contents.
   - **Strict Mode** refused it and listed exactly the two DLLs with original name, proposed name and reason. No ISO was written.
   - **Compatibility Mode (approved)** created a 24.3 MB image. Verification against the approved plan passed all 12 checks: structure, both directory trees, duplicates, label, file system, files and folders, sizes, *File names* ("2 converted as approved, 726 identical"), SHA-256 of all 591 files, and the Windows mount check.
   - **Strict re-verification:** checking the same image under Strict rules **failed** on *File names*, even though SHA-256 passed.
5. **Real exe, Stage 3.2 user interface.** Strict Mode: *Create ISO* opened the compatibility window with *"Strict Mode: 2 name(s) cannot be preserved"* and no Create button, and no file was written. After switching to Compatibility Mode, *Create ISO* stayed disabled until the confirmation box was ticked. The image was then created and verified with SHA-256, the completion dialog showed *"File names: 2 converted (Compatibility Mode)"*, and the mode was saved in the settings.

6. **Stage 3.3 regression on a real project copy** (this solution with its `bin`/`obj` folders: 597 files, 157 folders, both 70-character MSTest DLLs), all in **Strict Mode**:
   - **ISO 9660 + Joliet:** still refused because of the two DLL names, as intended.
   - **UDF:** created in 2 seconds (26.9 MB). The size calculated beforehand was exact. Verification passed every check, including *File names: all 754 identical to the originals*, SHA-256 of all 597 files, and the Windows mount check (597 files and 157 folders with the same sizes).
   - **ISO 9660 + Joliet + UDF:** created in 3 seconds (27.6 MB) and passed the same checks. It also passed the ISO 9660 tree, Joliet tree and *UDF, ISO 9660 and Joliet views agree* checks.
7. **4.5 GB file in UDF:** a 4.5 GB image was created in 20 seconds, with the size calculated beforehand exact. Verification passed structure, sizes and the Windows mount check (Windows lists the file with the right size). It was run without SHA-256 to save time; the automated 4 GB test reads the file's last bytes back.
8. **Real exe with UDF selected:** the main window showed *File system: UDF* and *"Every file and folder name (754) will be preserved exactly."* Creating took 2 seconds. The completion dialog showed *File system: UDF (Windows IMAPI2)*, *Verification: Passed – paths, sizes and SHA-256 match the source* and *File names: All original names preserved*.

### Not yet verified, please check manually
- **UDF images on other systems:** reading them on macOS, Linux and older Windows versions, and burning them to DVD or Blu-ray (UDF 1.02 is widely supported, but this hasn't been tested here).
- **Windows 10**, and real **125% and 150%** display scaling. The automated test simulates scaling with fonts, which doesn't scale the separately set fonts of the header title, the Create ISO button and the summary labels.
- A **real mouse drag from File Explorer** (the drop handling is tested).
- The **Windows mount check from a standard (non-administrator) account** (it was run from the development account), and on systems where policy blocks mounting.
- **Real (non-sparse) files over 2 GB**, and opening the images in **third-party tools** such as 7-Zip.

## Tools
- `Tools\Verify-Iso.ps1`: mounts an image with Windows and compares every path, size and SHA-256 with a source folder.
- `Tools\Make-Icon.ps1`: regenerates `Resources\app.ico`.

## Releasing
1. Update the version in `HTISOCreator\Properties\AssemblyInfo.cs`.
2. Run `dotnet build -c Release` and `dotnet test -c Release`.
3. Ship everything in `HTISOCreator\bin\Release\net462\` except `*.pdb`. The `.exe.config` file is needed for long path support, and `THIRD-PARTY-NOTICES.txt` is needed for the MIT licence.

## Project layout
```
HTISOCreator.sln
HTISOCreator\
  Forms\      MainForm, SettingsForm, CompletionForm, AboutForm, VerificationForm, IsoInfoForm,
              HistoryForm, CompatibilityForm (each with .Designer.cs)
  Services\   IsoCreationService, IsoVerificationService, IsoImageReader, WindowsMountChecker,
              CompatibilityService, HistoryService, PendingOperationsRegistry, SafeFileReplacer,
              IsoSizeCalculator, FolderAnalysisService, IsoNameMapper, IsoShortNames,
              ImapiImageBuilder (UDF via Windows IMAPI2), UdfImageReader,
              FilenameConversionReport, SourceFileStream, SettingsService, LoggingService
  Models\     options, progress, plan, results, analysis, settings, FilenameMode, FileSystemFormats,
              CapacityProfile, HistoryEntry,
              IsoImageInfo, VerificationResult
  Helpers\    validation, formatting, disk space, theme, folder picker, taskbar progress, icon, drops
  Resources\  app.ico
Tests\HTISOCreator.Tests\   168 MSTest tests
Tools\        Verify-Iso.ps1, Make-Icon.ps1
THIRD-PARTY-NOTICES.txt     DiscUtils MIT licence
CHANGELOG.md                every change, file by file
```

## Roadmap
**Stage 4 (done, version 1.0.0):** installer, release build, documentation and distribution packages. Bootable images were deliberately left out of 1.0.0.

**Possible later work, not committed to:** bootable images. IMAPI2 has boot options, including multiple boot images for BIOS and UEFI, and DiscUtils supports El Torito; both would need a user-supplied boot image and compatibility testing.

**Not planned on the current engines:** Rock Ridge (Unix permissions and symbolic links). Neither DiscUtils nor IMAPI2 writes it; it would need an external tool such as xorriso, which is GPL-licensed.
