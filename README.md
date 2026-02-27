# Ghost VPN

Собственный VPN-клиент для Windows, macOS и Android (с фокусом на простое подключение и маршрутизацию по правилам).

## Быстро скачать

[![Download Installer](https://img.shields.io/badge/Windows-Download%20Installer-00B7FF?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-Setup-x64.exe)

## Скачать

- Установщик Windows x64: [GhostVPN-Setup-x64.exe](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-Setup-x64.exe)
- macOS Apple Silicon (arm64): [GhostVPN-macOS-arm64.pkg](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-macOS-arm64.pkg)
- macOS Intel (x64): [GhostVPN-macOS-x64.pkg](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-macOS-x64.pkg)
- Страница релиза: [ghost-vpn-latest](https://github.com/Cayman152/vpn-client/releases/tag/ghost-vpn-latest)

## Android

- Android-модуль находится в `GhostVPN.Android`.
- Интерфейс адаптивный: телефонный режим и планшетный режим.
- В текущей версии реализованы:
  - Главная с кнопкой подключения и логами.
  - Конфигурации (импорт ссылок, выбор активной, удаление).
  - Настройка правил (DIRECT/PROXY пресеты).
  - Вкладка «О программе» с кнопкой Telegram.
- Инструкция запуска в Android Studio: `GhostVPN.Android/README-ANDROID.md`.

## Поддерживаемые ядра

- [Xray](https://github.com/XTLS/Xray-core)
- [sing-box](https://github.com/SagerNet/sing-box)

## Сборка Windows

- Workflow: `Ghost VPN Windows Build`
- Артефакт после успешной сборки: `GhostVPN-Setup-x64.exe`

## Сборка macOS

- Workflow: `Ghost VPN macOS Build`
- Артефакты после успешной сборки:
  - `GhostVPN-macOS-arm64.pkg`
  - `GhostVPN-macOS-x64.pkg`

## Сборка Android

- Открыть в Android Studio папку `GhostVPN.Android`.
- Сборка APK: `Build > Build Bundle(s) / APK(s) > Build APK(s)`.
- Либо CLI: `gradle -p GhostVPN.Android :app:assembleRelease`.

## Поддержка

- Telegram: [nkvv_ghost_bot](https://t.me/nkvv_ghost_bot)
