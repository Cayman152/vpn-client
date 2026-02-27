package com.ghostvpn.android.vpn

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.net.VpnService
import android.os.Build
import android.system.Os
import androidx.core.app.NotificationCompat
import com.ghostvpn.android.MainActivity
import com.ghostvpn.android.R
import com.ghostvpn.android.data.VpnStartPayload
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.serialization.json.Json
import java.io.File

class GhostVpnService : VpnService() {

    private val serviceScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val json = Json { ignoreUnknownKeys = true }

    private var vpnProcess: Process? = null
    private var vpnTunFd: Int? = null
    private var logJob: Job? = null
    private var monitorJob: Job? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_CONNECT -> {
                val payloadJson = intent.getStringExtra(EXTRA_PAYLOAD)
                if (payloadJson.isNullOrBlank()) {
                    publishState(false, "Отсутствуют параметры подключения")
                    stopSelf()
                    return START_NOT_STICKY
                }

                val payload = runCatching {
                    json.decodeFromString(VpnStartPayload.serializer(), payloadJson)
                }.getOrElse {
                    publishState(false, "Не удалось разобрать параметры подключения")
                    stopSelf()
                    return START_NOT_STICKY
                }

                startForeground(NOTIFICATION_ID, createNotification("Подключение..."))
                serviceScope.launch {
                    connect(payload)
                }
            }

            ACTION_DISCONNECT -> {
                serviceScope.launch {
                    disconnect("VPN отключен")
                }
            }
        }

        return START_STICKY
    }

    override fun onRevoke() {
        serviceScope.launch {
            disconnect("VPN отключен системой Android")
        }
    }

    override fun onDestroy() {
        cleanupRuntime()
        serviceScope.cancel()
        super.onDestroy()
    }

    private suspend fun connect(payload: VpnStartPayload) {
        try {
            cleanupRuntime()

            val vpnInterface = Builder()
                .setSession("Ghost VPN")
                .setMtu(1500)
                .addAddress("172.19.0.2", 30)
                .addRoute("0.0.0.0", 0)
                .addRoute("::", 0)
                .addDnsServer("1.1.1.1")
                .addDnsServer("8.8.8.8")
                .apply {
                    runCatching { addDisallowedApplication(packageName) }
                }
                .establish()
                ?: throw IllegalStateException("Не удалось создать VPN-интерфейс")

            vpnTunFd = vpnInterface.detachFd()

            val configContent = XrayConfigFactory.build(payload)
            val configFile = File(filesDir, "xray-config.json")
            configFile.writeText(configContent)

            val xrayBinary = XrayBinaryManager(filesDir).ensureBinary()

            val process = ProcessBuilder(
                xrayBinary.absolutePath,
                "run",
                "-c",
                configFile.absolutePath
            )
                .redirectErrorStream(true)
                .apply {
                    environment()["xray.tun.fd"] = (vpnTunFd ?: 0).toString()
                    environment()["xray.location.asset"] = filesDir.absolutePath
                    environment()["xray.location.cert"] = filesDir.absolutePath
                }
                .start()

            vpnProcess = process

            logJob?.cancel()
            logJob = serviceScope.launch {
                process.inputStream.bufferedReader().useLines { lines ->
                    lines.forEach { line ->
                        if (line.isNotBlank() && isImportantLogLine(line)) {
                            val isError = line.contains("failed", ignoreCase = true) ||
                                line.contains("error", ignoreCase = true)
                            publishState(!isError, line)
                        }
                    }
                }
            }

            monitorJob?.cancel()
            monitorJob = serviceScope.launch {
                val exitCode = process.waitFor()
                publishState(false, "Ядро xray завершилось с кодом: $exitCode")
                disconnectInternal(null)
            }

            delay(1200)
            if (!process.isAlive) {
                throw IllegalStateException("Ядро xray не запустилось")
            }

            updateNotification("Подключено: ${payload.configuration.name}")
            publishState(true, "VPN подключен: ${payload.configuration.name}")
        } catch (t: Throwable) {
            publishState(false, t.message ?: "Ошибка запуска VPN")
            disconnectInternal(null)
        }
    }

    private fun disconnect(reason: String?) {
        disconnectInternal(reason)
    }

    private fun disconnectInternal(reason: String?) {
        cleanupRuntime()

        if (reason != null) {
            publishState(false, reason)
        }

        stopForeground(STOP_FOREGROUND_REMOVE)
        stopSelf()
    }

    private fun cleanupRuntime() {
        monitorJob?.cancel()
        monitorJob = null

        logJob?.cancel()
        logJob = null

        vpnProcess?.destroy()
        vpnProcess = null

        vpnTunFd?.let { fd ->
            runCatching { Os.close(fd) }
        }
        vpnTunFd = null
    }

    private fun publishState(connected: Boolean, message: String) {
        val intent = Intent(ACTION_STATE).apply {
            setPackage(packageName)
            putExtra(EXTRA_CONNECTED, connected)
            putExtra(EXTRA_MESSAGE, message)
        }
        sendBroadcast(intent)
    }

    private fun isImportantLogLine(line: String): Boolean {
        val lower = line.lowercase()
        return lower.contains("started") ||
            lower.contains("ready") ||
            lower.contains("failed") ||
            lower.contains("error")
    }

    private fun createNotification(content: String): Notification {
        ensureNotificationChannel()
        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_ghostvpn_logo)
            .setContentTitle("Ghost VPN")
            .setContentText(content)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .setOngoing(true)
            .setContentIntent(pendingIntent)
            .build()
    }

    private fun updateNotification(content: String) {
        val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        manager.notify(NOTIFICATION_ID, createNotification(content))
    }

    private fun ensureNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            return
        }

        val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val channel = NotificationChannel(
            CHANNEL_ID,
            "Ghost VPN",
            NotificationManager.IMPORTANCE_LOW
        ).apply {
            description = "Сервис VPN"
        }
        manager.createNotificationChannel(channel)
    }

    companion object {
        const val ACTION_CONNECT = "com.ghostvpn.android.vpn.CONNECT"
        const val ACTION_DISCONNECT = "com.ghostvpn.android.vpn.DISCONNECT"
        const val ACTION_STATE = "com.ghostvpn.android.vpn.STATE"

        const val EXTRA_PAYLOAD = "payload"
        const val EXTRA_CONNECTED = "connected"
        const val EXTRA_MESSAGE = "message"

        private const val CHANNEL_ID = "ghost_vpn_channel"
        private const val NOTIFICATION_ID = 2007

        fun buildConnectIntent(context: Context, payloadJson: String): Intent {
            return Intent(context, GhostVpnService::class.java).apply {
                action = ACTION_CONNECT
                putExtra(EXTRA_PAYLOAD, payloadJson)
            }
        }

        fun buildDisconnectIntent(context: Context): Intent {
            return Intent(context, GhostVpnService::class.java).apply {
                action = ACTION_DISCONNECT
            }
        }
    }
}
