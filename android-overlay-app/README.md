# Game Overlay — Android

Real floating overlay app (draws on top of other apps/games using the standard
`SYSTEM_ALERT_WINDOW` permission — the same mechanism Facebook Messenger chat
heads and other legitimate overlay apps use).

## What is REAL vs. simulated in this project
| Stat | Status | Source |
|---|---|---|
| RAM used | ✅ Real | `ActivityManager.MemoryInfo` |
| Battery temperature (shown as TEMP) | ✅ Real | `BatteryManager` sticky intent |
| CPU % | ⚠️ Real where allowed | `/proc/stat` deltas — some OEMs/Android 8+ restrict this for non-system apps; app shows `N/A` rather than a fake number when it can't read it |
| FPS | ⚠️ Real, but only the HUD's own render rate | `Choreographer` — Android does **not** expose another app/game's FPS to a normal app without root |
| GPU % | ❌ Not included | No public non-root API exists on Android for this — not faked |

## How to build
1. Install **Android Studio** (free).
2. Open this folder as a project (`File → Open`).
3. Let Gradle sync (needs internet the first time to fetch dependencies).
4. Run on a device/emulator (`minSdk 26`, i.e. Android 8.0+).
5. In the app: tap **"อนุญาตให้แสดงทับแอปอื่น"** → grant the permission in system settings →
   go back → tap **"เปิด Overlay"**. A draggable HUD will appear over other apps.

I can't compile this into a signed `.apk` myself — Android builds require the
Android SDK/Gradle plus network access to fetch dependencies, which this
sandbox doesn't have. Android Studio does all of that automatically.
