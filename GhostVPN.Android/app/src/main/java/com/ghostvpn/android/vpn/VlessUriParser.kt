package com.ghostvpn.android.vpn

import java.net.URI
import java.net.URLDecoder
import java.nio.charset.StandardCharsets

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
        val source = rawUrl.trim()
        val uri = URI(source)
        require(uri.scheme.equals("vless", ignoreCase = true)) { "Поддерживается только VLESS" }

        val authority = uri.rawAuthority.orEmpty()
        val userInfo = authority.substringBefore('@', "")
        val id = decode(userInfo.substringBefore(':', "")).ifBlank {
            throw IllegalArgumentException("VLESS URL не содержит UUID")
        }

        val host = uri.host ?: authority.substringAfter('@', "").substringBefore(':')
        require(host.isNotBlank()) { "VLESS URL не содержит host" }

        val query = parseQuery(uri.rawQuery)
        val security = query["security"]?.lowercase() ?: "none"
        val network = query["type"]?.lowercase() ?: "tcp"

        return VlessEndpoint(
            id = id,
            host = host,
            port = if (uri.port > 0) uri.port else 443,
            address = host,
            security = security,
            network = network,
            sni = query["sni"] ?: query["host"],
            flow = query["flow"],
            encryption = query["encryption"] ?: "none",
            wsPath = query["path"],
            wsHost = query["host"],
            grpcServiceName = query["serviceName"] ?: query["service_name"],
            grpcAuthority = query["authority"],
            realityPublicKey = query["pbk"],
            realityShortId = query["sid"],
            realityFingerprint = query["fp"],
            realitySpiderX = query["spx"]
        )
    }

    private fun parseQuery(rawQuery: String?): Map<String, String> {
        if (rawQuery.isNullOrBlank()) {
            return emptyMap()
        }

        return rawQuery.split('&')
            .mapNotNull { part ->
                if (part.isBlank()) return@mapNotNull null
                val key = decode(part.substringBefore('=')).trim()
                if (key.isBlank()) return@mapNotNull null
                val value = decode(part.substringAfter('=', "")).trim()
                key to value
            }
            .toMap()
    }

    private fun decode(value: String): String {
        if (value.isEmpty()) {
            return value
        }
        return URLDecoder.decode(value, StandardCharsets.UTF_8.name())
    }
}
