#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
export AVALONIA_TELEMETRY_OPTOUT=1
APP_NAME="전자세금계산서 변환기"
EXECUTABLE_NAME="TaxInvoiceExtractorMac"
PUBLISH_DIR="$ROOT_DIR/artifacts/osx-arm64-publish"
DIST_DIR="$ROOT_DIR/artifacts/dist"
APP_BUNDLE="$DIST_DIR/$APP_NAME.app"
STAGING_DIR="$DIST_DIR/dmg-staging"
DMG_PATH="$DIST_DIR/TaxInvoiceExtractor_Mac_v2_AppleSilicon.dmg"

echo "[1/5] Apple Silicon용 앱을 게시합니다."
rm -rf "$PUBLISH_DIR"
dotnet publish "$ROOT_DIR/TaxInvoiceExtractor.Mac.csproj" \
  --configuration Release \
  --runtime osx-arm64 \
  --self-contained true \
  --output "$PUBLISH_DIR" \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false

echo "[2/5] macOS .app 번들을 구성합니다."
rm -rf "$APP_BUNDLE" "$STAGING_DIR" "$DMG_PATH"
mkdir -p "$APP_BUNDLE/Contents/MacOS" "$APP_BUNDLE/Contents/Resources"
cp "$ROOT_DIR/packaging/Info.plist" "$APP_BUNDLE/Contents/Info.plist"
cp -R "$PUBLISH_DIR/." "$APP_BUNDLE/Contents/MacOS/"
chmod +x "$APP_BUNDLE/Contents/MacOS/$EXECUTABLE_NAME"

echo "[3/5] Apple Silicon 실행에 필요한 로컬 임시 서명을 적용합니다."
codesign --force --deep --sign - "$APP_BUNDLE"

echo "[4/5] 개발자 서명·공증 없는 DMG 설치 이미지를 생성합니다."
mkdir -p "$STAGING_DIR"
cp -R "$APP_BUNDLE" "$STAGING_DIR/"
ln -s /Applications "$STAGING_DIR/Applications"
hdiutil create \
  -volname "$APP_NAME" \
  -srcfolder "$STAGING_DIR" \
  -ov \
  -format UDZO \
  "$DMG_PATH"
rm -rf "$STAGING_DIR"

echo "[5/5] 패키지 기본 구조를 검사합니다."
plutil -lint "$APP_BUNDLE/Contents/Info.plist"
file "$APP_BUNDLE/Contents/MacOS/$EXECUTABLE_NAME"
codesign --verify --deep --strict "$APP_BUNDLE"
echo
echo "완료: $DMG_PATH"
echo "Developer ID 서명·공증이 없으므로 최초 실행은 Finder에서 우클릭 > 열기를 사용하세요."
