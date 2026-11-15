package ua.zunu.reports.domain;

import java.time.LocalDate;
import java.util.List;

public record ReportResult(ReportType type, DateRange range, List<String> columns, List<List<String>> rows, List<ReportPoint> chartPoints, String chartSeriesName) {
    public ReportResult {
        columns = List.copyOf(columns);
        rows = rows.stream().map(List::copyOf).toList();
        chartPoints = List.copyOf(chartPoints);
        chartSeriesName = chartSeriesName == null ? "" : chartSeriesName;
    }

    public String periodLabel() {
        return range.from() + " — " + range.to();
    }
}
