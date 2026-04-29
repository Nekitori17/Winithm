using System;

namespace Winithm.Core.Common
{
  public static class LayerUtils
  {
    public static int ComposeLayerIndex(int layer, int subLayer)
    {
      ClampLayerIndex(ref layer, ref subLayer);

      int digits = subLayer == 0 ? 0 : (int)Math.Floor(Math.Log10(subLayer) + 1);
      int paddedSubLayer = subLayer * (int)Math.Pow(10, 5 - digits);

      return layer * 100000 + paddedSubLayer;
    }

    public static (int layer, int subLayer) DecomposeLayerIndex(int index)
    {
      return (index / 100000, index % 100000);
    }

    public static void ClampLayerIndex(ref int layer, ref int subLayer)
    {
      if (layer < -9999) layer = -9999;
      if (layer > 9999) layer = 9999;
      if (subLayer < 0) subLayer = 0;
      if (subLayer > 99999) subLayer = 99999;
    }
  }
}