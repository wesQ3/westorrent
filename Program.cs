if (args.Contains("--file"))
{
    var index = Array.IndexOf(args, "--file");
    if (index == -1 || index - 1 == args.Length)
        Bail();

    var filename = args[index + 1] ?? "../example.torrent";
    await Read(filename);
}
else
{
    var localFile = "../example.torrent";
    await Read(localFile);
}

void Bail()
{
    Console.WriteLine("bad args");
    Environment.Exit(1);
}

async Task Read(string filename)
{
    var tor = new Torrent(filename);
    await Protocol.Announce(tor);
}

