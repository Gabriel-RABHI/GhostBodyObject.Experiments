# GhostBodyObject : High Performance, Off-Heap, Zero Allocation Object Store
### Short Introduction

After one full month of intense development, I am proud to present the first benchmarks of the **Ghost-Body-Object (GBO)** engine - the core, memory management system. Many optimizations and critical features are still to be implemented, but the results are already very promising and may not change, but positivly evolve. Whill the GBO engine is reaching the limits of what is possible in terme of performance, the performances may not evolve that much, but the feature set will grow a lot.

**Ghost-Body-Object (GBO)** is a new .NET 10+ technology designed to break the limits of the standard Garbage Collector (GC) by moving data management off-heap while maintaining a high-level, object-oriented programming model with complete ACID durability and concurrency control.

It is designed for **mission-critical applications** requiring consistent zero-latency, large dataset handling (TB+), and high concurrency without the "stop-the-world" pauses associated with large managed heaps.

Please, read the README.md file of the GitHub repository.

### January 22, 2026
First "real" results of the GBO engine, running on an i9-9900K 8-core CPU / NVMe SSD:
- Up to **1M+ object inserts** per second in an ACID transactional manner.
- **100M+ entity LINQ enumerations** per second, using 8 threads.
- **Robust, flexible, lock-free storage engine** that scales gracefully in both throughput and data size.
- **In-Memory Index efficiency**: 1.5 GB to index 100M ghosts.

These preliminary results are the basis for the implementation of the complete, documented technology: the **GBO White Paper**, coming in February 2026.

### These Benchmarks
They demonstrate the core, critical memory management that is the foundation of this technology:
- **Ensuring memory access security by design**: It uses the GC as the end controller to release unmanaged memory. It is compliant with .NET native threading and memory management principles.
- The Virtual Memory is **write-protected** to avoid damage from unmanaged write errors.
- **Provides leak-free MVCC views**: Efficient, versioned, Epoch-based, cache-friendly ghost map.
- **Sharded Maps** for blazing-fast LINQ filter queries (predicates on up to 100M entries using 8 cores).
- **ACID transactions**: Disk-flush based. Compliant with crash recovery, replication, and multi-process write-enabled sharing.

### Comparison with FASTER
Raw data managed using FASTER is fast—faster than GBO. However, any attempt to build GBO's advanced high-level features on top of FASTER would likely be an order of magnitude slower—or even more—than GBO. This is true for a few obvious reasons:
- FASTER does not manage high-level entities: It provides a Key/Value API. Any high-level object management induces **serialization**, which means a lot of **memory copying** and **object allocation**.
- Event if you try to implement zero-copy entities like GBO does, you still need to manage the mapping between FASTER's Key/Value pairs and your high-level entities, which adds complexity and overhead. The most critical part is the security of the access to raw data and buffers lifecycle. GBO use the GC to manage the release of native memory (even with virtual memory), **wich is the single safe way to do that in .NET**. That's why GBO is a "new memory - object model" for .NET. FASTER may not be used as a base for that.
- FASTER is low-level. GBO provides both a high-end, high-level programming experience **AND** "zero-copy" and "zero-allocation" object management. The complete API is "ambient": any operation (creation, modification, deletion of any object) requires zero API calls (no Insert or Update calls needed on entities). The persistence and transactionality is fully managed by the GBO Context/Transaction system - it is completly invisible.
- FASTER is not transactional in the sense of an MVCC snapshot view of data from a point in time. This is a critical feature for any high-concurrency application. GBO provides true MVCC with epoch-based garbage collection of old data versions. It makes it in pare with PostgreSQL MVCC model.
- FASTER is not ACID: The ACID Commit concept is not supported—it can recover stale writes, but it is not ACID.
- The replication model of FASTER is not designed for high-level entities. GBO provides a simple, fast replication model based on Segment appending. It may (will) be a lot faster than FASTER replication.

### Comparison with LMDB / MDBX
While LMDB provides the highest read performance, the write performance is far from what GBO provides:
- The B+Tree data structure with COW is a powerful design but has a terrible write amplification problem.
- Long-lived read transactions lead to unmanageable fragmentation and a dramatic increase in store size. The GBO storage engine manages this without the same problems.
- The GBO object store does not provide sorted key maps: It implements indexes (sorted-based or bucket-based) as a higher-order feature because the engine is a fast and flexible object store. It is possible to develop a B-Tree on top of GBO, where nodes are GBO objects.
- Defragmentation and compacting are much simpler in GBO. GBO can optimize tables for cache locality.
- The GBO Segment system provides a fast (it is only a matter of appending raw segment data from another one) and simple replication principle.

### Comparison with PostgreSQL / EF Core
Comparison is moot:
- GBO is truly simpler: Full C#, no `IQueryable` limitations, ambient API, no flaws linked to the Unit of Work pattern, a complete, full-featured relational engine (to come), and no thread-safety concerns.
- 1 to 3 orders of magnitude faster: Zero latency, no N+1 loading problems, zero GC overhead.

There is no possible comparison, both in term of **developper experience, code simplicity and volume, and performance**.

## Core Concepts : Ghost, Body and Segments
The architecture relies on three fundamental concepts that separate *data storage* from *data access*.

### The Ghost (The Data)
The **Ghost** is the "single memory block" that contains the raw entity data.
- It is a **Bytes Array** stored in a **Segment** (Pinned Memory or Memory Mapped File).
- It is invisible to the GC (Off-Heap or Pinned).
- It is immutable in its committed state (MVCC).
- Support schema version for ascendant AND descendant versions, for reading AND writing operations.

### The Body (The Proxy)
The **Body** (e.g., `BloggerUser`) is a lightweight **proxy object** that points to a Ghost.
- **Zero-Copy**: It does not copy data; it reads directly from the Ghost's memory pointer.
- **Flyweight Pattern**: A single Body instance can "jump" from one Ghost to another (Cursor Mode), enabling zero-allocation iteration over millions of entities.
- **Code Generation**: The classes you see in `GhostBodyObject.HandWritten` (like `BloggerUser`) are examples of what the GBO Code Generator produces from your interface definitions.

### The Segment (The Storage)
Data is organized into **Segments**, which are large contiguous blocks of memory (2MB - 1GB+).
- **Memory Segment**: Allocated in the Pinned Object Heap (POH) or Large Object Heap (LOH).
- **Virtual Memory Segment**: Backed by Memory Mapped Files (MMF), allowing datasets larger than physical RAM (letting the OS handle paging).
- **Persistence**: Segments can be flushed to disk with ACID guarantees.

## Architecture & Design Philosophy
### Zero Allocation & "Cursor" Mode
In traditional .NET, iterating over 10 million objects creates 10 million instances, flooding the Gen0 heap and triggering frequent GCs.

GBO introduces **Cursor Mode**:
- A single Body instance (the cursor) is reused.
- The cursor's internal pointer is updated to the next Ghost in the collection.
- **Result**: Iterating 100M items allocates **zero** new objects.

Srings and arrays are Span like ref struct objects, made to minimize data copy and allocations for usual features.

### MVCC (Multi-Version Concurrency Control)
GBO implements a lock-free reader model using MVCC.
- **Writers** append new versions of objects (Ghosts) to new Segments.
- **Readers** use a snapshot (transaction ID) to see a consistent view of the world.
- **Epochs**: Old object versions are garbage collected (segments released) only when no active reader needs them.
- **Compaction**: when old segments are almost empty (the contained Ghosts are not anymore reachable by any opened transactions), the remaining valid Ghost are moved to the head of the segment chain.
- **Shared**: multiple process can read AND write to the same repository Segment chain.

---
## Benchmark Walkthrough (`BloggerAppBenchmarks`)
The `BloggerAppBenchmarks.cs` file demonstrates the capabilities of the engine step-by-step.

### OBJ-01: In-Memory Baseline (The "New New" C#)
*Benchmark: `InsertMutateRemove_1`*
- **What it does**: Inserts 10M objects into an In-Memory Repository.
- **Key Takeaway**: GBO transforms C# into a systems programming language. The storage is purely in-memory (like a `List<T>`), but with the transactional safety and memory layout of a database.
- **Scalability**: Performance scales linearly. Ghost size (Fat vs Light objects) has minimal impact because data is streamed, not object-managed.  

### OBJ-02: Concurrency & MVCC
*Benchmark: `InsertMutateRemove_2`*
- **What it does**: 1M writes + high-concurrency enumerations (readers) running in parallel.
- **Key Takeaway**:
    - **No Locking**: Readers do not block writers.
    - **Cursor vs Instance**:
        - `useCursor = true`: **Zero Allocation**. Extremely fast.
        - `useCursor = false`: **Instance Mode**. Creates one Body per Ghost (traditional behavior). Slower due to GC pressure.
    - **Outcome**: Proof that you can run analytics (aggregations, LINQ) on live production data without locking the transaction log.

### OBJ-03: Virtual Memory (Out of Heap)
*Benchmark: `InsertMutateRemove_3`*
- **What it does**: Inserts **10GB** of data using `SegmentStoreMode.InVirtualMemoryVolatileRepository`.
- **Key Takeaway**:
    - The process memory footprint remains low (e.g., 200MB) even with 10GB of data.
    - **OS Paging**: The Operating System handles swapping pages in and out of RAM. GBO plays nicely with the OS Virtual Memory Manager.
    - **Massive Datasets**: You can load datasets larger than available RAM.

### OBJ-04: ACID Persistence
*Benchmark: `InsertMutateRemove_4`*
- **What it does**: Enables `SegmentStoreMode.PersistantRepository`.
- **Key Takeaway**:
    - **Durability**: Data is flushed to disk on Commit.
    - **Checksums**: Every transaction is checksummed (Hash) to detect corruption or incomplete writes on restart.
    - **Performance**: Sequential disk I/O is fast; persistence cost is negligible compared to the benefits.

### OBJ-05: High-Speed Enumeration & Point Lookups
*Benchmark: `ConcurrentEnumerations`*
- **What it does**: Compares random access (`Retreive(id)`) and sequential scanning.
- **Key Takeaway**:
    - **Parallel Filtering**: Collections are sharded. You can use `.AsParallel()` or multiple threads to scan the store at memory bandwidth speeds.
    - **Point Retrieve**: `O(1)` access time via the `GhostMap`.

### OBJ-06: Safety & Lifecycle
*Benchmark: `SegmentReleasing`*
- **What it does**: Updates objects to make old versions obsolete, forcing Segments to be dropped.
- **Key Takeaway**:
    - **Safety**: If you hold onto a Body after its Transaction/Context creates it, GBO prevents you from accessing invalid memory.
    - **Memory Reclamation**: Segments are reference-counted. When all Ghosts in a Segment are obsolete (updated/deleted) and no readers are looking at them, the Segment is disposed and the file deleted (or memory freed).

---
## Conclusion for Engineers
The key-point is to understand that GhostBodyObject is not just an ORM or a Cache. It is a **Memory Engine** for .NET.
1.  **Stop optimizing GC**: Don't waste time tuning GC server modes. Use Off-Heap memory.
2.  **Code like Domain Experts**: Define your Entities (Bodies) with rich behavior. GBO handles the "Ghost" storage transparently. No more DTOs, no more mapping, no more serialization, and **no more integration tests**.
3.  **Unified Data**: One single API for In-Memory, Virtual Memory, and Persistent storage.

### The next major implementations
To complete this highly critical, first implementation, I have to write this key features :
- The **Compactor** background processing, to reduce fragmentation and perform garbage collection.
- The **Reload / Recovery** of a persisted repository using compact, near instantly loaded on disk segment indexes, generated when a segment is "sealed".
- The **Multi-process Sharing** of Segment chains and complete repository sync with mutations.

I hope I'll be able to release the complete White Paper and the full implementation of the GhostBodyObject "storage" engine in March 2026.

Time is money. This project is AGPLv3 licensed, with commercial licenses available for enterprises. Contact me for more information.

Gabriel RABHI