package com.ghostvpn.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.material3.windowsizeclass.calculateWindowSizeClass
import com.ghostvpn.android.ui.GhostVpnApp
import com.ghostvpn.android.ui.theme.GhostVpnTheme
import com.ghostvpn.android.viewmodel.MainViewModel

class MainActivity : ComponentActivity() {

    private val viewModel by viewModels<MainViewModel> {
        MainViewModel.factory(applicationContext)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        setContent {
            val windowSizeClass = calculateWindowSizeClass(this)
            GhostVpnTheme {
                GhostVpnApp(
                    windowSizeClass = windowSizeClass,
                    viewModel = viewModel
                )
            }
        }
    }
}
