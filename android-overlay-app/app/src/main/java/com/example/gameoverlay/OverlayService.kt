package com.example.gameoverlay

import android.app.*
import android.content.Intent
import android.graphics.PixelFormat
import android.os.*
import android.view.Choreographer
import android.view.Gravity
import android.view.LayoutInflater
import android.view.MotionEvent
import android.view.View
import android.view.WindowManager
import android.widget.TextView
import androidx.core.app.NotificationCompat
import java.util.Locale

class OverlayService : Service() {

    private lateinit var windowManager: WindowManager
    private var hudView: View? = null
    private lateinit var params: WindowManager.LayoutParams

    private lateinit var stats: HardwareStats
    private val handler = Handler(Looper.getMainLooper())

    // --- real FPS of the HUD's own rendering, via Choreographer ---
    private var frameCount = 0
    private var lastFpsTime = 0L
    private var currentFps = 0.0
    private val choreographer = Choreographer.getInstance()
    private val frameCallback = object : Choreographer.FrameCallback {
        override fun doFrame(frameTimeNanos: Long) {
            frameCount++
            val now = System.nanoTime()
            if (lastFpsTime == 0L) lastFpsTime = now
            val elapsedMs = (now - lastFpsTime) / 1_000_000.0
            if (elapsedMs >= 1000) {
                currentFps = frameCount * 1000.0 / elapsedMs
                frameCount = 0
                lastFpsTime = now
            }
            choreographer.postFrameCallback(this)
        }
    }

    private val updateRunnable = object : Runnable {
        override fun run() {
            updateReadings()
            handler.postDelayed(this, 500) // matches the 500ms refresh spec
        }
    }

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        stats = HardwareStats(this)
        windowManager = getSystemService(WINDOW_SERVICE) as WindowManager
        startForegroundWithNotification()
        addHudView()
        choreographer.postFrameCallback(frameCallback)
        handler.post(updateRunnable)
    }

    override fun onDestroy() {
        super.onDestroy()
        handler.removeCallbacks(updateRunnable)
        choreographer.removeFrameCallback(frameCallback)
        hudView?.let { runCatching { windowManager.removeView(it) } }
    }

    private fun startForegroundWithNotification() {
        val channelId = "overlay_channel"
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                channelId, "Game Overlay", NotificationManager.IMPORTANCE_MIN
            )
            (getSystemService(NOTIFICATION_SERVICE) as NotificationManager)
                .createNotificationChannel(channel)
        }
        val notification = NotificationCompat.Builder(this, channelId)
            .setContentTitle("Game Overlay กำลังทำงาน")
            .setSmallIcon(android.R.drawable.ic_menu_view)
            .setOngoing(true)
            .build()
        startForeground(1, notification)
    }

    private fun addHudView() {
        val inflater = getSystemService(LAYOUT_INFLATER_SERVICE) as LayoutInflater
        val view = inflater.inflate(R.layout.overlay_hud, null)
        hudView = view

        val overlayType =
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
                WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY
            else
                @Suppress("DEPRECATION") WindowManager.LayoutParams.TYPE_PHONE

        params = WindowManager.LayoutParams(
            WindowManager.LayoutParams.WRAP_CONTENT,
            WindowManager.LayoutParams.WRAP_CONTENT,
            overlayType,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or
                WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN,
            PixelFormat.TRANSLUCENT
        )
        params.gravity = Gravity.TOP or Gravity.START
        params.x = 16
        params.y = 120

        windowManager.addView(view, params)
        attachDrag(view)

        view.findViewById<View>(R.id.btnClose).setOnClickListener {
            stopSelf()
        }
    }

    /** Real drag-to-move using ACTION_MOVE deltas against the WindowManager params. */
    private fun attachDrag(view: View) {
        val handle = view.findViewById<View>(R.id.dragHandle)
        var initialX = 0
        var initialY = 0
        var touchX = 0f
        var touchY = 0f

        handle.setOnTouchListener { _, event ->
            when (event.action) {
                MotionEvent.ACTION_DOWN -> {
                    initialX = params.x; initialY = params.y
                    touchX = event.rawX; touchY = event.rawY
                    true
                }
                MotionEvent.ACTION_MOVE -> {
                    params.x = initialX + (event.rawX - touchX).toInt()
                    params.y = initialY + (event.rawY - touchY).toInt()
                    windowManager.updateViewLayout(view, params)
                    true
                }
                else -> false
            }
        }
    }

    private fun updateReadings() {
        val view = hudView ?: return

        view.findViewById<TextView>(R.id.valFps).text =
            String.format(Locale.US, "%.0f", currentFps)

        val cpu = stats.cpuUsagePercent()
        view.findViewById<TextView>(R.id.valCpu).text =
            if (cpu != null) String.format(Locale.US, "%.0f%%", cpu) else "N/A"

        val temp = stats.batteryTempCelsius()
        view.findViewById<TextView>(R.id.valTemp).text =
            if (temp != null) String.format(Locale.US, "%.0f°C", temp) else "N/A"

        val (used, _) = stats.ramUsedGb()
        view.findViewById<TextView>(R.id.valRam).text =
            String.format(Locale.US, "%.1fGB", used)
    }
}
