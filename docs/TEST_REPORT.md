# HT ISO Creator 1.0.0 — Test Report

This report lists the tests that were **actually run** for version 1.0.0, with their results. Anything not tested is listed under [Not tested and open issues](#not-tested-and-open-issues).

| | |
|---|---|
| Version | 1.0.0 (file version 1.0.0.0), Release build, .NET Framework 4.6.2 target |
| Test date | 11 November 2026 (Stage 4.3 quality assurance, then Stage 4.4 tests of the final release packages) |
| Test PC | Windows 11 Pro, build 26300, x64; .NET Framework 4.8.1 (release 534296); 1920 × 1080 display; long path support on |
| Account | Standard (not elevated) user account, so every result applies without administrator rights. |
| Tools | .NET SDK 11.0.100, MSTest; Inno Setup 6.7.4 for the installer |

## Summary
| Area | Result |
|---|---|
| Automated tests | **179 of 179 passed**, 0 skipped; the build has 0 warnings (warnings are treated as errors). |
| All four file systems, Strict and Compatibility Mode, real data | **Passed.** 16 images created and verified with SHA-256 and a Windows mount; all passed. Strict Mode blocked all 8 runs where a name would have changed. |
| Large file (4.5 GB) | **Passed.** UDF stored and verified it. The two formats that include ISO 9660 refused it with a clear message. |
| Cancellation, existing ISO protection, low disk space, invalid paths, locked files | **Passed**, after one defect was fixed (reserved names such as `CON.iso`). |
| Corrupted image detection | **Passed**, after one defect was fixed (damaged UDF recognition sequence). |
| Installer: install, upgrade, uninstall, running app; portable version | **Passed** on the final release packages (Stage 4.4). |
| Final release packages | **Passed.** Built once from the final source with all tests; checksums, package contents and the real-data tests repeated on the released program. See [section 6](#6-final-release-packages-stage-44). |
| Defects found by this QA | **3 found and fixed**, each with a new automated test. See [Defects found and fixed](#defects-found-and-fixed-in-stage-43). |

## 1. Automated tests
`dotnet test -c Release` after the last code change: **179 passed, 0 failed, 0 skipped**, in about 60 seconds. The final release build (Stage 4.4) ran them again before making any package: **179 passed, 0 failed, 0 skipped** (51 seconds).

| Test class | Tests | Covers |
|---|---|---|
| IsoCreationServiceTests | 18 | creation for each format, labels, dates, empty folders, zero-byte files, source changes, locked files |
| ValidationTests | 18 | volume label and path validation |
| VerificationTests | 18 | structure, names, sizes, SHA-256, mount checks, cancellation, damaged images |
| FilenameModeTests | 16 | Strict and Compatibility Mode, collisions, conversion reports |
| UdfTests | 12 | UDF and hybrid via IMAPI2: long names, 4 GB+ files, size prediction, damaged images (now including a damaged recognition sequence) |
| UiStateTests | 11 | main window states and layout at 100%, 125%, 150% and 200% scaling, themes |
| ReleaseTests | 10 | version and publisher, About window, data folders and migration, settings from Stage 3.3, window position, read-only ISO, reserved names, readable mount errors, accessibility |
| OutputReplacementTests | 9 | safe replacement of an existing ISO |
| CompatibilityTests, CapacityTests, SourceAnalysisTests, Stage3UiTests | 8 each | disc capacity, source analysis, history, ISO information, verification windows |
| RecoveryTests | 7 | clean-up after an interrupted run |
| HistoryTests, ShortNameTests | 6 each | creation history; unique ISO 9660 short names |
| SettingsServiceTests | 5 | settings load and save, damaged settings file |
| DropValidatorTests, FilenameModeUiTests | 4 each | drag and drop; Filename Compatibility window |
| UdfUiTests | 3 | UDF choices in the main window |

## 2. File systems and filename modes on real data
**Method:** each source was prepared, created and then verified by the program's own code (the same code the main window runs). Verification compared every name, size and **SHA-256** hash with the source, and also mounted the image with **Windows** and compared what Windows lists. The image size predicted before creation was compared with the actual size.

**Datasets:**
- **A: real project copy.** 764 files and 278 folders (23.9 MB), including source code, build output, a Git repository and two MSTest DLLs with names longer than 64 characters. The "595-file dataset" named in the brief was not available as a separate folder. This is the same project copy that had 591–597 files in Stage 3, now larger because of build output.
- **B: synthetic edge cases.** 32 files and 23 folders:
  - Unicode names: accented Latin, Chinese, Arabic, Greek, Cyrillic, Japanese and an emoji.
  - Names of exactly 64, 65, 100 and 127 characters.
  - Names that collide when shortened: two names that differ only after character 64, and six that share their first 8 characters.
  - Zero-byte files, including one without an extension and one starting with a dot.
  - 3 empty folders, a folder nested 10 levels deep, and names with spaces, `& ( ) [ ] { } # % + = ; , '` and several dots.
  - Files of 1, 2048, 2049 bytes and 5 MB.
- **C: one name of 130 characters**, over UDF's 127-character limit.

| Format | Mode | A (764 files) | B (edge cases) | C (130-character name) |
|---|---|---|---|---|
| ISO 9660 + Joliet | Strict | Blocked: 2 names over 64 characters | Blocked: 6 names | Blocked: 1 name |
| ISO 9660 + Joliet | Compatibility | **Created and verified** (2 renames) | **Created and verified** (6 renames) | **Created and verified** |
| UDF | Strict | **Created and verified** | **Created and verified** (no renames) | Blocked: 1 name over 127 characters |
| UDF | Compatibility | **Created and verified** | **Created and verified** | **Created and verified** (1 rename) |
| ISO 9660 + Joliet + UDF | Strict | **Created and verified** | **Created and verified** | Blocked: 1 name |
| ISO 9660 + Joliet + UDF | Compatibility | **Created and verified** | **Created and verified** | **Created and verified** |
| ISO 9660 only | Strict | Blocked: 976 names | Blocked: 45 names | Blocked: 2 names |
| ISO 9660 only | Compatibility | **Created and verified** (976 renames) | **Created and verified** | **Created and verified** |

**Verified** means every check passed: structure, file count, folder count, names, sizes, SHA-256, volume label, file system and the Windows mount. In every run the image size matched the size predicted before creation exactly. In Compatibility Mode the names in each image matched the approved conversion plan exactly, and no two converted names collided. A blocked run created no file.

**Large file (dataset D):** a 4.5 GB file plus a small file.
- **UDF:** created a 4,833,017,856-byte image (exactly as predicted), and verification passed including SHA-256 of the 4.5 GB file and the Windows mount.
- **ISO 9660 + Joliet** and **ISO 9660 + Joliet + UDF:** refused before writing, with the message "four-and-a-half-GB.bin is 4.50 GB … cannot store files of 4 GB or larger".

## 3. Safety and error handling
| Test | Result |
|---|---|
| **Existing ISO, replacement not confirmed** | Refused ("The file … already exists"). The existing file was unchanged (hash compared). |
| **Existing read-only ISO** | Rejected with a message explaining how to clear *Read-only*. Even with replacement confirmed, the file was unchanged and no `.partial` or `.backup` file was left. |
| **Not enough free space** (the free-space report was replaced with 1 MB; no small drive was available) | Reported before writing: "The image needs 1.35 MB but only 1.00 MB is available." When the space disappeared between preparing and writing, writing was refused and nothing was left on disk. |
| **Source file locked by another program** | "Cannot read open-for-writing.db: the file is in use by another program". Accepted once the file was closed. |
| **Output paths** | Every invalid path was rejected with a specific message: empty, relative, a drive that doesn't exist, an invalid character `\|`, a missing folder, an existing folder, inside the source folder, an unreachable network share, a wrong extension, and `CON.iso` (after the fix). A valid path was accepted. |
| **Cancelling ISO 9660 + Joliet and ISO 9660 only** (764 files) | Cancelled within 20–200 ms. No output file and no `.partial` file. |
| **Cancelling UDF** (764 files) | Cancelled at 3.5–3.9 s. No output file and no `.partial` file. The delay is IMAPI2's layout step, which can't be interrupted: 0.4 s for 100 files, 3.1 s for 1,000, 9.2 s for 3,000. This is documented as a known limitation. |
| **Cancelling while replacing an existing ISO** (4.5 GB UDF image, cancelled during writing) | Cancelled. The original ISO was still there and unchanged (hash compared), with no `.partial` file. |

Carried over from Stage 4.1, on this PC:
- read-only source files and folders;
- paths of 435 and 438 characters with long path support on;
- a network source (`\\localhost\C$\…`);
- a OneDrive folder whose files were stored locally;
- migration of real settings and history from the development builds (history hash identical, old folder kept).

## 4. Detection of damaged and non-ISO files
Each image was created, then damaged in a copy, then verified with SHA-256. "Detected" means verification failed with the reason shown.

| Damage | ISO 9660 + Joliet | UDF | ISO 9660 + Joliet + UDF |
|---|---|---|---|
| None (control) | Passed | Passed | Passed |
| One byte changed inside a file's data | Detected: SHA-256 | Detected: SHA-256 | Detected: SHA-256 |
| Truncated to half | Detected: "image is truncated" | Detected: structure | Detected: structure |
| Last 8 KB missing | Detected: "image is truncated" | Passes; this is correct* | Passes; this is correct* |
| Sector 16 wiped (ISO 9660 descriptor or UDF recognition sequence) | Detected: not an ISO image | Detected: no NSR descriptor (after fix) | Detected: no NSR descriptor (after fix) |
| Sector 256 wiped (UDF anchor) | Detected: SHA-256 (file data lived there) | Detected: not an ISO image | Passes as ISO 9660 + Joliet**; detected when the expected format is given |

\* IMAPI2 fills the last 263 sectors of a UDF image with copies of the anchor. Losing a few of them leaves a valid UDF volume: Windows mounted it and every file's SHA-256 matched.
\** With the UDF anchor gone, the image is still a complete and valid ISO 9660 + Joliet image (Windows mounts it). Verification after creation knows the intended format and reports "Expected ISO 9660 + Joliet + UDF but found ISO 9660 + Joliet".

Files that aren't disc images were rejected:
- random data named `.iso`: no ISO 9660 or UDF volume descriptor;
- a zero-byte `.iso`: too small;
- a ZIP renamed to `.iso`: not a whole number of sectors, and no volume descriptor.

## 5. Installer and portable version
Tested on this PC as a standard user ("Install for me only") with the **final** `HTISOCreator_Setup_v1.0.0.exe` and `HTISOCreator_Portable_v1.0.0.zip` from Stage 4.4, built by Inno Setup 6.7.4. The user's real settings and history were backed up first and restored afterwards. (The same tests had also passed in Stages 4.2 and 4.3 on earlier builds.)

| Test | Result |
|---|---|
| Install a test build marked 0.9.0, silently, with the desktop shortcut | Exit code 0. 16 files, Start Menu and desktop shortcuts, one uninstall entry (publisher HTREASURE LTD). The installed program started. |
| Upgrade 0.9.0 → 1.0.0 (the final setup) | Exit code 0. Same folder, still one uninstall entry (now 1.0.0). The **settings file was byte-for-byte unchanged**, a marker value was kept, and the desktop shortcut was kept. Every installed file was **identical** (SHA-256) to the release files, and the setup file matched `SHA256SUMS.txt`. |
| Upgrade while the program is running | Setup asks you to close it. A silent install stopped with exit code 1 and the program kept running. |
| Uninstall | Exit code 0. Program files, both shortcuts and the uninstall entry removed. **Settings and history kept.** (The settings file had changed since the upgrade only because the program saves its window position when it closes.) |
| Installer wizard (Stage 4.2) | Shows the install-mode choice, folder, tasks and Ready pages with the app icon. Cancelling installed nothing. |
| Portable ZIP | Extracted to a folder with spaces and an accent (`Portable Final Café 4.4`). Runs without administrator rights and shows the main window. Exit code 0. Wrote nothing into its own folder. |
| Help menu documents | Open in the default `.md` program, or in Notepad when there is none (Stage 4.1). |

## 6. Final release packages (Stage 4.4)
All packages were made by `Tools\Build-Release.ps1 -Version 1.0.0` in one run: a clean Release build (0 warnings), the 179 automated tests (all passed), then the packages. No package was made from a build that skipped the tests.

| Check | Result |
|---|---|
| `SHA256SUMS.txt` | Every hash recomputed and matched. |
| Program in the packages | `HTISOCreator.exe` in the portable ZIP and in the installed folder is identical to the tested Release build: version 1.0.0, company HTREASURE LTD. |
| Installer file | Product "HT ISO Creator" 1.0.0, company HTREASURE LTD, "Copyright © 2026 HTREASURE LTD". **Not signed.** |
| Portable ZIP | 15 files in one folder: the program, its configuration, 3 DiscUtils DLLs, `THIRD-PARTY-NOTICES.txt`, the README, and 8 documents. |
| Documentation ZIP | The 8 user documents plus this test report. |
| Source ZIP | Solution, program, tests, installer script, tools and documents. No build output, `.git`, `.vs`, test results or `artifacts` folder. |
| No bootable features advertised | User documents only say that bootable images are **not** created. The developer notes in the source ZIP were corrected: they had still listed bootable images as planned for Stage 4. |
| Real data on the released program | The section 2 tests were run again with the program loaded from the extracted portable ZIP. Same results as in section 2: all 16 images created and verified (SHA-256 and Windows mount, size exactly as predicted), all 8 Strict runs where a name would change blocked, and the 4.5 GB UDF image verified. Dataset A had grown to 765 files by then (977 ISO 9660 renames). |

This report was completed after those tests, so the documentation and source ZIPs were then made again with `Build-Release.ps1 -RefreshDocs`. That step keeps the tested installer and portable ZIP unchanged, and refuses to run if the program or any document inside them has changed. `SHA256SUMS.txt` lists the final hash of every package.

| Package | SHA-256 |
|---|---|
| `HTISOCreator_Setup_v1.0.0.exe` (2,333,861 bytes) | `e37b6c3aef1bf7f9b731f5aa6bbce112b2461d9a9052847830ddf78f8e9cb9a5` |
| `HTISOCreator_Portable_v1.0.0.zip` (317,204 bytes) | `371b1fdd81943ef23da229b75901b2b9330050c358a309822ebb4453a045f9a3` |

## Defects found and fixed in Stage 4.3
| # | Defect | Fix | New test |
|---|---|---|---|
| 1 | An output file named after a Windows device (`CON.iso`, `NUL.iso`, `COM1.iso` …) was accepted and failed later, when the image was written. | Rejected straight away with an explanation. | `ReleaseTests.ReservedDeviceNames_AreRejectedAsOutputFile` |
| 2 | When Windows couldn't mount an image, the result showed `#< CLIXML` instead of the reason. | Windows' own message is shown, for example "The file or directory is corrupted and unreadable." | `ReleaseTests.MountErrors_AreReadableText_NotClixml` |
| 3 | A UDF image with a damaged recognition sequence passed verification when the mount check was off, although Windows refuses to mount it. | The UDF reader requires the NSR descriptor. | `UdfTests.DamagedUdfImage_IsRejectedOrReported` (extended) |

After the fixes, every real-data, edge-case and damage test in sections 2–4 was run again. The results above are from that final run.

## Not tested and open issues
| Item | Status |
|---|---|
| **Windows 10** | Not tested. Only Windows 11 build 26300 was available. Windows 10 support relies on .NET Framework 4.6.2+ and IMAPI2, which Windows 10 includes. See the checklist below. |
| **A clean PC or VM** | Not tested. Virtual machines exist on the test PC, but they belong to the owner and need their sign-in, so they weren't used. See the checklist below. |
| **"Install for all users"** | Not tested; it needs administrator rights. The per-user install was tested. |
| **.NET Framework missing** | The installer's message wasn't shown, because every supported Windows 10 (1607+) and Windows 11 PC has .NET 4.6.2 or later. |
| **A real full disk** | Simulated by replacing the free-space report (section 3); no small drive was available. |
| **OneDrive online-only files** | Not tested with files that had to be downloaded during creation. |
| **200% display scaling** | Checked only by automated layout tests. A 1080-pixel screen is too small for the window at 200%, so it is shrunk to fit. |
| **Code signing** | The program and installer are **not digitally signed**, so SmartScreen may warn. Users are told to check `SHA256SUMS.txt`. |
| **Licence** | No end-user licence has been chosen. The README says "All rights reserved". Third-party licences are in `THIRD_PARTY_NOTICES.md`. |
| **Contact and support details** | None are included; HTREASURE LTD hasn't supplied any. |
| **Cancelling UDF creation** | Takes effect after IMAPI2's layout step (about 3 s per 1,000 files). This is documented. |

### Checklist for a clean Windows 10 or Windows 11 machine
1. Copy `HTISOCreator_Setup_v1.0.0.exe` and `SHA256SUMS.txt`. Check the hash with `Get-FileHash`.
2. Install "for all users" from an administrator account. Confirm the Start Menu shortcut, the uninstall entry and that the program starts.
3. Create an ISO 9660 + Joliet image and a UDF image of any folder, with *Verify* and the SHA-256 check on. Confirm "Verification passed".
4. Install again over it (upgrade), then uninstall. Confirm the settings in `%APPDATA%\HTREASURE LTD\HT ISO Creator` remain.
5. Extract the portable ZIP and start it from a standard user account.

## How these results were produced
- **Automated tests:** `dotnet test -c Release` from the repository.
- **Real-data, edge-case and damage tests:** PowerShell scripts that load the Release `HTISOCreator.exe` and call the same creation and verification services as the main window. Windows mounting used `Mount-DiskImage`.
- **Installer tests:** silent installs (`/VERYSILENT /CURRENTUSER`) and the installer log, checking files, shortcuts, the uninstall entry and the settings file hash before and after.
- **Release packages:** `Tools\Build-Release.ps1`, then `Get-FileHash` and the ZIP contents checked against the build.
