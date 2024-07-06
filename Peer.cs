using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

public class Peer
{
    // tracker provided info
    IPAddress Address { get; set; }
    int Port { get; set; }
    Torrent? TorrentInfo { get; set; }

    // connected peer info
    TcpClient? Conn { get; set; }
    private Stream? Stream { get; set; }
    private CancellationTokenSource Canceller;
    bool? IsChokingUs { get; set; }
    bool? IsInterestingToUs { get; set; }
    string? PeerId { get; set; }
    BitArray? Pieces { get; set; }

    int CurrentPieceId;
    byte[] CurrentPieceBytes;
    int OpenRequests;
    int OpenBytesRequested;
    int CurrentBytesDownloaded;

    const int MAX_BLOCK_SIZE = 16384;
    const int MAX_OPEN_REQUESTS = 5;

    public Peer(byte[] bytes)
    {
        if (bytes.Length != 6)
            throw new Exception("Peer constructor needs 6 bytes");

        Address = new IPAddress(bytes[0..4]);
        Port = (bytes[4] << 8) + bytes[5];
        Canceller = new();
    }

    public Peer(string ip, int port)
    {
        Address = IPAddress.Parse(ip);
        Port = port;
        Canceller = new();
    }

    public async Task StartConnection(string ourPeerId, Torrent torrent)
    {
        TorrentInfo = torrent;
        CurrentPieceBytes = new byte[torrent.PieceLength];
        await Connect(ourPeerId);
        var receiveTask = ReceiveMessages(Canceller.Token);
        var keepAliveTask = SendKeepAlives(Canceller.Token);
        // NOTE test message send
        await Task.Delay(TimeSpan.FromSeconds(2));
        IsInterestingToUs = true;
        await SendMessage(new Message(Message.Id.Interested, []), Canceller.Token);

        await Task.WhenAll(receiveTask, keepAliveTask);
    }

    private async Task ReceiveMessages(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            await ReceiveMessage(cancel);
        }
    }

    private async Task ReceiveMessage(CancellationToken cancel)
    {
        var nextMsg = await Protocol.ReadMessage(Stream, cancel);
        await HandleMessage(nextMsg);

    }

    private async Task HandleMessage(Message msg)
    {
        Log($"< {msg}");
        switch (msg.MessageId)
        {
            case null:
                break; // keep-alive
            case Message.Id.Choke:
                IsChokingUs = true;
                break;
            case Message.Id.Unchoke:
                IsChokingUs = false;
                // NOTE testing Request/Piece
                if (IsInterestingToUs ?? false)
                {
                    await GetPiece(0);
                    await GetPiece(TorrentInfo.Pieces.Count - 1);
                }
                break;
            case Message.Id.Bitfield:
                Pieces = Protocol.ParseBitfield(msg.Payload, TorrentInfo.Pieces.Count);
                break;
            case Message.Id.Piece:
                var written = Protocol.ParsePiece(CurrentPieceId, CurrentPieceBytes, msg.Payload);
                // Log(Convert.ToHexString(CurrentPiece));
                CurrentBytesDownloaded += written;
                OpenRequests--;
                // Log($"wrote {written} - {CurrentBytesDownloaded}");
                break;
            case Message.Id.Interested:
            case Message.Id.NotInterested:
            case Message.Id.Port:
                break; // ignore
            default:
                Log($"!! Unhandled: {msg.MessageId}");
                break;
        }
    }

    public async Task<byte[]> GetPiece(int pieceId)
    {
        await DownloadPiece(pieceId, Canceller.Token);
        VerifyPiece(pieceId);
        await SendMessage(Message.Have(pieceId), Canceller.Token);
        var completePiece = CurrentPieceBytes;
        CurrentPieceId = -1; // clear current work
        return completePiece;
    }

    private async Task DownloadPiece(int pieceId, CancellationToken cancel)
    {
        var pieceSize = TorrentInfo.PieceSize(pieceId);
        CurrentPieceId = pieceId;
        CurrentPieceBytes = new byte[pieceSize];
        CurrentBytesDownloaded = 0;
        OpenRequests = 0;
        OpenBytesRequested = 0;
        
        while (CurrentBytesDownloaded < pieceSize)
        {
            if (!IsChokingUs ?? false)
            {
                // pipeline multiple open reqs
                while (OpenRequests < MAX_OPEN_REQUESTS && OpenBytesRequested < pieceSize)
                {
                    // last request can be smaller than MAX_BLOCK
                    int remainingBytes = pieceSize - OpenBytesRequested;
                    int requestSize = remainingBytes < MAX_BLOCK_SIZE ? remainingBytes : MAX_BLOCK_SIZE;

                    await SendMessage(Message.Request(pieceId, OpenBytesRequested, requestSize), cancel);
                    OpenRequests++;
                    OpenBytesRequested += requestSize;
                }
            }
            await ReceiveMessage(cancel);
        }
    }

    private bool VerifyPiece(int targetPiece)
    {
        var sha = SHA1.HashData(CurrentPieceBytes);
        if (TorrentInfo.Pieces[targetPiece].SequenceEqual(sha))
        {
            Log($"Piece {targetPiece} hash ok!");
            return true;
        }
        else
        {
            Log($"Piece download failed; hash mismatch");
            Log($"Target: {Convert.ToHexString(TorrentInfo.Pieces[targetPiece])}");
            Log($"Dl:     {Convert.ToHexString(sha)}");
            return false;
        }

    }

    private async Task SendMessages(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            // var nextMsg = await SendMessage(msg, cancel);
            // await HandleMessage(nextMsg);
        }
    }

    private async Task SendKeepAlives(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), cancel);

            var keepAliveMessage = new Message();
            await SendMessage(keepAliveMessage, cancel);
        }
    }

    public async Task SendMessage(Message msg, CancellationToken cancel)
    {
        var bytes = msg.Serialize();
        await Stream.WriteAsync(bytes, 0, bytes.Length, cancel);
        Log($"> {msg}");
    }

    public void Stop()
    {
        Canceller.Cancel();
        Stream.Close();
        Conn.Close();
    }

    public async Task Connect(string ourPeerId)
    {
        try
        {
            // todo timeout?
            Log($"init connection");
            Conn = new TcpClient();
            await Conn.ConnectAsync(new IPEndPoint(Address, Port));
            Log("connected");
            var ourHand = new Handshake(TorrentInfo.InfoHash, ourPeerId);
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

    public bool HasPiece(int pieceId)
    {
        if (Pieces == null)
            return false;
        return Pieces[pieceId];
    }

    public bool IsBusy() => CurrentPieceId >= 0;

    override public string ToString()
    {
        return $"{Address}:{Port}";
    }

    private void Log(string message)
    {
        Console.WriteLine($"{PeerId ?? ToString(),20}:{message,30}");
    }
}
