# Game Overlay — Windows

A real always-on-top, draggable HUD (borderless WinForms window) showing live
hardware sensor data.

## What is REAL vs. limited in this project
| Stat | Status | Source |
|---|---|---|
| CPU %, RAM used | ✅ Real | `LibreHardwareMonitorLib` (open-source sensor library) |
| CPU temp, GPU temp, VRAM used | ✅ Real, **requires Admin** | same library — most temperature sensors on Windows are only readable by processes running as Administrator |
| FPS | ✅ Real, but needs **RTSS** installed & running | Reads RTSS's public shared-memory API (`RTSSSharedMemoryV2`) — the standard way third-party overlays get FPS without writing their own DirectX/Vulkan hooking. Download RTSS free from Guru3D. Without RTSS running, FPS shows `N/A*`. |

## How to build
1. Install the free **.NET 8 SDK** and (optionally) **Visual Studio 2022** on a
   Windows machine.
2. Open `OverlayApp.csproj` in Visual Studio, or from a terminal:
   ```
   cd OverlayApp
   dotnet restore
   dotnet build -c Release
   ```
3. Run `dotnet run`, or launch the built `.exe` from `bin/Release/net8.0-windows/`.
4. **Run it as Administrator** to get real temperature readings (right-click →
   "Run as administrator"). Without admin, CPU/GPU temp will show `N/A`.
5. Install and start **RTSS** if you want real FPS numbers.

I can't compile this into a signed `.exe` myself — I'm running in a Linux
sandbox with no .NET SDK and no internet access to fetch the NuGet package,
so I can't build or test-run a Windows binary here. The code above is a
complete, real project; building it just needs the steps above on an actual
Windows machine.

## Drag / close
- Click-drag anywhere on the bar to move it.
- Right-click → "ปิดโปรแกรม" to exit.
