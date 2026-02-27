# Ghost VPN

Собственный VPN-клиент для Windows и macOS (с фокусом на простое подключение и маршрутизацию по правилам).

## Быстро скачать

[![Download Installer](https://img.shields.io/badge/Windows-Download%20Installer-00B7FF?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-Setup-x64.exe)

## Скачать

- Установщик Windows x64: [GhostVPN-Setup-x64.exe](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-Setup-x64.exe)
- macOS Apple Silicon (arm64): [GhostVPN-macOS-arm64.pkg](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-macOS-arm64.pkg)
- macOS Intel (x64): [GhostVPN-macOS-x64.pkg](https://github.com/Cayman152/vpn-client/releases/latest/download/GhostVPN-macOS-x64.pkg)
- Страница релиза: [ghost-vpn-latest](https://github.com/Cayman152/vpn-client/releases/tag/ghost-vpn-latest)

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

## Подпись И Notarization macOS

- Чтобы сборка выходила с Developer ID подписью и без предупреждений Gatekeeper, добавь в GitHub `Settings -> Secrets and variables -> Actions`:
- `MACOS_SIGN_CERT_BASE64` (base64 от `.p12` c сертификатами Developer ID Application + Installer)
- `MACOS_SIGN_CERT_PASSWORD`
- `MACOS_SIGN_IDENTITY_APP` (пример: `Developer ID Application: Your Name (TEAMID)`)
- `MACOS_SIGN_IDENTITY_INSTALLER` (пример: `Developer ID Installer: Your Name (TEAMID)`)
- `APPLE_ID`
- `APPLE_APP_PASSWORD` (app-specific password для notarization)
- `APPLE_TEAM_ID`

## Поддержка

- Telegram: [nkvv_ghost_bot](https://t.me/nkvv_ghost_bot)
