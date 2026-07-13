[CmdletBinding()]
param(
    [string]$OutputPath = "TestResults/manhattan-suite-report.pdf"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $ProjectRoot "src/PlaywrightRunner/PlaywrightRunner.csproj"

$FlowFiles = @(
    "manhattan_qgao_uat.yaml"
    "manhattan_qgao_dev.yaml"
    "manhattan_qgao_sup.yaml"
    "manhattan_qgao_trn.yaml"
    "manhattan_qgao_prod.yaml"
    "manhattan_cba_uat.yaml"
    "manhattan_cba_uat4.yaml"
    "manhattan_cba_uat5.yaml"
    "manhattan_cba_sup.yaml"
    "manhattan_cba_pp.yaml"
    "manhattan_cba_prod.yaml"
    "manhattan_vha_sup.yaml"
    "manhattan_vha_prod.yaml"
    "manhattan_vha_uat.yaml"
    "manhattan_vha_dev.yaml"
    "manhattan_hn_dev.yaml"
    "manhattan_hn_sup.yaml"
    "manhattan_hn_uat.yaml"
    "manhattan_hn_prod.yaml"
    # "manhattan_ee_dev.yaml"
    "manhattan_ee_sup.yaml"
    "manhattan_ee_uat.yaml"
    "manhattan_ee_prod.yaml"
    # "manhattan_inlandrail_dev.yaml"
    # "manhattan_inlandrail_sup.yaml"
    "manhattan_inlandrail_uat.yaml"
    "manhattan_inlandrail_prod.yaml"
    "manhattan_mri_demo.yaml"
    "manhattan_std_cfg.yaml"
)

$MissingFlows = @(
    $FlowFiles | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $ProjectRoot $_) -PathType Leaf)
    }
)

if ($MissingFlows.Count -gt 0) {
    throw "Missing Manhattan flow files: $($MissingFlows -join ', ')"
}

$FailedFlows = [System.Collections.Generic.List[string]]::new()

Push-Location $ProjectRoot

try {
    foreach ($FlowFile in $FlowFiles) {
        Write-Host ""
        Write-Host "=== Running $FlowFile ===" -ForegroundColor Cyan

        & dotnet run --project $ProjectPath -- $FlowFile

        if ($LASTEXITCODE -ne 0) {
            $FailedFlows.Add($FlowFile)
            Write-Warning "$FlowFile exited with code $LASTEXITCODE. Continuing with the remaining flows."
        }
    }

    Write-Host ""
    Write-Host "=== Generating combined PDF ===" -ForegroundColor Cyan

    $ReportArguments = @(
        "run"
        "--project"
        $ProjectPath
        "--"
        "--report"
        "--report-name"
        "Manhattan Test Report"
        "--output"
        $OutputPath
    )

    foreach ($FlowFile in $FlowFiles) {
        $ReportArguments += @("--input", $FlowFile)
    }

    & dotnet @ReportArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Combined PDF generation failed with exit code $LASTEXITCODE."
    }

    $ResolvedOutputPath = [System.IO.Path]::GetFullPath(
        (Join-Path $ProjectRoot $OutputPath))

    Write-Host ""
    Write-Host "Combined PDF: $ResolvedOutputPath" -ForegroundColor Green

    if ($FailedFlows.Count -gt 0) {
        Write-Warning "$($FailedFlows.Count) flow(s) returned a non-zero exit code: $($FailedFlows -join ', ')"
        exit 1
    }
}
finally {
    Pop-Location
}
