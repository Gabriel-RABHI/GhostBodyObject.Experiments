using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Helpers
{
    public unsafe static class MemoryFlusher
    {
        // --- Linux Constants ---
        // MS_ASYNC (1) - Schedule write, return immediately.
        // MS_SYNC (4)  - Request write, wait for completion (Blocking).
        // MS_INVALIDATE (2) - Invalidate other mappings.
        private const int MS_SYNC = 4;
        private const int MS_ASYNC = 1;

        // --- P/Invoke Definitions ---

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlushViewOfFile(void* lpBaseAddress, nuint dwNumberOfBytesToFlush);

        [DllImport("libc", SetLastError = true)]
        private static extern int msync(void* addr, nuint len, int flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int getpagesize();

        // Cache the page size to avoid repeated system calls on Linux
        private static readonly nuint _pageSize;

        static MemoryFlusher()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                _pageSize = (nuint)getpagesize();
            }
        }

        /// <summary>
        /// Flushes a specific range of memory to the OS file cache (and disk, depending on OS).
        /// Safe to call with unaligned pointers.
        /// </summary>
        public static void FlushRange(byte* pointer, nuint length, bool flushToDisk = true)
        {
            if (OperatingSystem.IsWindows())
            {
                // Windows handles alignment automatically.
                // Note: This flushes to the OS File Cache. For absolute 100% durability 
                // against power loss, you still need to call FlushFileBuffers on the file handle occasionally.
                if (!FlushViewOfFile(pointer, length))
                {
                    ThrowLastWin32Error();
                }
            } else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Linux msync requires the address to be aligned to the page size.
                // We must calculate the start of the page containing our data.

                // 1. Calculate the offset from the nearest previous page boundary
                nuint alignmentOffset = (nuint)pointer % _pageSize;

                // 2. Move the pointer back to the page boundary
                byte* alignedPointer = pointer - alignmentOffset;

                // 3. Increase length to cover the extra bytes we included at the start
                nuint alignedLength = length + alignmentOffset;

                // 4. Perform the sync
                // MS_SYNC: Similar to 'fsync', waits for physical write. 
                // MS_ASYNC: Similar to 'Write' + returning, lets OS decide when to write.
                int flags = flushToDisk ? MS_SYNC : MS_ASYNC;

                if (msync(alignedPointer, alignedLength, flags) != 0)
                {
                    ThrowLastLibCError();
                }
            } else
            {
                throw new PlatformNotSupportedException("Only Windows and Linux/MacOS are supported for granular flushing.");
            }
        }

        private static void ThrowLastWin32Error()
        {
            throw new IOException($"Windows Flush failed: {Marshal.GetLastWin32Error()}");
        }

        private static void ThrowLastLibCError()
        {
            throw new IOException($"Linux msync failed: {Marshal.GetLastSystemError()}");
        }
    }
}
