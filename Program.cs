if (args.Contains("--file"))
{
    var index = Array.IndexOf(args, "--file");
    if (index == -1 || index - 1 == args.Length)
        Bail();

    var filename = args[index + 1] ?? "../example.torrent";
    Read(filename);
}
var localFile = "../example.torrent";
Read(localFile);

void Bail()
{
    Console.WriteLine("bad args");
    Environment.Exit(1);
}

void Read(string filename)
{
    var tor = new Torrent(filename);
}

