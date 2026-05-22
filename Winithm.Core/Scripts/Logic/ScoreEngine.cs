using System;
using Winithm.Core.Behaviors.ScoreUI;
using Constants = Winithm.Core.Constants;

namespace Winithm.Core.Logic
{
  /// <summary>
  /// Pure scoring engine. Stateless beyond accumulated weight and combo counts.
  /// Has no knowledge of hit results or note types — callers feed normalized
  /// values (weight, comboEarned, missed) directly.
  ///
  /// Suitable for both gameplay (Client) and editor live-preview (Autoplay).
  /// </summary>
  public class ScoreEngine
  {
    private int _totalCombos = 0;   // total hittable combos in the chart
    private float _weightGained = 0f;  // accumulated hit weight (0.0–1.0 per combo)
    private int _comboEvaluated = 0;  // combos judged so far (hit or missed)
    private int _currentCombo = 0;
    private int _maxCombo = 0;
    private bool _missed = false;

    public const int MaxScore = 1_000_000;

    // ── Read-only state ──────────────────────────────────────────────────────────

    public int TotalCombos => _totalCombos;
    public int ComboEvaluated => _comboEvaluated;
    public int CurrentCombo => _currentCombo;
    public int MaxCombo => _maxCombo;
    public bool HasMissed => _missed;

    // ── Derived metrics ──────────────────────────────────────────────────────────

    /// <summary>Accuracy of combos evaluated so far (0.0–1.0). 1.0 before first note.</summary>
    public float RealtimeAccuracy => _comboEvaluated > 0
      ? (_weightGained / _comboEvaluated)
      : 1f;

    /// <summary>Score based on progress and accuracy so far.</summary>
    public int RealtimeScore => (_comboEvaluated > 0 && _totalCombos > 0)
      ? (int)((double)_comboEvaluated / _totalCombos * MaxScore * Math.Pow(RealtimeAccuracy, 2))
      : 0;

    private int ComboRemain => Math.Max(0, _totalCombos - _comboEvaluated);

    /// <summary>Best possible accumulated weight if all remaining combos are perfect.</summary>
    private float WeightPredict => _weightGained + ComboRemain;

    /// <summary>Best possible accuracy assuming all remaining combos are perfect (0.0–1.0).</summary>
    public float AccuracyPredict => _totalCombos > 0
      ? (WeightPredict / _totalCombos)
      : 1f;

    /// <summary>Best possible final score assuming all remaining combos are perfect.</summary>
    public int ScorePredict => _comboEvaluated > 0
      ? (int)(MaxScore * Math.Pow(AccuracyPredict, 2))
      : MaxScore; // before any note: predict a perfect run

    // ── Setup ────────────────────────────────────────────────────────────────────

    public void SetTotalCombos(int count) => _totalCombos = count;

    // ── Feeding data ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Records the result of a single judgement.
    /// </summary>
    /// <param name="weight">Hit weight in [0.0, 1.0]. 0 or negative = miss.</param>
    /// <param name="comboEarned">Combo units this note contributes (1 for tap, 2 for hold).</param>
    public void RecordJudgement(float weight, int comboEarned)
    {
      if (weight <= 0f)
      {
        _currentCombo = 0;
        _missed = true;
      }
      else
      {
        _currentCombo += comboEarned;
        _maxCombo = Math.Max(_currentCombo, _maxCombo);
      }

      _weightGained += weight;
      _comboEvaluated += comboEarned;
    }

    /// <summary>Directly sets accumulated weight (used by Autoplay).</summary>
    public void SetWeightGained(float weight) => _weightGained = weight;

    /// <summary>Directly sets evaluated combo count (used by Autoplay).</summary>
    public void SetComboEvaluated(int combo)
    {
      _comboEvaluated = combo;
      _currentCombo = combo;
      _maxCombo = Math.Max(combo, _maxCombo);
    }

    // ── Status ───────────────────────────────────────────────────────────────────

    public PlayerCombo.Status GetStatus()
    {
      // No notes evaluated yet — assume a perfect run.
      if (_comboEvaluated == 0) return PlayerCombo.Status.AP;

      // No misses but best-case score can't reach AP → FC.
      if (!_missed && ScorePredict < Constants.Scoring.GradeMinimumScore[Constants.Scoring.Grade.AP])
        return PlayerCombo.Status.FC;

      switch (Constants.Scoring.GetGrade(ScorePredict))
      {
        case Constants.Scoring.Grade.AP: return PlayerCombo.Status.AP;
        case Constants.Scoring.Grade.FC: return PlayerCombo.Status.FC;
        case Constants.Scoring.Grade.S: return PlayerCombo.Status.CL;
        case Constants.Scoring.Grade.A: return PlayerCombo.Status.CL;
        case Constants.Scoring.Grade.B: return PlayerCombo.Status.CL;
        default: return PlayerCombo.Status.FL;
      }
    }

    // ── Reset ────────────────────────────────────────────────────────────────────

    public void Reset()
    {
      _weightGained = 0f;
      _comboEvaluated = 0;
      _currentCombo = 0;
      _maxCombo = 0;
      _missed = false;
    }
  }
}