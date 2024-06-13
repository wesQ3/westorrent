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
    Console.WriteLine($"read from {filename}");
    var fs = File.OpenRead(filename);
    Console.WriteLine($"  file size {fs.Length}");
    var br = new BinaryReader(fs);
    var magic = br.ReadBytes(1);
    Console.WriteLine($"  magic: {Convert.ToHexString(magic)}");
}

