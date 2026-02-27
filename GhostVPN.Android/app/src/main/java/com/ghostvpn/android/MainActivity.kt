package com.ghostvpn.android

import android.Manifest
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.pm.PackageManager
import android.net.VpnService
import android.os.Bundle
import android.os.Build
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.core.content.ContextCompat
import androidx.compose.material3.windowsizeclass.ExperimentalMaterial3WindowSizeClassApi
import androidx.compose.material3.windowsizeclass.calculateWindowSizeClass
import com.ghostvpn.android.ui.GhostVpnApp
import com.ghostvpn.android.ui.theme.GhostVpnTheme
import com.ghostvpn.android.vpn.GhostVpnService
import com.ghostvpn.android.viewmodel.MainViewModel

class MainActivity : ComponentActivity() {

    private val viewModel by viewModels<MainViewModel> {
        MainViewModel.factory(applicationContext)
    }

    private var pendingPayloadJson: String? = null

    private val notificationPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (!granted) {
            viewModel.onVpnStateChanged(false, "Разрешение на уведомления отклонено")
        }
    }

    private val vpnPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) {
        val payload = pendingPayloadJson
        pendingPayloadJson = null

        if (it.resultCode == RESULT_OK && !payload.isNullOrBlank()) {
            startVpnService(payload)
        } else {
            viewModel.onVpnStateChanged(false, "Разрешение на VPN не выдано")
        }
    }

    private val vpnStateReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            if (intent?.action != GhostVpnService.ACTION_STATE) {
                return
            }
            val connected = intent.getBooleanExtra(GhostVpnService.EXTRA_CONNECTED, false)
            val message = intent.getStringExtra(GhostVpnService.EXTRA_MESSAGE).orEmpty()
            viewModel.onVpnStateChanged(connected, message)
        }
    }

    @OptIn(ExperimentalMaterial3WindowSizeClassApi::class)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        requestNotificationPermissionIfNeeded()

        setContent {
            val windowSizeClass = calculateWindowSizeClass(this)
            GhostVpnTheme {
                GhostVpnApp(
                    windowSizeClass = windowSizeClass,
                    viewModel = viewModel,
                    onToggleVpn = ::onToggleVpnRequested
                )
            }
        }
    }

    override fun onStart() {
        super.onStart()
        ContextCompat.registerReceiver(
            this,
            vpnStateReceiver,
            IntentFilter(GhostVpnService.ACTION_STATE),
            ContextCompat.RECEIVER_NOT_EXPORTED
        )
    }

    override fun onStop() {
        runCatching { unregisterReceiver(vpnStateReceiver) }
        super.onStop()
    }

    private fun onToggleVpnRequested(shouldConnect: Boolean) {
        if (!shouldConnect) {
            startService(GhostVpnService.buildDisconnectIntent(this))
            return
        }

        val payloadJson = viewModel.buildVpnStartPayloadJson()
        if (payloadJson.isNullOrBlank()) {
            viewModel.onVpnStateChanged(false, "Нет активной конфигурации")
            return
        }

        val prepareIntent = VpnService.prepare(this)
        if (prepareIntent != null) {
            pendingPayloadJson = payloadJson
            vpnPermissionLauncher.launch(prepareIntent)
        } else {
            startVpnService(payloadJson)
        }
    }

    private fun startVpnService(payloadJson: String) {
        ContextCompat.startForegroundService(
            this,
            GhostVpnService.buildConnectIntent(this, payloadJson)
        )
    }

    private fun requestNotificationPermissionIfNeeded() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
            return
        }
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) ==
            PackageManager.PERMISSION_GRANTED
        ) {
            return
        }
        notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
    }
}
