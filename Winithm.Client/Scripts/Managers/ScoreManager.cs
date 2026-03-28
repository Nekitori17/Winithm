using System;
using System.Collections.Generic;
using Winithm.Client.Data;

namespace Winithm.Client.Managers
{
  /// <summary>
  /// Accumulates hit weights from the HitManager and calculates score.
  /// </summary>
  public class ScoreManager
  {
    private float _totalWeight = 0f;
    private int _totalHittableNotes = 0;
    private int _notesEvaluated = 0;
    private int _maxScore = 1000000;

    public float TotalWeight => _totalWeight;
    public int TotalHittableNotes => _totalHittableNotes;
    public int NotesEvaluated => _notesEvaluated;
    public int MaxScore => _maxScore;

    /// <summary>Accuracy of notes hit so far (0.0 to 1.0).</summary>
    public float RealtimeAccuracy => _notesEvaluated > 0 
      ? (_totalWeight / _notesEvaluated) 
      : 1f; // 100% before first note

    /// <summary>Current score based on notes passed and accuracy.</summary>
    public int RealtimeScore => _totalHittableNotes > 0 
      ? (int)((_notesEvaluated / (float)_totalHittableNotes * _maxScore) * Math.Pow(RealtimeAccuracy, 4))
      : 0;

    /// <summary>Remaining notes to be evaluated.</summary>
    public int NoteRemain => Math.Max(0, _totalHittableNotes - _notesEvaluated);
    
    /// <summary>Max possible weight if all remaining notes are hit perfectly (1.0).</summary>
    public float WeightPredict => _totalWeight + NoteRemain;

    /// <summary>Predicted maximum accuracy at the end of the song (0.0 to 1.0).</summary>
    public float AccuracyPredict => _totalHittableNotes > 0 
      ? (WeightPredict / _totalHittableNotes)
      : 1f;

    /// <summary>Predicted maximum score at the end of the song.</summary>
    public int ScorePredict => _totalHittableNotes > 0
      ? (int)(_maxScore * Math.Pow(AccuracyPredict, 4))
      : 0;

    public void SetTotalHittableNotes(int count)
    {
      _totalHittableNotes = count;
    }

    public void RegisterHit(Data.HitResult result)
    {
      _totalWeight += result.Weight;
      _notesEvaluated++;
    }

    public void Reset()
    {
      _totalWeight = 0f;
      _notesEvaluated = 0;
    }
  }
}
