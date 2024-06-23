using System.Collections;

var outFile = GetArg("out") ?? ".torrent/download";
var torrentFile = GetArg("torrent") ?? ".torrent/example.torrent";

// var tor = new Torrent(torrentFile);
// var client = new Client(tor);
// client.Start();
// await client.DownloadToFile(outFile);
// Test case 1: Normal bitfield message
byte[] bitfieldMessage1 = new byte[] { 0b10101010, 0b01010101, 0b11110000 };
int pieceCount1 = 24;
BitArray bitArray1 = Protocol.ParseBitfield(bitfieldMessage1, pieceCount1);
Console.WriteLine($"Test case 1: {string.Join("", bitArray1.Cast<bool>().Select(b => b ? '1' : '0'))}");

// Test case 2: Bitfield message with trailing zero bits
byte[] bitfieldMessage2 = new byte[] { 0b11000000 };
int pieceCount2 = 6;
BitArray bitArray2 = Protocol.ParseBitfield(bitfieldMessage2, pieceCount2);
Console.WriteLine($"Test case 2: {string.Join("", bitArray2.Cast<bool>().Select(b => b ? '1' : '0'))}");

// Test case 3: Bitfield message with all pieces available
byte[] bitfieldMessage3 = new byte[] { 0b11111111, 0b11111111 };
int pieceCount3 = 16;
BitArray bitArray3 = Protocol.ParseBitfield(bitfieldMessage3, pieceCount3);
Console.WriteLine($"Test case 3: {string.Join("", bitArray3.Cast<bool>().Select(b => b ? '1' : '0'))}");

// Test case 4: Bitfield message with no pieces available
byte[] bitfieldMessage4 = new byte[] { 0b00000000, 0b00000000 };
int pieceCount4 = 16;
BitArray bitArray4 = Protocol.ParseBitfield(bitfieldMessage4, pieceCount4);
Console.WriteLine($"Test case 4: {string.Join("", bitArray4.Cast<bool>().Select(b => b ? '1' : '0'))}");

// Test case 5: Bitfield message with a single piece available
byte[] bitfieldMessage5 = new byte[] { 0b00000011 };
int pieceCount5 = 7;
BitArray bitArray5 = Protocol.ParseBitfield(bitfieldMessage5, pieceCount5);
Console.WriteLine($"Test case 5: {string.Join("", bitArray5.Cast<bool>().Select(b => b ? '1' : '0'))}");

// Test case 6: Bitfield message with some pieces available and some missing
byte[] bitfieldMessage6 = new byte[] { 0b10100101, 0b01001010 };
int pieceCount6 = 16;
BitArray bitArray6 = Protocol.ParseBitfield(bitfieldMessage6, pieceCount6);
Console.WriteLine($"Test case 6: {string.Join("", bitArray6.Cast<bool>().Select(b => b ? '1' : '0'))}");


string? GetArg(string label)
{
    var index = Array.IndexOf(args, $"--{label}");
    if (index == -1)
        return null;

    if (index - 1 == args.Length)
        Bail(label);

    return args[index + 1];
}

void Bail(string label)
{
    Console.WriteLine($"bad args: {label}");
    Environment.Exit(1);
}
