namespace GhostBodyObject.Repository.Ghost.Constants
{
    public enum GhostStatus : byte
    {
        // -------- Statuses that can be the one of a Ghost in a MemorySegment -------- //

        /// <summary>
        /// Indicates that the Ghost is in a MemorySegment (in memory, or in memory mapped file).
        /// </summary>
        Mapped = 0x02,
        /// <summary>
        /// Indicates that the Ghost is in a MemorySegment, but it signal that is have been commited as deleted.
        /// This status cannot be the one of a Ghost that is owned by a Body in any transaction.
        /// This statis is only used to do not create Body for transactions, or to rebuild the map on start.
        /// </summary>
        Tombstone = 0x04,

        // -------- Statuses that cannot be the one of a Ghost in a MemorySegment, but with an older in MemorySegment -------- //

        /// <summary>
        /// Indicates that the Ghost is owned by a Body in the transaction.
        /// It is a copy of a Ghost that exist in a MemorySegment (in memory, or in memory mapped file).
        /// This status cannot be the one of a Ghost in a MemorySegment.
        /// </summary>
        MappedModified = 0x06,
        /// <summary>
        /// Indicates that the Ghost is owned by a Body in the transaction - an invisible Body in Transaction collections.
        /// It is a copy of a Ghost that exist in a MemorySegment (in memory, or in memory mapped file), but taged deleted (Delete had been called on the Body).
        /// This status cannot be the one of a Ghost in a MemorySegment.
        /// </summary>
        MappedDeleted = 0x08,

        // ------- Statuses that cannot be the one of a Ghost in a MemorySegment -------- //

        /// <summary>
        /// Indicates that the Ghost is owned by a Body inserted in the transaction.
        /// This Ghost do not live anywhere else.
        /// This status cannot be the one of a Ghost in a MemorySegment.
        /// </summary>
        Inserted = 0x0A,
    }
}
