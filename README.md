# PlaywrightRunner

Runs Playwright browser flows from YAML or JSON files.

## Project Layout

- `src/PlaywrightRunner/` - application source code
- `scripts/package.sh` - builds, publishes, installs bundled browser assets, copies `saucedemo.yaml`, and creates a zip
- `saucedemo.yaml` - sample flow kept at the project root
- `tests/fixtures/` - local integration fixture for runner actions

## Run

```bash
dotnet run --project src/PlaywrightRunner/PlaywrightRunner.csproj -- saucedemo.yaml
```

The runner expects one argument: the path to a `.yaml`, `.yml`, or `.json` flow file.

## Package

```bash
bash scripts/package.sh
```

By default this creates:

```text
artifacts/zips/PlaywrightRunner-osx-arm64.zip
```

You can pass another .NET runtime identifier:

```bash
bash scripts/package.sh linux-x64
```

By default the package includes Chromium only. Pass a second argument to choose browser assets:

```bash
bash scripts/package.sh osx-arm64 chromium
bash scripts/package.sh osx-arm64 firefox
bash scripts/package.sh osx-arm64 webkit
bash scripts/package.sh osx-arm64 all
```

`chrome` is accepted as an alias for `chromium`. `all` packages Chromium, Firefox, and WebKit.

## Flow Format

```yaml
name: Example Flow
baseUrl: https://www.saucedemo.com
browser: chromium
headless: true

steps:
  - name: Open login page
    action: goto
    url: /
```

Top-level fields:

- `name` - display name for the flow.
- `baseUrl` - optional base URL used for relative `url` values.
- `browser` - `chromium`, `firefox`, or `webkit`.
- `headless` - `true` or `false`.
- `steps` - ordered list of actions.

Common step fields:

- `name` - display name for the step.
- `action` - action name.
- `selector` - element selector for element-based actions.
- `value` - text, key, option value, or assertion value depending on the action.
- `values` - list of values for multi-select or multi-file upload.
- `url` - navigation or API URL.
- `path` - file path for screenshots, uploads, downloads, API output, or traces.
- `timeoutMs` - optional timeout in milliseconds. Defaults to `10000`.
- `index` - optional zero-based index when a selector matches multiple elements.

## Selectors

Supported selector shortcuts:

```yaml
selector: placeholder=Username
selector: text=Products
selector: testid=submit-button
selector: role=button[name='Login']
selector: "#cart"
selector: ".shopping_cart_badge"
```

Anything that does not match a shortcut is passed to Playwright as a normal locator selector.

## Actions

### `goto`

Navigates to `url`. Relative URLs are resolved against `baseUrl`.

```yaml
- name: Open inventory
  action: goto
  url: /inventory.html
```

### `click`

Clicks an element.

```yaml
- name: Click login
  action: click
  selector: role=button[name='Login']
```

Use `index` when multiple elements match:

```yaml
- name: Add first item
  action: click
  selector: role=button[name='Add to cart']
  index: 0
```

### `fill`

Fills an input with `value`.

```yaml
- name: Enter username
  action: fill
  selector: placeholder=Username
  value: standard_user
```

### `select`

Selects one or more `<option>` values in a `<select>`.

```yaml
- name: Select country
  action: select
  selector: "#country"
  value: AU
```

For a multi-select:

```yaml
- name: Select tags
  action: select
  selector: "#tags"
  values:
    - smoke
    - checkout
```

### `check`

Checks a checkbox or radio input.

```yaml
- name: Accept terms
  action: check
  selector: "#terms"
```

### `uncheck`

Unchecks a checkbox.

```yaml
- name: Clear subscription
  action: uncheck
  selector: "#subscribe"
```

### `press`

Presses a keyboard key on an element. Use Playwright key names such as `Enter`, `Tab`, `Escape`, or combinations such as `Control+A`.

```yaml
- name: Submit search
  action: press
  selector: "#search"
  value: Enter
```

### `hover`

Moves the mouse over an element.

```yaml
- name: Open menu hover state
  action: hover
  selector: "#account-menu"
```

### `upload`

Sets files on a file input. `path` is used for one file.

```yaml
- name: Upload avatar
  action: upload
  selector: input[type='file']
  path: fixtures/avatar.png
```

Use `values` for multiple files:

```yaml
- name: Upload documents
  action: upload
  selector: input[type='file']
  values:
    - fixtures/a.pdf
    - fixtures/b.pdf
```

### `download`

Clicks an element that triggers a download and saves the file to `path`.

```yaml
- name: Download invoice
  action: download
  selector: "#invoice-download"
  path: TestResults/invoice.pdf
```

### `api-request`

Sends an HTTP request through Playwright's API request context. Relative URLs are resolved against `baseUrl`.

```yaml
- name: Check health endpoint
  action: api-request
  method: GET
  url: /health
  status: 200
  path: TestResults/health.json
```

With headers and a body:

```yaml
- name: Create record
  action: api-request
  method: POST
  url: /api/records
  status: 201
  headers:
    content-type: application/json
  data: '{"name":"demo"}'
  path: TestResults/create-record.json
```

Fields:

- `method` - HTTP method. Defaults to `GET`.
- `url` - required.
- `status` - optional expected response status.
- `headers` - optional request headers.
- `data` - optional request body.
- `path` - optional file path where the response body is written.

### `trace-start`

Starts Playwright tracing for the current browser context. Screenshots, snapshots, and sources are captured.

```yaml
- name: Start trace
  action: trace-start
```

### `trace-stop`

Stops tracing and writes a `.zip` trace file.

```yaml
- name: Stop trace
  action: trace-stop
  path: TestResults/trace.zip
```

Open the trace with Playwright tooling:

```bash
pwsh artifacts/publish/osx-arm64/playwright.ps1 show-trace TestResults/trace.zip
```

### `expect-visible`

Asserts that an element is visible.

```yaml
- name: Check products title
  action: expect-visible
  selector: text=Products
```

### `expect-text`

Asserts that an element has exact text.

```yaml
- name: Check cart badge
  action: expect-text
  selector: .shopping_cart_badge
  value: "1"
```

### `expect-url`

Asserts that the current URL matches a regular expression.

```yaml
- name: Check inventory URL
  action: expect-url
  value: .*/inventory.html
```

### `screenshot`

Captures a screenshot.

```yaml
- name: Screenshot page
  action: screenshot
  path: TestResults/page.png
  fullPage: true
```

### `wait`

Waits for `timeoutMs` milliseconds.

```yaml
- name: Wait briefly
  action: wait
  timeoutMs: 500
```

## Local Action Fixture

The fixture flow in `tests/fixtures/new-actions.yaml` exercises the new action set.

Start the fixture server:

```bash
python3 -m http.server 8765 --directory tests/fixtures
```

Run the fixture:

```bash
dotnet run --project src/PlaywrightRunner/PlaywrightRunner.csproj -- tests/fixtures/new-actions.yaml
```
