#!/bin/bash
set -euo pipefail
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$ROOT"
VERSION=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)
UNITY=${NYANLINK_UNITY:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}
[[ -x "$UNITY" ]] || { echo "Unity $VERSION not found; set NYANLINK_UNITY." >&2; exit 1; }
[[ ! -e Temp/UnityLockfile ]] || { echo "Close this project's Unity Editor first." >&2; exit 1; }
export NYANLINK_ANDROID_PATH=${NYANLINK_ANDROID_PATH:-"$ROOT/Builds/Android/NyanLink-test.apk"}
export NYANLINK_ANDROID_OUTPUT=${NYANLINK_ANDROID_OUTPUT:-"$ROOT/output/playtests/$(TZ=Asia/Seoul date +%Y-%m-%d-%H%M%S)-android"}
mkdir -p Builds/AndroidToolchain "$NYANLINK_ANDROID_OUTPUT"
BACKUP=$(mktemp -d "$ROOT/Builds/AndroidToolchain/settings.XXXXXX")
cp ProjectSettings/ProjectSettings.asset "$BACKUP/ProjectSettings.asset"
cp ProjectSettings/UnityConnectSettings.asset "$BACKUP/UnityConnectSettings.asset"
mkdir "$BACKUP/tmp"
TEMP_LINK=$(mktemp -d /tmp/nyan-android.XXXXXX)
rmdir "$TEMP_LINK"
ln -s "$BACKUP/tmp" "$TEMP_LINK"
restore() {
  cp "$BACKUP/ProjectSettings.asset" ProjectSettings/ProjectSettings.asset
  cp "$BACKUP/UnityConnectSettings.asset" ProjectSettings/UnityConnectSettings.asset
  [[ ! -L "$TEMP_LINK" ]] || rm -- "$TEMP_LINK"
}
trap restore EXIT
# The prototype uses the new Input System. Both is unsupported on Android.
python3 - <<'PY'
from pathlib import Path
p = Path('ProjectSettings/ProjectSettings.asset')
s = p.read_text()
assert s.count('  activeInputHandler: ') == 1
import re
p.write_text(re.sub(r'^  activeInputHandler: \d+$', '  activeInputHandler: 1', s, flags=re.M))
PY
TMPDIR="$TEMP_LINK" BEE_CACHE_DIRECTORY="$ROOT/Builds/AndroidToolchain/bee-cache" \
  "$UNITY" -batchmode -quit -projectPath "$ROOT" -buildTarget Android \
  -executeMethod NyanLinkAndroidBuild.Build -logFile "$NYANLINK_ANDROID_OUTPUT/build.log"
shasum -a 256 "$NYANLINK_ANDROID_PATH" > "$NYANLINK_ANDROID_OUTPUT/apk.sha256"
cp Assets/Resources/Fonts/Galmuri/LICENSE.txt "$(dirname "$NYANLINK_ANDROID_PATH")/Galmuri-LICENSE.txt"
cp Assets/Resources/Fonts/Galmuri/NOTICE.txt "$(dirname "$NYANLINK_ANDROID_PATH")/Galmuri-NOTICE.txt"
