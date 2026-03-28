using Godot;
using Winithm.Core.Managers;
using Winithm.Core.Data;
using Winithm.Core.Behaviors;

namespace Winithm.Editor.Managers
{
  public class EditorWindowManager : WindowManager
  {
    protected override void ApplyFlags(Window w, WindowData wd, float currentBeat)
    {
      base.ApplyFlags(w, wd, currentBeat);

      // Editor: no unresponsive states ever
      w.Unresponsive = false;
      w.UnresponsiveOverlayOpacity = 0f;
      w.IsNotRespondingTitle = false;

      // Unfocus is driven perfectly by the note timeline
      // Initially, it might be Unfocused (if wd.UnFocus is true)
      bool currentUnfocus = wd.UnFocus;

      // Check all notes in the timeline up to the current beat
      if (wd.Notes != null)
      {
          foreach (var note in wd.Notes)
          {
             // If we have passed a Focus note, the window is no longer unfocused
             if (note.Type == NoteType.Focus && currentBeat >= note.Start.AbsoluteValue)
             {
                currentUnfocus = false;
             }
          }
      }

      // We do not need to explicitly handle Close notes here because CalculateLifeCycleScale 
      // in the base WindowManager already uses the last Close note to determine despawn time (EndBeat).
      // The Editor plays strictly based on time, so despawns happen exactly on time.

      w.Unfocus = currentUnfocus;
      w.Focusable = false; // Editor doesn't need to flash 'Focusable'
    }
  }
}
