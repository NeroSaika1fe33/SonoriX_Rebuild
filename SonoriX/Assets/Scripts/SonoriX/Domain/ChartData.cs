using System.Collections.Generic;
using SonoriX.Domain.Charts;

namespace SonoriX.Domain
{
    public sealed class ChartData
    {
        //存储读取一次谱面后生成的Note列表
        private readonly List<RuntimeNote> notes;
        //对外暴露接口为只读
        public IReadOnlyList<RuntimeNote>  Notes => notes;

        public ChartDefinition Definition { get; }

        //遍历，IEnumerable保证能接受多种数据类型
        public ChartData(
            ChartDefinition definition,
            IEnumerable<RuntimeNote> notes)
        {
            Definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            
            this.notes = new List<RuntimeNote>(notes);
            //比较遍历的note的时间数据，进行从前到后的排序
            this.notes.Sort((a,b)=>a.Time.CompareTo(b.Time));
        }
    }
}