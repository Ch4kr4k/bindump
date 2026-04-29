using System.Security.Cryptography;
using System.Text;

const string Marker    = "BINARY_DUMP_V1";
const string HashLabel = "SHA256:";
const string SizeLabel = "SIZE:";
const string ExtLabel  = "EXT:";
const int    BytesPerLine = 16;

static string Hash(byte[] data)
{
    using var sha = SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(data));
}

static void Deconstruct(string src, string dst)
{
    byte[] data = File.ReadAllBytes(src);
    string hash = Hash(data);
    string ext  = Path.GetExtension(src);

    Console.WriteLine($"Read   : {src} ({data.Length} bytes)");
    Console.WriteLine($"SHA256 : {hash}");

    using var w = new StreamWriter(dst, append: false, Encoding.ASCII);
    w.WriteLine(Marker);
    w.WriteLine($"{HashLabel}{hash}");
    w.WriteLine($"{SizeLabel}{data.Length}");
    w.WriteLine($"{ExtLabel}{ext}");

    for (int i = 0; i < data.Length; i += BytesPerLine)
    {
        int len = Math.Min(BytesPerLine, data.Length - i);
        w.WriteLine(Convert.ToHexString(data.AsSpan(i, len)));
    }

    Console.WriteLine($"Written: {dst}");
}

static void Reconstruct(string src, string dst)
{
    using var r = new StreamReader(src, Encoding.ASCII);

    string? marker = r.ReadLine();
    if (marker != Marker)
        throw new InvalidDataException($"Bad marker: '{marker}'");

    string hashLine = r.ReadLine() ?? throw new InvalidDataException("Missing hash line");
    if (!hashLine.StartsWith(HashLabel)) throw new InvalidDataException("Malformed hash line");
    string expectedHash = hashLine[HashLabel.Length..];

    string sizeLine = r.ReadLine() ?? throw new InvalidDataException("Missing size line");
    if (!sizeLine.StartsWith(SizeLabel)) throw new InvalidDataException("Malformed size line");
    int expectedSize = int.Parse(sizeLine[SizeLabel.Length..]);

    string extLine = r.ReadLine() ?? throw new InvalidDataException("Missing ext line");
    string originalExt = extLine.StartsWith(ExtLabel) ? extLine[ExtLabel.Length..] : "";

    if (!string.IsNullOrEmpty(originalExt) && Path.GetExtension(dst) != originalExt)
        Console.WriteLine($"Warning: original extension was '{originalExt}', output is '{Path.GetExtension(dst)}'");

    using var ms = new MemoryStream(expectedSize);
    string? line;
    while ((line = r.ReadLine()) != null)
        if (!string.IsNullOrWhiteSpace(line))
            ms.Write(Convert.FromHexString(line));

    byte[] data = ms.ToArray();

    if (data.Length != expectedSize)
        throw new InvalidDataException($"Size mismatch: expected {expectedSize}, got {data.Length}");

    string actualHash = Hash(data);
    if (actualHash != expectedHash)
        throw new InvalidDataException(
            $"Hash mismatch\n  expected : {expectedHash}\n  actual   : {actualHash}");

    File.WriteAllBytes(dst, data);
    Console.WriteLine($"SHA256 verified : {actualHash}");
    Console.WriteLine($"Reconstructed   : {dst} ({data.Length} bytes)");
}

// ── arg parsing ───────────────────────────────────────────────────────────────

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  bindump -d <input>    <output.txt>");
    Console.Error.WriteLine("  bindump -r <input.txt> <output>");
}

if (args.Length != 3 || (args[0] != "-d" && args[0] != "-r"))
{
    PrintUsage();
    Environment.Exit(1);
}

try
{
    if (args[0] == "-d") Deconstruct(args[1], args[2]);
    else                 Reconstruct(args[1], args[2]);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(2);
}
