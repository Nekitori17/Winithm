using Godot;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Client.Managers
{
  public class SceneManager : Node
  {
    public override void _Ready()
    {
      GD.Print("═══════════════════════════════════════════════");
      GD.Print("  Winithm Engine — Parser Test");
      GD.Print("═══════════════════════════════════════════════");

      string levelFolder = ProjectSettings.GlobalizePath(
          "res://Winithm.Assets/Levels/frizka.allMyFellas");

      ChartData data = WinithmChartIO.LoadLevel(levelFolder, "info");

      // ── Metadata ──
      GD.Print("\n[METADATA]");
      GD.Print($"  ID:       {data.Metadata.ID}");
      GD.Print($"  Name:     {data.Metadata.Name}");
      GD.Print($"  Artist:   {data.Metadata.Artist}");
      GD.Print($"  Tags:     {data.Metadata.Tags}");
      GD.Print($"  Preview:  {data.Metadata.PreviewStart}s - {data.Metadata.PreviewEnd}s");
      GD.Print($"  Level:    {data.Metadata.Level}");
      GD.Print($"  Constant: {data.Metadata.Constant}");

      // ── Resources ──
      GD.Print("\n[RESOURCES]");
      GD.Print($"  Song Path:         {data.Resources.SongPath}");
      GD.Print($"  Illustration Path: {data.Resources.IllustrationPath}");
      GD.Print($"  Illustrator:       {data.Resources.Illustrator}");
      GD.Print($"  BPM Stops:         {data.Resources.BPMList.Count}");
      foreach (var bpm in data.Resources.BPMList)
        GD.Print($"    ├ Time={bpm.StartTimeSeconds}s BPM={bpm.BPM} Sig={bpm.TimeSignature} AbsBeat={bpm.AbsoluteBeat}");

      // ── Chart References ──
      GD.Print($"\n[CHARTS] ({data.ChartReferences.Count} references)");
      foreach (var c in data.ChartReferences)
        GD.Print($"  ├ ID={c.ID} Name={c.Name} Charter={c.Charter} Level={c.Level} Constant={c.Constant}");

      // ── Overlays ──
      GD.Print($"\n[OVERLAYS] ({data.Overlays.Count} overlays)");
      foreach (var over in data.Overlays)
      {
        GD.Print($"+ ID: {over.ID} | InitParams: {string.Join(", ", over.InitParams.Values)}");
        GD.Print($"  Name: {over.Name}");
        GD.Print($"  Shader: {over.ShaderFile}");
        int overlayEventCount = over.StoryboardEvents != null ? over.StoryboardEvents.Values.Sum(l => l.Count) : 0;
        GD.Print($"  Event Count: {overlayEventCount}");
      }

      // ── Components ──
      GD.Print($"\n[COMPONENTS] ({data.Components.Count} components)");
      foreach (var comp in data.Components)
      {
        int compEventCount = comp.StoryboardEvents != null ? comp.StoryboardEvents.Values.Sum(l => l.Count) : 0;
        GD.Print($"  ├ Type={comp.Type} X={comp.InitX} Y={comp.InitY} Scale={comp.InitScale} Alpha={comp.InitAlpha} Events={compEventCount}");
        if (comp.StoryboardEvents != null)
          foreach (var kvp in comp.StoryboardEvents)
            foreach (var e in kvp.Value)
              GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(kvp.Key, null)} Start={e.Start} Len={e.Length} From={e.From} To={e.To} Easing={e.Easing}");
      }

      // ── Theme Channels ──
      GD.Print($"\n[THEME_CHANNELS] ({data.ThemeChannels.Count} channels)");
      foreach (var tc in data.ThemeChannels)
      {
        int tcEventCount = tc.StoryboardEvents != null ? tc.StoryboardEvents.Values.Sum(l => l.Count) : 0;
        GD.Print($"  ├ ID={tc.ID} R={tc.InitR} G={tc.InitG} B={tc.InitB} A={tc.InitA} NoteA={tc.InitNoteA} Events={tcEventCount}");
        if (tc.StoryboardEvents != null)
          foreach (var kvp in tc.StoryboardEvents)
            foreach (var e in kvp.Value)
              GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(kvp.Key, null)} Start={e.Start} Len={e.Length} From={e.From} To={e.To} Easing={e.Easing}");
      }

      // ── Groups ──
      GD.Print($"\n[GROUPS] ({data.Groups.Count} groups)");
      foreach (var g in data.Groups)
      {
        int gEventCount = g.StoryboardEvents != null ? g.StoryboardEvents.Values.Sum(l => l.Count) : 0;
        GD.Print($"  ├ ID={g.ID} Parent={g.ParentGroupID} X={g.InitX} Y={g.InitY} ScaleX={g.InitScaleX} ScaleY={g.InitScaleY} Events={gEventCount}");
        if (g.StoryboardEvents != null)
          foreach (var kvp in g.StoryboardEvents)
            foreach (var e in kvp.Value)
              GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(kvp.Key, null)} Start={e.Start} Len={e.Length} From={e.From} To={e.To} Easing={e.Easing}");
      }

      // ── Windows ──
      GD.Print($"\n[WINDOWS] ({data.Windows.Count} windows)");
      foreach (var w in data.Windows)
      {
        int wEventCount = w.StoryboardEvents != null ? w.StoryboardEvents.Values.Sum(l => l.Count) : 0;
        GD.Print($"  ├ ID={w.ID} Borderless={w.Borderless} UnFocus={w.UnFocus} Layer={w.Layer} Group={w.GroupID} Theme={w.ThemeChannelID}");
        GD.Print($"  │ Anchor=({w.AnchorX},{w.AnchorY}) Lifecycle=[{w.StartBeat} → {w.EndBeat}]");
        GD.Print($"  │ Events={wEventCount} SpeedSteps={w.SpeedSteps.Count} Notes={w.Notes.Count}");
        if (w.StoryboardEvents != null)
          foreach (var kvp in w.StoryboardEvents)
            foreach (var e in kvp.Value)
              GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(kvp.Key, null)} Start={e.Start} Len={e.Length} From={e.From} To={e.To} Easing={e.Easing}");
        foreach (var ss in w.SpeedSteps)
        {
          int ssEventCount = ss.StoryboardEvents != null ? ss.StoryboardEvents.Values.Sum(l => l.Count) : 0;
          GD.Print($"    ├ | Start={ss.Start} Speed={ss.Multiplier} ChildEvents={ssEventCount}");
          if (ss.StoryboardEvents != null)
            foreach (var kvp in ss.StoryboardEvents)
              foreach (var e in kvp.Value)
                GD.Print($"      ├ / {ParserUtils.FormatStoryboardProperty(kvp.Key, null)} Start={e.Start} Len={e.Length} From={e.From} To={e.To} Easing={e.Easing}");
        }
        foreach (var n in w.Notes)
          GD.Print($"    ├ # {n.Type} Start={n.Start} Len={n.Length} Side={n.Side} Fake={n.FakeType}");
      }

      GD.Print("\n═══════════════════════════════════════════════");
      GD.Print("  Parser Test Complete!");
      GD.Print("═══════════════════════════════════════════════");
    }
  }
}