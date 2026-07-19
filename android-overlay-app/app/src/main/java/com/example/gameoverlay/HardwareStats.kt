package com.example.gameoverlay

import android.app.ActivityManager
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.BatteryManager
import java.io.RandomAccessFile

/**
 * Reads real device stats where the Android platform allows it without root.
 *
 * - RAM: real, via ActivityManager.MemoryInfo (always available).
 * - Battery temperature: real, via the sticky ACTION_BATTERY_CHANGED intent
 *   (this is the closest thing to a device "TEMP" reading available without root;
 *   it is battery temp, not a full SoC temp sensor).
 * - CPU usage: computed from /proc/stat deltas. On stock Android 8+ this file is
 *   often restricted for non-system apps and may return null — callers must
 *   handle that and show "N/A" rather than a fake number.
 * - GPU usage: intentionally NOT implemented. There is no public, non-root API
 *   for per-process or system GPU utilization on Android. Do not fabricate it.
 */
class HardwareStats(private val context: Context) {

    private var lastCpuTotal: Long = -1
    private var lastCpuIdle: Long = -1

    fun ramUsedGb(): Pair<Double, Double> {
        val am = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
        val info = ActivityManager.MemoryInfo()
        am.getMemoryInfo(info)
        val totalGb = info.totalMem / 1024.0 / 1024.0 / 1024.0
        val usedGb = (info.totalMem - info.availMem) / 1024.0 / 1024.0 / 1024.0
        return Pair(usedGb, totalGb)
    }

    fun batteryTempCelsius(): Double? {
        return try {
            val filter = IntentFilter(Intent.ACTION_BATTERY_CHANGED)
            val batteryStatus = context.registerReceiver(null, filter)
            val tenths = batteryStatus?.getIntExtra(BatteryManager.EXTRA_TEMPERATURE, Int.MIN_VALUE)
                ?: Int.MIN_VALUE
            if (tenths == Int.MIN_VALUE) null else tenths / 10.0
        } catch (e: Exception) {
            null
        }
    }

    /** Returns 0-100 CPU usage since the last call, or null if /proc/stat is unreadable. */
    fun cpuUsagePercent(): Double? {
        return try {
            val reader = RandomAccessFile("/proc/stat", "r")
            val load = reader.readLine()
            reader.close()

            val toks = load.split(Regex("\\s+")).drop(1).filter { it.isNotBlank() }
            val values = toks.take(7).map { it.toLong() }
            // user, nice, system, idle, iowait, irq, softirq
            val idle = values[3] + values.getOrElse(4) { 0 }
            val total = values.sum()

            if (lastCpuTotal < 0) {
                lastCpuTotal = total; lastCpuIdle = idle
                null // need a second sample to compute a delta
            } else {
                val totalDelta = total - lastCpuTotal
                val idleDelta = idle - lastCpuIdle
                lastCpuTotal = total; lastCpuIdle = idle
                if (totalDelta <= 0) null
                else (100.0 * (totalDelta - idleDelta) / totalDelta).coerceIn(0.0, 100.0)
            }
        } catch (e: Exception) {
            // Common on Android 8+ where /proc/stat is restricted for non-system apps.
            null
        }
    }
}
