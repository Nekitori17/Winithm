namespace Winithm.Core.Data
{
    /// <summary>
    /// BPM stop defining tempo at a given time.
    /// Format: + <StartTimeSeconds> <BPM> <TimeSignature>
    /// </summary>
    public struct BPMStop
    {
        public float StartTimeSeconds;
        public float BPM;
        public int TimeSignature;

        /// <summary>Pre-calculated absolute beat at this stop's start time.</summary>
        public float AbsoluteBeat;

        public BPMStop(float startTime, float bpm, int timeSignature)
        {
            StartTimeSeconds = startTime;
            BPM = bpm;
            TimeSignature = timeSignature;
            AbsoluteBeat = 0f;
        }

        /// <summary>Beats per second at this BPM.</summary>
        public float BeatsPerSecond => BPM / 60f;
    }
}
