using System.Collections;
using System.Net;
using System.Net.Sockets;

public class Peer
{
    // tracker provided info
    IPAddress Address { get; set; }
    int Port { get; set; }

    // connected peer info
    TcpClient? Conn { get; set; }
    bool? IsChoked { get; set; }
    string? PeerId { get; set; }
    BitArray? Pieces{ get; set; }

    public Peer(byte[] bytes)
    {
        if (bytes.Length != 6)
            throw new Exception("Peer constructor needs 6 bytes");

        Address = new IPAddress(bytes[0..4]);
        Port = (bytes[4] << 8) + bytes[5];
    }

    public async Task Connect(string ourPeerId, Torrent torrent)
    {
        // todo timeout?
        Log($"init connection");
        Conn = new TcpClient();
        await Conn.ConnectAsync(new IPEndPoint(Address, Port));
        Log("connected");
        var ourHand = new Handshake(torrent.InfoHash, ourPeerId);
        await using NetworkStream stream = Conn.GetStream();
        await stream.WriteAsync(ourHand.Serialize());
        Log("wrote handshake");

        var readBuffer = new byte[Handshake.Length];
        await stream.ReadExactlyAsync(readBuffer);
        var theirHand = new Handshake(readBuffer);
        var hashMatches = ourHand.InfoHash.SequenceEqual(theirHand.InfoHash);
        if (!hashMatches)
        {
            Log("hash mismatch, disconnecting");
            Conn.Close();
            return;
        }
        Log($"hello {theirHand.PeerId}");
        PeerId = theirHand.PeerId;

        var bitfieldMsg = await Protocol.ReadMessage(stream);
        if (bitfieldMsg.MessageId == Message.Id.Bitfield && bitfieldMsg.Payload != null)
        {
            Log($"recieved bitfield");
            Log(Convert.ToHexString(bitfieldMsg.Payload));
            Pieces = Protocol.ParseBitfield(bitfieldMsg.Payload, torrent.Pieces.Count);
        }
    }

    override public string ToString()
    {
        return $"{Address}:{Port}";
    }

    private void Log(string message)
    {
        Console.WriteLine($"{PeerId ?? ToString()}:{message,25}");
    }
}
