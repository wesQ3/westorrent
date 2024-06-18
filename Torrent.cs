using System.Security.Cryptography;

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

        Tracker = Bencode.ReadString(bytes, "8:announce");
        Console.WriteLine($"  tracker:  {Tracker}");
        Filename = Bencode.ReadString(bytes, "4:name");
        Console.WriteLine($"  filename: {Filename}");
        Size = Bencode.ReadInt(bytes, "6:length");
        Console.WriteLine($"  length:   {Size}");
        PieceLength = (int)Bencode.ReadInt(bytes, "12:piece length");
        Console.WriteLine($"  piecelen: {PieceLength}");
        var pieces = Bencode.ReadBytes(bytes, "6:pieces");
        Pieces = pieces.Chunk(20).ToList();
        Console.WriteLine($"  pieces:   {Pieces.Count}");
        InfoHash = GetInfoHash(bytes);
        Console.WriteLine($"  infohash: {Convert.ToHexString(InfoHash)}");
    }

    public static byte[] ReadInfo(byte[] bytes)
    {
        // assume info hash stops at the end of pieces
        var start = Bencode.FindSequence(bytes, "4:info"); // + d
        var piecesStart = Bencode.FindSequence(bytes, "6:pieces");
        var (readStart, readLen) = Bencode.ReadLength(bytes, piecesStart);
        var end = readStart + readLen + 1; // + e
        var last = bytes[end];
        return bytes[start..end];
    }
    private static byte[] GetInfoHash(byte[] bytes)
    {
        var info = ReadInfo(bytes);
        return SHA1.HashData(info);

    }
}
