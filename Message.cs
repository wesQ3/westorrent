public class Message
{
    public enum Id
    {
        Choke,
        Unchoke,
        Interested,
        NotInterested,
        Have,
        Bitfield,
        Request,
        Piece,
        Cancel,
        Port,
    }

    public Id MessageId;
    public byte[] Payload;

    public Message(Id id, byte[] payload)
    {

    }

    public byte[] Serialize()
    {

    }
}
