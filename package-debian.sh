#!/bin/bash

Arch="$1"
OutputPath="$2"
Version="$3"

FileName="GhostVPN-${Arch}.zip"
wget -nv -O $FileName "https://github.com/Cayman152/vpn-client/releases/latest/download/$FileName"
7z x $FileName
cp -rf GhostVPN-${Arch}/* $OutputPath

PackagePath="GhostVPN-Package-${Arch}"
mkdir -p "${PackagePath}/DEBIAN"
mkdir -p "${PackagePath}/opt"
cp -rf $OutputPath "${PackagePath}/opt/GhostVPN"
echo "When this file exists, app will not store configs under this folder" > "${PackagePath}/opt/GhostVPN/NotStoreConfigHere.txt"

if [ $Arch = "linux-64" ]; then
    Arch2="amd64" 
else
    Arch2="arm64"
fi
echo $Arch2

# basic
cat >"${PackagePath}/DEBIAN/control" <<-EOF
Package: GhostVPN
Version: $Version
Architecture: $Arch2
Maintainer: https://github.com/Cayman152/vpn-client
Depends: libc6 (>= 2.34), fontconfig (>= 2.13.1), desktop-file-utils (>= 0.26), xdg-utils (>= 1.1.3), coreutils (>= 8.32), bash (>= 5.1), libfreetype6 (>= 2.11)
Description: A GUI client for Windows and Linux, support Xray core and sing-box-core and others
EOF

cat >"${PackagePath}/DEBIAN/postinst" <<-EOF
if [ ! -s /usr/share/applications/GhostVPN.desktop ]; then
    cat >/usr/share/applications/GhostVPN.desktop<<-END
[Desktop Entry]
Name=GhostVPN
Comment=A GUI client for Windows and Linux, support Xray core and sing-box-core and others
Exec=/opt/GhostVPN/GhostVPN
Icon=/opt/GhostVPN/GhostVPN.png
Terminal=false
Type=Application
Categories=Network;Application;
END
fi

update-desktop-database
EOF

sudo chmod 0755 "${PackagePath}/DEBIAN/postinst"
sudo chmod 0755 "${PackagePath}/opt/GhostVPN/GhostVPN"
sudo chmod 0755 "${PackagePath}/opt/GhostVPN/AmazTool"

# Patch
# set owner to root:root
sudo chown -R root:root "${PackagePath}"
# set all directories to 755 (readable & traversable by all users)
sudo find "${PackagePath}/opt/GhostVPN" -type d -exec chmod 755 {} +
# set all regular files to 644 (readable by all users)
sudo find "${PackagePath}/opt/GhostVPN" -type f -exec chmod 644 {} +
# ensure main binaries are 755 (executable by all users)
sudo chmod 755 "${PackagePath}/opt/GhostVPN/GhostVPN" 2>/dev/null || true
sudo chmod 755 "${PackagePath}/opt/GhostVPN/AmazTool" 2>/dev/null || true

# build deb package
sudo dpkg-deb -Zxz --build $PackagePath
sudo mv "${PackagePath}.deb" "GhostVPN-${Arch}.deb"
