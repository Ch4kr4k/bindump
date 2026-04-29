# bindump

Deconstructs any binary file into a plain-text hex dump and reconstructs it back.
SHA256 hash is embedded in the dump and verified on reconstruction.

## Supported formats

Any binary file — `.pdf`, `.zip`, `.exe`, `.xlsx`, `.png`, `.iso`, etc.

## Build

```bash
dotnet build
```

## Usage

### Deconstruct
Reads a binary file and writes a hex dump to a text file.
```bash
dotnet run -- -d <input> <output.txt>
```

### Reconstruct
Reads a hex dump and writes the original binary back.
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

Plain ASCII text, human-readable header followed by hex lines.
