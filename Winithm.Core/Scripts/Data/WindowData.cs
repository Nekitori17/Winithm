using System;
using Godot;
using Winithm.Core.Common;
using Winithm.Core.Interfaces;
using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Rendering container (Lane) from [WINDOWS].
  /// Format: + <ID> <initX> <initY> <initScale> <initScaleX> <initScaleY>
  ///           <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class WindowData : IStoryboardable<StoryboardProperty>, IDeepCloneable<WindowData>
  {
    public event Action<WindowData> OnLifeCycleChanged;
    public event Action<WindowData> OnUnFocusChanged;
    public event Action<WindowData> OnUnResponsiveChanged;
    public event Action<WindowData> OnUpdated;

    public string ID;
    private string _name;
    public string Name { get => _name; set { if (_name == value) return; _name = value; OnUpdated?.Invoke(this); } }

    private string _title;
    public string Title { get => _title; set { if (_title == value) return; _title = value; OnUpdated?.Invoke(this); } }

    private int _layer = 0;
    public int Layer { get => _layer; set { if (_layer == value) return; _layer = value; OnUpdated?.Invoke(this); } }

    private Vector2 _anchor = new Vector2(0.5f, 0.5f);
    public Vector2 Anchor { get => _anchor; set { if (_anchor == value) return; _anchor = value; OnUpdated?.Invoke(this); } }

    private string _groupID;
    public string GroupID { get => _groupID; set { if (_groupID == value) return; _groupID = value; OnUpdated?.Invoke(this); } }

    private string _themeChannelID;
    public string ThemeChannelID { get => _themeChannelID; set { if (_themeChannelID == value) return; _themeChannelID = value; OnUpdated?.Invoke(this); } }

    // Window Flags
    private bool _borderless = false;
    public bool Borderless { get => _borderless; set { if (_borderless == value) return; _borderless = value; OnUpdated?.Invoke(this); } }

    private bool _unFocus = false;
    public bool UnFocus { get => _unFocus; set { if (_unFocus == value) return; _unFocus = value; OnUnFocusChanged?.Invoke(this); } }

    // Transform init values
    private float _initX = 640f;
    public float InitX { get => _initX; set { if (_initX == value) return; _initX = value; OnUpdated?.Invoke(this); } }

    private float _initY = 360f;
    public float InitY { get => _initY; set { if (_initY == value) return; _initY = value; OnUpdated?.Invoke(this); } }

    private float _initScaleX = 300f;
    public float InitScaleX { get => _initScaleX; set { if (_initScaleX == value) return; _initScaleX = value; OnUpdated?.Invoke(this); } }

    private float _initScaleY = 500f;
    public float InitScaleY { get => _initScaleY; set { if (_initScaleY == value) return; _initScaleY = value; OnUpdated?.Invoke(this); } }

    // Color init values
    private float _initR = 0f;
    public float InitR { get => _initR; set { if (_initR == value) return; _initR = value; OnUpdated?.Invoke(this); } }

    private float _initG = 0f;
    public float InitG { get => _initG; set { if (_initG == value) return; _initG = value; OnUpdated?.Invoke(this); } }

    private float _initB = 0f;
    public float InitB { get => _initB; set { if (_initB == value) return; _initB = value; OnUpdated?.Invoke(this); } }

    private float _initA = 1f;
    public float InitA { get => _initA; set { if (_initA == value) return; _initA = value; OnUpdated?.Invoke(this); } }

    private float _initNoteA = 1f;
    public float InitNoteA { get => _initNoteA; set { if (_initNoteA == value) return; _initNoteA = value; OnUpdated?.Invoke(this); } }

    // Sub-managers
    public StoryboardManager<StoryboardProperty> StoryboardEvents { get; set; } = new StoryboardManager<StoryboardProperty>();
    public SpeedStepManager SpeedSteps { get; set; } = new SpeedStepManager();
    public NoteManager Notes { get; set; } = new NoteManager();

    /// <summary>Window spawn beat (first SpeedStep's start)</summary>
    public BeatTime StartBeat = BeatTime.NaN;

    /// <summary>Window despawn beat (first Close note's start, or last SpeedStep's start)</summary>
    public BeatTime EndBeat = BeatTime.NaN;

    // ---------- Unresponsive & Focusable ----------
    /// <summary>Whether the window will be unresponsive at end beat, triggerring a "Not Responding" visual state.</summary>
    private bool _unresponsive = false;
    public bool Unresponsive { get => _unresponsive; set { if (_unresponsive == value) return; _unresponsive = value; OnUnResponsiveChanged?.Invoke(this); } }

    /// <summary>Whether the window is currently focusable, preventing player interaction and capable of processing player input.</summary>
    public bool Focusable = false;

    /// <summary>The beat when this window becomes focusable.</summary>
    public double FocusableStartBeat = double.NaN;

    /// <summary>The beat when this window becomes unresponsive.</summary>
    public double FocusableEndBeat = double.NaN;

    // --------- Pre-computed values ---------

    /// <summary>The beat when this window finishes start animation.</summary>
    public double EndBeatStartIn = double.NaN;
    /// <summary>The beat when this window finishes close animation.</summary>
    public double EndBeatEndOut = double.NaN;

    /// <summary>
    /// The beat when this window starting close animation.
    /// Compute and Re-compute EndBeatEndOutAnimation when window is unresponsive.
    /// </summary>
    public double StartBeatEndOut = double.NaN;

    /// <summary>The beat when the Unresponsive overlay reaches 100% opacity.</summary>
    public double EndBeatUnresponsive = double.NaN;

    public WindowData()
    {
      // Bubble sub-manager changes up to WindowData level
      StoryboardEvents.OnUpdated += BubbleStoryboard;
      SpeedSteps.OnUpdated += BubbleSpeedStep;
      Notes.OnLifeCycleChanged += BubbleNoteLifeCycle;
      Notes.OnUpdated += BubbleNote;

      // Note requires reference to WindowData for Focus/Close boundary logic
      Notes.SetWindowData(this);
    }

    /// <summary>
    /// Pre-computes animation-related values.
    /// Call when TimeManager.Instance.IsReady
    /// </summary>
    public void PreComputeAnimation(Metronome metronome)
    {
      double startBeatInSecs = metronome.ToSeconds(StartBeat);
      double endBeatInSecs = metronome.ToSeconds(EndBeat);

      EndBeatStartIn = metronome.ToBeat(startBeatInSecs + 0.2);
      EndBeatEndOut = metronome.ToBeat(endBeatInSecs + 0.2);
    }

    /// <summary>
    /// Computes animation-related values when the window is unresponsive.
    /// Call when TimeManager.Instance.IsReady
    /// </summary>
    public void ComputeAnimationWhenUnresponsive(Metronome metronome)
    {
      double endBeatInSecs = metronome.ToSeconds(EndBeat);

      EndBeatUnresponsive = metronome.ToBeat(endBeatInSecs + 0.2);
      StartBeatEndOut = metronome.ToBeat(endBeatInSecs + 1);
      EndBeatEndOut = metronome.ToBeat(endBeatInSecs + 1.2);
    }

    public WindowData DeepClone(ObjectFactory objectFactory, BeatTime? offset)
    {
      var cloned = new WindowData();

      // Unsubscribe default sub-manager bubbling created in constructor
      // before replacing with cloned sub-managers
      cloned.StoryboardEvents.OnUpdated -= cloned.BubbleStoryboard;
      cloned.SpeedSteps.OnUpdated -= cloned.BubbleSpeedStep;
      cloned.Notes.OnLifeCycleChanged -= cloned.BubbleNoteLifeCycle;
      cloned.Notes.OnUpdated -= cloned.BubbleNote;

      cloned.ID = objectFactory.GenerateUID();
      cloned.Name = Name;
      cloned.Title = Title;
      cloned.Layer = Layer;
      cloned.Anchor = Anchor;
      cloned.GroupID = GroupID;
      cloned.ThemeChannelID = ThemeChannelID;
      cloned.Borderless = Borderless;
      cloned.UnFocus = UnFocus;
      cloned.InitX = InitX;
      cloned.InitY = InitY;
      cloned.InitScaleX = InitScaleX;
      cloned.InitScaleY = InitScaleY;
      cloned.InitR = InitR;
      cloned.InitG = InitG;
      cloned.InitB = InitB;
      cloned.InitA = InitA;
      cloned.InitNoteA = InitNoteA;
      cloned.StartBeat = StartBeat + (offset ?? BeatTime.Zero);
      cloned.EndBeat = EndBeat + (offset ?? BeatTime.Zero);

      // Clone sub-managers
      cloned.StoryboardEvents = StoryboardEvents?.DeepClone(objectFactory, offset) ?? new StoryboardManager<StoryboardProperty>();
      cloned.SpeedSteps = SpeedSteps?.DeepClone(objectFactory, offset) ?? new SpeedStepManager();
      cloned.Notes = Notes?.DeepClone(objectFactory, offset) ?? new NoteManager();

      // Re-wire sub-manager bubbling to the new clone
      cloned.StoryboardEvents.OnUpdated += cloned.BubbleStoryboard;
      cloned.SpeedSteps.OnUpdated += cloned.BubbleSpeedStep;
      cloned.Notes.OnLifeCycleChanged += cloned.BubbleNoteLifeCycle;
      cloned.Notes.OnUpdated += cloned.BubbleNote;

      // Bind Note's WindowData reference to the new clone
      cloned.Notes.SetWindowData(cloned);

      return cloned;
    }

    // Named delegates for clean subscribe/unsubscribe in DeepClone
    private void BubbleStoryboard(StoryboardManager<StoryboardProperty> sb) => OnUpdated?.Invoke(this);
    private void BubbleSpeedStep(SpeedStepManager sd)
    {
      if (SpeedSteps.SpeedStepCollection.Count == 0)
      {
        StartBeat = BeatTime.Zero;
        EndBeat = BeatTime.Zero;

        OnLifeCycleChanged?.Invoke(this);
        return;
      }

      if (
        SpeedSteps.GetFirst().StartBeat != StartBeat ||
        SpeedSteps.GetLast().StartBeat != EndBeat
      )
      {
        StartBeat = SpeedSteps.GetFirst().StartBeat;
        EndBeat = SpeedSteps.GetLast().StartBeat;

        OnLifeCycleChanged?.Invoke(this);
        return;
      }

      OnUpdated?.Invoke(this);
    }
    private void BubbleNote(NoteManager n) => OnUpdated?.Invoke(this);
    private void BubbleNoteLifeCycle(NoteManager n) => OnLifeCycleChanged?.Invoke(this);
  }
}
