package com.ghostvpn.android.ui

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.selection.LocalClipboardManager
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.PowerSettingsNew
import androidx.compose.material.icons.filled.Route
import androidx.compose.material.icons.outlined.ContentPaste
import androidx.compose.material.icons.outlined.Delete
import androidx.compose.material.icons.outlined.Download
import androidx.compose.material.icons.outlined.Send
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Divider
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationRail
import androidx.compose.material3.NavigationRailItem
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.windowsizeclass.WindowSizeClass
import androidx.compose.material3.windowsizeclass.WindowWidthSizeClass
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.ghostvpn.android.data.AppState
import com.ghostvpn.android.data.VpnConfiguration
import com.ghostvpn.android.ui.theme.GhostDanger
import com.ghostvpn.android.ui.theme.GhostSuccess
import com.ghostvpn.android.viewmodel.MainViewModel

enum class AppDestination(val title: String, val icon: ImageVector) {
    Home("Главная", Icons.Filled.PowerSettingsNew),
    Configurations("Конфигурации", Icons.Filled.List),
    Rules("Правила", Icons.Filled.Route),
    About("О программе", Icons.Filled.Info)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun GhostVpnApp(
    windowSizeClass: WindowSizeClass,
    viewModel: MainViewModel,
    onToggleVpn: (Boolean) -> Unit
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val activeConfig = state.configurations.firstOrNull { it.id == state.activeConfigId }
    val isCompact = windowSizeClass.widthSizeClass == WindowWidthSizeClass.Compact
    var destination by rememberSaveable { mutableStateOf(AppDestination.Home) }

    Scaffold(
        containerColor = MaterialTheme.colorScheme.background,
        bottomBar = {
            if (isCompact) {
                NavigationBar {
                    AppDestination.entries.forEach { item ->
                        NavigationBarItem(
                            selected = destination == item,
                            onClick = { destination = item },
                            icon = { Icon(item.icon, contentDescription = item.title) },
                            label = { Text(item.title) }
                        )
                    }
                }
            }
        }
    ) { padding ->
        Row(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            if (!isCompact) {
                NavigationRail {
                    Spacer(modifier = Modifier.height(16.dp))
                    AppDestination.entries.forEach { item ->
                        NavigationRailItem(
                            selected = destination == item,
                            onClick = { destination = item },
                            icon = { Icon(item.icon, contentDescription = item.title) },
                            label = { Text(item.title) }
                        )
                    }
                }
            }

            Box(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxHeight()
                    .padding(12.dp)
            ) {
                when (destination) {
                    AppDestination.Home -> HomeScreen(
                        state = state,
                        activeConfig = activeConfig,
                        onToggle = { onToggleVpn(!state.isConnected) },
                        onGoConfigurations = { destination = AppDestination.Configurations },
                        onGoRules = { destination = AppDestination.Rules },
                        onClearLogs = viewModel::clearLogs
                    )

                    AppDestination.Configurations -> ConfigurationsScreen(
                        state = state,
                        onImport = viewModel::importConfigurations,
                        onActivate = viewModel::activateConfiguration,
                        onDelete = viewModel::removeConfiguration
                    )

                    AppDestination.Rules -> RulesScreen(
                        state = state,
                        onCinemaDirectChange = viewModel::setCinemaDirect,
                        onBanksDirectChange = viewModel::setBanksDirect,
                        onProvidersDirectChange = viewModel::setProvidersDirect,
                        onGtaDirectChange = viewModel::setGtaDirect,
                        onDiscordProxyChange = viewModel::setDiscordProxy
                    )

                    AppDestination.About -> AboutScreen()
                }
            }
        }
    }
}

@Composable
private fun HomeScreen(
    state: AppState,
    activeConfig: VpnConfiguration?,
    onToggle: () -> Unit,
    onGoConfigurations: () -> Unit,
    onGoRules: () -> Unit,
    onClearLogs: () -> Unit
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Button(
                onClick = onToggle,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(64.dp),
                shape = RoundedCornerShape(18.dp)
            ) {
                Text(
                    text = if (state.isConnected) "Отключить VPN" else "Подключить VPN",
                    style = MaterialTheme.typography.titleMedium
                )
            }
        }

        item {
            Card(
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant),
                shape = RoundedCornerShape(20.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(14.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Box(
                        modifier = Modifier
                            .size(42.dp)
                            .background(MaterialTheme.colorScheme.primary, RoundedCornerShape(12.dp)),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = Icons.Filled.PowerSettingsNew,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.onPrimary
                        )
                    }

                    Spacer(modifier = Modifier.width(12.dp))

                    Column {
                        Text(
                            text = "Подписка: ${activeConfig?.name ?: "Не выбрана"}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Medium
                        )
                        Text(
                            text = "Страна подключения: ${activeConfig?.country ?: "Не определена"}",
                            style = MaterialTheme.typography.bodyMedium
                        )
                        Text(
                            text = "Протокол: ${activeConfig?.protocol ?: "-"}",
                            style = MaterialTheme.typography.bodyMedium
                        )
                    }
                }
            }
        }

        item {
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                Button(
                    onClick = onGoConfigurations,
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(14.dp)
                ) {
                    Icon(Icons.Outlined.Download, contentDescription = null)
                    Spacer(modifier = Modifier.width(6.dp))
                    Text("Импорт URL")
                }
                Button(
                    onClick = onGoRules,
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(14.dp)
                ) {
                    Icon(Icons.Filled.Route, contentDescription = null)
                    Spacer(modifier = Modifier.width(6.dp))
                    Text("Правила")
                }
            }
        }

        item {
            Card(
                shape = RoundedCornerShape(18.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(14.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text("Логи", style = MaterialTheme.typography.titleMedium)
                        TextButton(onClick = onClearLogs) {
                            Text("Очистить")
                        }
                    }

                    Divider()
                    Spacer(modifier = Modifier.height(8.dp))

                    state.logs.take(12).forEach { line ->
                        Text(
                            text = line,
                            style = MaterialTheme.typography.bodySmall,
                            modifier = Modifier.padding(vertical = 2.dp)
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun ConfigurationsScreen(
    state: AppState,
    onImport: (String) -> Unit,
    onActivate: (String) -> Unit,
    onDelete: (String) -> Unit
) {
    var input by remember { mutableStateOf("") }
    val clipboard = LocalClipboardManager.current

    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        Text(
            text = "Конфигурации",
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.SemiBold
        )

        OutlinedTextField(
            value = input,
            onValueChange = { input = it },
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Массив URL или одна ссылка") },
            trailingIcon = {
                IconButton(onClick = {
                    input = clipboard.getText()?.text.orEmpty()
                }) {
                    Icon(Icons.Outlined.ContentPaste, contentDescription = "Вставить")
                }
            }
        )

        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            Button(
                onClick = {
                    onImport(input)
                    input = ""
                },
                modifier = Modifier.weight(1f),
                shape = RoundedCornerShape(14.dp)
            ) {
                Icon(Icons.Outlined.Download, contentDescription = null)
                Spacer(modifier = Modifier.width(6.dp))
                Text("Импорт")
            }

            FilterChip(
                selected = false,
                onClick = {
                    input = ""
                },
                label = { Text("Очистить поле") }
            )
        }

        Divider()

        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(state.configurations, key = { it.id }) { item ->
                val isActive = item.id == state.activeConfigId
                Card(
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = if (isActive) MaterialTheme.colorScheme.surfaceVariant else MaterialTheme.colorScheme.surface
                    ),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Text(item.name, style = MaterialTheme.typography.titleMedium)
                        Text("Страна: ${item.country}", style = MaterialTheme.typography.bodyMedium)
                        Text("Протокол: ${item.protocol}", style = MaterialTheme.typography.bodySmall)

                        Spacer(modifier = Modifier.height(8.dp))

                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            Button(
                                onClick = { onActivate(item.id) },
                                shape = RoundedCornerShape(12.dp)
                            ) {
                                Text(if (isActive) "Активна" else "Сделать активной")
                            }

                            TextButton(onClick = { onDelete(item.id) }) {
                                Icon(Icons.Outlined.Delete, contentDescription = null, tint = GhostDanger)
                                Spacer(modifier = Modifier.width(4.dp))
                                Text("Удалить", color = GhostDanger)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun RulesScreen(
    state: AppState,
    onCinemaDirectChange: (Boolean) -> Unit,
    onBanksDirectChange: (Boolean) -> Unit,
    onProvidersDirectChange: (Boolean) -> Unit,
    onGtaDirectChange: (Boolean) -> Unit,
    onDiscordProxyChange: (Boolean) -> Unit
) {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(
            text = "Настройка правил",
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.SemiBold
        )

        Card(shape = RoundedCornerShape(16.dp), modifier = Modifier.fillMaxWidth()) {
            Column(modifier = Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                RuleSwitchRow(
                    title = "Онлайн-кинотеатры",
                    subtitle = "DIRECT",
                    checked = state.routingPresets.cinemaDirect,
                    onCheckedChange = onCinemaDirectChange
                )
                RuleSwitchRow(
                    title = "Банки",
                    subtitle = "DIRECT",
                    checked = state.routingPresets.banksDirect,
                    onCheckedChange = onBanksDirectChange
                )
                RuleSwitchRow(
                    title = "Провайдеры",
                    subtitle = "DIRECT",
                    checked = state.routingPresets.providersDirect,
                    onCheckedChange = onProvidersDirectChange
                )
                RuleSwitchRow(
                    title = "GTA V / RAGE MP",
                    subtitle = "DIRECT",
                    checked = state.routingPresets.gtaDirect,
                    onCheckedChange = onGtaDirectChange
                )
                RuleSwitchRow(
                    title = "Discord",
                    subtitle = "PROXY",
                    checked = state.routingPresets.discordProxy,
                    onCheckedChange = onDiscordProxyChange
                )
            }
        }
    }
}

@Composable
private fun RuleSwitchRow(
    title: String,
    subtitle: String,
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column {
            Text(text = title, style = MaterialTheme.typography.bodyLarge)
            Text(
                text = subtitle,
                color = if (checked) GhostSuccess else MaterialTheme.colorScheme.onSurfaceVariant,
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Medium
            )
        }
        Switch(checked = checked, onCheckedChange = onCheckedChange)
    }
}

@Composable
private fun AboutScreen() {
    val context = LocalContext.current

    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Top
    ) {
        Text(
            text = "О программе",
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(bottom = 12.dp)
        )

        Button(
            onClick = {
                val intent = Intent(Intent.ACTION_VIEW, Uri.parse("https://t.me/nkvv_ghost_bot"))
                context.startActivity(intent)
            },
            shape = RoundedCornerShape(14.dp)
        ) {
            Icon(Icons.Outlined.Send, contentDescription = null)
            Spacer(modifier = Modifier.width(6.dp))
            Text("Бот в Telegram")
        }

        Spacer(modifier = Modifier.height(14.dp))

        Text(
            text = "Android-клиент Ghost VPN. Интерфейс адаптируется под телефон и планшет.",
            style = MaterialTheme.typography.bodyMedium
        )
    }
}
