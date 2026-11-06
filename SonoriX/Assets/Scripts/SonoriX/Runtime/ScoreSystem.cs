using System.Collections.Generic;

namespace SonoriX.Runtime
{
    //根据判定结果更新分数、Combo和判定统计
    public class ScoreSystem
    {
        private int totalScore;
        private int combo;
        private int maxCombo;

        private int justPerfectCount;
        private int perfectCount;
        private int greatCount;
        private int goodCount;
        private int badCount;
        private int missCount;

        public LiveScore CurrentScore => new LiveScore(
            totalScore,
            combo,
            maxCombo,
            justPerfectCount,
            perfectCount,
            greatCount,
            goodCount,
            badCount,
            missCount);

        public void ApplyResults(IReadOnlyList<JudgmentResult> results)
        {
            foreach (JudgmentResult result in results)
            {
                ApplySingleResult(result);
            }
        }

        //产出分数，combo，计数
        private void ApplySingleResult(JudgmentResult result)
        {
            if (result.Judgment == NoteJudgment.None)
            {
                return;
            }

            totalScore += GetScore(result.Judgment);

            if (IsComboContinuing(result.Judgment))
            {
                combo++;

                if (combo > maxCombo)
                {
                    maxCombo = combo;
                }
            }
            else
            {
                combo = 0;
            }

            CountJudgment(result.Judgment);
        }

        //根据判定获得分数
        private int GetScore(NoteJudgment judgment)
        {
            switch (judgment)
            {
                case NoteJudgment.JustPerfect:
                    return 1000;

                case NoteJudgment.Perfect:
                    return 900;

                case NoteJudgment.Great:
                    return 700;

                case NoteJudgment.Good:
                    return 400;

                case NoteJudgment.Bad:
                    return 100;

                case NoteJudgment.Miss:
                case NoteJudgment.None:
                default:
                    return 0;
            }
        }

        //检查Combo是否还能维持
        private bool IsComboContinuing(NoteJudgment judgment)
        {
            switch (judgment)
            {
                case NoteJudgment.JustPerfect:
                case NoteJudgment.Perfect:
                case NoteJudgment.Great:
                case NoteJudgment.Good:
                    return true;

                case NoteJudgment.Bad:
                case NoteJudgment.Miss:
                case NoteJudgment.None:
                default:
                    return false;
            }
        }

        //判定计数
        private void CountJudgment(NoteJudgment judgment)
        {
            switch (judgment)
            {
                case NoteJudgment.JustPerfect:
                    justPerfectCount++;
                    break;

                case NoteJudgment.Perfect:
                    perfectCount++;
                    break;

                case NoteJudgment.Great:
                    greatCount++;
                    break;

                case NoteJudgment.Good:
                    goodCount++;
                    break;

                case NoteJudgment.Bad:
                    badCount++;
                    break;

                case NoteJudgment.Miss:
                    missCount++;
                    break;
            }
        }
    }
}
