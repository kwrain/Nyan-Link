#!/bin/bash
set -euo pipefail
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$ROOT"
VERSION=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)
UNITY=${NYANLINK_UNITY:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}
if [[ ! -x "$UNITY" ]]; then
  echo "Unity $VERSION not found. Set NYANLINK_UNITY to the Unity executable." >&2
  exit 1
fi
if [[ -e Temp/UnityLockfile ]]; then
  echo "Close this project's Unity Editor before running the web build." >&2
  exit 1
fi
export NYANLINK_WEB_PATH=${NYANLINK_WEB_PATH:-"$ROOT/Builds/Web/nyan-link"}
export NYANLINK_WEB_OUTPUT=${NYANLINK_WEB_OUTPUT:-"$ROOT/output/playtests/$(TZ=Asia/Seoul date +%Y-%m-%d-%H%M%S)-web-build"}
mkdir -p Builds/WebToolchain "$NYANLINK_WEB_OUTPUT"
BACKUP=$(mktemp -d "$ROOT/Builds/WebToolchain/settings.XXXXXX")
cp ProjectSettings/ProjectSettings.asset "$BACKUP/ProjectSettings.asset"
cp ProjectSettings/UnityConnectSettings.asset "$BACKUP/UnityConnectSettings.asset"
mkdir "$BACKUP/tmp"
# macOS IPC sockets require short paths. Store the actual temporary data externally.
TEMP_LINK=$(mktemp -d /tmp/nyan-web.XXXXXX)
rmdir "$TEMP_LINK"
ln -s "$BACKUP/tmp" "$TEMP_LINK"
restore() {
  cp "$BACKUP/ProjectSettings.asset" ProjectSettings/ProjectSettings.asset
  cp "$BACKUP/UnityConnectSettings.asset" ProjectSettings/UnityConnectSettings.asset
  [[ ! -L "$TEMP_LINK" ]] || rm -- "$TEMP_LINK"
}
trap restore EXIT
TMPDIR="$TEMP_LINK" BEE_CACHE_DIRECTORY="$ROOT/Builds/WebToolchain/bee-cache" \
  "$UNITY" -batchmode -quit -projectPath "$ROOT" -buildTarget WebGL \
  -executeMethod NyanLinkWebBuild.Build -logFile "$NYANLINK_WEB_OUTPUT/build.log"
python3 Tools/package_web.py --build "$NYANLINK_WEB_PATH" \
  --output "$(dirname "$NYANLINK_WEB_PATH")/nyan-link-deploy.zip" \
  > "$NYANLINK_WEB_OUTPUT/package-results.json"
cat "$NYANLINK_WEB_OUTPUT/package-results.json"
