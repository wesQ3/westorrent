using System.Web;
using System.Text;

class Protocol
{
    const string ClientId = "WB";
    const string Version = "0001";
    public static async Task<HttpResponseMessage> Announce(Torrent t)
    {
        Console.WriteLine($"announce to: {t.Tracker}");
        var ua = new HttpClient();
        var uri = AnnounceRequest(t);
        var res = await ua.GetAsync(uri);
        Console.WriteLine($"  announce res: {res.StatusCode}");
        var resContent = await res.Content.ReadAsByteArrayAsync();
        if (!res.IsSuccessStatusCode)
        {
            Console.WriteLine($"  announce res: {Encoding.UTF8.GetString(resContent)}");
            return res;
        }

        ParseAnnounceResponse(resContent);
        return res;
    }

    public static string AnnounceRequest(Torrent t)
    {
        var queryParams = HttpUtility.ParseQueryString("");
        queryParams["peer_id"] = $"-{ClientId}{Version}-f23408e26371"; // should be random
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

    public static void ParseAnnounceResponse(byte[] bytes)
    {
        // using var fs = File.OpenWrite("../last-announce-reponse.dat");
        // fs.Write(bytes);
        var interval = Bencode.ReadInt(bytes, "8:interval");
        Console.WriteLine($"  interval: {interval}");
        var peers = Bencode.ReadBytes(bytes, "5:peers");
        var peerList = peers.Chunk(6).Select(p => new Peer(p)).ToList();
        Console.WriteLine($"  peers ({peerList.Count}): sample {peerList[0]}");

    }

    private static string EncodeInfoHash(byte[] infoHash)
    {
        return string.Join("", infoHash.Select(x => "%"+x.ToString("X2")));
    }

}
