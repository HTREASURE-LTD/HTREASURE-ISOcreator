# HTREASURE brand assets used by HT ISO Creator

These files are copied from the HTREASURE LTD branding kit in
[HTREASURE-LTD/HT-Software-Template](https://github.com/HTREASURE-LTD/HT-Software-Template) (`HTREASURE-Branding`, commit `c1fbde6`).
Only the files HT ISO Creator uses are copied. Don't edit them here: change the kit and copy them again. One change: the company's registered office address is removed from `brand.json` here, because HT ISO Creator does not show it.

| File | From the kit | Used for |
|---|---|---|
| `brand.json` | `brand.json` | Reference: company details, colours and copyright rule used in the About window, status bar and `Helpers\BrandColors.cs` |
| `Icons/HT-iso-maker.ico`, `.svg` | `Icons/Products/iso-maker/` | Program icon, window icons and the installer icon. The kit's product id is `iso-maker` (listed in `brand.json` as "HT ISO Maker"); HT ISO Creator uses that product's icon and keeps its published name. |
| `Installer/inno-wizard-*.bmp` | `IntegrationExamples/Installer/Images/` | Installer wizard artwork at 100%, 150% and 200% display scaling |

Brand rules applied (kit `README.md` and `IntegrationExamples/Installer/README.md`):
- publisher `HTREASURE LTD`, attribution "An HTREASURE LTD product";
- copyright `Copyright © {year} HTREASURE LTD. All rights reserved.`, with the year set at build time;
- install folder `Program Files\HTREASURE\<product>`, Start menu folder `HTREASURE\<product>`;
- no ® or ™, and the program and installer are never described as digitally signed (`codeSigning.configured` is `false`).

The kit's .NET branding libraries (splash screen, About form) target .NET 10, so they are not used; HT ISO Creator stays on .NET Framework 4.6.2 to support Windows 7 SP1 and later.

Copyright © 2026 HTREASURE LTD. All rights reserved.
