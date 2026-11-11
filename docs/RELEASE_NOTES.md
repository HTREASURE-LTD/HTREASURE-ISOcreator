# HT ISO Creator 1.0.0 — Release Notes

**Publisher:** HTREASURE LTD
**Version:** 1.0.0 (file version 1.0.0.0)
**Requirements:** Windows 10 or Windows 11, .NET Framework 4.6.2 or later

HT ISO Creator 1.0.0 is the first public release. It creates **data ISO images** from folders and verifies them. It does not create bootable images.

## Highlights
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
- **Light and dark themes.** The window keeps a correct layout at 100%, 125% and 150% display scaling, and remembers its size and position.
- **Windows installer** (per-user or all users) and **portable** version.

## Installing
See [INSTALLATION.md](INSTALLATION.md). Run `HTISOCreator_Setup_v1.0.0.exe`, or extract `HTISOCreator_Portable_v1.0.0.zip`. Check downloads against `SHA256SUMS.txt`.

## Upgrading from development builds
Settings and history from the Stage 1–3 development builds (`%APPDATA%\HT Software\HT ISO Creator`) are copied automatically the first time you start 1.0.0.

## Known limitations
- **No bootable images:** HT ISO Creator 1.0.0 makes data images only.
- **No Rock Ridge:** Unix permissions and symbolic links aren't supported.
- **UDF images are UDF 1.02.** In hybrid images, the extra Joliet names written by Windows IMAPI2 can be up to about 110 characters (standard Joliet allows 64), and the ISO 9660 names are 8.3-style. Modern systems read the UDF names.
- **Names over 64 characters** (ISO 9660 + Joliet) or **127 characters** (UDF) can't be stored exactly. Strict Mode refuses them; Compatibility Mode shortens them after you confirm.
- **Files of 4 GB and larger** require UDF.
- **Paths over 260 characters** require Windows long path support to be turned on.
- **Not digitally signed.** Windows SmartScreen may warn when you run the downloaded installer. Check the SHA-256 checksum first.
- **Cancelling a UDF image** takes effect after Windows IMAPI2 has finished laying out the image, which can't be interrupted: about 3 seconds per 1,000 files (measured 0.4 s for 100 files, 9 s for 3,000). The ISO 9660 formats stop within a fraction of a second. Nothing is left behind either way.
- **Windows mount check:** the optional check briefly assigns a drive letter, and it can fail where policy blocks mounting.
- **Display scaling at 200%** was checked by simulation on a 1080-pixel screen; see TEST_REPORT.md.

## Verification of this release
[TEST_REPORT.md](TEST_REPORT.md) lists the automated and manual tests run on this exact build and their results.
