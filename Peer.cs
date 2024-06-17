using System.Net;

class Peer
{
    IPAddress Address {get; set;}
    int Port {get; set;}
    public Peer (byte[] bytes)
    {
        if (bytes.Length != 6)
            throw new Exception("Peer constructor needs 6 bytes");

        Address = new IPAddress(bytes[0..4]);
        Port = (bytes[4] << 8) + bytes[5];
    }

    override public string ToString()
    {
        return $"{Address}:{Port}";
    }
}
