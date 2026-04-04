using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Interfaces
{
  /// <summary>
  /// Defines an object that supports dynamic runtime modifications via Storyboards.
  /// <typeparam name="TKey">The property type (e.g. StoryboardProperty or string)</typeparam>
  /// </summary>
  public interface IStoryboardTarget<TKey>
  {

    /// <summary>
    /// The mapped event sequences per property. Null by default to save memory.
    /// Lists must be sorted by StartBeat.
    /// </summary>
    Dictionary<TKey, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
