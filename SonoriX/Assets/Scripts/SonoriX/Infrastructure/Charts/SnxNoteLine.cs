using System.Collections.Generic;

namespace SonoriX.Infrastructure.Charts
{
        //一行Note文本拆解后的语法数据
    public sealed class SnxNoteLine
    {
        public int Bar { get; }
        
        public IReadOnlyList<string> Path { get; }
        
        public IReadOnlyList<string> Tokens { get; }
        
        public int DivisionCount => Tokens.Count;
        
        public SnxNoteLine(
            int bar,
            IReadOnlyList<string> path,
            IReadOnlyList<string> tokens)
        {
            Bar = bar;
            Path = path;
            Tokens = tokens;
        }
        
        //沿用旧Converter的小节内位置计算
        public float GetBarProgress(int tokenIndex)
        {
            return (float)tokenIndex / DivisionCount;
        }
    }
}