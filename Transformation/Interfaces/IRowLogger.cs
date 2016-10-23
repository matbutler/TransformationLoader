using System;

namespace TransformationCore
{
    public interface IRowLogger
    {
        void Initialise(string processId);
        void Complete();
        void LogRow(bool rowSucess, bool rowDropped, long rowNumber, string rowError);
    }
}