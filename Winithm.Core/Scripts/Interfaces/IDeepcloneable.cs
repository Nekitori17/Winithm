
using Winithm.Core.Common;

namespace Winithm.Core.Interfaces
{
  public interface IDeepCloneable<T>
  {
    T DeepClone(BeatTime? offset);
  }
}