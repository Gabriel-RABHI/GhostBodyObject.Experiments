namespace GhostBodyObject.Repository.Repository.Helpers
{
    /// <summary>
    /// This class manage the transaction id range for a Ghost Repository.
    /// Each time a new transaction is opened, the CurrentTransactionId is assigned to the transaction and then incremented.
    /// For the lifetime of a transaction, the repository must retain the memory segments that are valid for the transaction's view.
    /// When the transaction is closed, the view counter for the transaction id is decremented.
    /// If dropping to 0, it is a signal that the repository's Store do not have to retain the MemorySegment for that transaction id.
    /// </summary>
    public class GhostRepositoryTransactionIdRange
    {
        private readonly object _lock = new object();
        private long _currentTxnId = 0;
        private readonly SortedList<long, int> _views = new SortedList<long, int>();

        public long CurrentTransactionId => System.Threading.Interlocked.Read(ref _currentTxnId);

        public long BottomTransactionId
        {
            get
            {
                lock (_lock)
                {
                    if (_views.Count == 0)
                        return Interlocked.Read(ref _currentTxnId);
                    return _views.Keys[0];
                }
            }
        }

        public long TopTransactionId => System.Threading.Interlocked.Read(ref _currentTxnId);

        /// <summary>
        /// Increments the current transaction identifier and returns the updated value.
        /// </summary>
        /// <returns>The new transaction identifier after incrementing the current value.</returns>
        public long IncrementCurrentTransactionId() => Interlocked.Increment(ref _currentTxnId);

        /// <summary>
        /// Increment the view counter for the specific transaction id.
        /// </summary>
        /// <param name="txnId">The transaction id for wich a new viewer is registered.</param>
        public void IncrementTransactionViewId(long txnId)
        {
            lock (_lock)
            {
                if (_views.TryGetValue(txnId, out int count))
                {
                    _views[txnId] = count + 1;
                }
                else
                {
                    _views.Add(txnId, 1);
                }
            }
        }

        /// <summary>
        /// Decerment the view counter for the specific transaction id.
        /// </summary>
        /// <param name="txnId"></param>
        /// <returns>True if the viewer counter drops to 0. It is a signal that the repository's Store do not have to retain the MemorySegment.</returns>
        public bool DecrementTransactionViewId(long txnId)
        {
            lock (_lock)
            {
                if (_views.TryGetValue(txnId, out int count))
                {
                    if (count <= 1)
                    {
                        _views.Remove(txnId);
                        return true;
                    }
                    else
                    {
                        _views[txnId] = count - 1;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
