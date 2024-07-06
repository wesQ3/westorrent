using System.Text;

public class Client
{
    List<Peer> Peers { get; set; }
    Torrent Torrent { get; set; }
    string PeerId { get; set; }
    private CancellationTokenSource Canceller;
    private List<int> RemainingPieces;
    private Random Rand;

    const string ClientId = "WB";
    const string Version = "0001";
    const int MAX_PEERS_CONNECTED = 10;

    public Client(Torrent tor)
    {
        Peers = [];
        Torrent = tor;
        PeerId = RandomPeerId();
        Canceller = new();
        Rand = new Random();
        var indexes = Enumerable.Range(0, tor.Pieces.Count).ToArray();
        Rand.Shuffle(indexes);
        RemainingPieces = [.. indexes];
        Console.WriteLine($"new client {PeerId}");
    }

    public static string RandomPeerId()
    {
        var rand = new Random();
        var randbytes = new byte[6].Select(b => (byte)rand.Next(256)).ToArray();
        return $"-{ClientId}{Version}-{Convert.ToHexString(randbytes)}";
    }
    public async Task Start()
    {
        var announceTask = AnnounceTimer();
        var dispatchTask = AssignPieces();
        await Task.WhenAll(announceTask, dispatchTask);
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

    public async Task AnnounceTimer()
    {
        var cancel = Canceller.Token;
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(cancel) && !cancel.IsCancellationRequested)
        {
            (var interval, var newPeers) = await Announce();
            timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
            Peers = newPeers;
            // todo merge peers
        }
    }

    public async Task AssignPieces()
    {
        while (RemainingPieces.Count > 0)
        {
            var next = RemainingPieces[0];
            var havers = Peers.Where(p => p.HasPiece(next))
                .Where(p => !p.IsBusy())
                .ToArray();

            if (havers.Length > 0)
            {
                Rand.Shuffle(havers);
                var dlTask = havers[0].GetPiece(next);
                // .then
                //  RemainingPieces.Remove(next);
            }

            await Task.Delay(2*1000);
        }
    }

    public async Task DownloadToFile(string outFile)
    {
        var mainTasks = Start();
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        // while (await timer.WaitForNextTickAsync() && Torrent.HasMissingPieces())
        // {
        //     // start download tasks
        // }
        // assemble file from pieces
    }
}
