using System.Net;
using System.Net.Sockets;

public class Peer
{
    // tracker provided info
    IPAddress Address {get; set;}
    int Port {get; set;}

    // connected peer info
    TcpClient? Conn {get; set;}
    bool? IsChoked {get; set;}
    string? PeerId {get; set;}
    public Peer (byte[] bytes)
    {
        if (bytes.Length != 6)
            throw new Exception("Peer constructor needs 6 bytes");

        Address = new IPAddress(bytes[0..4]);
        Port = (bytes[4] << 8) + bytes[5];
    }

    public async Task Connect(string ourPeerId, byte[] infoHash)
    {
        System.Console.WriteLine($"{ToString()}: init connection");
        Conn = new TcpClient();
        await Conn.ConnectAsync(new IPEndPoint(Address, Port));
        System.Console.WriteLine($"{ToString()}:       connected");
        var handshake = Protocol.Handshake(infoHash, ourPeerId);
        await using NetworkStream stream = Conn.GetStream();
        await stream.WriteAsync(handshake);
        System.Console.WriteLine($"{ToString()}: wrote handshake");
        // var bitfield = await ReadBitfield();
    }

    override public string ToString()
    {
        return $"{Address}:{Port}";
    }
}
