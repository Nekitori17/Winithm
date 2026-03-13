using System;
using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Engine
{
    /// <summary>
    /// Calculates visual scroll offset for notes using definite integral of velocity over time.
    /// Pre-computes cumulative distance array for O(log N) runtime lookups.
    /// <summary>
    /// Evaluates the visual positions of objects on the timeline.
    /// Performs real-time integration (area under the curve) of the scrolling speed 
    /// over time to account for dynamic timeline deformation (StoryboardEvents).
    /// </summary>
    public static class SpeedCalculator
    {
        // ──────────────────────────────────────────────
        // API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Calculates the visual offset (distance) of a target beat relative to the current playback beat.
        /// Performs real-time definite integration of velocity over time:
        /// integral from {currentBeat} to {targetBeat} of S(b) db
        /// </summary>
        /// <param name="steps">Sorted list of speed steps.</param>
        /// <param name="currentBeat">The current playback beat.</param>
        /// <param name="targetBeat">The beat position of the note/object.</param>
        /// <returns>The real-time visual offset distance.</returns>
        public static float GetVisualOffset(List<SpeedStep> steps, float currentBeat, float targetBeat)
        {
            if (steps == null || steps.Count == 0) return 0f;

            // Simple early exit to avoid pointless loop if current = target
            if (Math.Abs(currentBeat - targetBeat) < 0.0001f) return 0f;

            float distance = 0f;
            
            // Standard integration from start to end points
            float startInt = Math.Min(currentBeat, targetBeat);
            float endInt = Math.Max(currentBeat, targetBeat);

            // Iterate through every speed step to calculate the area under the curve
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                float stepStart = step.Start.AbsoluteValue;
                float stepEnd = (i + 1 < steps.Count) ? steps[i + 1].Start.AbsoluteValue : float.MaxValue;

                // Find the intersection window between this step and our integral limits
                float windowStart = Math.Max(startInt, stepStart);
                float windowEnd = Math.Min(endInt, stepEnd);

                if (windowStart < windowEnd)
                {
                    // Calculate duration in this speed segment
                    float durationInStep = windowEnd - windowStart;
                    
                    // Evaluate dynamic multiplier at the *current* real-time beat
                    // This creates the "rubber banding" deformation effect!
                    float activeMultiplier = StoryboardEvaluator.EvaluateFloat(step.Events, StoryboardProperty.Speed, currentBeat, step.Multiplier);

                    distance += durationInStep * activeMultiplier;
                }
            }

            // Directional flip
            if (targetBeat < currentBeat)
            {
                distance = -distance;
            }

            return distance;
        }

        // ──────────────────────────────────────────────
        //  Runtime Queries
        // ──────────────────────────────────────────────

        /// <summary>
        /// Get the current scroll speed at the given beat.
        /// </summary>
        public static float GetSpeedAt(List<SpeedStep> steps, float beat)
        {
            if (steps == null || steps.Count == 0) return 1f;

            int idx = FindStepIndex(steps, beat);
            return steps[idx].Multiplier;
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Finds the index of the active SpeedStep at the given absolute beat using Binary Search.
        /// Returns the last step whose start time is <= the target beat.
        /// </summary>
        private static int FindStepIndex(List<SpeedStep> steps, float beat)
        {
            if (steps == null || steps.Count == 0) return 0;
            
            int left = 0;
            int right = steps.Count - 1;
            int best = 0;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (steps[mid].Start.AbsoluteValue <= beat)
                {
                    best = mid;
                    left = mid + 1; // Try to find a later step that is still <= beat
                }
                else
                {
                    right = mid - 1;
                }
            }

            return best;
        }
    }
}
