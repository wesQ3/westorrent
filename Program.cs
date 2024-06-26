var outFile = GetArg("out") ?? ".torrent/download";
var torrentFile = GetArg("torrent") ?? ".torrent/example.torrent";

var tor = new Torrent(torrentFile);
// var client = new Client(tor);
// await client.DownloadToFile(outFile);
var localQBT = new Peer("127.0.0.1", 33854);
await localQBT.StartDownload(Client.RandomPeerId(), tor);

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
