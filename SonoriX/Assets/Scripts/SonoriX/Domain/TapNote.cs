using SonoriX.Domain.Charts;

namespace SonoriX.Domain
{
    public sealed class TapNote : RuntimeNote
    {
        public TapNote(ChartNoteDefinition definition)
            : base(definition)
        {
            if (definition.Category != ChartNoteCategory.FixedTap)
            {
                throw new System.ArgumentException(
                    "当前 TapNote 只支持 FixedTap。",
                    nameof(definition));
            }
        }
    }
}