using System.Collections.Generic;
using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  public struct PropertyDef
  {
    public AnyValueType Type;
    public bool CanStoryboard;

    public PropertyDef(AnyValueType type, bool canStoryboard = true)
    {
      Type = type;
      CanStoryboard = canStoryboard;
    }
  }

  /// <summary>
  /// Defines an object that supports dynamic runtime modifications via Storyboards.
  /// <typeparam name="TKey">The property type (e.g. StoryboardProperty or string)</typeparam>
  /// </summary>
  public interface IStoryboardTarget<TKey>
  {
    /// <summary>
    /// The predefined properties that this object is allowed to be animated or initialized.
    /// Used for validation during parsing and UI generation in Editor.
    /// </summary>
    Dictionary<TKey, PropertyDef> PropertyRegistry { get; }

    /// <summary>
    /// The mapped event sequences per property. Null by default to save memory.
    /// Lists must be sorted by StartBeat.
    /// </summary>
    Dictionary<TKey, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
