package com.ghostvpn.android.vpn

import android.net.Uri

data class VlessEndpoint(
    val id: String,
    val host: String,
    val port: Int,
    val address: String,
    val security: String,
    val network: String,
    val sni: String?,
    val flow: String?,
    val encryption: String,
    val wsPath: String?,
    val wsHost: String?,
    val grpcServiceName: String?,
    val grpcAuthority: String?,
    val realityPublicKey: String?,
    val realityShortId: String?,
    val realityFingerprint: String?,
    val realitySpiderX: String?
)

object VlessUriParser {

    fun parse(rawUrl: String): VlessEndpoint {
        val uri = Uri.parse(rawUrl.trim())
        require(uri.scheme.equals("vless", ignoreCase = true)) { "Поддерживается только VLESS" }

        val userInfo = uri.encodedUserInfo.orEmpty()
        val id = userInfo.substringBefore(':').ifBlank {
            throw IllegalArgumentException("VLESS URL не содержит UUID")
        }

        val host = uri.host.orEmpty()
        require(host.isNotBlank()) { "VLESS URL не содержит host" }

        val security = query(uri, "security")?.lowercase() ?: "none"
        val network = query(uri, "type")?.lowercase() ?: "tcp"

        return VlessEndpoint(
            id = id,
            host = host,
            port = if (uri.port > 0) uri.port else 443,
            address = host,
            security = security,
            network = network,
            sni = query(uri, "sni") ?: query(uri, "host"),
            flow = query(uri, "flow"),
            encryption = query(uri, "encryption") ?: "none",
            wsPath = query(uri, "path"),
            wsHost = query(uri, "host"),
            grpcServiceName = query(uri, "serviceName") ?: query(uri, "service_name"),
            grpcAuthority = query(uri, "authority"),
            realityPublicKey = query(uri, "pbk"),
            realityShortId = query(uri, "sid"),
            realityFingerprint = query(uri, "fp"),
            realitySpiderX = query(uri, "spx")
        )
    }

    private fun query(uri: Uri, key: String): String? {
        val value = uri.getQueryParameter(key)
        if (value.isNullOrBlank()) {
            return null
        }
        return value.trim()
    }
}
