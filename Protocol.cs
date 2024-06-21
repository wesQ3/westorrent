using System.Web;
using System.Text;
using System.Runtime.Serialization;

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

    public static byte[] Handshake(byte[] infoHash, string peerId)
    {
        return new Handshake(infoHash, peerId).Serialize();
    }

    private static string EncodeInfoHash(byte[] infoHash)
    {
        return string.Join("", infoHash.Select(x => "%" + x.ToString("X2")));
    }
}

class Handshake {
    // handshake: <pstrlen><pstr><reservedx8><info_hash><peer_id>
    const string Pstr = "BitTorrent protocol";
    const int ReservedLen = 8;
    public const int Length = 68; // 1 + Pstr.Length + ReservedLen + 20 + 20;
    public string PeerId {get; set;}
    public byte[] InfoHash {get; set;}

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
