# Third-Party Notices — HT ISO Creator 1.0.1

HT ISO Creator is Copyright © 2026 HTREASURE LTD. It includes or uses the components below.

## DiscUtils 0.16.13 (MIT License)
Packages: DiscUtils.Iso9660, DiscUtils.Core, DiscUtils.Streams. Source: https://github.com/DiscUtils/DiscUtils

These are distributed with HT ISO Creator as `DiscUtils.Core.dll`, `DiscUtils.Iso9660.dll` and `DiscUtils.Streams.dll`, and are used to write ISO 9660 and ISO 9660 + Joliet images.

```
Copyright (c) 2008-2011, Kenneth Bell
Copyright (c) 2014, Quamotion
Copyright (c) the DiscUtils contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
```

## Microsoft Windows IMAPI2 (part of Windows)
UDF and ISO 9660 + Joliet + UDF images are written by the IMAPI2 file system image component (`IMAPI2FS`), which is part of Microsoft Windows. HT ISO Creator calls it through its documented COM interface. Nothing from IMAPI2 is redistributed.

## Microsoft .NET Framework (part of Windows)
HT ISO Creator runs on Microsoft .NET Framework 4.6.2 or later, which Windows provides. It is not redistributed.

## Installer: Inno Setup 6.7.4
The Windows installer was built with Inno Setup, Copyright (C) 1997-2026 Jordan Russell and Martijn Laan (https://jrsoftware.org). Installers made with Inno Setup may be distributed freely, including commercially, under the Inno Setup License. Inno Setup itself is not included in HT ISO Creator.

## Development and test tools (not distributed)
MSTest and Microsoft.NET.Test.Sdk (MIT), and Microsoft.NETFramework.ReferenceAssemblies (MIT), are used to build and test HT ISO Creator. They are not part of the released program.
