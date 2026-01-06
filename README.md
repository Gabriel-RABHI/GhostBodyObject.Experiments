# GhostBodyObject : Experiments
## Introduction
This code repository is a first step, exploratory purpose - implementations, tests, benchmarks - for the GBO (Ghost Body Object) technology. This project is the basis for the production ready version.

The GBO  technology introduces a new CPU cache and I/O friendly C# data structure and an accompanying repository system designed to change how applications manage memory, data and concurrency. This data structure is complementary to the existing class, struct, and record type declarations. By rethinking the memory layout of native objects, in many common use cases it is faster than POCO (Plain Old CLR Object). It reduce garbage collector overhead and removes the need for serialization.

This new data structure enables the creation of ultra-high-performance, latency-free, persistent, transactional, and distributed object repositories that reside in virtual memory. It allows .NET applications to manage hundreds of gigabytes of data as native C# objects collections with the robust ACID/MVCC transactional guarantees demanded by mission-critical business software. These objects are not transient representations of data pulled from and pushed to a storage system - they are physically the data itself. This object store eliminates garbage collector overhead.

The GBO technology bridges the gap between application logic and data storage, removing the complexities and performance bottlenecks of traditional Database Management Systems (DBMS). It eliminate both the overhead of ORMs, and flaws and mental burden of the Unit-Of-Work pattern. The data model - defined in pure C# - is a mix of Object Oriented, Edge-based Graph, Entity Component Systems and Key-Value pairs. An application become a dedicated business domain database engine entirely written in C#. It bring together job and message queues, caches, event logs, time-serie and a constrained, indexed data model in an unified, distributed, transactional, shared virtual-memory system.

(...)

**More in the Ghost Body Object white paper.**

# The project status
## What is in the code ?

The solution is organized into 11 projects spanning core libraries, hand-written implementations (prototyping code generation patterns), tests, benchmarks, and experiments.

### Core Libraries

#### GhostBodyObject.Common
Low-level, high-performance building blocks shared across the solution:

- **Memory Management**
  - `ManagedArenaAllocator` - Thread-local arena allocator using pinned memory for fast, GC-friendly temporary allocations
  - `TransientGhostMemoryAllocator` - Specialized allocator for Ghost object memory with resize capabilities
  - `PinnedMemory<T>` - Struct wrapper for pinned byte arrays enabling safe pointer access
  - `FastBuffer` - Ultra-fast buffer read/write operations using `Unsafe` and `MemoryMarshal` for zero-overhead struct serialization
  - `ChunkSizeComputation` - Utilities for computing optimal memory chunk sizes

- **Synchronization Primitives**
  - `ShortSpinLock` - Minimal spinlock for narrow critical sections
  - `ShortCountSpinLock` - Spinlock allowing a configurable number of concurrent threads
  - `ShortRecursiveSpinLock` - Spinlock supporting recursive entry by the same thread
  - `ShortReadWriteSpinLock` - Reader-writer spinlock allowing multiple readers or a single writer
  - `ShortTicketSpinLock` - Ticket-based fair spinlock

- **Utilities**
  - `XorShift64` - Fast pseudo-random number generator for high-throughput ID generation

#### GhostBodyObject.Repository
The core Ghost-Body repository infrastructure:

- **Ghost Structures** (`Ghost/Structs/`)
  - `GhostId` - 16-byte unique identifier with embedded kind, type, timestamp, and random components
  - `GhostHeader` - 40-byte header containing ID, transaction ID, model version, status, flags, and mutation counter
  - `ArrayMapSmallEntry` / `ArrayMapLargeEntry` - Compact structures tracking variable-length arrays within Ghost memory

- **Ghost Values** (`Ghost/Values/`)
  - `GhostStringUtf16` - Ref struct wrapping UTF-16 string data in pinned memory with full string-like API and in-place modification support
  - `GhostStringUtf8` - UTF-8 variant of the Ghost string type
  - `GhostSpan` - Generic span wrapper for typed array access within Ghost memory

- **Ghost Constants** (`Ghost/Constants/`)
  - `GhostIdKind` - Enumeration of Ghost ID types (Entity, Component, Edge, etc.")
  - `GhostStatus` - Entity lifecycle states (Active, Deleted, etc.)

- **Ghost Utilities** (`Ghost/Utils/`)
  - `GhostHeaderIncrementor` - Atomic mutation counter management

- **Body Contracts** (`Body/Contracts/`)
  - `BodyBase` - Abstract base class for all Body implementations with array swap/append/prepend/insert/remove operations
  - `BodyUnion` - Union type for unsafe casting between Body types
  - `IEntityBody` - Interface for entity body implementations

- **Body Vectors** (`Body/Vectors/`)
  - `VectorTableHeader` - Header structure for virtual method dispatch tables containing function pointers for array operations
  - `VectorTableRecord` - Record tracking vector table metadata
  - `VectorTableRegistry` - Registry for building and managing version-specific vector tables

- **Repository Infrastructure** (`Repository/`)
  - `GhostRepository` - Main repository class managing segments, indexes, and transactions
  - `RepositoryTransaction` - Transaction management for ACID guarantees
  - `MemorySegment` / `MemorySegmentStore` / `MemorySegmentHolder` - Memory segment management for Ghost storage
  - `SegmentGhostMap` / `ShardedSegmentGhostMap` - High-performance hash maps for Ghost indexing with tombstone-aware probing and MVCC support
  - `RepositoryGhostIndex` - Index layer for Ghost lookup by ID

- **Model Attributes** (`Model/Attributs/`)
  - `EntityBodyAttribute` / `EntityPropertyAttribute` - Attributes for defining entity schemas

### Hand-Written Implementations

#### GhostBodyObject.HandWritten
Prototype implementations demonstrating code generation patterns:

- **Blogger Application** (`BloggerApp/`)
  - `BloggerContext` - Application context managing repositories and transactions
  - `BloggerRepository` / `BloggerTransaction` - Repository and transaction implementations
  - `BloggerBodyBase` / `BloggerGhostWriteLock` - Base classes with write scope guards
  - **Entities:**
    - `BloggerUser` - Full entity with scalar properties (DateTime, int, bool) and GhostString properties (FirstName, LastName, etc.")
    - `BloggerUserFlat` - Flat entity variant for comparison
    - `BloggerPost` - Blog post entity
    - `*_VectorTable` / `*_MappingVectorTableBuilder` - Version-specific vector tables and builders

- **Test Model** (`TestModel/`)
  - `ArraysAsStringsAndSpansSmall` / `ArraysAsStringsAndSpansLarge` - Test entities for array handling
  - `TestModelContext` / `TestModelRepository` / `TestModelTransaction` - Complete test model implementation

#### GhostBodyObject.Experiments
Exploratory code and prototypes:
- `BabyBody/Customer.cs` - Early prototype of the Body pattern

### Benchmark Infrastructure

#### GhostBodyObject.BenchmarkRunner
Custom brute-force benchmarking framework:
- `BenchmarkEngine` - Discovery, execution, and reporting engine with console UI (Spectre.Console)
- `BenchmarkBase` - Base class for benchmark implementations with result capture
- `BruteForceBenchmarkAttribute` - Attribute for marking benchmark methods with metadata
- `BenchmarkResult` / `BenchmarkResultRecord` - Result storage with memory and GC tracking
- `DefaultBenchmarks` - System-level baseline benchmarks

#### Benchmark Projects
- **GhostBodyObject.Common.Benchmarks** - Memory allocator and spinlock benchmarks
- **GhostBodyObject.Repository.Benchmarks** - GhostId and SegmentGhostMap benchmarks
- **GhostBodyObject.HandWritten.Benchmarks** - Entity creation, property access, and body implementation benchmarks

### Test Projects
- **GhostBodyObject.Common.Tests** - Tests for memory allocators, pinned memory, fast buffers, and spinlocks
- **GhostBodyObject.Repository.Tests** - Tests for GhostId, GhostSpan, GhostStringUtf8/Utf16, and SegmentGhostMap
- **GhostBodyObject.HandWritten.Tests** - Tests for Blogger entities, context, and array handling

### Key Implementation Highlights

1. **Zero-Copy String Access**: `GhostStringUtf16` provides string-like operations directly on pinned memory without copying, with implicit conversion to/from `string`.

2. **In-Place Array Modifications**: The `BodyBase` class implements sophisticated array swap/append/prepend/insert/remove operations that maintain proper alignment and efficiently shift subsequent arrays.

3. **Lock-Free Reading with MVCC**: `SegmentGhostMap` supports lock-free reads with retry-on-resize semantics, enabling snapshot isolation for enumerators.

4. **Version-Aware Schema Evolution**: Vector tables are versioned, allowing entities to be read using the schema version they were written with.

5. **Custom Spinlocks**: Purpose-built synchronization primitives optimized for short critical sections common in high-frequency data access patterns.

# Few Benchmarks Results
## Body vs POCO
*BloggerUser performance comparison with POCO*

This test create 10M GBO instance and set 12 strings properties, and do the same with POCO. This test show that GBO object is 3 time faster than POCO in this use case.

| Label                                                       | GC 0/1/2   | Memory | Op Cost   | Total Op/s | Factor   |
| ----------------------------------------------------------- | ---------- | ------ | --------- | ---------- | -------- |
| **Assign 12 strings properties for 10 000 000 BloggerUser** | 858/3/3    | 8,7 GB | 535,03 ns | 1 869,1k   | **1,00** |
| Assign 12 strings properties for 10 000 000 UserPOCO        | 866/865/11 | 6,7 GB | 1,50 µs   | 664,8k     | 0,36     |

*Retainned Garbage collection delay*

The same objects are retained in a List, then a `GC.Collect` is performed. This test show that the GBO objects (Body and Ghost), in-memory, are 10 time faster to scan than POCOs.

| Label                              | GC 0/1/2 | Memory | Op Cost  | Total Op/s | Factor   |
| ---------------------------------- | -------- | ------ | -------- | ---------- | -------- |
| **Collect retainned BloggerUser.** | 1/1/1    | 3,9 KB | 8,36 ns  | 119,6M     | **1,00** |
| Collect retainned UserPOCO.        | 1/1/1    | 2,9 KB | 72,98 ns | 13 701,8k  | 0,11     |

*Garbage collection delay*

This test show that real release is as fast for GBO objects than for POCOs.

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| Release retainned BloggerUser. | 1/1/1 | 4,2 KB | 0,06 ns | 16 329,2M | 0,99 |
| **Release retainned UserPOCO.** | 1/1/1 | 2,0 KB | 0,06 ns | 16 496,2M | **1,00** |

This tests show the efficiency of the GBO Objects. There is few more "magic" they are able to perform in field of string aggregation, insertions, append and prepend. The GhostString and GhostSpan are inner Ghost accessor and modifier, that present high performance profil. The zero copy string (UTF8 / UTF16) and span large operations set is a "next generation" data processing paradigm.

## Main Index
At the core of the GBO Repository is a Entity index. This index record all the existing Entities. It is a Hash based sharded Map, storing 8 bytes per entity (around 1.2 GB per 100M Ghosts registered). It is lock-free, transactional, support MVCC (multiple version per ID, for transactional views). The GBO use the garbage collector as a powerful lifetime Entity (the Body) manager to release the unseen memory space. This Map is a core piece of the GBO technology.

One of the most important feature is Enumeration and per ID resolution. For Enumeration, it scale linearly with threads.

For a 16M size Map, recording 10 526 173 entries, it takes 128 MB of memory. The enumerator performance, to enumerate all the 10M entries with 10% transactional multi-valued entries (for each entry, the enumerator return the visible version for the given transaction Id), here the throughput :

- 1 thread : **12M entries / s** (the map is cold - it is a cold start).
- 2 threads : **42M entries / s** (the map is then hot).
- 4 threads : **148M entries / s**
- 8 threads : **383M entries / s** (optimal will the processor is 8 cores)

This first result promise strong performances for transactional Linq queries and aggregations - with efficient parallelization options.

## Memory Allocation
Memory allocation of small to medium size memory blocks is a critical feature for GBO. The Arena based allocator provide high allocation performance of resizable memory blocs (12.5% memory over-allocation, 64 byte aligned for cache optimization).

#### 100 bytes - 1 thread(s)
*1 000 000 allocations/thread, 1 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **Ghost** | 16/16/16 | 122,0 MB | 11,92 ns | 83 906,0k | **1,00** |
| Native | None | 5,2 KB | 108,13 ns | 9 248,1k | 0,11 |
| byte[] | 15/0/0 | 122,1 MB | 13,65 ns | 73 264,9k | 0,87 |

#### 100 bytes - 2 thread(s)
*1 000 000 allocations/thread, 2 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **Ghost** | 26/26/26 | 244,2 MB | 6,86 ns | 145,8M | **1,00** |
| Native | None | 5,7 KB | 46,57 ns | 21 471,0k | 0,15 |
| byte[] | 30/0/0 | 244,1 MB | 15,51 ns | 64 487,8k | 0,44 |

#### 100 bytes - 4 thread(s)
*1 000 000 allocations/thread, 4 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **Ghost** | 32/32/32 | 488,5 MB | 9,35 ns | 107,0M | **1,00** |
| Native | None | 6,6 KB | 30,04 ns | 33 290,3k | 0,31 |
| byte[] | 61/0/0 | 488,3 MB | 9,68 ns | 103,3M | 0,96 |

## Lock Primitives
Tiny to small critical section are sometime as fast, or faster than lock-free data structures. Here the internal, spin based lock primitive used in the GBO engine, to provide near lock-free data processing.

#### Basic Exclusive Locks - 1 thread(s)
*10 000 000 lock/unlock cycles per thread, 10 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **ShortSpinLock** | None | 5,0 KB | 6,00 ns | 166,8M | **1,00** |
| .NET SpinLock | None | 10,7 KB | 15,14 ns | 66 048,7k | 0,40 |
| Monitor (lock) | None | 5,0 KB | 15,36 ns | 65 113,2k | 0,39 |

#### Basic Exclusive Locks - 2 thread(s)
*10 000 000 lock/unlock cycles per thread, 20 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **ShortSpinLock** | None | 5,5 KB | 7,70 ns | 129,8M | **1,00** |
| .NET SpinLock | None | 5,5 KB | 15,37 ns | 65 074,1k | 0,50 |
| Monitor (lock) | None | 11,2 KB | 17,47 ns | 57 226,7k | 0,44 |

#### Basic Exclusive Locks - 4 thread(s)
*10 000 000 lock/unlock cycles per thread, 40 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **ShortSpinLock** | None | 6,4 KB | 6,71 ns | 149,1M | **1,00** |
| .NET SpinLock | None | 6,4 KB | 15,33 ns | 65 252,6k | 0,44 |
| Monitor (lock) | None | 12,1 KB | 25,12 ns | 39 808,6k | 0,27 |

#### Basic Exclusive Locks - 8 thread(s)
*10 000 000 lock/unlock cycles per thread, 80 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **ShortSpinLock** | None | 13,7 KB | 7,07 ns | 141,4M | **1,00** |
| .NET SpinLock | None | 13,7 KB | 15,45 ns | 64 744,9k | 0,46 |
| Monitor (lock) | None | 19,3 KB | 27,11 ns | 36 889,7k | 0,26 |

#### Basic Exclusive Locks - 16 thread(s)
*10 000 000 lock/unlock cycles per thread, 160 000 000 total ops*

| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |
|-------|----------|--------|---------|------------|--------|
| **ShortSpinLock** | None | 29,6 KB | 7,51 ns | 133,2M | **1,00** |
| .NET SpinLock | None | 22,8 KB | 15,35 ns | 65 135,5k | 0,49 |
| Monitor (lock) | None | 34,0 KB | 26,08 ns | 38 344,6k | 0,29 |

# Next Steps
Validate the choices :
- Abandon of B+Tree to use pure Map (Hash) Memory Mapped File storage.
- Implement the Memory Mapped File, transactional store with ACID (Hash Txn Validation), MVCC support and COW (Copy On Write).
- Implement continuous, background store compaction and lineare Ghost - in table - continuous locality for fast enumerations.
- Implement concurrent Read / Write transactions with Write concurrency (Optimistic, Pessimistic conflict checks).
- Validate performances : expected amazingly fast write and read throughput.
