# bindump

Deconstructs any binary file into a compact plain-text dump and reconstructs it
back. The payload is GZip-compressed (when compression reduces size) and
Base64-encoded, keeping the output file as small as possible. A SHA256 hash is
embedded in the dump and verified on reconstruction.

## Supported formats

Any binary file — `.pdf`, `.zip`, `.exe`, `.xlsx`, `.png`, `.iso`, etc.

## Build

```bash
dotnet build
```

## Usage

### Deconstruct
Reads a binary file, compresses it with GZip (if beneficial), Base64-encodes
the result, and writes it to a plain-text file.
```bash
dotnet run -- -d <input> <output.txt>
```

### Reconstruct
Reads a dump file and writes the original binary back, verifying the SHA256
hash in the process.
```bash
dotnet run -- -r <input.txt> <output>
```

### Examples
```bash
dotnet run -- -d document.pdf dump.txt
dotnet run -- -r dump.txt document.pdf

dotnet run -- -d archive.zip dump.txt
dotnet run -- -r dump.txt archive.zip
```

## Dump format

Plain ASCII text with a human-readable header followed by Base64 lines (76
characters wide).

```
BINARY_DUMP_V2
SHA256:<hex-hash>
SIZE:<original-byte-count>
EXT:<file-extension>
COMPRESSED:<True|False>
<Base64 data lines…>
```

- **COMPRESSED: True** — payload was GZip-compressed before encoding (used when
  compression shrinks the data).
- **COMPRESSED: False** — payload is the raw original bytes Base64-encoded
  (used for already-compressed formats such as `.zip`, `.png`, `.jpg`).
