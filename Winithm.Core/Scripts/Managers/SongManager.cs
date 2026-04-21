namespace Winithm.Core.Managers
{
  public class SongManager
  {
    public float Version = 1f;

    public string ID = "prototype.test";
    public string Name = "Unnamed";
    public string? NameAlt;
    public string Artist = "Noname";
    public string ArtistAlt = "";
    public string Tags = "Genreless";
  }

  public class SongResource
  {
    public string SongPath = "song.mp3";
    public double PreviewStart = 0;
    public double PreviewEnd = 15;

    public Metronome Metronome = new Metronome();
  }
}