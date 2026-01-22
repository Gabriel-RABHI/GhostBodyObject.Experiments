# GhostBodyObject : High Performance, Off-Heap, Zero Allocation Object Store
### Short Introduction
**Ghost-Body-Object (GBO)** is a new .NET 10+ technology designed to break the limits of the standard Garbage Collector (GC) by moving data management off-heap while maintaining a high-level, object-oriented programming model with complete ACID durability and concurrency control.

It is designed for **mission-critical applications** requiring consistent zero-latency, large dataset handling (TB+), and high concurrency without the "stop-the-world" pauses associated with large managed heaps.

Please, read the README.md file of the GitHub repository.

### 22 january 2026
First "real" results of the GBO engine, on an I9 9900K 8 cores CPU / NvME SSD :
- **1M+ insert** of objects in ACID transactional way, per second.
- **100M+ entities Linq enumerations**, per second, using 8 threads. This can be done un single thread / multi-shard (maps are internally sharded).
- This demonstrate the critical **robust, flexible, lock-free storage engine** that gracefully scale both in throughput and data size.
- The **In-Memory Index efficiency**: 1.5 GB to index 100M ghosts.

This primarily results are the basis for the complete implementation of the complete, documented (White Paper to come) technology.

### This benchmarks
The state of the current benchmark project code demonstrate the core, critical memory management that is the foundation of this technology :
- **Ensuring memory access security by design** : it use the GC as end controller to release the unmanaged memory. It is compliant with the .Net native threading and memory management principles.
- The Virtual Memory is **write protected** to avoid unmanaged write errors damages.
- Provide **leak free MVCC views** : efficient versioned, Epoch based, cache friendly ghost map.
- **Sharded Maps** for blazing fast filters Linq queries (up to 100M entries predicates using 8 cores).
- **ACID transactions** : disk flush based. Is compliant with crash recovery, replication and multi-process write enabled sharing.

### Comparison with FASTER
Raw data managed using FASTER is fast - faster than GBO. But any attempt to build the GBO advanced high level features on top of FASTER will provide a order of magnitude, or more, less performances. This is true for few obvious reasons :
- FASTER is not transactional - in the meaning of a MVCC snapshot view of data from a point in time.
- FASTER is not ACID : the ACID Commit concept do not even exist - he can recover stale writes, but is not ACID.
- FASTER do not manage high level entities : it provide a Key / Value API. Any high level object management induce serialization, which mean lot of memory copy and objects allocations.
- FASTER is low level: GBO provide both a high end, high level programming experience **AND** "zero-copy" and "zero-allocation" object management. The complete API is "ambiant" : any object modification (creation, modification, deletion) need zero API call (no Insert, or Update to call on entities).

### Comparison with LMDB / MDBX
While LMDB provide the highest read performances, the write performances are far from what is providing GBO :
- The B+Tree data structure with COW is an a powerful design, but have a terrible write amplification problem.
- The long lived read transaction lead to unmanageable fragmentation and store size dramatic increase. The GBO store engine is managing this without the same problems.
- The GBO object store do not provide sorted key maps : it implement indexes (sorted based, or bucket based) as an higher order feature, because the engine is a fast and flexible object store. It is possible to develop a B-Tree on top of GBO, where nodes are GBO objects.
- Defragmentation and compacting is a lot simpler in GBO. GBO can optimize tables for cache locality.
- GBO Segment system provide a fast (it is only a matter of append segment raw data from an another one) and simple replication principle.

### Comparison with Postgre / EF Core
Comparison is useless :
- GBO is really - really - more simple: full C#, no IQueryable limitations, ambiant API, no flaws linked to Unit of Work pattern, a complete, full featured relational engine (to come), no thread safety concerns.
- 1 to 3 order of magnitude faster: zero latency, no N+1 loading problem, zero GC overhead.

There is no comparison even possible.

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
GhostBodyObject is not just an ORM or a Cache. It is a **Memory Engine** for .NET.
1.  **Stop optimizing GC**: Don't waste time tuning GC server modes. Use Off-Heap memory.
2.  **Code like Domain Experts**: Define your Entities (Bodies) with rich behavior. GBO handles the "Ghost" storage transparently.
3.  **Unified Data**: One single API for In-Memory, Virtual Memory, and Persistent storage.

### The next major implementations
To complete this highly critical, first implementation, I have to write this key features :
- The **Compactor** background processing, to reduce fragmentation and perform garbage collection.
- The **Reload / Recovery** of a persisted repository using compact, near instantly loaded on disk segment indexes, generated when a segment is "sealed".
- The **Multi-process Sharing** of Segment chains and complete repository sync with mutations.