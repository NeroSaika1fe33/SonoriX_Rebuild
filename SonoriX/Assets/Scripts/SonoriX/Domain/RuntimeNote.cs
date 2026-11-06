using SonoriX.Domain.Charts;

namespace SonoriX.Domain
{
    //谱面和时间，生成事件
    public abstract class RuntimeNote
    {
        //生成Note的基本定义
        public ChartNoteDefinition Definition { get; }
        
        //生成的Note的ID
        public int Id=>Definition.Id;
        
        //Note应该被打击的时间点
        public float Time => Definition.Time;
        
        //生成Note的Lane的ID
        public ChartLaneRange LaneRange => Definition.LaneRange;
        //生成的Note类型
        public ChartNoteCategory Category => Definition.Category;
        
        protected RuntimeNote(ChartNoteDefinition definition)
        {
            Definition = definition
                         ?? throw new System.ArgumentNullException(
                             nameof(definition));
        }
    }
}