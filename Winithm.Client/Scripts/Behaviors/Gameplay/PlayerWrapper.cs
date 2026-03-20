using Godot;

namespace Winithm.Client.Behaviors.Gameplay
{
  public enum GameplayAspectMode
  {
    Ratio16_9,
    Expand
  }

  public class PlayerWrapper : Control
  {
    [Export] public GameplayAspectMode AspectMode { get; set; } = GameplayAspectMode.Ratio16_9;

    private Control _gameArea;

    private const float BaseWidth = 1280f;
    private const float BaseHeight = 720f;

    public override void _Ready()
    {
      _gameArea = GetNode<Control>("PlayerArea");
      ApplyAspectMode();
      GetTree().Root.Connect("size_changed", this, nameof(OnWindowResize));
    }

    public void OnWindowResize()
    {
      ApplyAspectMode();
    }

    public void SetAspectMode(GameplayAspectMode mode)
    {
      AspectMode = mode;
      ApplyAspectMode();
    }

    private void ApplyAspectMode()
    {
      if (_gameArea == null) return;

      float containerW = RectSize.x;
      float containerH = RectSize.y;

      switch (AspectMode)
      {
        case GameplayAspectMode.Ratio16_9:
          {
            float targetW = containerH * (BaseWidth / BaseHeight);
            float targetH = containerH;

            if (targetW > containerW)
            {
              targetW = containerW;
              targetH = containerW * (BaseHeight / BaseWidth);
            }

            _gameArea.RectPosition = new Vector2(
              (containerW - targetW) / 2f,
              (containerH - targetH) / 2f
            );
            _gameArea.RectSize = new Vector2(targetW, targetH);
            break;
          }

        case GameplayAspectMode.Expand:
          _gameArea.RectPosition = Vector2.Zero;
          _gameArea.RectSize = new Vector2(containerW, containerH);
          break;
      }

      // Trigger GameArea redraw border debug
      _gameArea.Update();
    }
  }
}