using System.Text;

public class Torrent
{
    public string Name { get; set; }
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
        var piecesStr = ReadString(bytes, "6:pieces");
        Console.WriteLine($"  pieces:   {piecesStr.Length}");
    }

    private static string ReadString(byte[] bytes, string label)
    {
        var loc = FindSequence(bytes, label);
        var readLenBytes = bytes.Skip(loc).TakeWhile((b, check) => b != 0x3a).ToArray();
        var readLen = int.Parse(Encoding.ASCII.GetString(readLenBytes));
        var sequence = bytes.Skip(loc + readLenBytes.Length + 1).Take(readLen).ToArray();
        return Encoding.ASCII.GetString(sequence);
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
