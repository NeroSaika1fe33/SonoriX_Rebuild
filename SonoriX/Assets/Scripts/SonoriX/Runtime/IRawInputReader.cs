using System.Collections.Generic;

namespace SonoriX.Runtime
{
    public interface IRawInputReader
    {
        IReadOnlyList<RawInputCommand> Read();
    }
}