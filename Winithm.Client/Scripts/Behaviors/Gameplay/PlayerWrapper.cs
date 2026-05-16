using Godot;

namespace Winithm.Client.Behaviors.Gameplay
{
  public enum GameplayAspectMode { Ratio16_9, Expand }

  public class PlayerWrapper : Control
  {
    public static PlayerWrapper Instance { get; private set; }

    private GameplayAspectMode _aspectMode = GameplayAspectMode.Ratio16_9;
        
    [Export]
    public GameplayAspectMode AspectMode
    {
      get => _aspectMode;
      set => SetAspectMode(value);
    }

    private Control _gameArea;
    private const float BaseWidth = 1280f;
    private const float BaseHeight = 720f;
    private const float AspectRatio = BaseWidth / BaseHeight;

    public override void _Ready()
    {
      Instance = this;
      _gameArea = GetNode<Control>("PlayerArea");
            
      Connect("item_rect_changed", this, nameof(ApplyAspectMode));

      AspectMode = GameplayAspectMode.Ratio16_9;       
    }

    public void SetAspectMode(GameplayAspectMode mode)
    {
      _aspectMode = mode;
      ApplyAspectMode();
    }

    public void ToggleAspectMode()
    {
      SetAspectMode(AspectMode == GameplayAspectMode.Expand
          ? GameplayAspectMode.Ratio16_9
          : GameplayAspectMode.Expand);
    }

    private void ApplyAspectMode()
    {
      if (_gameArea == null || !IsInsideTree()) return;

      Vector2 containerSize = RectSize;

      switch (AspectMode)
      {
        case GameplayAspectMode.Ratio16_9:
          float targetW = containerSize.y * AspectRatio;
          float targetH = containerSize.y;

          if (targetW > containerSize.x)
          {
            targetW = containerSize.x;
            targetH = containerSize.x / AspectRatio;
          }

          _gameArea.RectSize = new Vector2(targetW, targetH);
          _gameArea.RectPosition = (containerSize - _gameArea.RectSize) * 0.5f;
          break;

        case GameplayAspectMode.Expand:
          _gameArea.RectPosition = Vector2.Zero;
          _gameArea.RectSize = containerSize;
          break;
      }

      _gameArea.Update();
    }
  }
}