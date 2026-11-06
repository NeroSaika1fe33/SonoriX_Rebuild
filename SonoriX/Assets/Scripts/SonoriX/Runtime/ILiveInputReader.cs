using System.Collections.Generic;

namespace SonoriX.Runtime
{
    //LiveInput 抽象接口
    public interface ILiveInputReader
    {
        IReadOnlyList<LiveInput> Read(float musicTime);
    }
}