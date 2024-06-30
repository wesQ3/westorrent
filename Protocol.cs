using System.Web;
using System.Text;
using System.Buffers.Binary;
using System.Collections;

class Protocol
{
    public static string AnnounceRequest(Torrent t, string peerId)
    {
        var queryParams = HttpUtility.ParseQueryString("");
        queryParams["peer_id"] = peerId;
        queryParams["port"] = "6881";
        queryParams["uploaded"] = "0";
        queryParams["downloaded"] = "0";
        queryParams["left"] = $"{t.Size}";
        queryParams["compact"] = "1";
        queryParams["numwant"] = "10";
        var uri = new UriBuilder(t.Tracker)
        // var uri = new UriBuilder("http://localhost/app/announce")
        {
            Query = queryParams + $"&info_hash={EncodeInfoHash(t.InfoHash)}"
        };
        return uri.ToString();
    }

    private static string EncodeInfoHash(byte[] infoHash)
    {
        return string.Join("", infoHash.Select(x => "%" + x.ToString("X2")));
    }

    public static (int, List<Peer>) ParseAnnounceResponse(byte[] bytes)
    {
        using var fs = File.OpenWrite("../last-announce-reponse.dat");
        fs.Write(bytes);
        var interval = (int)Bencode.ReadInt(bytes, "8:interval");
        Console.WriteLine($"  interval: {interval}");
        var peers = Bencode.ReadBytes(bytes, "5:peers");
        var peerList = peers.Chunk(6).Select(p => new Peer(p)).ToList();
        Console.WriteLine($"  peers: {peerList.Count}");
        return (interval, peerList);
    }

    public static async Task<Message> ReadMessage(Stream stream, CancellationToken cancel)
    {
        var lenbuf = new byte[4];
        await stream.ReadExactlyAsync(lenbuf, cancel);
        var msgLen = BinaryPrimitives.ReadUInt32BigEndian(lenbuf);
        if (msgLen == 0)
            return new Message(); // keepalive

        var msgBuf = new byte[msgLen];
        await stream.ReadExactlyAsync(msgBuf);

        return new Message((Message.Id)msgBuf[0], msgBuf[1..]);

    }

    public static BitArray ParseBitfield(byte[] payload, int count)
    {
        var expectedBytes = (int)Math.Ceiling(count / 8.0);
        if (payload.Length != expectedBytes)
            throw new ArgumentException($"bitfield length {payload.Length} != piece count {count} ({expectedBytes})");

        var bits = new BitArray(count);
        for (int i = 0; i < payload.Length; i++)
        {
            var b = payload[i];
            for (int j = 7; j >= 0; j--)
            {
                var bindex = 7-j + i*8;
                if (bindex > count - 1) break;
                bits[bindex] = (b & (1 << j)) != 0;
            }
        }
        return bits;
    }

    // Piece is just one chunk of a piece actually so copy into a buffer that
    // will eventually hold the whole piece
    public static int ParsePiece(int pieceId, byte[] target, byte[] payload)
    {
        var index = BinaryPrimitives.ReadUInt32BigEndian(payload[0..4]);
        if (pieceId != index)
            return 0; // not the expected piece

        var begin = BinaryPrimitives.ReadUInt32BigEndian(payload[4..8]);
        if (begin > target.Length)
            return 0; // begin outside expected range
        
        var data = payload[8..];
        if (begin + data.Length > target.Length)
            return 0; // data too long for target span

        Array.Copy(data, 0, target, (int)begin, data.Length);
        return data.Length;
    }
}

class Handshake
{
    // handshake: <pstrlen><pstr><reservedx8><info_hash><peer_id>
    const string Pstr = "BitTorrent protocol";
    const int ReservedLen = 8;
    public const int Length = 68; // 1 + Pstr.Length + ReservedLen + 20 + 20;
    public string PeerId { get; set; }
    public byte[] InfoHash { get; set; }

    public Handshake(byte[] infoHash, string peerId)
    {
        if (infoHash.Length != 20)
            throw new ArgumentException("InfoHash must be 20 bytes long.");
        InfoHash = infoHash;

        if (peerId.Length != 20)
            throw new ArgumentException("PeerId must be 20 characters long.");
        PeerId = peerId;
    }

    public byte[] Serialize()
    {
        var l = new List<byte>();
        l.Add((byte)Encoding.ASCII.GetByteCount(Pstr));
        l.AddRange(Encoding.ASCII.GetBytes(Pstr));
        l.AddRange(new byte[ReservedLen]);
        l.AddRange(InfoHash);
        l.AddRange(Encoding.ASCII.GetBytes(PeerId));

        return l.ToArray();
    }

    public Handshake(byte[] data)
    {
        if (data == null || data.Length != Length)
            throw new ArgumentException("bad handshake: wrong length");

        int pstrlen = data[0];
        var pstr = Encoding.ASCII.GetString(data, 1, pstrlen);
        if (pstr != Pstr)
            throw new ArgumentException($"bad protocol: {pstr}");

        InfoHash = new byte[20];
        Array.Copy(data, 1 + pstrlen + ReservedLen, InfoHash, 0, 20);

        PeerId = Encoding.ASCII.GetString(data, 1 + pstrlen + ReservedLen + InfoHash.Length, 20);
    }
}
