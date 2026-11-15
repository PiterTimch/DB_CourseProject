package ua.zunu.reports.domain;

import java.util.List;

public record ChartData(String seriesName, List<ReportPoint> points) {
    public ChartData {
        points = List.copyOf(points);
    }
}
