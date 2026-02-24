#!/bin/bash

Arch="$1"
OutputPath="$2"

OutputArch="GhostVPN-${Arch}"
FileName="GhostVPN-${Arch}.zip"

wget -nv -O $FileName "https://github.com/2dust/GhostVPN-core-bin/raw/refs/heads/master/$FileName"

ZipPath64="./$OutputArch"
mkdir $ZipPath64

cp -rf $OutputPath "$ZipPath64/$OutputArch"
7z a -tZip $FileName "$ZipPath64/$OutputArch" -mx1