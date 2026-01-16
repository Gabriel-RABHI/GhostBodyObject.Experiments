using GhostBodyObject.HandWritten.Entities.Repository;

namespace GhostBodyObject.HandWritten.Entities.Contracts
{
    public interface ITestModelScope : IDisposable
    {
        TestModelTransaction Transaction { get; }

        TestModelRepository Repository { get; }
    }
}
