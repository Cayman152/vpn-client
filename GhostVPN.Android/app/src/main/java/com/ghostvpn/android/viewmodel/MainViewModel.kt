package com.ghostvpn.android.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.ghostvpn.android.data.AppState
import com.ghostvpn.android.data.AppStateStore
import com.ghostvpn.android.data.DEFAULT_CONFIG_ID
import com.ghostvpn.android.data.VpnConfiguration
import com.ghostvpn.android.data.defaultLatviaConfig
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.net.URLDecoder
import java.nio.charset.StandardCharsets
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.util.UUID

class MainViewModel(
    private val store: AppStateStore
) : ViewModel() {

    private val _uiState = MutableStateFlow(AppState())
    val uiState: StateFlow<AppState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            store.state.collect { saved ->
                _uiState.value = saved.ensureValidState()
            }
        }
    }

    fun toggleConnection() {
        val current = _uiState.value
        val active = current.configurations.firstOrNull { it.id == current.activeConfigId }
        if (active == null) {
            appendLog("Нет активной конфигурации")
            return
        }

        val nextState = current.copy(isConnected = !current.isConnected)
        persist(
            nextState.withLog(
                if (nextState.isConnected) {
                    "Подключение: ${active.name} (${active.protocol})"
                } else {
                    "VPN отключен"
                }
            )
        )
    }

    fun activateConfiguration(configId: String) {
        val current = _uiState.value
        if (current.configurations.none { it.id == configId }) {
            return
        }
        persist(current.copy(activeConfigId = configId).withLog("Активная конфигурация изменена"))
    }

    fun removeConfiguration(configId: String) {
        val current = _uiState.value
        if (current.configurations.size <= 1) {
            appendLog("Нельзя удалить единственную конфигурацию")
            return
        }

        val nextConfigs = current.configurations.filterNot { it.id == configId }
        if (nextConfigs.isEmpty()) {
            persist(AppState())
            return
        }

        val nextActiveId = if (current.activeConfigId == configId) {
            nextConfigs.first().id
        } else {
            current.activeConfigId
        }

        persist(
            current.copy(
                configurations = nextConfigs,
                activeConfigId = nextActiveId
            ).withLog("Конфигурация удалена")
        )
    }

    fun importConfigurations(rawInput: String) {
        val lines = rawInput
            .split("\n", ",", ";")
            .map { it.trim() }
            .filter { it.isNotBlank() }

        if (lines.isEmpty()) {
            appendLog("Добавь хотя бы одну ссылку")
            return
        }

        val current = _uiState.value
        val existingUrls = current.configurations.map { it.url }.toSet()

        val imported = lines
            .asSequence()
            .filter { it.contains("://") }
            .filterNot { existingUrls.contains(it) }
            .map { parseConfiguration(it) }
            .toList()

        if (imported.isEmpty()) {
            appendLog("Новых ссылок не найдено")
            return
        }

        val nextConfigs = current.configurations + imported
        persist(
            current.copy(
                configurations = nextConfigs,
                activeConfigId = imported.first().id
            ).withLog("Импортировано конфигураций: ${imported.size}")
        )
    }

    fun setCinemaDirect(enabled: Boolean) {
        val current = _uiState.value
        persist(current.copy(routingPresets = current.routingPresets.copy(cinemaDirect = enabled)))
    }

    fun setBanksDirect(enabled: Boolean) {
        val current = _uiState.value
        persist(current.copy(routingPresets = current.routingPresets.copy(banksDirect = enabled)))
    }

    fun setProvidersDirect(enabled: Boolean) {
        val current = _uiState.value
        persist(current.copy(routingPresets = current.routingPresets.copy(providersDirect = enabled)))
    }

    fun setGtaDirect(enabled: Boolean) {
        val current = _uiState.value
        persist(current.copy(routingPresets = current.routingPresets.copy(gtaDirect = enabled)))
    }

    fun setDiscordProxy(enabled: Boolean) {
        val current = _uiState.value
        persist(current.copy(routingPresets = current.routingPresets.copy(discordProxy = enabled)))
    }

    fun clearLogs() {
        val current = _uiState.value
        persist(current.copy(logs = listOf("Логи очищены")))
    }

    private fun persist(newState: AppState) {
        _uiState.value = newState.ensureValidState()
        viewModelScope.launch {
            store.save(_uiState.value)
        }
    }

    private fun appendLog(message: String) {
        persist(_uiState.value.withLog(message))
    }

    private fun parseConfiguration(rawUrl: String): VpnConfiguration {
        val cleaned = rawUrl.trim()
        val decodedName = cleaned.substringAfter('#', "Сервер").let {
            URLDecoder.decode(it, StandardCharsets.UTF_8)
        }
        val normalizedName = decodedName.ifBlank { "Сервер" }

        val country = inferCountry(normalizedName, cleaned)
        val protocol = cleaned.substringBefore("://", "VLESS").uppercase()

        return VpnConfiguration(
            id = UUID.randomUUID().toString(),
            name = normalizedName,
            country = country,
            protocol = protocol,
            url = cleaned
        )
    }

    private fun inferCountry(name: String, url: String): String {
        val combined = (name + " " + url).lowercase()
        return when {
            combined.contains("latvia") || combined.contains("латв") -> "Латвия"
            combined.contains("germany") || combined.contains("герман") -> "Германия"
            combined.contains("netherlands") || combined.contains("нидерланд") -> "Нидерланды"
            combined.contains("turkey") || combined.contains("турц") -> "Турция"
            combined.contains("finland") || combined.contains("финлян") -> "Финляндия"
            else -> "Не определена"
        }
    }

    private fun AppState.withLog(message: String): AppState {
        val stamp = LocalTime.now().format(DateTimeFormatter.ofPattern("HH:mm:ss"))
        val nextLogs = (listOf("[$stamp] $message") + logs).take(50)
        return copy(logs = nextLogs)
    }

    private fun AppState.ensureValidState(): AppState {
        if (configurations.isEmpty()) {
            return copy(
                configurations = listOf(defaultLatviaConfig()),
                activeConfigId = DEFAULT_CONFIG_ID
            )
        }

        if (configurations.none { it.id == activeConfigId }) {
            return copy(activeConfigId = configurations.first().id)
        }

        return this
    }

    companion object {
        fun factory(context: Context): ViewModelProvider.Factory = object : ViewModelProvider.Factory {
            @Suppress("UNCHECKED_CAST")
            override fun <T : ViewModel> create(modelClass: Class<T>): T {
                return MainViewModel(AppStateStore(context.applicationContext)) as T
            }
        }
    }
}
