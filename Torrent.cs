using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text;

public class Torrent
{
    public long Size { get; set; }
    public string Tracker { get; set; }
    public string Filename { get; set; }
    public int PieceLength { get; set; }
    public List<byte[]> Pieces { get; set; }
    public byte[] InfoHash {get; set;}

    public Torrent(string inFile)
    {
        Console.WriteLine($"read from {inFile}");
        var bytes = File.ReadAllBytes(inFile);
        Console.WriteLine($"  file size {bytes.Length}");
        if (bytes[0] != 0x64)
            throw new InvalidDataException();

        Tracker = ReadString(bytes, "8:announce");
        Console.WriteLine($"  tracker:  {Tracker}");
        Filename = ReadString(bytes, "4:name");
        Console.WriteLine($"  filename: {Filename}");
        Size = ReadInt(bytes, "6:length");
        Console.WriteLine($"  length:   {Size}");
        PieceLength = ReadInt(bytes, "12:piece length");
        Console.WriteLine($"  piecelen: {PieceLength}");
        var pieces = ReadBytes(bytes, "6:pieces");
        Pieces = pieces.Chunk(20).ToList();
        Console.WriteLine($"  pieces:   {Pieces.Count}");
        InfoHash = GetInfoHash(bytes);
        Console.WriteLine($"  infohash: {Convert.ToHexString(InfoHash)}");
    }

    private static byte[] ReadBytes(byte[] bytes, string label)
    {
        var loc = FindSequence(bytes, label);
        var (readStart, readLen) = ReadLength(bytes, loc);
        return bytes.Skip(readStart).Take(readLen).ToArray();

    }
    private static string ReadString(byte[] bytes, string label)
    {
        return Encoding.ASCII.GetString(ReadBytes(bytes, label));
    }

    private static int ReadInt(byte[] bytes, string label)
    {
        var loc = FindSequence(bytes, label);
        // i262144e
        var intBytes = bytes.Skip(loc + 1).TakeWhile((b, check) => b != 0x65).ToArray();
        return int.Parse(Encoding.ASCII.GetString(intBytes));
    }

    private static byte[] ReadInfo(byte[] bytes)
    {
        var start = FindSequence(bytes, "4:info"); // + d
        var piecesStart = FindSequence(bytes, "6:pieces");
        var (readStart, readLen) = ReadLength(bytes, piecesStart);
        var end = readStart + readLen + 1; // + e
        var last = bytes[end];
        return bytes[start..end];
    }
    private static byte[] GetInfoHash(byte[] bytes)
    {
        var info = ReadInfo(bytes);
        return SHA1.HashData(info);

    }

    private static int FindSequence(byte[] bytes, string label)
    {
        return FindSequence(bytes, label, 0);
    }
    private static int FindSequence(byte[] bytes, string label, int start)
    {
        var target = Encoding.ASCII.GetBytes(label);
        for (int i = start; i < bytes.Length; i++)
        {
            var found = bytes.Skip(i)
                .Take(target.Length)
                .SequenceEqual(target);
            if (found)
                return i + target.Length;
        }
        throw new KeyNotFoundException();
    }

    private static (int,int) ReadLength(byte[] bytes, int start)
    {
        var readLenBytes = bytes.Skip(start).TakeWhile((b, check) => b != 0x3a).ToArray();
        var readLen = int.Parse(Encoding.ASCII.GetString(readLenBytes));
        return (start + readLenBytes.Length + 1, readLen);
    }
}
