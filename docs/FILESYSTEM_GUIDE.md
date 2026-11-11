# HT ISO Creator 1.0.0 — File System Guide

An ISO image stores its files in a **file system**: the rules for names, folder structure and file sizes. HT ISO Creator offers four. All of them create **data images**; none of them makes an image bootable.

## Quick choice
| You need… | Choose |
|---|---|
| A general data disc or archive that almost anything can read | **ISO 9660 + Joliet** (default) |
| Names longer than 64 characters kept exactly (for example `Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.dll`) | **UDF** or **ISO 9660 + Joliet + UDF** |
| Files of 4 GB or larger | **UDF** |
| UDF, but also readable by older systems that don't understand UDF | **ISO 9660 + Joliet + UDF** (files under 4 GB) |
| Compatibility with very old systems only | **ISO 9660 only** |

## Comparison
| | ISO 9660 + Joliet | UDF | ISO 9660 + Joliet + UDF | ISO 9660 only |
|---|---|---|---|---|
| Names shown by Windows, macOS, Linux | Joliet | UDF | UDF | ISO 9660 |
| Longest name kept exactly | 64 characters | 127 characters | 127 characters | 30 (upper case, A–Z 0–9 _) |
| Characters | any except `* / : ; ? \` | any Windows allows | any Windows allows | A–Z, 0–9, _ |
| Upper/lower case kept | Yes | Yes | Yes | No |
| Largest file | under 4 GB | no practical limit (a 4.5 GB file was tested) | under 4 GB | under 4 GB |
| Written by | DiscUtils | Windows IMAPI2 | Windows IMAPI2 | DiscUtils |
| Readable by | practically every system since the 1990s | Windows 10/11, macOS, Linux and most modern systems | the same as UDF, and also by readers that only understand ISO 9660 or Joliet | everything, including very old systems |

## ISO 9660 + Joliet
ISO 9660 is the original CD-ROM file system. **Joliet** adds a second set of names alongside it, with Unicode support and up to **64 characters** per name. That limit is part of the Joliet standard; tools that go beyond it produce non-standard images, so HT ISO Creator doesn't.

Every item also gets a short upper-case ISO 9660 *alias* name for very old readers. Aliases aren't filename changes: Windows and other systems show the Joliet names, and Strict Mode checks those.

## UDF
UDF (Universal Disk Format) is the file system of DVDs and Blu-ray discs. HT ISO Creator writes **UDF 1.02**, the most widely readable revision, using the IMAPI2 component built into Windows. It keeps names of up to **127 characters** with any character Windows allows, and stores files of **4 GB and larger**.

A UDF-only image has no ISO 9660 or Joliet names. Systems that only understand ISO 9660 (very old ones) can't read it.

## ISO 9660 + Joliet + UDF (hybrid)
The hybrid holds UDF names together with ISO 9660 and Joliet names for the same files:
- **Modern systems** read the UDF names (up to 127 characters).
- **Older systems** read the Joliet or ISO 9660 names.
- **Joliet names here:** IMAPI2 writes them itself, up to about 110 characters, which is more than the Joliet standard's 64. A very old Joliet-only reader might not show those long names.
- **ISO 9660 names here:** they are 8.3-style aliases such as `MICROS~1.DLL`.

Because the ISO 9660 part stores 32-bit file sizes, files must be under 4 GB. HT ISO Creator checks that the UDF and ISO 9660 views list the same files and data.

## ISO 9660 only
Names are converted to upper case, `A–Z 0–9 _`, at most 30 characters with an extension of up to 8. Almost every modern name changes, so **Strict Mode refuses most folders** with this file system. Use it only for very old readers.

## Name checks in Strict Mode
| File system | Names checked |
|---|---|
| ISO 9660 + Joliet | Joliet names (64-character limit, no `* / : ; ? \`) |
| UDF, hybrid | UDF names (127-character limit) |
| ISO 9660 only | ISO 9660 names |

Two names that differ only in letter case, such as `Readme.txt` and `README.TXT`, count as a clash, because Windows treats them as the same file.

## Not provided
- **Bootable images:** El Torito, BIOS or UEFI boot.
- **Rock Ridge:** Unix permissions and symbolic links.
- **UDF revisions above 1.02.**
