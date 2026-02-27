package com.ghostvpn.android.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable

private val GhostColorScheme = darkColorScheme(
    primary = GhostPrimary,
    onPrimary = GhostOnPrimary,
    secondary = GhostPrimaryDark,
    background = GhostBackground,
    onBackground = GhostText,
    surface = GhostSurface,
    onSurface = GhostText,
    surfaceVariant = GhostSurfaceAlt,
    onSurfaceVariant = GhostTextMuted,
)

@Composable
fun GhostVpnTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = GhostColorScheme,
        typography = MaterialTheme.typography,
        content = content
    )
}
