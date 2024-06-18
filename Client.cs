using System.Text;

public class Client
{
    List<Peer> Peers {get; set;}
    Torrent Torrent {get; set;}

    public Client (Torrent tor)
    {
        Peers = [];
        Torrent = tor;
    }

    public async Task<bool> Start()
    {
        SetupAnnounceTimer();
        return true;
    }

    public async Task<(int, List<Peer>)> Announce()
    {
        Console.WriteLine($"announce to: {Torrent.Tracker}");
        var ua = new HttpClient();
        var uri = Protocol.AnnounceRequest(Torrent);
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
}
