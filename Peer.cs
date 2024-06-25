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
    private Stream Stream { get; set; }
    bool? IsChoked { get; set; }
    string? PeerId { get; set; }
    BitArray? Pieces { get; set; }

    public Peer(byte[] bytes)
    {
        if (bytes.Length != 6)
            throw new Exception("Peer constructor needs 6 bytes");

        Address = new IPAddress(bytes[0..4]);
        Port = (bytes[4] << 8) + bytes[5];
    }

    public Peer(string ip, int port)
    {
        Address = IPAddress.Parse(ip);
        Port = port;
    }
    public async Task Start(string ourPeerId, Torrent torrent)
    {
        if (Conn == null)
        {
            Log($"init connection");
            Conn = new TcpClient();
            await Conn.ConnectAsync(new IPEndPoint(Address, Port));
            Log("connected");
            await Connect(ourPeerId, torrent);
        }

        while (Stream.CanRead)
        {
            var nextMsg = await Protocol.ReadMessage(Stream);
            if (nextMsg.MessageId == Message.Id.Bitfield && nextMsg.Payload != null)
            {
                Log($"recieved bitfield");
                Log(Convert.ToHexString(nextMsg.Payload));
                Pieces = Protocol.ParseBitfield(nextMsg.Payload, torrent.Pieces.Count);
            }
            else
            {
                Log($"other message: {nextMsg.MessageId}");
            }
        }
    }

    public async Task Connect(string ourPeerId, Torrent torrent)
    {
        try
        {
            // todo timeout?
            var ourHand = new Handshake(torrent.InfoHash, ourPeerId);
            Stream = Conn.GetStream();
            await Stream.WriteAsync(ourHand.Serialize());
            Log("wrote handshake");

            var readBuffer = new byte[Handshake.Length];
            await Stream.ReadAsync(readBuffer);
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
        }
        catch
        {
            Log("something wrong with the connection");
            Conn?.Close();
            return;

        }
    }

    public bool IsReady() => PeerId != null;

    override public string ToString()
    {
        return $"{Address}:{Port}";
    }

    private void Log(string message)
    {
        Console.WriteLine($"{PeerId ?? ToString()}:{message,25}");
    }
}
