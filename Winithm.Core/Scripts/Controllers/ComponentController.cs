using Godot;
using Winithm.Core.Behaviors.ScoreUI;
using Winithm.Core.Managers;
using Winithm.Core.Constants;
using Winithm.Core.Data;
using Winithm.Core.Common;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class ComponentController : Control
  {
    private Metronome _metronome;
    private ComponentManager _componentManager;
    private SongMetaData _songMetaData;
    private ChartMetadata _chartMetaData;

    private struct LastState
    {
      public Vector2 ScreenSize;
      public Color TextColor, TextOutLineColor;
    }

    [Export] public Vector2 ScreenSize = Visual.DESIGN_RESOLUTION;
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;

    private LastState _lastState;

    private DynamicFont _font30;
    private DynamicFont _font20;
    private DynamicFont _font35;
    private DynamicFont _font25;

    private Control _songInfoTransform;
    private SongInfo _songInfo;
    private Control _chartInfoTransform;
    private ChartInfo _chartInfo;
    private Control _playerComboTransform;
    private PlayerCombo _playerCombo;
    private Control _playerScoreTransform;
    private Control _playerScore;

    private double _lastUpdateBeat;

    public void Initialize(
      ComponentManager manager, Metronome metronome, SongMetaData songMeta, ChartMetadata chartMeta
    )
    {
      _componentManager = manager;
      _metronome = metronome;
      _songMetaData = songMeta;
      _chartMetaData = chartMeta;

      _songInfoTransform = GetNodeOrNull<Control>("SongInfoTransform");
      _songInfo = _songInfoTransform?.GetNodeOrNull<SongInfo>("SongInfo");

      _chartInfoTransform = GetNodeOrNull<Control>("ChartInfoTransform");
      _chartInfo = _chartInfoTransform?.GetNodeOrNull<ChartInfo>("ChartInfo");

      _playerComboTransform = GetNodeOrNull<Control>("PlayerComboTransform");
      _playerCombo = _playerComboTransform?.GetNodeOrNull<PlayerCombo>("PlayerCombo");

      _playerScoreTransform = GetNodeOrNull<Control>("PlayerScoreTransform");
      _playerScore = _playerScoreTransform?.GetNodeOrNull<Control>("PlayerScore");

      _font30 = GetNodeOrNull<Label>("SongInfoTransform/SongInfo/Name")?.GetFont("font") as DynamicFont;
      _font20 = GetNodeOrNull<Label>("SongInfoTransform/SongInfo/BPM")?.GetFont("font") as DynamicFont;
      _font35 = GetNodeOrNull<Label>("ChartInfoTransform/ChartInfo/Difficult")?.GetFont("font") as DynamicFont;
      _font25 = GetNodeOrNull<Label>("PlayerComboTransform/PlayerCombo/Status")?.GetFont("font") as DynamicFont;

      UpdateLayout();
    }

    public void Update(double currentBeat)
    {
      if (currentBeat == _lastUpdateBeat) return;

      ForceUpdate(currentBeat, false);
    }

    public void ForceUpdate(double currentBeat, bool _force = true)
    {
      bool isLayoutDirty = _lastState.ScreenSize != ScreenSize;
      bool isColorDirty =
        _lastState.TextColor != TextColor 
        || _lastState.TextOutLineColor != TextOutLineColor;

      if (isLayoutDirty) UpdateLayout();
      if (isColorDirty) UpdateColor();

      if (_songInfo != null && _songMetaData != null && _metronome != null)
      {
        _songInfo.SongName = _songMetaData.Name;
        _songInfo.BPM = _metronome.GetBPMAtBeat(currentBeat);
        _songInfo.SongIcon = _songMetaData.Illustration.IllustrationTexture;
        _songInfo.IconCenter = _songMetaData.Illustration.IconCenter;
        _songInfo.IconSize = _songMetaData.Illustration.IconSize;
        _songInfo.UpdateVisual();
      }

      if (_chartInfo != null && _chartMetaData != null)
      {
        _chartInfo.DifficultText = $"{_chartMetaData.ChartName} {_chartMetaData.Level}";
        _chartInfo.UpdateVisual();
      }

      UpdateComponentStoryboard(
        ComponentType.Info, _songInfoTransform, _songInfo, currentBeat, _force
      );
      UpdateComponentStoryboard(
        ComponentType.Difficulty, _chartInfoTransform, _chartInfo, currentBeat, _force
      );
      UpdateComponentStoryboard(
        ComponentType.Combo, _playerComboTransform, _playerCombo, currentBeat, _force
      );
      UpdateComponentStoryboard(
        ComponentType.Score, _playerScoreTransform, _playerScore, currentBeat, _force
      );

      _lastUpdateBeat = currentBeat;
    }

    public void SetCombo(int combo, bool instant = false) => _playerCombo?.SetCombo(combo, instant);
    public void SetGrade(Scoring.Grade grade) => _playerCombo?.SetGrade(grade);
    public void DrainPauseBar() => _playerCombo?.DrainPauseBar();
    public void FillPauseBar() => _playerCombo?.FillPauseBar();

    private void UpdateComponentStoryboard(
      ComponentType compType,
      Control transformControl,
      Control targetControl,
      double currentBeat,
      bool force
    )
    {
      if (transformControl == null || targetControl == null || _componentManager == null) return;

      var targetCompData = _componentManager[compType];
      if (targetCompData == null) return;

      float x = targetCompData.StoryboardEvents.Evaluate(
        StoryboardProperty.X, currentBeat, new AnyValue(targetCompData.InitX), force
      ).X;
      float y = targetCompData.StoryboardEvents.Evaluate(
        StoryboardProperty.Y, currentBeat, new AnyValue(targetCompData.InitY), force
      ).X;
      float scale = targetCompData.StoryboardEvents.Evaluate(
        StoryboardProperty.Scale, currentBeat, new AnyValue(targetCompData.InitScale), force
      ).X;
      float a = targetCompData.StoryboardEvents.Evaluate(
        StoryboardProperty.ColorA, currentBeat, new AnyValue(targetCompData.InitAlpha), force
      ).X;

      float viewScale = Mathf.Abs(Mathf.Min(
        ScreenSize.x / Visual.DESIGN_RESOLUTION.x,
        ScreenSize.y / Visual.DESIGN_RESOLUTION.y
      ));

      transformControl.RectPosition = new Vector2(x * viewScale, y * viewScale);
      transformControl.RectScale = new Vector2(scale, scale);
      transformControl.Modulate = new Color(1f, 1f, 1f, a);
    }

    private void UpdateLayout()
    {
      float viewScale = Mathf.Abs(Mathf.Min(
        ScreenSize.x / Visual.DESIGN_RESOLUTION.x,
        ScreenSize.y / Visual.DESIGN_RESOLUTION.y
      ));

      if (_font30 != null) _font30.Size = Mathf.Max(1, (int)(30 * viewScale));
      if (_font20 != null) _font20.Size = Mathf.Max(1, (int)(20 * viewScale));
      if (_font35 != null) _font35.Size = Mathf.Max(1, (int)(35 * viewScale));
      if (_font25 != null) _font25.Size = Mathf.Max(1, (int)(25 * viewScale));

      if (_songInfo != null)
      {
        _songInfo.ScreenSize = ScreenSize;
        _songInfo.UpdateVisual();
      }
      if (_chartInfo != null)
      {
        _chartInfo.ScreenSize = ScreenSize;
        _chartInfo.UpdateVisual();
      }
      if (_playerCombo != null)
      {
        _playerCombo.ScreenSize = ScreenSize;
        _playerCombo.UpdateVisual();
      }

      _lastState.ScreenSize = ScreenSize;
    }

    private void UpdateColor()
    {
      if (_songInfo != null)
      {
        _songInfo.TextColor = TextColor;
        _songInfo.TextOutLineColor = TextOutLineColor;
        _songInfo.UpdateVisual();
      }
      if (_chartInfo != null)
      {
        _chartInfo.TextColor = TextColor;
        _chartInfo.TextOutLineColor = TextOutLineColor;
        _chartInfo.UpdateVisual();
      }
      if (_playerCombo != null)
      {
        _playerCombo.TextColor = TextColor;
        _playerCombo.TextOutLineColor = TextOutLineColor;
        _playerCombo.UpdateVisual();
      }

      _lastState.TextColor = TextColor;
      _lastState.TextOutLineColor = TextOutLineColor;
    }
  }
}