using Godot;
using Winithm.Core.Common;
using Winithm.Core.Data;

public class SceneManager : Node
{
    public override async void _Ready()
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
            GD.Print($"+ ID: {over.ID} | InitParams: {string.Join(", ", over.InitParams)}");
            GD.Print($"  Name: {over.Name}");
            GD.Print($"  Shader: {over.ShaderFile}");
            GD.Print($"  Event Count: {over.Events.Count}");
        }

        // ── Components ──
        GD.Print($"\n[COMPONENTS] ({data.Components.Count} components)");
        foreach (var comp in data.Components)
        {
            GD.Print($"  ├ Type={comp.Type} X={comp.InitX} Y={comp.InitY} Scale={comp.InitScale} Alpha={comp.InitAlpha} Events={comp.Events.Count}");
            foreach (var e in comp.Events)
                GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(e.Type, e.CustomProperty)} Start={e.Start} Len={e.Length} From={e.FromRaw} To={e.ToRaw} Easing={e.Easing}");
        }

        // ── Theme Channels ──
        GD.Print($"\n[THEME_CHANNELS] ({data.ThemeChannels.Count} channels)");
        foreach (var tc in data.ThemeChannels)
        {
            GD.Print($"  ├ ID={tc.ID} R={tc.InitR} G={tc.InitG} B={tc.InitB} A={tc.InitA} NoteA={tc.InitNoteA} Events={tc.Events.Count}");
            foreach (var e in tc.Events)
                GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(e.Type, e.CustomProperty)} Start={e.Start} Len={e.Length} From={e.FromRaw} To={e.ToRaw} Easing={e.Easing}");
        }

        // ── Groups ──
        GD.Print($"\n[GROUPS] ({data.Groups.Count} groups)");
        foreach (var g in data.Groups)
        {
            GD.Print($"  ├ ID={g.ID} Parent={g.ParentGroupID} X={g.InitX} Y={g.InitY} ScaleX={g.InitScaleX} ScaleY={g.InitScaleY} Events={g.Events.Count}");
            foreach (var e in g.Events)
                GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(e.Type, e.CustomProperty)} Start={e.Start} Len={e.Length} From={e.FromRaw} To={e.ToRaw} Easing={e.Easing}");
        }

        // ── Windows ──
        GD.Print($"\n[WINDOWS] ({data.Windows.Count} windows)");
        foreach (var w in data.Windows)
        {
            GD.Print($"  ├ ID={w.ID} UnFocus={w.IsUnFocus} Layer={w.Layer} Group={w.GroupID} Theme={w.ThemeChannelID}");
            GD.Print($"  │ Anchor=({w.AnchorX},{w.AnchorY}) Lifecycle=[{w.StartBeat} → {w.EndBeat}]");
            GD.Print($"  │ Events={w.Events.Count} SpeedSteps={w.SpeedSteps.Count} Notes={w.Notes.Count}");
            foreach (var e in w.Events)
                GD.Print($"    ├ / {ParserUtils.FormatStoryboardProperty(e.Type, e.CustomProperty)} Start={e.Start} Len={e.Length} From={e.FromRaw} To={e.ToRaw} Easing={e.Easing}");
            foreach (var ss in w.SpeedSteps)
            {
                GD.Print($"    ├ | Start={ss.Start} Speed={ss.Multiplier} ChildEvents={ss.Events.Count}");
                foreach (var e in ss.Events)
                    GD.Print($"      ├ / {ParserUtils.FormatStoryboardProperty(e.Type, e.CustomProperty)} Start={e.Start} Len={e.Length} From={e.FromRaw} To={e.ToRaw} Easing={e.Easing}");
            }
            foreach (var n in w.Notes)
                GD.Print($"    ├ # {n.Type} Start={n.Start} Len={n.Length} Side={n.Side} Fake={n.FakeType}");
        }

        GD.Print("\n═══════════════════════════════════════════════");
        GD.Print("  Parser Test Complete!");
        GD.Print("═══════════════════════════════════════════════");
    }
}