using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PlaywrightRunner.Reporting;

public sealed class PdfReportWriter
{
    public const string DefaultReportName = "Playwright Test Report";

    private const string TextColor = "#111827";
    private const string MutedColor = "#6B7280";
    private const string BorderColor = "#D1D5DB";
    private const string PanelColor = "#F3F4F6";

    public string Write(
        TestReport report,
        string outputPath,
        string reportName = DefaultReportName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportName);

        var fullOutputPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullOutputPath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            AddOverallCover(document, report, reportName);

            foreach (var flow in report.Flows)
            {
                AddFlowCover(document, flow);

                foreach (var step in flow.Steps)
                    AddStepPage(document, flow, step);
            }
        }).GeneratePdf(fullOutputPath);

        return fullOutputPath;
    }

    private static void AddOverallCover(
        IDocumentContainer document,
        TestReport report,
        string reportName)
    {
        document.Page(page =>
        {
            ConfigurePage(page);

            page.Content()
                .AlignMiddle()
                .Column(column =>
                {
                    column.Spacing(14);

                    column.Item()
                        .Text(reportName)
                        .FontSize(30)
                        .Bold()
                        .FontColor(TextColor);

                    column.Item()
                        .Text($"Generated {report.GeneratedAt:dd MMMM yyyy HH:mm zzz}")
                        .FontSize(11)
                        .FontColor(MutedColor);

                    column.Item()
                        .PaddingTop(16)
                        .Element(container => ComposeSummary(
                            container,
                            [
                                new("Overall result", OverallStatus(report.FailedCount, report.NotRunCount)),
                                new("Test flows", report.Flows.Count.ToString()),
                                new("Steps", report.StepCount.ToString()),
                                new("Passed", report.PassedCount.ToString()),
                                new("Failed", report.FailedCount.ToString()),
                                new("Not run", report.NotRunCount.ToString()),
                                new("Total duration", FormatDuration(report.DurationMs))
                            ]));
                });

            ComposeFooter(page.Footer());
        });
    }

    private static void AddFlowCover(
        IDocumentContainer document,
        FlowReport flow)
    {
        document.Page(page =>
        {
            ConfigurePage(page);

            page.Content()
                .AlignMiddle()
                .Column(column =>
                {
                    column.Spacing(12);

                    column.Item()
                        .Text(flow.Flow.Name)
                        .FontSize(26)
                        .Bold()
                        .FontColor(TextColor);

                    column.Item()
                        .Text("Flow overview")
                        .FontSize(12)
                        .FontColor(MutedColor);

                    column.Item()
                        .PaddingTop(12)
                        .Element(container => ComposeSummary(
                            container,
                            BuildFlowSummary(flow)));
                });

            ComposeFooter(page.Footer());
        });
    }

    private static void AddStepPage(
        IDocumentContainer document,
        FlowReport flow,
        ReportStep step)
    {
        document.Page(page =>
        {
            ConfigurePage(page);

            page.Header()
                .PaddingBottom(8)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(flow.Flow.Name)
                        .SemiBold()
                        .FontColor(TextColor);

                    row.AutoItem()
                        .Text($"Step {step.Number} of {flow.Steps.Count}")
                        .FontColor(MutedColor);
                });

            page.Content().Column(column =>
            {
                column.Spacing(12);

                column.Item().Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Step {step.Number}: {step.Definition.Name}")
                        .FontSize(20)
                        .Bold()
                        .FontColor(TextColor);

                    row.AutoItem()
                        .Element(container => ComposeStatusBadge(container, step.Status));
                });

                column.Item()
                    .Text($"Action: {step.Definition.Action}")
                    .FontSize(11)
                    .FontColor(MutedColor);

                column.Item()
                    .Text("Required")
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(TextColor);

                column.Item()
                    .Element(container => ComposeSummary(container, step.Requirements));

                column.Item()
                    .Text("Result")
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(TextColor);

                column.Item().Element(container => ComposeResult(container, step));

                if (!string.IsNullOrWhiteSpace(step.ScreenshotPath))
                {
                    column.Item()
                        .Text("Screenshot")
                        .FontSize(13)
                        .SemiBold()
                        .FontColor(TextColor);

                    column.Item()
                        .Height(300)
                        .Border(1)
                        .BorderColor(BorderColor)
                        .Padding(6)
                        .Image(step.ScreenshotPath)
                        .FitArea();
                }

                if (!string.IsNullOrWhiteSpace(step.Warning))
                {
                    column.Item()
                        .Background("#FFF7ED")
                        .Border(1)
                        .BorderColor("#FDBA74")
                        .Padding(10)
                        .Text(step.Warning)
                        .FontColor("#9A3412");
                }
            });

            ComposeFooter(page.Footer());
        });
    }

    private static IReadOnlyCollection<ReportField> BuildFlowSummary(FlowReport flow)
    {
        var fields = new List<ReportField>
        {
            new("Overall result", OverallStatus(flow.FailedCount, flow.NotRunCount)),
            new("Specification version", flow.Flow.SpecVersion.ToString()),
            new("Base URL", flow.Flow.BaseUrl ?? "Not specified"),
            new("Browser", flow.Flow.Browser),
            new("Headless", flow.Flow.Headless ? "Yes" : "No")
        };

        if (!string.IsNullOrWhiteSpace(flow.Flow.Locale))
            fields.Add(new("Locale", flow.Flow.Locale));

        if (!string.IsNullOrWhiteSpace(flow.Flow.TimezoneId))
            fields.Add(new("Timezone", flow.Flow.TimezoneId));

        if (flow.Flow.Viewport is not null)
        {
            fields.Add(new(
                "Viewport",
                $"{flow.Flow.Viewport.Width} x {flow.Flow.Viewport.Height}"));
        }

        fields.Add(new("Input", flow.InputPath));
        fields.Add(new("Results", flow.ResultPath));
        fields.Add(new("Steps", flow.Steps.Count.ToString()));
        fields.Add(new("Passed", flow.PassedCount.ToString()));
        fields.Add(new("Failed", flow.FailedCount.ToString()));
        fields.Add(new("Not run", flow.NotRunCount.ToString()));
        fields.Add(new("Duration", FormatDuration(flow.DurationMs)));

        return fields;
    }

    private static void ComposeResult(IContainer container, ReportStep step)
    {
        var fields = new List<ReportField>
        {
            new("Status", StatusText(step.Status))
        };

        if (step.Result is not null)
        {
            fields.Add(new("Duration", FormatDuration(step.Result.DurationMs)));

            if (!string.IsNullOrWhiteSpace(step.Result.Data) &&
                !string.Equals(
                    step.Definition.Action,
                    "screenshot",
                    StringComparison.OrdinalIgnoreCase))
            {
                fields.Add(new("Output", step.Result.Data));
            }

            if (!string.IsNullOrWhiteSpace(step.Result.Error))
                fields.Add(new("Error", step.Result.Error));
        }
        else
        {
            fields.Add(new("Details", "The flow ended before this step was executed."));
        }

        ComposeSummary(container, fields);
    }

    private static void ComposeStatusBadge(
        IContainer container,
        ReportStepStatus status)
    {
        container
            .Background(StatusColor(status))
            .PaddingHorizontal(10)
            .PaddingVertical(5)
            .Text(StatusText(status))
            .Bold()
            .FontSize(10)
            .FontColor(Colors.White);
    }

    private static void ComposeSummary(
        IContainer container,
        IReadOnlyCollection<ReportField> fields)
    {
        container
            .Background(PanelColor)
            .Border(1)
            .BorderColor(BorderColor)
            .Padding(12)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(125);
                    columns.RelativeColumn();
                });

                foreach (var field in fields)
                {
                    table.Cell()
                        .PaddingVertical(4)
                        .Text(field.Label)
                        .SemiBold()
                        .FontColor(TextColor);

                    table.Cell()
                        .PaddingVertical(4)
                        .Text(field.Value)
                        .FontColor(TextColor);
                }
            });
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(18, Unit.Millimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style.FontSize(10).FontColor(TextColor));
    }

    private static void ComposeFooter(IContainer container)
    {
        container
            .AlignCenter()
            .DefaultTextStyle(style => style
                .FontSize(9)
                .FontColor(MutedColor))
            .Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
    }

    private static string OverallStatus(int failedCount, int notRunCount)
    {
        if (failedCount > 0)
            return "FAILED";

        if (notRunCount > 0)
            return "INCOMPLETE";

        return "PASSED";
    }

    private static string StatusText(ReportStepStatus status) => status switch
    {
        ReportStepStatus.Passed => "PASSED",
        ReportStepStatus.Failed => "FAILED",
        _ => "NOT RUN"
    };

    private static string StatusColor(ReportStepStatus status) => status switch
    {
        ReportStepStatus.Passed => "#15803D",
        ReportStepStatus.Failed => "#B91C1C",
        _ => "#6B7280"
    };

    private static string FormatDuration(long durationMs) =>
        durationMs < 1_000
            ? $"{durationMs:N0} ms"
            : $"{TimeSpan.FromMilliseconds(durationMs).TotalSeconds:N3} seconds";
}
