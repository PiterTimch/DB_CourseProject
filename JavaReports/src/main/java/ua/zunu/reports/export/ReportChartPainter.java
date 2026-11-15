package ua.zunu.reports.export;

import ua.zunu.reports.domain.ChartType;
import ua.zunu.reports.domain.ReportPoint;
import ua.zunu.reports.domain.ReportResult;

import java.awt.BasicStroke;
import java.awt.Color;
import java.awt.Font;
import java.awt.RenderingHints;
import java.awt.image.BufferedImage;
import java.util.List;

public final class ReportChartPainter {
    private static final Color INK = new Color(24, 34, 56);
    private static final Color BLUE = new Color(59, 112, 232);
    private static final Color MUTED = new Color(123, 137, 155);
    private static final Color GRID = new Color(229, 234, 241);

    public BufferedImage render(ReportResult result, int width, int height) {
        var image = new BufferedImage(width, height, BufferedImage.TYPE_INT_RGB);
        var graphics = image.createGraphics();
        graphics.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
        graphics.setColor(Color.WHITE);
        graphics.fillRect(0, 0, width, height);
        var points = result.chartPoints().stream().limit(14).toList();
        if (points.isEmpty()) {
            graphics.dispose();
            return image;
        }
        var left = 54;
        var right = width - 24;
        var top = 24;
        var bottom = height - 52;
        graphics.setFont(new Font("SansSerif", Font.PLAIN, 11));
        graphics.setColor(MUTED);
        var max = Math.max(1, points.stream().mapToDouble(ReportPoint::value).max().orElse(1));
        for (var line = 0; line <= 4; line++) {
            var y = top + (bottom - top) * line / 4;
            graphics.setColor(GRID);
            graphics.drawLine(left, y, right, y);
            graphics.setColor(MUTED);
            graphics.drawString(shortValue(max * (4 - line) / 4), 8, y + 4);
        }
        if (result.type().chartType() == ChartType.LINE) {
            drawLine(points, graphics, left, right, top, bottom, max);
        } else {
            drawBars(points, graphics, left, right, top, bottom, max);
        }
        graphics.dispose();
        return image;
    }

    private static void drawBars(List<ReportPoint> points, java.awt.Graphics2D graphics, int left, int right, int top, int bottom, double max) {
        var slot = (right - left) / (double) points.size();
        for (var index = 0; index < points.size(); index++) {
            var point = points.get(index);
            var barHeight = (bottom - top) * Math.max(0, point.value()) / max;
            var x = (int) (left + slot * index + slot * .18);
            var width = Math.max(8, (int) (slot * .64));
            graphics.setColor(BLUE);
            graphics.fillRoundRect(x, (int) (bottom - barHeight), width, (int) barHeight, 8, 8);
            drawLabel(point.label(), graphics, x + width / 2, bottom + 18, slot);
        }
    }

    private static void drawLine(List<ReportPoint> points, java.awt.Graphics2D graphics, int left, int right, int top, int bottom, double max) {
        var slot = (right - left) / (double) Math.max(1, points.size() - 1);
        graphics.setColor(BLUE);
        graphics.setStroke(new BasicStroke(3, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        var previousX = 0;
        var previousY = 0;
        for (var index = 0; index < points.size(); index++) {
            var x = (int) (left + slot * index);
            var y = (int) (bottom - (bottom - top) * Math.max(0, points.get(index).value()) / max);
            if (index > 0) {
                graphics.drawLine(previousX, previousY, x, y);
            }
            graphics.fillOval(x - 4, y - 4, 8, 8);
            drawLabel(points.get(index).label(), graphics, x, bottom + 18, slot);
            previousX = x;
            previousY = y;
        }
    }

    private static void drawLabel(String label, java.awt.Graphics2D graphics, int center, int y, double slot) {
        var clipped = label.length() > 14 ? label.substring(0, 13) + "…" : label;
        var textWidth = graphics.getFontMetrics().stringWidth(clipped);
        graphics.setColor(MUTED);
        graphics.drawString(clipped, center - Math.min(textWidth, (int) slot) / 2, y);
    }

    private static String shortValue(double value) {
        if (value >= 1_000_000) {
            return String.format("%.1fM", value / 1_000_000);
        }
        if (value >= 1_000) {
            return String.format("%.1fK", value / 1_000);
        }
        return String.format("%.0f", value);
    }
}
