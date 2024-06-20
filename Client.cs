using System.Text;

public class Client
{
    List<Peer> Peers {get; set;}
    Torrent Torrent {get; set;}
    string PeerId {get; set;}

    const string ClientId = "WB";
    const string Version = "0001";
    const int MAX_PEERS_CONNECTED = 10;

    public Client (Torrent tor)
    {
        Peers = [];
        Torrent = tor;
        PeerId = RandomPeerId();
        Console.WriteLine($"new client {PeerId}");
    }

    private string RandomPeerId()
    {
        var rand = new Random();
        var randbytes = new byte[6].Select(b => (byte)rand.Next(256)).ToArray();
        return $"-{ClientId}{Version}-{Convert.ToHexString(randbytes)}";
    }
    public void Start()
    {
        SetupAnnounceTimer();
    }

    public async Task<(int, List<Peer>)> Announce()
    {
        Console.WriteLine($"announce to: {Torrent.Tracker}");
        var ua = new HttpClient();
        var uri = Protocol.AnnounceRequest(Torrent, PeerId);
        var res = await ua.GetAsync(uri);
        Console.WriteLine($"  announce res: {res.StatusCode}");
        var resContent = await res.Content.ReadAsByteArrayAsync();
        if (!res.IsSuccessStatusCode)
        {
            Console.WriteLine($"  announce res: {Encoding.UTF8.GetString(resContent)}");
            throw new Exception("announce response error");
        }

        return Protocol.ParseAnnounceResponse(resContent);
    }

    public async void SetupAnnounceTimer()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync())
        {
            (var interval, var newPeers) = await Announce();
            timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
            Peers = newPeers;
            // todo merge peers
        }
    }

    public async Task DownloadToFile(string outFile)
    {
        var hs = new Handshake(Torrent.InfoHash, PeerId);
        var hs2 = new Handshake(hs.Serialize());
        var ok = hs.Serialize() == hs2.Serialize();
        System.Console.WriteLine($"{Convert.ToHexString(hs.Serialize())}");
        System.Console.WriteLine($"{Convert.ToHexString(hs2.Serialize())}");
        System.Console.WriteLine($"round trip handshake check: {ok}");
        // while (Torrent.HasMissingPieces())
        // {
        //     // start download tasks
        // }
    }
}
