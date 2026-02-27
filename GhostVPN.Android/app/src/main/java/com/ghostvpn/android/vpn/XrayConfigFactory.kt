package com.ghostvpn.android.vpn

import com.ghostvpn.android.data.RoutingPresets
import com.ghostvpn.android.data.VpnStartPayload
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import kotlinx.serialization.json.putJsonArray

object XrayConfigFactory {

    private val json = Json {
        prettyPrint = true
        encodeDefaults = true
    }

    fun build(payload: VpnStartPayload): String {
        val endpoint = VlessUriParser.parse(payload.configuration.url)
        val routingRules = buildRoutingRules(payload.routingPresets)

        val config = buildJsonObject {
            put("log", buildJsonObject {
                put("loglevel", "warning")
            })

            putJsonArray("inbounds") {
                add(createTunInbound())
            }

            putJsonArray("outbounds") {
                add(createProxyOutbound(endpoint))
                add(createDirectOutbound())
                add(createBlockOutbound())
            }

            put("routing", buildJsonObject {
                put("domainStrategy", "AsIs")
                put("rules", routingRules)
            })

            put("dns", buildJsonObject {
                putJsonArray("servers") {
                    add(JsonPrimitive("1.1.1.1"))
                    add(JsonPrimitive("8.8.8.8"))
                }
            })
        }

        return json.encodeToString(JsonObject.serializer(), config)
    }

    private fun createTunInbound(): JsonObject {
        return buildJsonObject {
            put("tag", "tun")
            put("port", 0)
            put("protocol", "tun")
            put("settings", buildJsonObject {
                put("name", "xray0")
                put("MTU", 1500)
                put("userLevel", 8)
            })
            put("sniffing", buildJsonObject {
                put("enabled", true)
                putJsonArray("destOverride") {
                    add(JsonPrimitive("http"))
                    add(JsonPrimitive("tls"))
                }
            })
        }
    }

    private fun createProxyOutbound(endpoint: VlessEndpoint): JsonObject {
        val streamSettings = buildJsonObject {
            put("network", endpoint.network)

            when (endpoint.security) {
                "tls" -> {
                    put("security", "tls")
                    put("tlsSettings", buildJsonObject {
                        endpoint.sni?.let { put("serverName", it) }
                        put("allowInsecure", false)
                    })
                }

                "reality" -> {
                    put("security", "reality")
                    put("realitySettings", buildJsonObject {
                        endpoint.sni?.let { put("serverName", it) }
                        endpoint.realityFingerprint?.let { put("fingerprint", it) }
                        endpoint.realityPublicKey?.let { put("publicKey", it) }
                        endpoint.realityShortId?.let { put("shortId", it) }
                        endpoint.realitySpiderX?.let { put("spiderX", it) }
                    })
                }

                else -> put("security", "none")
            }

            when (endpoint.network) {
                "ws" -> put("wsSettings", buildJsonObject {
                    put("path", endpoint.wsPath ?: "/")
                    val host = endpoint.wsHost
                    if (!host.isNullOrBlank()) {
                        put("headers", buildJsonObject {
                            put("Host", host)
                        })
                    }
                })

                "grpc" -> put("grpcSettings", buildJsonObject {
                    put("serviceName", endpoint.grpcServiceName ?: "")
                    endpoint.grpcAuthority?.let { put("authority", it) }
                    put("multiMode", false)
                })
            }
        }

        return buildJsonObject {
            put("tag", "proxy")
            put("protocol", "vless")
            put("settings", buildJsonObject {
                putJsonArray("vnext") {
                    add(buildJsonObject {
                        put("address", endpoint.address)
                        put("port", endpoint.port)
                        putJsonArray("users") {
                            add(buildJsonObject {
                                put("id", endpoint.id)
                                put("encryption", endpoint.encryption)
                                endpoint.flow?.let { put("flow", it) }
                                put("level", 8)
                            })
                        }
                    })
                }
            })
            put("streamSettings", streamSettings)
            put("mux", buildJsonObject {
                put("enabled", false)
            })
        }
    }

    private fun createDirectOutbound(): JsonObject {
        return buildJsonObject {
            put("tag", "direct")
            put("protocol", "freedom")
            put("settings", buildJsonObject {
                put("domainStrategy", "UseIP")
            })
        }
    }

    private fun createBlockOutbound(): JsonObject {
        return buildJsonObject {
            put("tag", "block")
            put("protocol", "blackhole")
            put("settings", buildJsonObject {
                put("response", buildJsonObject {
                    put("type", "http")
                })
            })
        }
    }

    private fun buildRoutingRules(presets: RoutingPresets): JsonArray {
        val rules = mutableListOf<JsonObject>()

        if (presets.cinemaDirect) {
            rules += createDomainRule(CINEMA_DOMAINS, "direct")
        }
        if (presets.banksDirect) {
            rules += createDomainRule(BANK_DOMAINS, "direct")
        }
        if (presets.providersDirect) {
            rules += createDomainRule(PROVIDER_DOMAINS, "direct")
        }
        if (presets.gtaDirect) {
            rules += createDomainRule(GTA_DOMAINS, "direct")
        }

        if (presets.discordProxy) {
            rules += createDomainRule(DISCORD_DOMAINS, "proxy")
        }

        return JsonArray(rules)
    }

    private fun createDomainRule(domains: List<String>, outboundTag: String): JsonObject {
        return buildJsonObject {
            put("type", "field")
            put("outboundTag", outboundTag)
            putJsonArray("domain") {
                domains.forEach { domain ->
                    add(JsonPrimitive("domain:$domain"))
                }
            }
        }
    }

    private val CINEMA_DOMAINS = listOf(
        "kinopoisk.ru",
        "hd.kinopoisk.ru",
        "ivi.ru",
        "okko.tv",
        "start.ru",
        "premier.one",
        "wink.ru",
        "more.tv",
        "kion.ru"
    )

    private val BANK_DOMAINS = listOf(
        "sberbank.ru",
        "sber.ru",
        "tbank.ru",
        "tinkoff.ru",
        "alfabank.ru",
        "vtb.ru",
        "gazprombank.ru",
        "raiffeisen.ru"
    )

    private val PROVIDER_DOMAINS = listOf(
        "rt.ru",
        "rostelecom.ru",
        "beeline.ru",
        "mts.ru",
        "megafon.ru",
        "t2.ru",
        "tele2.ru",
        "domru.ru"
    )

    private val GTA_DOMAINS = listOf(
        "rockstargames.com",
        "ros.rockstargames.com",
        "rgl.rockstargames.com",
        "signin.rockstargames.com",
        "majestic-files.net",
        "cdn.alt-mp.com",
        "rage.mp",
        "gta5rp.com"
    )

    private val DISCORD_DOMAINS = listOf(
        "discord.com",
        "discordapp.com",
        "discordapp.net",
        "discord.gg",
        "gateway.discord.gg",
        "cdn.discordapp.com",
        "media.discordapp.net",
        "images-ext-1.discordapp.net",
        "images-ext-2.discordapp.net"
    )
}
