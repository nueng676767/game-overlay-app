using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace GameOverlay;

/// <summary>
/// Reads live FPS from RivaTuner Statistics Server (RTSS) shared memory.
/// This is the standard, public, sanctioned way third-party overlays (not made
/// by the RTSS author) obtain per-game FPS on Windows without doing their own
/// DirectX/Vulkan API hooking — RTSS does the hooking once, and any app can
/// read its results from a memory-mapped file it exposes for exactly this
/// purpose. Requires RTSS to be installed and running
/// (https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/).
///
/// The struct layout below matches RTSS Shared Memory v2.x as documented in
/// the "rtss.h" header shipped inside the RTSS install folder. If a future
/// RTSS version changes the layout, re-check that header and adjust the
/// offsets/field order here to match — do not guess.
/// </summary>
public sealed class RtssReader
{
    private const string MapName = "RTSSSharedMemoryV2";

    [StructLayout(LayoutKind.Sequential)]
    private struct RTSS_SHARED_MEMORY_HEADER
    {
        public uint dwSignature;
        public uint dwVersion;
        public uint dwAppEntrySize;
        public uint dwAppArrOffset;
        public uint dwAppArrSize;
        public uint dwOSDEntrySize;
        public uint dwOSDArrOffset;
        public uint dwOSDArrSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct RTSS_SHARED_MEMORY_APP_ENTRY
    {
        public uint dwProcessID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        public byte[] szName;
        public uint dwFlags;
        public uint dwTime0;
        public uint dwTime1;
        public uint dwFrames;
        public uint dwFrameTime; // microseconds for the most recent frame
    }

    /// <summary>
    /// Returns instantaneous FPS for the foreground game process RTSS is
    /// currently tracking, or null if RTSS isn't running / has no active app.
    /// </summary>
    public double? GetForegroundFps()
    {
        try
        {
            using var mmf = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read);
            using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            accessor.Read(0, out RTSS_SHARED_MEMORY_HEADER header);
            if (header.dwSignature != 0x52545353) return null; // "RTSS" not present

            int entrySize = (int)header.dwAppEntrySize;
            int count = (int)(header.dwAppArrSize);
            long baseOffset = header.dwAppArrOffset;

            for (int i = 0; i < count; i++)
            {
                long offset = baseOffset + i * entrySize;
                accessor.Read(offset, out RTSS_SHARED_MEMORY_APP_ENTRY entry);
                if (entry.dwProcessID == 0) continue;

                // dwTime1 - dwTime0 spans dwFrames frames, both in milliseconds.
                if (entry.dwTime1 > entry.dwTime0 && entry.dwFrames > 0)
                {
                    double seconds = (entry.dwTime1 - entry.dwTime0) / 1000.0;
                    if (seconds > 0)
                        return entry.dwFrames / seconds;
                }
            }
            return null;
        }
        catch (FileNotFoundException)
        {
            return null; // RTSS not running
        }
        catch
        {
            return null;
        }
    }
}
