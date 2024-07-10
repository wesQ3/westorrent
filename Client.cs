using System.Text;

public class Client
{
    List<Peer> ConnectedPeers { get; set; }
    Torrent Torrent { get; set; }
    string PeerId { get; set; }
    private CancellationTokenSource Canceller;
    private List<int> RemainingPieces;
    private Random Rand;
    private List<Task> InFlight;
    private PeerList KnownPeers;
    private string OutFileName;

    const string ClientId = "WB";
    const string Version = "1000";
    const int MAX_PEERS_CONNECTED = 30;

    public Client(Torrent tor)
    {
        ConnectedPeers = [];
        KnownPeers = new();
        InFlight = [];
        Torrent = tor;
        PeerId = RandomPeerId();
        Canceller = new();
        Rand = new Random();
        var indexes = Enumerable.Range(0, tor.Pieces.Count).ToArray();
        Rand.Shuffle(indexes);
        RemainingPieces = [.. indexes];
        Log($"new client {PeerId}");
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
        var cleanTask = CleanTaskList();
        var connectTask = ConnectPeers();
        await Task.WhenAll(announceTask, cleanTask, connectTask);
    }

    public async Task ConnectPeers()
    {
        var cancel = Canceller.Token;
        while (!cancel.IsCancellationRequested)
        {
            var openSlots = MAX_PEERS_CONNECTED - ConnectedPeers.Count;
            var available = KnownPeers.ExcludeConnected(ConnectedPeers.ToArray());
            if (openSlots > 0 && available.Length > 0)
            {
                Log($"connect more peers {ConnectedPeers.Count}/{MAX_PEERS_CONNECTED} ({KnownPeers.Count()})");
                Rand.Shuffle(available);
                for (var i = 0; i < openSlots && i < available.Length; i++)
                {
                    Peer newPeer = new(available[i]);
                    ConnectedPeers.Add(newPeer);
                    newPeer.StartConnection(PeerId, Torrent);
                }
            }
            await Task.Delay(2*1000);
        }
    }

    public async Task<(int, List<PeerInfo>)> Announce()
    {
        Log($"announce to: {Torrent.Tracker}");
        var ua = new HttpClient();
        var uri = Protocol.AnnounceRequest(Torrent, PeerId);
        var res = await ua.GetAsync(uri);
        Log($"  announce res: {res.StatusCode}");
        var resContent = await res.Content.ReadAsByteArrayAsync();
        if (!res.IsSuccessStatusCode)
        {
            Log($"  announce res: {Encoding.UTF8.GetString(resContent)}");
            throw new Exception("announce response error");
        }

        return Protocol.ParseAnnounceResponse(resContent);
    }

    public async Task AnnounceTimer()
    {
        var cancel = Canceller.Token;
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cancel) && !cancel.IsCancellationRequested)
        {
            (var interval, var newPeers) = await Announce();
            timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
            KnownPeers.MergePeers(newPeers);
        }
    }

    public async Task CleanTaskList()
    {
        var cancel = Canceller.Token;
        while (!cancel.IsCancellationRequested)
        {
            if (InFlight.Count > 0)
            {
                var task = await Task.WhenAny(InFlight);
                InFlight.Remove(task);
                Log($"request complete; open requests: {InFlight.Count}");
            }
            else
                await Task.Delay(2000);
        }
    }

    public async Task AssignPieces()
    {
        while (RemainingPieces.Count > 0)
        {
            var next = RemainingPieces[0];
            var havers = ConnectedPeers.Where(p => p.HasPiece(next))
                .Where(p => !p.IsBusy())
                .ToArray();

            if (havers.Length > 0 && InFlight.Count < MAX_PEERS_CONNECTED)
            {
                Log($"remaining pieces: {RemainingPieces.Count}");
                Rand.Shuffle(havers);
                await havers[0].SetInterested();
                var dlTask = GetPiece(havers[0], next);
                InFlight.Add(dlTask);
            }

            await Task.Delay(1 * 1000);
        }
    }

    public async Task GetPiece(Peer peer, int pieceId)
    {
        Log($"assign   {peer.PeerId}: {pieceId}");
        byte[] piece = await peer.GetPiece(pieceId);
        await SavePiece(pieceId, piece);
        Log($"complete {peer}: {pieceId}");
        RemainingPieces.Remove(pieceId);
    }

    public async Task SavePiece(int pieceId, byte[] data)
    {
        using var fs = new FileStream(OutFileName, FileMode.Open, FileAccess.Write);
        (var begin, _) = Torrent.PieceBounds(pieceId);
        fs.Seek(begin, SeekOrigin.Begin);
        await fs.WriteAsync(data);
    }

    public void InitializeOutFile()
    {
        using var fs = new FileStream(OutFileName, FileMode.Create, FileAccess.Write);
        fs.SetLength(Torrent.Size);
    }

    public async Task DownloadToFile(string outFile)
    {
        OutFileName = outFile;
        InitializeOutFile();
        var mainTasks = Start();
        await AssignPieces();
        Log("download complete");
    }

    private void Log(string message)
    {
        Console.WriteLine($"{message}");
    }
}
