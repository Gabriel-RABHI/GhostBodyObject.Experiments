# GhostBodyObject : Experiments
## Introduction
This code repository is a first step, exploratory purpose - implementations, tests, benchmarks - for the GBO (Ghost Body Object) technology. The GBO  technology introduces a new CPU cache and I/O friendly C# data structure and an accompanying repository system designed to change how applications manage memory, data and concurrency. This data structure is complementary to the existing class, struct, and record type declarations. By rethinking the memory layout of native objects, in many common use cases it is faster than POCO (Plain Old CLR Object). It reduce garbage collector overhead and removes the need for serialization.

This new data structure enables the creation of ultra-high-performance, latency-free, persistent, transactional, and distributed object repositories that reside in virtual memory. It allows .NET applications to manage hundreds of gigabytes of data as native C# objects collections with the robust ACID/MVCC transactional guarantees demanded by mission-critical business software. These objects are not transient representations of data pulled from and pushed to a storage system - they are physically the data itself. This object store eliminates garbage collector overhead.

The GBO technology bridges the gap between application logic and data storage, removing the complexities and performance bottlenecks of traditional Database Management Systems (DBMS). It eliminate both the overhead of ORMs, and flaws and mental burden of the Unit-Of-Work pattern. The data model - defined in pure C# - is a mix of Object Oriented, Edge-based Graph, Entity Component Systems and Key-Value pairs. An application become a dedicated business domain database engine entirely written in C#. It bring together job and message queues, caches, event logs, time-serie and a constrained, indexed data model in an unified, distributed, transactional, shared virtual-memory system.

**More in the Ghost Body Object white paper.**

## Step #1
### Goals
This step goals is to show the very basics GBO object principle, viability and benefits.
### Implementation
Create a reference in-memory Body-Ghost system that show :
- POCO transparent usage.
- Concurrency model.
- Value properties.
- `GhostString` and `GhostArray` types with Properties mutations.
- `VectorTable` principle : initial Ghost, transition.
- Arena Memory allocation principle (over-allocation) and GC compliant memory management.
- First hand made, then builded with a Code Generator from an `interface` model.
### Benchmarks
- Create, configure, update, delete objects : POCO vs GBO object.
- GC Collect performances.