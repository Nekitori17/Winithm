using System;
using System.Collections.Generic;
using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  public class SongMetaData
  {
    public event Action<SongMetaData> OnMetronomeUpdated;
    public event Action<SongMetaData> OnAudioUpdated;
    public event Action<SongMetaData> OnIllustrationUpdated;
    public event Action<SongMetaData> OnUpdated;

    private float _version = 1f;
    public float Version { get => _version; set { if (_version == value) return; _version = value; OnUpdated?.Invoke(this); } }

    public string _id = "prototype.test";
    public string ID { get => _id; set { if (_id == value) return; _id = value; OnUpdated?.Invoke(this); } }

    private string _name = "Unnamed";
    public string Name { get => _name; set { if (_name == value) return; _name = value; OnUpdated?.Invoke(this); } }

    private string _nameAlt = "";
    public string NameAlt { get => _nameAlt; set { if (_nameAlt == value) return; _nameAlt = value; OnUpdated?.Invoke(this); } }

    private string _artist = "Noname";
    public string Artist { get => _artist; set { if (_artist == value) return; _artist = value; OnUpdated?.Invoke(this); } }

    private string _artistAlt = "";
    public string ArtistAlt { get => _artistAlt; set { if (_artistAlt == value) return; _artistAlt = value; OnUpdated?.Invoke(this); } }

    private string _tags = "Genreless";
    public string Tags { get => _tags; set { if (_tags == value) return; _tags = value; OnUpdated?.Invoke(this); } }

    public AudioResource Audio { get; } = new AudioResource();
    public IllustrationResource Illustration { get; } = new IllustrationResource();
    public List<ChartReference> Charts { get; } = new List<ChartReference>();

    public SongMetaData()
    {
      Audio.OnMetronomeUpdated += (a) => OnMetronomeUpdated?.Invoke(this);
      Audio.OnUpdated += (a) => { OnAudioUpdated?.Invoke(this); OnUpdated?.Invoke(this); };
      Illustration.OnUpdated += (i) => { OnIllustrationUpdated?.Invoke(this); OnUpdated?.Invoke(this); };
    }
  }

  public class AudioResource
  {
    public event Action<AudioResource> OnMetronomeUpdated;
    public event Action<AudioResource> OnUpdated;

    public string SongPath = "song.mp3";

    private double _previewStart = 0;
    public double PreviewStart { get => _previewStart; set { if (_previewStart == value) return; _previewStart = value; OnUpdated?.Invoke(this); } }
    private double _previewEnd = 15;
    public double PreviewEnd { get => _previewEnd; set { if (_previewEnd == value) return; _previewEnd = value; OnUpdated?.Invoke(this); } }

    public Metronome Metronome = new Metronome();

    public AudioResource()
    {
      Metronome.OnUpdated += (m) => OnMetronomeUpdated?.Invoke(this);
    }
  }

  public class IllustrationResource
  {
    public event Action<IllustrationResource> OnUpdated;

    public string IllustrationPath = "illustration.png";
    private string _illustrator = "Noname";
    public string Illustrator { get => _illustrator; set { if (_illustrator == value) return; _illustrator = value; OnUpdated?.Invoke(this); } }
    private float _iconCenterX = 0.5f;
    public float IconCenterX { get => _iconCenterX; set { if (_iconCenterX == value) return; _iconCenterX = value; OnUpdated?.Invoke(this); } }
    private float _iconCenterY = 0.5f;
    public float IconCenterY { get => _iconCenterY; set { if (_iconCenterY == value) return; _iconCenterY = value; OnUpdated?.Invoke(this); } }
    private float _iconSize = 1f;
    public float IconSize { get => _iconSize; set { if (_iconSize == value) return; _iconSize = value; OnUpdated?.Invoke(this); } }
  }

  public class ChartReference
  {
    public string ID = "test";
    public int Index = 0;
    public string Name = "Unamed";
    public string Charter = "Noname";
    public string Level = "1";
    public float Constant = 1f;
  }
}