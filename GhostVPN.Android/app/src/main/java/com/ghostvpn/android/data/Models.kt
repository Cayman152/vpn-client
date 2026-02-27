package com.ghostvpn.android.data

import kotlinx.serialization.Serializable

const val DEFAULT_CONFIG_ID = "latvia-default"

@Serializable
data class VpnConfiguration(
    val id: String,
    val name: String,
    val country: String,
    val protocol: String = "VLESS",
    val url: String
)

@Serializable
data class RoutingPresets(
    val cinemaDirect: Boolean = true,
    val banksDirect: Boolean = true,
    val providersDirect: Boolean = true,
    val gtaDirect: Boolean = false,
    val discordProxy: Boolean = true
)

@Serializable
data class AppState(
    val isConnected: Boolean = false,
    val configurations: List<VpnConfiguration> = listOf(defaultLatviaConfig()),
    val activeConfigId: String = DEFAULT_CONFIG_ID,
    val routingPresets: RoutingPresets = RoutingPresets(),
    val logs: List<String> = listOf("Ghost VPN Android запущен")
)

fun defaultLatviaConfig() = VpnConfiguration(
    id = DEFAULT_CONFIG_ID,
    name = "Латвия",
    country = "Латвия",
    protocol = "VLESS",
    url = "vless://example@ghostvpn.lv:443?security=tls#Латвия"
)
