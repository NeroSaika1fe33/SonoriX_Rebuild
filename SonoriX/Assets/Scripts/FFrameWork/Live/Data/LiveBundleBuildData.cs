using UnityEngine;
using UnityEngine.Serialization;

namespace FFrameWork.Live
{
    // Per-live tuning for judgment, clear gauge, score, and display offsets.
    public class LiveBundleBuildData : ScriptableObject
    {
        public const float DefaultFlickDistance = 0.083333f;

        [SerializeField] private float judgmentOffsetX = 1.8f;
        [SerializeField] private float judgmentOffsetY = 2.5f;
        [SerializeField] private int longNoteComboBeat = 8;
        [SerializeField] private int freeLaneLongNoteComboBeat = 8;
        [SerializeField] private int sliderLongNoteComboBeat = 8;
        [SerializeField] private int volumeLongNoteComboBeat = 4;

        [FormerlySerializedAs("life")]
        [SerializeField] private int initialClearGauge = 0;
        [FormerlySerializedAs("maxLife")]
        [SerializeField] private int maxClearGauge = 100;
        [FormerlySerializedAs("dyingLife")]
        [SerializeField] private int warningClearGauge = 20;
        [FormerlySerializedAs("clearLife")]
        [SerializeField] private int clearGaugeThreshold = 70;
        [FormerlySerializedAs("justPerfectLifeRecovery")]
        [SerializeField] private int justPerfectClearGaugeRecovery = 2;
        [FormerlySerializedAs("perfectLifeRecovery")]
        [SerializeField] private int perfectClearGaugeRecovery = 2;
        [FormerlySerializedAs("greatLifeRecovery")]
        [SerializeField] private int greatClearGaugeRecovery = 1;
        [FormerlySerializedAs("goodLifeRecovery")]
        [SerializeField] private int goodClearGaugeRecovery = 1;
        [FormerlySerializedAs("badLifeDamage")]
        [SerializeField] private int badClearGaugeDamage = 3;
        [FormerlySerializedAs("missLifeDamage")]
        [SerializeField] private int missClearGaugeDamage = 5;

        [SerializeField] private int maxScore = 1000000;
        [SerializeField] private float justPerfectScoreRate = 1f;
        [SerializeField] private float perfectScoreRate = 1f;
        [SerializeField] private float autoScoreRate = 1f;
        [SerializeField] private float greatScoreRate = 0.7f;
        [SerializeField] private float goodScoreRate = 0.5f;
        [SerializeField] private float badScoreRate = 0f;
        [SerializeField] private float defaultNoteWeight = 1f;
        [SerializeField] private float longHoldComboWeight = 0.1f;
        [SerializeField] private float cutinDelayTime = 2.5f;
        [SerializeField] private float normalNoteOffsetX = 1.25f;
        [SerializeField] private float flickNoteOffsetX = 1.5f;
        [SerializeField] private float longNoteOffsetX = 1.5f;
        [SerializeField] private float flickDistance = DefaultFlickDistance;

        private readonly float initLaneOffsetX = 1.25f;

        public float JudgmentOffsetX => judgmentOffsetX;
        public float JudgmentOffsetY => judgmentOffsetY;
        public int LongNoteComboBeat => longNoteComboBeat;
        public int FreeLaneLongNoteComboBeat => freeLaneLongNoteComboBeat > 0 ? freeLaneLongNoteComboBeat : LongNoteComboBeat;
        public int SliderLongNoteComboBeat => sliderLongNoteComboBeat > 0 ? sliderLongNoteComboBeat : LongNoteComboBeat;
        public int VolumeLongNoteComboBeat => volumeLongNoteComboBeat > 0 ? volumeLongNoteComboBeat : LongNoteComboBeat;

        public int InitialClearGauge => initialClearGauge;
        public int MaxClearGauge => maxClearGauge;
        public int WarningClearGauge => warningClearGauge;
        public int ClearGaugeThreshold => clearGaugeThreshold;
        public int JustPerfectClearGaugeRecovery => justPerfectClearGaugeRecovery;
        public int PerfectClearGaugeRecovery => perfectClearGaugeRecovery;
        public int GreatClearGaugeRecovery => greatClearGaugeRecovery;
        public int GoodClearGaugeRecovery => goodClearGaugeRecovery;
        public int BadClearGaugeDamage => badClearGaugeDamage;
        public int MissClearGaugeDamage => missClearGaugeDamage;

        public int MaxScore => maxScore;
        public float JustPerfectScoreRate => justPerfectScoreRate;
        public float PerfectScoreRate => perfectScoreRate;
        public float AutoScoreRate => autoScoreRate;
        public float GreatScoreRate => greatScoreRate;
        public float GoodScoreRate => goodScoreRate;
        public float BadScoreRate => badScoreRate;
        public float DefaultNoteWeight => defaultNoteWeight;
        public float LongHoldComboWeight => longHoldComboWeight;

        public int CutinDelayTime
        {
            get
            {
                float milliseconds = cutinDelayTime * 1000f;
                return float.IsPositiveInfinity(milliseconds) ? (int)(0f - milliseconds) : (int)milliseconds;
            }
        }

        public float NormalNoteOffsetX
        {
            get { return normalNoteOffsetX <= 0f ? initLaneOffsetX : normalNoteOffsetX; }
            set { normalNoteOffsetX = value; }
        }

        public float FlickNoteOffsetX
        {
            get { return flickNoteOffsetX <= 0f ? initLaneOffsetX : flickNoteOffsetX; }
            set { flickNoteOffsetX = value; }
        }

        public float LongNoteOffsetX
        {
            get { return longNoteOffsetX <= 0f ? initLaneOffsetX : longNoteOffsetX; }
            set { longNoteOffsetX = value; }
        }

        public float FlickDistance
        {
            get { return flickDistance; }
            set { flickDistance = value; }
        }
    }
}
