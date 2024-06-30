using System.Text;

public class Bencode
{
    public static byte[] ReadBytes(byte[] bytes, string label)
    {
        var loc = FindSequence(bytes, label);
        var (readStart, readLen) = ReadLength(bytes, loc);
        return bytes.Skip(readStart).Take(readLen).ToArray();

    }

    public static string ReadString(byte[] bytes, string label)
    {
        return Encoding.ASCII.GetString(ReadBytes(bytes, label));
    }

    public static int ReadInt(byte[] bytes, string label)
    {
        var loc = FindSequence(bytes, label);
        // i262144e
        var intBytes = bytes.Skip(loc + 1).TakeWhile((b, check) => b != 0x65).ToArray();
        return int.Parse(Encoding.ASCII.GetString(intBytes));
    }

    public static int FindSequence(byte[] bytes, string label)
    {
        return FindSequence(bytes, label, 0);
    }

    public static int FindSequence(byte[] bytes, string label, int start)
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

    public static (int, int) ReadLength(byte[] bytes, int start)
    {
        var readLenBytes = bytes.Skip(start).TakeWhile((b, check) => b != 0x3a).ToArray();
        var readLen = int.Parse(Encoding.ASCII.GetString(readLenBytes));
        return (start + readLenBytes.Length + 1, readLen);
    }
}
