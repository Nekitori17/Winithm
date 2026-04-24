using System;
using Winithm.Core.Common;
using Winithm.Core.Data;


namespace Winithm.Core.Managers
{
  public class ObjectFactory
  {
    public long NextIDSeed = 0;

    public readonly BeatTime DEFAULT_WINDOW_LIFECYCLE_DURATION = new BeatTime(15, 0, 0);

    public string GenerateUID()
    {
      var (nextSeed, uid) = UniqueIDGenerator.Generate(NextIDSeed);
      NextIDSeed = nextSeed;

      return uid;
    }

    public void SyncMaxIDSeed(string ID)
    {
      if (string.IsNullOrEmpty(ID) || ID.Length != 6) return;

      long seed = UniqueIDGenerator.Decode(ID);
      if (seed <= 0) return;

      NextIDSeed = Math.Max(NextIDSeed, seed);
    }

    public void IncrementNextIDSeed() => NextIDSeed++;

    // ==========================================
    // Factory Methods
    // ==========================================

    public BPMStop CreateBPMStop(BeatTime startBeat, float bpm, int signature) => new BPMStop(startBeat, bpm, signature);

    public OverlayData CreateOverlay() => new OverlayData { ID = GenerateUID() };
    public GroupData CreateGroup() => new GroupData { ID = GenerateUID() };
    public ThemeChannelData CreateThemeChannel() => new ThemeChannelData { ID = GenerateUID() };


    public WindowData CreateWindow(BeatTime startBeat)
    {
      var current = new WindowData
      {
        ID = GenerateUID()
      };

      current.SpeedSteps.AddSpeedStep(CreateSpeedStep(startBeat, 1.0f));
      current.SpeedSteps.AddSpeedStep(
        CreateSpeedStep(startBeat + DEFAULT_WINDOW_LIFECYCLE_DURATION, 1.0f)
      );

      return current;
    }
    public NoteData CreateNote(
      BeatTime startBeat,
      NoteType type = NoteType.Tap,
      double length = 0,
      float x = 0.5f,
      float width = 0.5f,
      int fakeType = 0
    )
      => new NoteData
      {
        ID = GenerateUID(),
        StartBeat = startBeat,
        Type = type,
        Length = length,
        X = x,
        Width = width,
        FakeType = fakeType
      };
      
    public SpeedStepData CreateSpeedStep(BeatTime startBeat, float multiplier) =>
      new SpeedStepData
      {
        ID = GenerateUID(),
        StartBeat = startBeat,
        Multiplier = multiplier
      };

    public EventData CreateStoryboardEvent(BeatTime startBeat) =>
      new EventData
      {
        ID = GenerateUID(),
        StartBeat = startBeat
      };
  }
}