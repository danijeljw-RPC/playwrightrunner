# ScriptTrail

Runs Playwright browser flows from YAML or JSON files and generates JSON execution results. Existing results from one or more flows can be combined into a simple PDF report with embedded screenshots.

## Project layout

- `src/ScriptTrail/` - application source code
- `src/Tests/ScriptTrail.Tests/` - unit and CLI tests
- `scripts/package.sh` - macOS/Linux packaging
- `scripts/package.ps1` - Windows packaging
- `package-smoke.yaml` - deterministic self-contained packaged smoke flow
- `saucedemo.yaml` - external website sample flow
- `src/Tests/fixtures/` - local integration fixtures

## Run a flow

```bash
ScriptTrail saucedemo.yaml
```

From source:

```bash
dotnet run --project src/ScriptTrail/ScriptTrail.csproj -- saucedemo.yaml
```

A flow executes its steps in order. Execution stops after the first failed step. The JSON result path is determined by the flow specification version.

## Generate a PDF report

Generate a report from a single flow:

```bash
ScriptTrail --report --input saucedemo.yaml
```

The default output is:

```text
TestResults/playwright-report.pdf
```

The default cover title is `Playwright Test Report`. Set a custom title with
`--report-name`:

```bash
ScriptTrail --report \
  --report-name "Manhattan Test Report" \
  --input manhattan_qgao_uat.yaml
```

Choose an output path:

```bash
ScriptTrail \
  --report \
  --output TestResults/saucedemo-report.pdf \
  --input saucedemo.yaml
```

Combine multiple flows in the order supplied:

```bash
ScriptTrail \
  --report \
  --output TestResults/combined-report.pdf \
  --input saucedemo.yaml \
  --input app_uat_2.yaml
```

Equals syntax is also supported:

```bash
ScriptTrail --report \
  --output=TestResults/combined-report.pdf \
  --input=saucedemo.yaml \
  --input=app_uat_2.yaml
```

`--path` is accepted as an alias for `--output`:

```bash
ScriptTrail --report --path=TestResults/report.pdf --input=flow.yaml
```

The PDF contains:

- an overall cover page and combined result counts
- one overview page for each flow
- one page for each declared flow step
- required selector, URL, assertion, path, timeout, and other action details
- passed, failed, or not-run status
- execution duration
- action output such as `get-text` results
- failure errors
- embedded images for screenshot steps

`fill` values, API request bodies, and sensitive API headers are shown as `[redacted]` so credentials are not copied into reports.

Report mode reads existing files only. It does not start Playwright or require an installed browser.

## PDF library

PDF generation uses QuestPDF 2026.7.1 and configures its Community licence. The repository is open source; review the QuestPDF licence again if the project or distribution model changes.

## Result path rules

### Specification version 1

Version 1 uses the legacy JSON result path when `reportPath` is omitted:

```text
TestResults/report.json
```

Example:

```yaml
specVersion: 1
name: SauceDemo Checkout Flow
baseUrl: https://www.saucedemo.com
browser: chromium
headless: true
steps: []
```

Omitting `specVersion` also means version 1.

### Specification version 2

Version 2 requires an explicit `reportPath`:

```yaml
specVersion: 2
name: APP UAT
reportPath: TestResults/app-uat/app-uat.json
baseUrl: https://uat.app.one/app_uat
browser: chromium
headless: true
steps: []
```

Report generation first checks paths relative to the flow file, then the current working directory. Screenshot resolution also checks the JSON result directory.

If execution stopped after a failed step, remaining YAML steps are included in the PDF as `NOT RUN`.

## CLI options

```text
Usage:
  ScriptTrail <flow.json|flow.yaml>
  ScriptTrail --report [--output <report.pdf>] [--report-name <name>] --input <flow.yaml> [--input <flow.yaml> ...]

Options:
  -h, --help            Show help.
  -v, --version         Print version number.
  --report              Generate a PDF report from existing flow result files.
  --report-name         Cover title. Defaults to Playwright Test Report.
  -o, --output, --path  PDF output path. Defaults to TestResults/playwright-report.pdf.
  -i, --input           Flow YAML or JSON file. Repeat for multiple report sections.
```

## Package

Windows:

```powershell
./scripts/package.ps1
```

macOS or Linux:

```bash
bash scripts/package.sh
```

The default package includes Chromium. Select another browser bundle:

```bash
bash scripts/package.sh linux-x64 chromium
bash scripts/package.sh linux-x64 firefox
bash scripts/package.sh linux-x64 webkit
bash scripts/package.sh linux-x64 all
```

Windows examples:

```powershell
./scripts/package.ps1 -Runtime win-x64 -Browsers chromium
./scripts/package.ps1 -Runtime win-x64 -Browsers all
```

`chrome` is accepted as an alias for `chromium` by both package scripts.

The scripts restore and build the test project, execute the test suite, publish the application, install the selected Playwright browser assets, copy `package-smoke.yaml`, and create the package ZIP.

## Flow format

```yaml
specVersion: 2
name: Example Flow
reportPath: TestResults/example/results.json
baseUrl: https://www.saucedemo.com
browser: chromium
headless: true

steps:
  - name: Open login page
    action: goto
    url: /
```

Top-level fields:

- `specVersion` - version 1 or 2. Defaults to version 1 when omitted.
- `name` - display name for the flow.
- `reportPath` - JSON result path. Optional for version 1 and required for version 2.
- `baseUrl` - optional base URL used for relative `url` values.
- `browser` - `chromium`, `firefox`, or `webkit`.
- `headless` - optional. Must be `true` for execution.
- `userAgent` - optional browser context user agent.
- `locale` - optional browser context locale, such as `en-US`.
- `timezoneId` - optional browser context timezone, such as `America/New_York`.
- `viewport` - optional browser context viewport with `width` and `height`.
- `extraHttpHeaders` - optional browser context HTTP headers.
- `steps` - ordered list of actions.

The same fields are supported in JSON.

Common step fields:

- `name` - display name for the step.
- `action` - action name.
- `frameSelector` - optional iframe selector used before resolving `selector`.
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

Use `frameSelector` for elements inside an iframe:

```yaml
- name: Read application version
  action: get-text
  frameSelector: iframe
  selector: "#versiontext"
```

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

Selects one or more `<option>` values.

```yaml
- name: Select country
  action: select
  selector: "#country"
  value: AU
```

```yaml
- name: Select tags
  action: select
  selector: "#tags"
  values:
    - smoke
    - checkout
```

### `check` and `uncheck`

```yaml
- name: Accept terms
  action: check
  selector: "#terms"
```

```yaml
- name: Clear subscription
  action: uncheck
  selector: "#subscribe"
```

### `press`

```yaml
- name: Submit search
  action: press
  selector: "#search"
  value: Enter
```

### `hover`

```yaml
- name: Open menu hover state
  action: hover
  selector: "#account-menu"
```

### `upload`

```yaml
- name: Upload avatar
  action: upload
  selector: input[type='file']
  path: fixtures/avatar.png
```

```yaml
- name: Upload documents
  action: upload
  selector: input[type='file']
  values:
    - fixtures/a.pdf
    - fixtures/b.pdf
```

### `download`

```yaml
- name: Download invoice
  action: download
  selector: "#invoice-download"
  path: TestResults/invoice.pdf
```

### `api-request`

```yaml
- name: Check health endpoint
  action: api-request
  method: GET
  url: /health
  status: 200
  path: TestResults/health.json
```

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

### `trace-start` and `trace-stop`

```yaml
- name: Start trace
  action: trace-start

- name: Stop trace
  action: trace-stop
  path: TestResults/trace.zip
```

### `expect-visible`

```yaml
- name: Check products title
  action: expect-visible
  selector: text=Products
```

### `expect-text`

```yaml
- name: Check cart badge
  action: expect-text
  selector: .shopping_cart_badge
  value: "1"
```

### `get-text`

Reads visible element text and stores it in the JSON result `Data` field.

```yaml
- name: Read application version
  action: get-text
  selector: "#versiontext"
```

### `expect-url`

```yaml
- name: Check inventory URL
  action: expect-url
  value: .*/inventory.html
```

### `screenshot`

```yaml
- name: Screenshot page
  action: screenshot
  path: TestResults/page.png
  fullPage: true
```

The screenshot output path is stored in the JSON result `Data` field and is embedded in generated PDF reports.

### `wait`

```yaml
- name: Wait briefly
  action: wait
  timeoutMs: 500
```

## Local action fixture

The fixture flow in `src/Tests/fixtures/new-actions.yaml` exercises the extended action set.

Start the fixture server:

```bash
python3 -m http.server 8765 --directory src/Tests/fixtures
```

Run the fixture:

```bash
dotnet run --project src/ScriptTrail/ScriptTrail.csproj -- src/Tests/fixtures/new-actions.yaml
```
