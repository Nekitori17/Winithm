using System;
using Winithm.Core.Common;
using Winithm.Core.Managers;

using Winithm.Core.Interfaces;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Hit object data: # <NoteType> <Start> <Length> <Side> <FakeType>
  /// </summary>
  public enum NoteType
  {
    Tap,
    Hold,
    Drag,
    Focus,
    Close
  }

  public enum NoteSide
  {
    Top,
    Bottom,
    Left,
    Right
  }

  public class NoteData : IDeepCloneable<NoteData>
  {
    /// <summary>Fired when StartBeat changes; Manager uses this to re-sort.</summary>
    public event Action<NoteData> OnStartBeatChanged;
    /// <summary>Fired when Side changes; Manager uses this to migrate lanes.</summary>
    public event Action<NoteData> OnSideChanged;
    /// <summary>Fired on any non-structural property change.</summary>
    public event Action<NoteData> OnDataChanged;

    public string ID;

    private NoteType _type = NoteType.Tap;
    public NoteType Type { get => _type; set { if (_type == value) return; _type = value; OnDataChanged?.Invoke(this); } }

    private BeatTime _startBeat = BeatTime.NaN;
    public BeatTime StartBeat { get => _startBeat; set { if (_startBeat == value) return; _startBeat = value; OnStartBeatChanged?.Invoke(this); } }

    private double _length = 0;
    public double Length { get => _length; set { if (_length == value) return; _length = value; OnDataChanged?.Invoke(this); } }

    private float _x = 0;
    public float X { get => _x; set { if (_x == value) return; _x = value; OnDataChanged?.Invoke(this); } }

    private float _width = 1;
    public float Width { get => _width; set { if (_width == value) return; _width = value; OnDataChanged?.Invoke(this); } }

    private NoteSide _side = NoteSide.Bottom;
    public NoteSide Side { get => _side; set { if (_side == value) return; _side = value; OnSideChanged?.Invoke(this); } }

    private int _fakeType = 0;
    public int FakeType { get => _fakeType; set { if (_fakeType == value) return; _fakeType = value; OnDataChanged?.Invoke(this); } }

    public bool IsHittable => FakeType == 0;
    public bool IsMutedGhost => FakeType == 1;
    public bool IsLoudGhost => FakeType == 2;

    // --- State for all Notes ---
    /// <summary>True if the note has been fully processed (Hit or Miss).</summary>
    public bool IsEvaluated = false;

    /// <summary>Session token for auto-fired notes.</summary>
    public int AutoFiredSessionToken = -1;

    public int LastSeenFrameSessionToken = -1;

    // --- State for Hold Notes ---
    /// <summary>Hold Phase 1: Key pressed at StartBeat, waiting for Phase 2 at EndBeat.</summary>
    public bool IsHoldActive = false;

    /// <summary>Timing offset (ms) captured during Phase 1 for Phase 2 scoring.</summary>
    public float HoldStartOffsetMs = float.NaN;

    // Resource Pack
    public ResourcePack? ResourcePack;

    public NoteData DeepClone(BeatTime? offset)
    {
      return new NoteData()
      {
        ID = ID,
        Type = Type,
        StartBeat = StartBeat + (offset ?? BeatTime.Zero),
        Length = Length,
        X = X,
        Width = Width,
        Side = Side,
        FakeType = FakeType,
        ResourcePack = ResourcePack
      };
    }

    public static NoteData Parse(string text)
    {
      var current = new NoteData();

      string[] parts = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length >= 1) current.ID = parts[0];
      if (parts.Length >= 2) current.Type = 
        Enum.TryParse<NoteType>(parts[1], true, out var t) ? t : NoteType.Tap;
      if (parts.Length >= 3) current.StartBeat =
        BeatTime.TryParse(parts[2], out var sb) ? sb : BeatTime.Zero;
      if (parts.Length >= 4) current.Length =
        ParserUtils.TryParseDouble(parts[3], out double l) ? l : 0.0;
      if (parts.Length >= 5) current.X =
        ParserUtils.TryParseFloat(parts[4], out float x) ? x : 0.0f;
      if (parts.Length >= 6) current.Width =
        ParserUtils.TryParseFloat(parts[5], out float w) ? w : 1.0f;
      if (parts.Length >= 7) current.Side =
        Enum.TryParse<NoteSide>(parts[6], true, out var s) ? s : NoteSide.Bottom;
      if (parts.Length >= 8) current.FakeType =
        int.TryParse(parts[7], out int f) ? f : 0;

      return current;
    }

    public override string ToString() 
      => $"{ID} {Type} {StartBeat} {Length} {Side} {FakeType}";
  }
}
