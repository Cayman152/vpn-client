# Ghost VPN Android

Android-клиент Ghost VPN (Jetpack Compose) с адаптивным интерфейсом для телефонов и планшетов.

## Что уже реализовано

- Главная: одна кнопка подключения/отключения, статус и логи.
- Конфигурации: импорт массива URL, выбор активной, удаление.
- Правила: предустановленные переключатели DIRECT/PROXY.
- О программе: кнопка перехода в Telegram-бот.

## Быстрый запуск в Android Studio

1. Установи Android Studio (последняя стабильная).
2. Открой папку `GhostVPN.Android` как отдельный проект.
3. Дождись синхронизации Gradle.
4. Нажми `Run` для запуска на устройстве/эмуляторе.

## Сборка APK

- Через Android Studio:
  - `Build > Build Bundle(s) / APK(s) > Build APK(s)`
- Через CLI:
  - `gradle -p GhostVPN.Android :app:assembleRelease`

Готовый файл:

- `GhostVPN.Android/app/build/outputs/apk/release/app-release.apk`

## Технически

- Язык: Kotlin.
- UI: Jetpack Compose + Material 3.
- Локальное хранение состояния: DataStore.
- Локализация: русский язык по умолчанию.

## Следующий шаг

Чтобы VPN реально поднимался на Android, нужно подключить Android `VpnService` и ядро (`xray`/`sing-box`) в рантайме. Текущая версия уже готова как UI/MVP-клиент и база под эту интеграцию.
