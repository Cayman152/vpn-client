package com.ghostvpn.android.data

import android.content.Context
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.emptyPreferences
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.map
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.io.IOException

private val Context.dataStore by preferencesDataStore(name = "ghostvpn_state")

class AppStateStore(private val context: Context) {
    private val appStateKey = stringPreferencesKey("app_state_json")
    private val json = Json {
        encodeDefaults = true
        ignoreUnknownKeys = true
    }

    val state: Flow<AppState> = context.dataStore.data
        .catch { exception ->
            if (exception is IOException) {
                emit(emptyPreferences())
            } else {
                throw exception
            }
        }
        .map(::mapPreferencesToState)

    suspend fun save(state: AppState) {
        context.dataStore.edit { prefs ->
            prefs[appStateKey] = json.encodeToString(state)
        }
    }

    private fun mapPreferencesToState(preferences: Preferences): AppState {
        val payload = preferences[appStateKey]
        if (payload.isNullOrBlank()) {
            return AppState()
        }
        return try {
            json.decodeFromString<AppState>(payload)
        } catch (_: Exception) {
            AppState()
        }
    }
}
