#!/usr/bin/env bash
set -euo pipefail

PROJECT="PlaywrightRunner"
PROJECT_PATH="src/$PROJECT/$PROJECT.csproj"
TEST_PROJECT_PATH="src/Tests/PlaywrightRunner.Tests/PlaywrightRunner.Tests.csproj"
FLOW_FILE="package-smoke.yaml"
CONFIGURATION="Release"
FRAMEWORK="net10.0"
RUNTIME="${1:-osx-arm64}"
BROWSERS="${2:-chromium}"
VERSION_SUFFIX="${3:-${PACKAGE_VERSION:-}}"
BROWSERS_NORMALIZED="$(printf '%s' "$BROWSERS" | tr '[:upper:]' '[:lower:]')"

OUTPUT_ROOT="artifacts"
PUBLISH_ROOT="$OUTPUT_ROOT/publish"
ZIP_DIR="$OUTPUT_ROOT/zips"

case "$BROWSERS_NORMALIZED" in
  chromium|chrome)
    PLAYWRIGHT_BROWSERS=(chromium)
    BROWSER_ZIP_SUFFIX="chrome"
    ;;
  firefox)
    PLAYWRIGHT_BROWSERS=(firefox)
    BROWSER_ZIP_SUFFIX="firefox"
    ;;
  webkit)
    PLAYWRIGHT_BROWSERS=(webkit)
    BROWSER_ZIP_SUFFIX="webkit"
    ;;
  all)
    PLAYWRIGHT_BROWSERS=(chromium firefox webkit)
    BROWSER_ZIP_SUFFIX="all"
    ;;
  *)
    echo "Unsupported browser bundle: $BROWSERS" >&2
    echo "Use one of: chromium, chrome, firefox, webkit, all" >&2
    exit 2
    ;;
esac

PUBLISH_DIR="$PUBLISH_ROOT/$RUNTIME-$BROWSER_ZIP_SUFFIX"

ZIP_BASENAME="$PROJECT-$RUNTIME-$BROWSER_ZIP_SUFFIX"
if [[ -n "$VERSION_SUFFIX" ]]; then
  ZIP_BASENAME="$ZIP_BASENAME-$VERSION_SUFFIX"
fi
ZIP_FILE="$ZIP_DIR/$ZIP_BASENAME.zip"

echo "Runtime: $RUNTIME"
echo "Browsers: $BROWSERS"

echo "Cleaning publish directory for current package..."
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"
mkdir -p "$ZIP_DIR"
rm -f "$ZIP_FILE"

if [[ ! -f "$PROJECT_PATH" ]]; then
  echo "Project file not found: $PROJECT_PATH" >&2
  exit 2
fi

if [[ ! -f "$TEST_PROJECT_PATH" ]]; then
  echo "Test project file not found: $TEST_PROJECT_PATH" >&2
  exit 2
fi

echo "Restoring..."
dotnet restore "$TEST_PROJECT_PATH"

echo "Building..."
dotnet build "$TEST_PROJECT_PATH" \
  -c "$CONFIGURATION" \
  --no-restore

echo "Testing..."
dotnet test "$TEST_PROJECT_PATH" \
  -c "$CONFIGURATION" \
  --no-build \
  --no-restore

echo "Publishing..."
dotnet publish "$PROJECT_PATH" \
  -c "$CONFIGURATION" \
  -f "$FRAMEWORK" \
  -r "$RUNTIME" \
  --self-contained true \
  -o "$PUBLISH_DIR" \
  /p:PublishSingleFile=false \
  /p:PublishTrimmed=false

if [[ ! -f "$PUBLISH_DIR/playwright.ps1" ]]; then
  echo "Playwright install script not found in publish directory: $PUBLISH_DIR/playwright.ps1" >&2
  exit 2
fi

echo "Installing bundled Playwright browsers: ${PLAYWRIGHT_BROWSERS[*]}"
pushd "$PUBLISH_DIR" >/dev/null

PLAYWRIGHT_BROWSERS_PATH="$(pwd)/ms-playwright" \
  pwsh -NoProfile ./playwright.ps1 install "${PLAYWRIGHT_BROWSERS[@]}"

popd >/dev/null

if [[ -f "$FLOW_FILE" ]]; then
  echo "Copying sample flow..."
  cp "$FLOW_FILE" "$PUBLISH_DIR/"
fi

echo "Zipping..."
pushd "$PUBLISH_DIR" >/dev/null
zip -qr "../../zips/$ZIP_BASENAME.zip" .
popd >/dev/null

echo "Done:"
echo "$ZIP_FILE"
