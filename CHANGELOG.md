# Changelog

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

- Initial release