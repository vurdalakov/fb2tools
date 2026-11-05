# Fb2Tools

[Fb2Tools](https://github.com/vurdalakov/fb2tools) repository contains tools to work with .FB2/.FB2.ZIP files and generate OPDS catalogs out of them.

## fb2zip

Command-line utility that finds zip files that are not plain "Deflate + UTF-8 names" (algorithms not supported by the .NET 10 `ZipFile` class, missing UTF-8 flag) and repacks them; also packs `.fb2` files that have no `.fb2.zip`.

### Usage

* `fb2zip test <folder>`   - reports problems and changes nothing:
  * zip entries compressed with anything but Deflate;
  * zip entries with a non-ASCII name but without the UTF-8 flag (ASCII-only names do not need it);
  * `.fb2` files without a matching `.fb2.zip`.
* `fb2zip repack <folder>` - repacks zip files that have either of the first two problems, so that all entries use Deflate and UTF-8 names.
* `fb2zip pack <folder>`   - creates `<name>.fb2.zip` (Deflate, UTF-8 name) for every `<name>.fb2` that has none. The `.fb2` is kept.

Options (`test`, `repack` and `pack` each accept `--help`):

* `--no-recursive` - scan only the given folder; by default sub-folders are scanned too.
* `--encoding <e>` (`test`, `repack`) - code page (number or name) used for entry names that are not UTF-8 (default `866`; use `437` for Western DOS-era zips). Repacked archives always store names as UTF-8.
* `--backup` (`repack` only) - keep the original as `<name>.zip.bak`.

Deflate is the only accepted compression method. Everything else (Stored, Deflate64, BZip2, LZMA, PPMd, Zstandard, XZ, Shrink, Implode, ...) is reported and repacked to Deflate. Directory entries are ignored. Encrypted entries are reported but cannot be repacked.

Each repacked archive is verified (names, sizes, CRC32) before it replaces the original.

## fb2opds

Command-line utility that generates an [OPDS](https://opds.io/) catalog from a collection of `.fb2` books, so that e-book readers can browse and download them. Book metadata and cover images are read from the `.fb2` files; each sub-folder with books becomes a catalog folder.

### Usage

```
fb2opds --path "C:\MyLibrary" --url "https://vurdalakov.net/books" --name "My library"
```

* `--path <dir>` (required) - root directory of the library.
* `--url <url>` (required) - base URL under which the library is published.
* `--name <name>` - catalog name; if missing, the library directory name is used.
* `--webhook-token <secret>` - also writes `opds/rebuild.php` alongside `opds/opds.xml`: an authenticated HTTP endpoint that re-runs the same command in the background. Must be supplied on every run that should keep the webhook; a run without it removes `rebuild.php`.

Only one instance can run per library at a time. Run `fb2opds --help` for the full help.

## Build

```
dotnet publish src\Fb2Tools.sln -c Release -r win-x64
```

Output: `bin\Release\win-x64\publish\fb2zip.exe` and `fb2opds.exe` (single files, require the .NET 10 runtime).

Other platforms (all dependencies are managed code, so only the runtime identifier changes):

```
dotnet publish src\Fb2Tools.sln -c Release -r win-arm64
dotnet publish src\Fb2Tools.sln -c Release -r linux-x64
dotnet publish src\Fb2Tools.sln -c Release -r linux-arm64
dotnet publish src\Fb2Tools.sln -c Release -r osx-x64
dotnet publish src\Fb2Tools.sln -c Release -r osx-arm64
```

Output: `bin\Release\<rid>\publish\fb2zip` and `fb2opds` (`.exe` on Windows).

## Credits and license

Claude Sonnet 5.5 assisted with code development and documentation.

`Fb2Tools` is distributed under the terms of the [MIT license](https://opensource.org/licenses/MIT).
