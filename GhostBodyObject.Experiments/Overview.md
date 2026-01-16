# Ghost Body Object

## What is Ghost Body Object?

### Purpose

The Ghost Body Object technology is a transactional object store.

## Basic Principles

The GBO technology is built upon a radical rethinking of how .NET objects are represented in memory to align with modern hardware realities. It relies on several core architectural pillars:

### 1. The Ghost-Body Separation

GBO splits the concept of an "object" into two distinct entities to optimize lifecycle and access:

- **The Ghost (The Data):** This is the physical storage of the entity. It is a single, contiguous block of memory (essentially a byte array) that contains all the object's state—primitive values, strings, and internal arrays.
    
- **The Body (The Interface):** This is a lightweight C# proxy class (a facade) that the developer interacts with. It contains no data itself; instead, it holds a pointer to the _Ghost_ and provides the properties and logic to interpret and manipulate that raw memory.
    

### 2. Contiguous Memory Layout ("The Pizza in a Box")

Standard .NET objects (POCOs) are often scattered across the heap, requiring the CPU to "chase pointers" to retrieve related data (e.g., a Customer object pointing to a separate String object for the Name).

- **GBO Approach:** A GBO object stores the entire entity "inline." Strings and arrays are stored within the same memory block as the parent object.
    
- **Benefit:** This ensures **Spatial Locality**, maximizing CPU cache efficiency (L1/L2) and eliminating the performance penalties of pointer chasing.
    

### 3. Zero-Serialization & In-Situ Access

Because the _Ghost_ is already a serialized block of bytes in memory, the system eliminates the traditional "serialization tax".

- **Mapping vs. Loading:** Data is not "deserialized" into a new object graph. Instead, a _Body_ is simply instantiated to "map" directly over the existing memory source.
    
- **Versatility:** This allows objects to be read and manipulated directly from various sources (Memory-Mapped Files, Network Buffers, or Unmanaged Memory) without copying data.
    

### 4. Garbage Collector (GC) Immunity

GBO significantly reduces GC pressure. Since the complex data (the _Ghost_) resides in a single binary block (often in native/unmanaged memory), it is invisible to the .NET Garbage Collector. The GC only tracks the lightweight _Body_ handles, allowing applications to manage terabytes of data without triggering "stop-the-world" pauses.

### 5. Transactional Virtual Memory

The Ghost Repository acts as a **Transactional Big Memory** system.

- **Virtual Memory:** It leverages the OS virtual memory pager to stream data transparently from disk to RAM, allowing datasets to exceed physical RAM capacity.
    
- **ACID & MVCC:** It provides full ACID (Atomicity, Consistency, Isolation, Durability) guarantees and uses Multi-Version Concurrency Control (MVCC). This ensures that readers see a consistent snapshot of data without blocking writers, and vice versa.