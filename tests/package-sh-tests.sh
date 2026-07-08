#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

make_test_repo() {
  local work_dir="$1"

  mkdir -p "$work_dir/scripts" "$work_dir/src/PlaywrightRunner" "$work_dir/fake-bin"
  cp "$ROOT_DIR/scripts/package.sh" "$work_dir/scripts/package.sh"
  touch "$work_dir/src/PlaywrightRunner/PlaywrightRunner.csproj"
  printf 'flow: test\n' > "$work_dir/saucedemo.yaml"

  cat > "$work_dir/fake-bin/dotnet" <<'STUB'
#!/usr/bin/env bash
set -euo pipefail
if [[ "${1:-}" == "publish" ]]; then
  out_dir=""
  while [[ $# -gt 0 ]]; do
    if [[ "$1" == "-o" ]]; then
      out_dir="$2"
      break
    fi
    shift
  done
  mkdir -p "$out_dir"
  touch "$out_dir/PlaywrightRunner"
  printf 'install script\n' > "$out_dir/playwright.ps1"
fi
STUB

  cat > "$work_dir/fake-bin/pwsh" <<'STUB'
#!/usr/bin/env bash
set -euo pipefail
exit 0
STUB

  cat > "$work_dir/fake-bin/zip" <<'STUB'
#!/usr/bin/env bash
set -euo pipefail
target="$2"
mkdir -p "$(dirname "$target")"
printf 'zip\n' > "$target"
STUB

  chmod +x "$work_dir/fake-bin/dotnet" "$work_dir/fake-bin/pwsh" "$work_dir/fake-bin/zip" "$work_dir/scripts/package.sh"
}

run_package() {
  local work_dir="$1"
  shift

  (
    cd "$work_dir"
    PATH="$work_dir/fake-bin:$PATH" bash scripts/package.sh "$@"
  )
}

assert_file_exists() {
  local path="$1"
  [[ -f "$path" ]] || fail "Expected file to exist: $path"
}

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

repo_one="$tmp_dir/chrome-default"
make_test_repo "$repo_one"
run_package "$repo_one" osx-arm64
assert_file_exists "$repo_one/artifacts/zips/PlaywrightRunner-osx-arm64-chrome.zip"

repo_two="$tmp_dir/chromium-alias"
make_test_repo "$repo_two"
run_package "$repo_two" osx-arm64 chromium
assert_file_exists "$repo_two/artifacts/zips/PlaywrightRunner-osx-arm64-chrome.zip"

repo_three="$tmp_dir/all-tagged"
make_test_repo "$repo_three"
run_package "$repo_three" linux-x64 all v1.2.3
assert_file_exists "$repo_three/artifacts/zips/PlaywrightRunner-linux-x64-all-v1.2.3.zip"

echo "package.sh naming tests passed"
