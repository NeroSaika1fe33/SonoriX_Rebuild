namespace SonoriX.Runtime
{
    //判定窗口详细定义
    public readonly struct JudgmentRules
    {
        public float JustPerfectWindow { get; }
        public float PerfectWindow { get; }
        public float GreatWindow { get; }
        public float GoodWindow { get; }
        public float BadWindow { get; }

        public float MaxWindow => BadWindow;

        public JudgmentRules(
            float justPerfectWindow,
            float perfectWindow,
            float greatWindow,
            float goodWindow,
            float badWindow)
        {
            JustPerfectWindow = justPerfectWindow;
            PerfectWindow = perfectWindow;
            GreatWindow = greatWindow;
            GoodWindow = goodWindow;
            BadWindow = badWindow;
        }

        public static JudgmentRules Default => new JudgmentRules(
            0.025f,
            0.05f,
            0.10f,
            0.15f,
            0.20f);
    }
}