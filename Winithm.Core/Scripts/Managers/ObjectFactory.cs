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
  }
}