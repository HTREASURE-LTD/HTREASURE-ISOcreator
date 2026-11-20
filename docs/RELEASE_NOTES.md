# HT ISO Creator 1.1.0 — Release Notes

**Publisher:** HTREASURE LTD
**Version:** 1.1.0 (file version 1.1.0.0)
**Website:** [www.htreasure.com](https://www.htreasure.com)
**Requirements:** Windows 7 SP1, Windows 8.1, Windows 10 or Windows 11, with .NET Framework 4.6.2 or later

HT ISO Creator creates **data ISO images** from folders and verifies them. It does not create bootable images. Version 1.1.0 applies the HTREASURE software template and standards, and adds Windows 7 SP1 and Windows 8.1. 1.0.0 was the first public release.

## New in 1.1.0
- **Security Advisory History** (**Help > Security Advisories**, or **Help > About > Security Advisory History**): the security advisories HTREASURE LTD has published for HT ISO Creator, and what each means for your version (Fixed, Affected, Under Review, Unknown or Not Affected), with details and **Export Security Report** (Markdown or JSON, without computer or user names). It works offline from advisory data bundled with the release and signed by HTREASURE LTD; a banner always says that this data is historical. HTREASURE LTD has not published any advisory for HT ISO Creator, so the list is empty. As the window says, a lack of known advisories is not proof that the software is free from vulnerabilities.
- **Windows 7 SP1 and Windows 8.1.** HT ISO Creator now installs and runs on Windows 7 SP1 and later (it needed Windows 10). On Windows 7 the optional Windows mount check is unavailable, because Windows 7 cannot mount ISO images; everything else works the same. The original Windows 8 is not supported; upgrade it to Windows 8.1 (free).
- **HTREASURE branding:** the HT ISO Creator product icon, the HTREASURE colours, "An HTREASURE LTD product", the company registration (England and Wales, no. 17178941) and support address (support@htreasure.com) in the About window, and a footer in the status bar.
- **Installer:** HTREASURE artwork. New installations go to `Program Files\HTREASURE\HT ISO Creator` and the Start menu **HTREASURE** folder; an upgrade keeps the existing folder and moves its Start menu shortcut.

Image creation and verification are unchanged from 1.0.2. HT ISO Creator still never connects to the internet by itself: online advisory checks and security updates are not available yet.

## In 1.0.2
- **End-user licence agreement** (`LICENSE.txt`): HT ISO Creator is free for **personal, non-commercial use**. Use by or for a business or other organisation needs a separate licence from HTREASURE LTD ([www.htreasure.com](https://www.htreasure.com)). The agreement is governed by the law of England and Wales.
- The installer shows the licence agreement, and it must be accepted before installing.
- The licence is installed next to the program, included in the portable and documentation packages, and opens from **Help > Licence Agreement** and **About > View licence agreement**.


## In 1.0.1
- **Fixed-size main window:** it can no longer be resized or maximised, and it still opens where you last left it. On a screen too small for it (very high display scaling), it fits the screen and scrolls.
- **Menu below the banner:** File, Tools and Help sit directly below the HT ISO Creator logo and title.
- **HTREASURE LTD website:** a [www.htreasure.com](https://www.htreasure.com) link in the main window and the About window, and in Windows' installed apps list.


## Features
- **Four file systems:**
  - **ISO 9660 + Joliet** (default) and **ISO 9660 only**, written by DiscUtils.
  - **UDF** and **ISO 9660 + Joliet + UDF**, written by the IMAPI2 component built into Windows.
- **Long names and large files:** UDF keeps names of up to 127 characters exactly and stores files of 4 GB and larger.
- **Strict Mode (default):** an image is never created if any file or folder name would change. **Compatibility Mode** converts names only after you have reviewed and confirmed every conversion.
- **Filename conversion preview and reports** in TXT and CSV.
- **Verification:**
  - The image is read back by HT ISO Creator's own ISO 9660, Joliet and UDF readers, which share no code with the writers.
  - It compares names (letter case included), sizes and **SHA-256** of every file with the source.
  - It can optionally mount the image with Windows and compare what Windows sees.
- **Safe by design:**
  - Free space is checked first.
  - An existing ISO is replaced only once the new one is complete, and never if it is read-only.
  - Cancelling or an interruption leaves no partial files behind.
  - The source folder is never modified.
- **Target disc check** for CD, DVD, DVD Dual Layer, Blu-ray and custom sizes.
- **ISO Information viewer** and **Creation History**.
- **Light and dark themes.** The window keeps a correct layout at 100%, 125% and 150% display scaling, and remembers its position.
- **Windows installer** (per-user or all users) and **portable** version.

## Installing
See [INSTALLATION.md](INSTALLATION.md). Run `HTISOCreator_Setup_v1.1.0.exe`, or extract `HTISOCreator_Portable_v1.1.0.zip`. Check downloads against `SHA256SUMS.txt`.

## Upgrading
**From 1.0.0, 1.0.1 or 1.0.2:** run the 1.1.0 installer and accept the licence agreement, or replace the portable folder. Settings and history are kept, and the program stays in its current folder. A window size saved by 1.0.0 is no longer used, because the window has a fixed size.

**From development builds:** Settings and history from the Stage 1–3 development builds (`%APPDATA%\HT Software\HT ISO Creator`) are copied automatically the first time you start 1.0.0 or later.

## Known limitations
- **No bootable images:** HT ISO Creator 1.1.0 makes data images only.
- **No Rock Ridge:** Unix permissions and symbolic links aren't supported.
- **UDF images are UDF 1.02.** In hybrid images, the extra Joliet names written by Windows IMAPI2 can be up to about 110 characters (standard Joliet allows 64), and the ISO 9660 names are 8.3-style. Modern systems read the UDF names.
- **Names over 64 characters** (ISO 9660 + Joliet) or **127 characters** (UDF) can't be stored exactly. Strict Mode refuses them; Compatibility Mode shortens them after you confirm.
- **Files of 4 GB and larger** require UDF.
- **Paths over 260 characters** require Windows long path support to be turned on.
- **Program and installer not digitally signed.** Windows SmartScreen may warn when you run the downloaded installer. Check the SHA-256 checksum first. (Only the bundled advisory data is signed, by HTREASURE LTD.)
- **Cancelling a UDF image** takes effect after Windows IMAPI2 has finished laying out the image, which can't be interrupted: about 3 seconds per 1,000 files (measured 0.4 s for 100 files, 9 s for 3,000). The ISO 9660 formats stop within a fraction of a second. Nothing is left behind either way.
- **Windows mount check:** the optional check briefly assigns a drive letter, and it can fail where policy blocks mounting. It needs Windows 8 or later.
- **Windows 7 SP1 and 8.1** are supported, but this release was not tested on them; see TEST_REPORT.md.
- **Security advisories are offline only.** No online advisory service or update service exists for HT ISO Creator yet, so newer advisories appear only with a newer release.
- **Display scaling at 200%** was checked by simulation on a 1080-pixel screen; see TEST_REPORT.md.

## Verification of this release
[TEST_REPORT.md](TEST_REPORT.md) lists the automated and manual tests run on this exact build and their results.
