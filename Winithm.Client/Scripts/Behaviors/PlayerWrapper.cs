using Godot;
using System;

namespace Winithm.Client.Behaviors
{
  public enum GameplayAspectMode
  {
    Ratio16_9,
    Expand
  }

  /// <summary>
  /// Controls how the gameplay viewport is presented within the outer OS window.
  /// Ratio16_9: Forces the game to stay in a 16:9 box (adds letterbox/pillarbox).
  /// Expand: The game viewport expands to fill the entire OS window area (useful for arbitrary resolutions).
  /// </summary>
  public class PlayerWrapper : Control
  {
    [Export] public GameplayAspectMode AspectMode { get; set; } = GameplayAspectMode.Expand;

    private AspectRatioContainer _aspectRatioContainer;
    private ViewportContainer _viewportContainer;

    public override void _Ready()
    {
      _aspectRatioContainer = GetNode<AspectRatioContainer>("AspectRatioContainer");
      _viewportContainer = GetNode<ViewportContainer>("AspectRatioContainer/ViewportContainer");

      SetAspectMode(AspectMode);
    }

    public void SetAspectMode(GameplayAspectMode mode)
    {
      if (_aspectRatioContainer == null || _viewportContainer == null) return;

      switch (mode)
      {
        case GameplayAspectMode.Ratio16_9:
          // Constrain the game to exactly 16:9. Black bars or transparency outside based on OS settings.
          _aspectRatioContainer.Ratio = 1280f / 720f;
          _aspectRatioContainer.StretchMode = AspectRatioContainer.StretchModeEnum.Fit;

          // Reset ViewportContainer anchors so AspectRatioContainer controls it
          _viewportContainer.AnchorBottom = 0;
          _viewportContainer.AnchorRight = 0;
          _viewportContainer.AnchorTop = 0;
          _viewportContainer.AnchorLeft = 0;
          break;

        case GameplayAspectMode.Expand:
          // Maintain 16:9 internal resolution even when expanding to fill the screen
          _aspectRatioContainer.Ratio = 1280f / 720f;
          _aspectRatioContainer.StretchMode = AspectRatioContainer.StretchModeEnum.Fit;

          // Force the ViewportContainer to take full control
          _viewportContainer.AnchorBottom = 1;
          _viewportContainer.AnchorRight = 1;
          _viewportContainer.AnchorTop = 0;
          _viewportContainer.AnchorLeft = 0;
          _viewportContainer.MarginBottom = 0;
          _viewportContainer.MarginTop = 0;
          _viewportContainer.MarginRight = 0;
          _viewportContainer.MarginLeft = 0;
          break;
      }
    }
  }
}
