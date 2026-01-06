using GhostBodyObject.HandWritten.TestModel.Repository;

namespace GhostBodyObject.HandWritten.TestModel.Contracts
{
    public interface ITestModelScope : IDisposable
    {
        TestModelTransaction Transaction { get; }

        TestModelRepository Repository { get; }
    }
}
