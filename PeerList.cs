using System.Net;

public class PeerInfo
{
    public IPAddress Address { get; set; }
    public int Port { get; set; }
    public DateTime LastSeen { get; set; }

    public PeerInfo(byte[] bytes)
    {
        if (bytes.Length != 6)
            throw new Exception("PeerInfo constructor needs 6 bytes");

        Address = new IPAddress(bytes[0..4]);
        Port = (bytes[4] << 8) + bytes[5];
        LastSeen = DateTime.UtcNow;
    }

    public PeerInfo()
    {
    }

    public void Touch() => LastSeen = DateTime.UtcNow;

    override public string ToString()
    {
        return $"{Address}:{Port}";
    }

}

public class PeerList
{
    private readonly Dictionary<string, PeerInfo> _peers;

    public PeerList()
    {
        _peers = [];
    }

    public void Update(PeerInfo info)
    {
        if (_peers.TryGetValue($"{info}", out var existingPeer))
        {
            existingPeer.Touch();
        }
        else
        {
            _peers[$"{info}"] = info;
        }
    }

    public void Remove(string ip, int port) => _peers.Remove($"{ip}:{port}");

    public List<PeerInfo> ToList() => _peers.Values.ToList();
    public PeerInfo[] ToArray() => _peers.Values.ToArray();
    public int Count() => _peers.Count;

    public void MergePeers(IEnumerable<PeerInfo> newPeers)
    {
        foreach (var peer in newPeers)
        {
            Update(peer);
        }
    }

    public PeerInfo[] ExcludeConnected(Peer[] connectedPeers)
    {
        HashSet<string> connected = new(connectedPeers.Select(p => p.ToString()).ToArray());
        return _peers.Where(p => !connected.Contains($"{p.Key}"))
            .Select(pair => pair.Value)
            .ToArray();
    }
}
