namespace GhostBodyObject.Common.SpinLocks
{
    /// <summary>
    /// Provides a simple ticket-based spin lock for synchronizing access to a resource across multiple threads.
    /// </summary>
    /// <remarks>TicketSpinLock ensures that threads acquire the lock in the order they request it, preventing
    /// starvation. This type does not support recursion and is intended for scenarios where locks are held for very
    /// short durations. Use with care, as spinning can lead to high CPU usage if the lock is contended. This struct is
    /// not reentrant and should not be used with asynchronous code.</remarks>
    public struct ShortTicketSpinLock
    {
        private volatile int _ticketsTaken;
        private volatile int _ticketsServed;

        /// <summary>
        /// Acquires exclusive access to the critical section, blocking the calling thread until it is safe to enter.
        /// </summary>
        /// <remarks>This method implements a ticket-based spin lock. Threads are granted access in the
        /// order they call this method. The calling thread will spin-wait until it is its turn to enter the critical
        /// section. This method does not support reentrancy; calling it multiple times on the same thread without an
        /// intervening exit may result in deadlock.</remarks>
        public void Enter()
        {
            int myTicket = Interlocked.Increment(ref _ticketsTaken) - 1;
            SpinWait spin = new SpinWait();
            while (Volatile.Read(ref _ticketsServed) != myTicket)
                spin.SpinOnce();
        }

        /// <summary>
        /// Signals that the current ticket has been served and allows the next waiting operation to proceed.
        /// </summary>
        /// <remarks>Call this method when the operation associated with the current ticket is complete.
        /// This method is typically used in coordination scenarios where operations are processed in a strict order.
        /// This method is thread-safe.</remarks>
        public void Exit() => Interlocked.Increment(ref _ticketsServed);
    }
}
