using System.Web;
using System.Text;

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
        var uri =  new UriBuilder(t.Tracker)
        // var uri = new UriBuilder("http://localhost/app/announce")
        {
            Query = queryParams + $"&info_hash={EncodeInfoHash(t.InfoHash)}"
        };
        return uri.ToString();
    }

    public static (int, List<Peer>) ParseAnnounceResponse(byte[] bytes)
    {
        // using var fs = File.OpenWrite("../last-announce-reponse.dat");
        // fs.Write(bytes);
        var interval = (int)Bencode.ReadInt(bytes, "8:interval");
        Console.WriteLine($"  interval: {interval}");
        var peers = Bencode.ReadBytes(bytes, "5:peers");
        var peerList = peers.Chunk(6).Select(p => new Peer(p)).ToList();
        Console.WriteLine($"  peers ({peerList.Count}): sample {peerList[0]}");
        return (interval, peerList);
    }

    public static byte[] Handshake(byte[] infoHash, string peerId)
    {
                if (infoHash.Length != 20)
            throw new ArgumentException("InfoHash must be 20 bytes long.");
        
        if (peerId.Length != 20)
            throw new ArgumentException("PeerId must be 20 characters long.");

        // handshake: <pstrlen><pstr><reservedx8><info_hash><peer_id> 
        var pstr = "BitTorrent protocol";
        var handshake = new List<byte>();
        handshake.Add((byte)Encoding.ASCII.GetByteCount(pstr));
        handshake.AddRange(Encoding.ASCII.GetBytes(pstr));
        handshake.AddRange(new byte[8]);
        handshake.AddRange(infoHash);
        handshake.AddRange(Encoding.ASCII.GetBytes(peerId));

        return handshake.ToArray();
    }

    private static string EncodeInfoHash(byte[] infoHash)
    {
        return string.Join("", infoHash.Select(x => "%"+x.ToString("X2")));
    }

}
