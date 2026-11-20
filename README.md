# HT ISO Creator 1.0.2

**HT ISO Creator** is a Windows desktop application from **HTREASURE LTD** ([www.htreasure.com](https://www.htreasure.com)). It creates and verifies **data ISO images** (ISO 9660, Joliet and UDF) from folders.

- **Four file systems:** ISO 9660 + Joliet (default), UDF, ISO 9660 + Joliet + UDF hybrid, and ISO 9660 only.
- **Long names and large files:** UDF keeps file names of up to 127 characters exactly and stores files of 4 GB and larger.
- **Strict Mode (default):** no image is created if any name would change. **Compatibility Mode** converts names only after you confirm every conversion. You can preview the conversions and export them as TXT or CSV.
- **Verification:** the image is read back with HT ISO Creator's own ISO 9660, Joliet and UDF readers. Names, sizes and SHA-256 of every file are compared with the source, and the image can optionally be mounted with Windows.
- **Safe replacement:** an existing ISO is replaced only once the new image is complete. Cancelling or an interruption leaves nothing half-written.
- **Other tools:** target disc check, ISO information viewer, creation history, light and dark themes.

> HT ISO Creator creates data images only. It does not create bootable images.

## Download and install
| File | What it is |
|---|---|
| `HTISOCreator_Setup_v1.0.2.exe` | Windows installer (all users, or "for me only" without administrator rights) |
| `HTISOCreator_Portable_v1.0.2.zip` | Portable version: extract and run, no installation |
| `HTISOCreator_Documentation_v1.0.2.zip` | All documentation |
| `HTISOCreator_Stage4_Complete.zip` | Complete source code |
| `SHA256SUMS.txt` | SHA-256 checksums of the files above |

**Requirements:** Windows 7 SP1, Windows 8.1, Windows 10 or Windows 11, with .NET Framework 4.6.2 or later (Windows 10 version 1607 and later and Windows 11 include it; on Windows 7 SP1 and 8.1, install .NET Framework 4.8 from Microsoft). The original Windows 8 is not supported: upgrade it to Windows 8.1, which is free. See [docs/INSTALLATION.md](docs/INSTALLATION.md).

## Documentation
| Document | Contents |
|---|---|
| [docs/USER_GUIDE.md](docs/USER_GUIDE.md) | Using the application, step by step |
| [docs/FILESYSTEM_GUIDE.md](docs/FILESYSTEM_GUIDE.md) | Choosing ISO 9660, Joliet or UDF |
| [docs/INSTALLATION.md](docs/INSTALLATION.md) | Installer, portable version, upgrades, uninstalling |
| [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Messages and their solutions |
| [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md) | What's in 1.0.2, known limitations |
| [docs/TEST_REPORT.md](docs/TEST_REPORT.md) | Tests run on this release and their results |
| [CHANGELOG.md](CHANGELOG.md) | Changes by version |
| [docs/THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md) | Third-party components and licences |
| [docs/DEVELOPMENT_NOTES.md](docs/DEVELOPMENT_NOTES.md) | Design notes from development (Stages 1–3) |

## Building from source
| | |
|---|---|
| Language / UI | C#, Windows Forms |
| Framework | .NET Framework 4.6.2 (AnyCPU) |
| Image writers | DiscUtils.Iso9660 0.16.13 (MIT) for ISO 9660 / Joliet; Windows IMAPI2 for UDF |
| Image readers | HT ISO Creator's own `IsoImageReader` and `UdfImageReader` (used for verification) |

### Visual Studio
1. Install **Visual Studio 2022** (17.x) or later with the **.NET desktop development** workload. You don't need the .NET Framework 4.6.2 targeting pack: the project uses the `Microsoft.NETFramework.ReferenceAssemblies` package.
2. Put the source in a short folder such as `C:\Dev\HTISOCreator`. Very long paths stop the .NET Framework test host from finding the tests.
3. Open `HTISOCreator.sln`. The first build restores the NuGet packages, which needs internet access.
4. Select **Release** and choose **Build > Build Solution**. The result is 0 errors and 0 warnings.
5. Choose **Test > Run All Tests**. Tests write temporary files under `%TEMP%\HTISOCreator.Tests`. One test creates a 4 GB UDF image, so it needs about 9 GB of free space while it runs.

### Command line
```
dotnet build HTISOCreator.sln -c Release
dotnet test HTISOCreator.sln -c Release
```

### Release packages
`Tools\Build-Release.ps1` builds every release file from one clean Release build. It needs the .NET SDK and Inno Setup 6.
```
powershell -ExecutionPolicy Bypass -File Tools\Build-Release.ps1
```
In order, it:
1. checks the version is consistent;
2. does a clean Release build, with warnings treated as errors;
3. runs all tests;
4. generates the advisory snapshot and `ht-compatibility.json` with the HTREASURE `ht-standards` tool;
5. creates the installer, the portable ZIP, the documentation ZIP and the source ZIP, plus `SHA256SUMS.txt`, in `artifacts\v1.0.2\`.

It needs [HTREASURE-LTD/ht-software-standards](https://github.com/HTREASURE-LTD/ht-software-standards) checked out next to this repository (`..\ht-software-standards`), or `-StandardsRoot <folder>`. As the HTREASURE standards require, a release signs the advisory snapshot: add `-AdvisorySigningKey <offline PEM> -AdvisoryKeyId <id>`. Until HTREASURE LTD provisions a signing key, only an internal test build is possible, with `-AllowUnsignedSnapshot`.

### HTREASURE standards
HT ISO Creator follows the HTREASURE software template and standards:
- **Branding** (`branding\`, from HTREASURE-LTD/HT-Software-Template): HT ISO Creator's product icon, the HTREASURE colours, company details, installer artwork and the "An HTREASURE LTD product" attribution. The copyright year is set at build time.
- **Security Advisory History** (mandatory component): **Help > Security Advisories** and **Help > About > Security Advisory History**. The standards' libraries need .NET 10, so `HTISOCreator\Security\` is a .NET Framework 4.6.2 port of their offline part (status rules, validation, ECDSA P-256 signature checks, reports). It passes the standards' conformance fixtures (`Tests\HTISOCreator.Tests\StandardsFixtures`) and verifies snapshots signed by the standards' own tool. Online advisory checks and the software update service are not configured: HTREASURE LTD has no advisory or release feed for this product yet.

### Repository layout
```
HTISOCreator.sln
HTISOCreator\              application: Forms, Services, Models, Helpers, Security, Properties, Resources
Tests\HTISOCreator.Tests\  MSTest tests
installer\HTISOCreator.iss Inno Setup script
Tools\                     Build-Release.ps1, Verify-Iso.ps1
branding\                  HTREASURE brand assets used by the app and installer (see branding\README.md)
docs\                      user and release documentation
LICENSE.txt                end-user licence agreement (shown by the installer, shipped next to the program)
THIRD-PARTY-NOTICES.txt    licence text shipped next to the program
```

## Licence
Copyright © 2026 HTREASURE LTD. All rights reserved.

HT ISO Creator is licensed under the HTREASURE LTD [End-User Licence Agreement](LICENSE.txt): free for **personal, non-commercial use**, governed by the law of England and Wales. Business or other organisational use needs a separate licence from HTREASURE LTD (www.htreasure.com). It is not an open-source licence. Third-party components keep their own licences; see [docs/THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md).
