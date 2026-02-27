package com.ghostvpn.android.data

import kotlinx.serialization.Serializable

@Serializable
data class VpnStartPayload(
    val configuration: VpnConfiguration,
    val routingPresets: RoutingPresets
)
