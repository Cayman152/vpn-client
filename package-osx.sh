#!/bin/bash

Arch="$1"
OutputPath="$2"
Version="$3"

FileName="GhostVPN-${Arch}.zip"
wget -nv -O $FileName "https://github.com/2dust/GhostVPN-core-bin/raw/refs/heads/master/$FileName"
7z x $FileName
cp -rf GhostVPN-${Arch}/* $OutputPath

PackagePath="GhostVPN-Package-${Arch}"
mkdir -p "$PackagePath/GhostVPN.app/Contents/Resources"
cp -rf "$OutputPath" "$PackagePath/GhostVPN.app/Contents/MacOS"
cp -f "$PackagePath/GhostVPN.app/Contents/MacOS/GhostVPN.icns" "$PackagePath/GhostVPN.app/Contents/Resources/AppIcon.icns"
echo "When this file exists, app will not store configs under this folder" > "$PackagePath/GhostVPN.app/Contents/MacOS/NotStoreConfigHere.txt"
chmod +x "$PackagePath/GhostVPN.app/Contents/MacOS/GhostVPN"

cat >"$PackagePath/GhostVPN.app/Contents/Info.plist" <<-EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>English</string>
  <key>CFBundleDisplayName</key>
  <string>GhostVPN</string>
  <key>CFBundleExecutable</key>
  <string>GhostVPN</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>CFBundleIconName</key>
  <string>AppIcon</string>
  <key>CFBundleIdentifier</key>
  <string>com.ghostvpn.client</string>
  <key>CFBundleName</key>
  <string>GhostVPN</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>${Version}</string>
  <key>CSResourcesFileMapped</key>
  <true/>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>LSMinimumSystemVersion</key>
  <string>12.7</string>
</dict>
</plist>
EOF

create-dmg \
    --volname "GhostVPN Installer" \
    --window-size 700 420 \
    --icon-size 100 \
    --icon "GhostVPN.app" 160 185 \
    --hide-extension "GhostVPN.app" \
    --app-drop-link 500 185 \
    "GhostVPN-${Arch}.dmg" \
    "$PackagePath/GhostVPN.app"
