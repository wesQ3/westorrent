public class Torrent
{
   public string Name { get; set; }
   public long Size { get; set; }
   public string Tracker { get; set; }
   public string Filename { get; set; }
   public List<byte[]> Pieces { get; set; } // List of pieces

}
