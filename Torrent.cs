using System.Text;

public class Torrent
{
    public long Size { get; set; }
    public string Tracker { get; set; }
    public string Filename { get; set; }
    public int PieceLength { get; set; }
    public List<byte[]> Pieces { get; set; }

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
    }

    private static byte[] ReadBytes(byte[] bytes, string label)
    {
        var loc = FindSequence(bytes, label);
        var readLenBytes = bytes.Skip(loc).TakeWhile((b, check) => b != 0x3a).ToArray();
        var readLen = int.Parse(Encoding.ASCII.GetString(readLenBytes));
        return bytes.Skip(loc + readLenBytes.Length + 1).Take(readLen).ToArray();

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

    private static int FindSequence(byte[] bytes, string label)
    {
        var target = Encoding.ASCII.GetBytes(label);
        for (int i = 0; i < bytes.Length; i++)
        {
            var found = bytes.Skip(i)
                .Take(target.Length)
                .SequenceEqual(target);
            if (found)
                return i + target.Length;
        }
        throw new KeyNotFoundException();
    }
}
