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
    public event Action<NoteData> OnInvalidate;
    /// <summary>Fired on any non-structural property change.</summary>
    public event Action<NoteData> OnUpdated;

    public string ID;

    private NoteType _type = NoteType.Tap;
    public NoteType Type { get => _type; set { if (_type == value) return; _type = value; OnInvalidate?.Invoke(this); } }

    private BeatTime _startBeat = BeatTime.NaN;
    public BeatTime StartBeat { get => _startBeat; set { if (_startBeat == value) return; _startBeat = value; OnStartBeatChanged?.Invoke(this); } }

    private double _length = 0;
    public double Length { get => _length; set { if (_length == value) return; _length = value; OnInvalidate?.Invoke(this); } }

    private float _x = 0;
    public float X { get => _x; set { if (_x == value) return; _x = value; OnUpdated?.Invoke(this); } }

    private float _width = 1;
    public float Width { get => _width; set { if (_width == value) return; _width = value; OnUpdated?.Invoke(this); } }

    private int _fakeType = 0;
    public int FakeType { get => _fakeType; set { if (_fakeType == value) return; _fakeType = value; OnInvalidate?.Invoke(this); } }

    private ResourcePack? _resourcePack;
    public ResourcePack? ResourcePack { get => _resourcePack; set { if (_resourcePack.Equals(value)) return; _resourcePack = value; OnUpdated?.Invoke(this); } }

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

    public NoteData DeepClone(ObjectFactory objectFactory, BeatTime? offset)
    {
      return new NoteData()
      {
        ID = objectFactory.GenerateUID(),
        Type = Type,
        StartBeat = StartBeat + (offset ?? BeatTime.Zero),
        Length = Length,
        X = X,
        Width = Width,
        FakeType = FakeType,
        ResourcePack = ResourcePack
      };
    }
  }
}
