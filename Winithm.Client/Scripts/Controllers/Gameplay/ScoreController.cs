using System;
using Winithm.Core.Behaviors.ScoreUI;
using Winithm.Core.Data;
using Constants = Winithm.Core.Constants;

namespace Winithm.Client.Controllers.Gameplay
{
  /// <summary>
  /// Accumulates hit weights from the HitController and calculates score.
  /// </summary>
  public class ScoreController
  {
    private int _totalWeight = 0;
    private float _currentWeight = 0f;
    private int _comboEvaluated = 0;
    private int _maxScore = 1000000;
    private int _maxCombo = 0;
    private int _currentCombo = 0;

    private bool _missed = false;

    public float TotalWeight => _totalWeight;
    public int TotalComboCount => _totalWeight;
    public int ComboEvaluated => _comboEvaluated;
    public int MaxScore => _maxScore;

    /// <summary>Accuracy of notes hit so far (0.0 to 1.0).</summary>
    public float RealtimeAccuracy => _comboEvaluated > 0
      ? (_currentWeight / _comboEvaluated)
      : 1f; // 100% before first note

    /// <summary>Current score based on notes passed and accuracy.</summary>
    public int RealtimeScore => (_comboEvaluated > 0 && _totalWeight > 0)
      ? (int)((double)_comboEvaluated / _totalWeight * _maxScore * Math.Pow(RealtimeAccuracy, 2))
      : 0;

    /// <summary>Remaining notes to be evaluated.</summary>
    public int comboRemain => Math.Max(0, TotalComboCount - _comboEvaluated);

    /// <summary>Max possible weight if all remaining notes are hit perfectly (1.0).</summary>
    public float WeightPredict => _currentWeight + comboRemain;

    /// <summary>Predicted maximum accuracy at the end of the song (0.0 to 1.0).</summary>
    public float AccuracyPredict => _comboEvaluated > 0
      ? (WeightPredict / _totalWeight)
      : 1f;

    /// <summary>Predicted maximum score at the end of the song.</summary>
    public int ScorePredict => _comboEvaluated > 0
      ? (int)(_maxScore * Math.Pow(AccuracyPredict, 2))
      : 0;

    public void SetTotalCombos(int count) => _totalWeight = count;
    

    public void RegisterHit(HitResult result)
    {
      var comboEarned = result.Note.Type == NoteType.Hold ? 2 : 1;

      if (result.Weight <= 0f)
      {
        _currentCombo = 0;
        _missed = true;
      }
      else
      {
        _currentCombo += comboEarned;
        _maxCombo = Math.Max(_currentCombo, _maxCombo);
      }

      _currentWeight += result.Weight;
      _comboEvaluated += comboEarned;
    }

    public void Reset()
    {
      _currentWeight = 0f;
      _comboEvaluated = 0;
    }

    public int GetCurrentCombo() => _currentCombo;
    public int GetMaxCombo() => _maxCombo;
    public int GetRealtimeScore() => RealtimeScore;
    public int GetScorePredict() => ScorePredict;
    public float GetRealtimeAccuracy() => RealtimeAccuracy;
    public float GetAccuracyPredict() => AccuracyPredict;

    public PlayerCombo.Status GetStatus()
    {
      if (ScorePredict < Constants.Scoring.GradeMinimumScore[Constants.Scoring.Grade.AP] && !_missed)
        return PlayerCombo.Status.FC;

      var grade = Constants.Scoring.GetGrade(ScorePredict);

      switch (grade)
      {
        case Constants.Scoring.Grade.F: return PlayerCombo.Status.FL;
        case Constants.Scoring.Grade.D: return PlayerCombo.Status.FL;
        case Constants.Scoring.Grade.C: return PlayerCombo.Status.FL;
        case Constants.Scoring.Grade.B: return PlayerCombo.Status.CL;
        case Constants.Scoring.Grade.A: return PlayerCombo.Status.CL;
        case Constants.Scoring.Grade.S: return PlayerCombo.Status.CL;
        case Constants.Scoring.Grade.FC: return PlayerCombo.Status.FC;
        case Constants.Scoring.Grade.AP: return PlayerCombo.Status.AP;
        default: return PlayerCombo.Status.CL;
      }
    }
  }
}
