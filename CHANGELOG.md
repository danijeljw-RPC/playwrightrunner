# Changelog

## Unreleased

- Added `--report-name` to customize the PDF cover title while retaining `Playwright Test Report` as the default.
- Updated the Manhattan suite script to generate reports titled `Manhattan Test Report`.
- Fixed QuestPDF footer styling compatibility with QuestPDF 2026.7.1.
- Added a fallback from invalid `PLAYWRIGHT_DRIVER_SEARCH_PATH` overrides to the bundled Playwright driver.
- Added PDF report generation from one or more existing YAML or JSON flow files.
- Added repeated `--input` arguments so multiple test flows can be combined in a single ordered PDF report.
- Added `--output`, `-o`, and `--path` options for selecting the PDF output path.
- Added a default PDF output path of `TestResults/playwright-report.pdf`.
- Added an overall report cover, per-flow overview pages, per-step result pages, pass/fail/not-run status, durations, extracted action output, errors, and embedded screenshots.
- Redacted `fill` values, sensitive API headers, and API request bodies from generated PDF reports.
- Added support for reading both specification version 1 and specification version 2 result layouts.
- Restored specification version 1 compatibility for legacy flows and flows without an explicit `specVersion`.
- Required `reportPath` for specification version 2 flows.
- Separated flow parsing from execution-only validation so historical result files can still be reported.
- Corrected packaging scripts to restore, build, and run the actual test project.
- Added CLI parsing, flow compatibility, report aggregation, screenshot resolution, and PDF generation tests.

## v0.2.0

- Added CLI help output with `-h` and `--help`.
- Added CLI version output with `-v` and `--version`; version output prints only the version number.
- Enforced headless-only browser execution for packaged and container-friendly CLI use.
- Rejected `headless: false` flow files with an explicit error.
- Added a deterministic packaged smoke flow, `package-smoke.yaml`, that uses a self-contained `data:` URL instead of public websites.
- Updated macOS/Linux and Windows packaging scripts to copy `package-smoke.yaml` into published output.
- Added browser-bundled package output by runtime and browser set, including Chromium, Firefox, WebKit, and all-browser bundles.
- Added support for tagged package naming through the package scripts.
- Added flow-file `specVersion` validation.
- Added browser context options for user agent, locale, timezone, viewport, and extra HTTP headers.
- Added runner actions for select, check, uncheck, press, hover, upload, download, API request, tracing, screenshots, URL assertions, visibility assertions, and text assertions.
- Added JSON report output under `TestResults/report.json`.

## v0.1.0

- Initial release.
