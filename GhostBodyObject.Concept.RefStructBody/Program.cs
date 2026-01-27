// See https://aka.ms/new-console-template for more information
using GhostBodyObject.Common.Memory;
using GhostBodyObject.Concepts.RefStructBody.Body;
using GhostBodyObject.HandWritten.Benchmarks.BloggerApp;
using System.Diagnostics;

Console.WriteLine("Hello, World!");

new TestBodyStructAccess().EnumerationTest();
new TestBodyStructAccess().TestBodyStructAllocator();
new TestBodyStructAccess().Test();

unsafe
{

    CrmUser user = new CrmUser();

    var mem = Process.GetCurrentProcess().PrivateMemorySize64;
    Console.WriteLine($"Memory = {mem / 1000}");

    var txn = new BloggerTransaction();

    var sw = Stopwatch.StartNew();
    var owner = new object();
    var n = 0;
    var ntxn = 0;
    while (sw.ElapsedMilliseconds < 10_000)
    {
        var mm = TransientGhostMemoryAllocator.Allocate(104);
        txn.AllocateSlot(mm.Ptr, IntPtr.Zero, mm.MemoryOwner);
        if (n % 20 == 0)
        {
            txn = new BloggerTransaction();
            ntxn++;
        }
        n++;
    }
    Console.WriteLine($"Total AllocateSlot = {n}   Txn = {ntxn}");

    txn = new BloggerTransaction();
    sw = Stopwatch.StartNew();
    var list = new List<nint>();
    while (sw.ElapsedMilliseconds < 1000)
    {
        list.Add((nint)txn.AllocateSlot(null, IntPtr.Zero, owner));
        n++;
    }

    list = list.Shuffle().ToList();
    sw = Stopwatch.StartNew();
    owner = new object();
    n = 0;
    var l = list.Count;
    for (int i=0;i<list.Count; i++)
    {
        var ptr = (LargeEntitySlot*)list[i];
        Interlocked.Increment(ref ptr->Sequence);
        var mm = TransientGhostMemoryAllocator.Allocate(104);
        txn.UpdateEntity((LargeEntitySlot*)list[i], mm.Ptr, IntPtr.Zero, mm.MemoryOwner);
        Interlocked.Increment(ref ptr->Sequence);
    }
    Console.WriteLine($"Delay for UpdateEntity = {sw.ElapsedMilliseconds}");

    mem = Process.GetCurrentProcess().PrivateMemorySize64;

    list.Clear();
    list = null;

    do
    {
        sw.Restart();
        txn = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.WaitForFullGCComplete();
        Console.WriteLine($"Delay for GC = {sw.ElapsedMilliseconds}");
        Thread.Sleep(500);
    }
    while (Process.GetCurrentProcess().PrivateMemorySize64 > mem * 0.15);

    mem = Process.GetCurrentProcess().PrivateMemorySize64;
    Console.WriteLine($"Memory = {mem / 1000}");

    var pocos = new List<POCOUser>();
    sw = Stopwatch.StartNew();
    for (int i = 0; i < l; i++)
        pocos.Add(new POCOUser());
    Console.WriteLine($"Delay to create Pocos = {sw.ElapsedMilliseconds}");

    Thread.Sleep(1000);
    pocos.Clear();

    do
    {
        sw.Restart();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.WaitForFullGCComplete();
        Console.WriteLine($"Delay for GC = {sw.ElapsedMilliseconds}");
        Thread.Sleep(500);
    }
    while (Process.GetCurrentProcess().PrivateMemorySize64 > mem * 0.15);

    mem = Process.GetCurrentProcess().PrivateMemorySize64;
    Console.WriteLine($"Done = {mem / 1000}");

    Console.ReadKey();
}

public class POCOUser
{
    public string UserName { get; set; }
}