using System;
using Winithm.Core.Common;


namespace Winithm.Core.Managers
{
  public class ObjectFactory
  {
    public long NextIDSeed = 0;

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

    public Data.WindowData CreateWindowData() => new Data.WindowData { ID = GenerateUID() };
    public Data.NoteData CreateNoteData() => new Data.NoteData { ID = GenerateUID() };
    public Data.SpeedStepData CreateSpeedStepData() => new Data.SpeedStepData { ID = GenerateUID() };
    public Data.ThemeChannelData CreateThemeChannelData() => new Data.ThemeChannelData { ID = GenerateUID() };
    public Data.GroupData CreateGroupData() => new Data.GroupData { ID = GenerateUID() };
    public Data.OverlayData CreateOverlayData() => new Data.OverlayData { ID = GenerateUID() };
    public Data.EventData CreateEventData() => new Data.EventData { ID = GenerateUID() };
  }
}