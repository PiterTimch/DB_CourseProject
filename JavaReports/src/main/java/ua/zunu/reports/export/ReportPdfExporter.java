package ua.zunu.reports.export;

import org.apache.pdfbox.pdmodel.PDDocument;
import org.apache.pdfbox.pdmodel.PDPage;
import org.apache.pdfbox.pdmodel.PDPageContentStream;
import org.apache.pdfbox.pdmodel.common.PDRectangle;
import org.apache.pdfbox.pdmodel.graphics.image.LosslessFactory;
import org.apache.pdfbox.pdmodel.font.PDType0Font;
import ua.zunu.reports.domain.ReportResult;

import java.awt.Color;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;

public final class ReportPdfExporter {
    private static final float MARGIN = 38;
    private static final float FONT_SIZE = 8.5f;
    private static final float LINE_HEIGHT = 11f;
    private final ReportChartPainter charts = new ReportChartPainter();

    public void export(ReportResult report, Path file) throws IOException {
        var parent = file.toAbsolutePath().getParent();
        if (parent != null) {
            Files.createDirectories(parent);
        }
        try (var document = new PDDocument()) {
            var font = PDType0Font.load(document, findFont().toFile());
            var pageSize = new PDRectangle(PDRectangle.A4.getHeight(), PDRectangle.A4.getWidth());
            var widths = columnWidths(report, font, pageSize.getWidth() - 2 * MARGIN);
            var pageNumber = 1;
            var page = new PDPage(pageSize);
            document.addPage(page);
            var stream = new PDPageContentStream(document, page);
            var y = drawPageHeading(stream, document, pageSize, font, report, pageNumber, true);
            y = drawTableHeader(stream, font, report.columns(), widths, y);
            for (var rowIndex = 0; rowIndex < report.rows().size(); rowIndex++) {
                var row = report.rows().get(rowIndex);
                var lines = wrapRow(row, widths, font);
                var rowHeight = Math.max(25, lines.stream().mapToInt(List::size).max().orElse(1) * LINE_HEIGHT + 12);
                if (y - rowHeight < MARGIN) {
                    stream.close();
                    pageNumber++;
                    page = new PDPage(pageSize);
                    document.addPage(page);
                    stream = new PDPageContentStream(document, page);
                    y = drawPageHeading(stream, document, pageSize, font, report, pageNumber, false);
                    y = drawTableHeader(stream, font, report.columns(), widths, y);
                }
                y = drawRow(stream, font, row, lines, widths, y, rowIndex % 2 == 0, rowHeight);
            }
            if (report.rows().isEmpty()) {
                write(stream, font, "За обраний період записів немає.", MARGIN + 8, y - 20, FONT_SIZE + 1, new Color(103, 116, 134));
            }
            stream.close();
            document.save(file.toFile());
        }
    }

    private float drawPageHeading(PDPageContentStream stream, PDDocument document, PDRectangle pageSize, PDType0Font font, ReportResult report, int pageNumber, boolean includeChart) throws IOException {
        var y = pageSize.getHeight() - MARGIN;
        write(stream, font, "GARAGEFLOW  /  ЗВІТИ СЕРВІСНОГО ЦЕНТРУ", MARGIN, y, 8, new Color(80, 96, 119));
        write(stream, font, "Сторінка " + pageNumber, pageSize.getWidth() - MARGIN - 60, y, 8, new Color(80, 96, 119));
        y -= 30;
        write(stream, font, report.type().title(), MARGIN, y, 18, new Color(24, 34, 56));
        y -= 19;
        write(stream, font, report.type().description(), MARGIN, y, 9, new Color(103, 116, 134));
        y -= 16;
        write(stream, font, "Період: " + report.periodLabel() + "     Сформовано: " + LocalDateTime.now().format(DateTimeFormatter.ofPattern("dd.MM.yyyy HH:mm")), MARGIN, y, 8, new Color(103, 116, 134));
        y -= 20;
        if (includeChart && !report.chartPoints().isEmpty()) {
            var chart = charts.render(report, 1050, 260);
            var chartImage = LosslessFactory.createFromImage(document, chart);
            var imageWidth = Math.min(390, pageSize.getWidth() - 2 * MARGIN);
            var imageHeight = imageWidth * chart.getHeight() / chart.getWidth();
            stream.drawImage(chartImage, MARGIN, y - imageHeight, imageWidth, imageHeight);
            y -= imageHeight + 15;
        }
        return y;
    }

    private static float drawTableHeader(PDPageContentStream stream, PDType0Font font, List<String> columns, float[] widths, float y) throws IOException {
        var height = 25f;
        var x = MARGIN;
        for (var index = 0; index < columns.size(); index++) {
            stream.setNonStrokingColor(new Color(24, 34, 56));
            stream.addRect(x, y - height, widths[index], height);
            stream.fill();
            write(stream, font, fit(columns.get(index), font, 8, widths[index] - 12), x + 6, y - 16, 8, Color.WHITE);
            x += widths[index];
        }
        return y - height;
    }

    private static float drawRow(PDPageContentStream stream, PDType0Font font, List<String> row, List<List<String>> lines, float[] widths, float y, boolean alternate, float height) throws IOException {
        var x = MARGIN;
        for (var index = 0; index < widths.length; index++) {
            stream.setNonStrokingColor(alternate ? new Color(248, 250, 253) : Color.WHITE);
            stream.addRect(x, y - height, widths[index], height);
            stream.fill();
            stream.setStrokingColor(new Color(226, 231, 239));
            stream.addRect(x, y - height, widths[index], height);
            stream.stroke();
            var lineY = y - 15;
            for (var text : lines.get(index)) {
                write(stream, font, text, x + 6, lineY, FONT_SIZE, new Color(37, 50, 72));
                lineY -= LINE_HEIGHT;
            }
            x += widths[index];
        }
        return y - height;
    }

    private static float[] columnWidths(ReportResult report, PDType0Font font, float totalWidth) throws IOException {
        var weights = new float[report.columns().size()];
        var total = 0f;
        for (var column = 0; column < weights.length; column++) {
            var max = font.getStringWidth(report.columns().get(column)) / 1000 * FONT_SIZE;
            for (var row = 0; row < Math.min(report.rows().size(), 25); row++) {
                max = Math.max(max, Math.min(220, font.getStringWidth(report.rows().get(row).get(column)) / 1000 * FONT_SIZE));
            }
            weights[column] = Math.max(40, Math.min(max + 18, 190));
            total += weights[column];
        }
        var widths = new float[weights.length];
        for (var index = 0; index < widths.length; index++) {
            widths[index] = totalWidth * weights[index] / total;
        }
        return widths;
    }

    private static List<List<String>> wrapRow(List<String> row, float[] widths, PDType0Font font) throws IOException {
        var result = new ArrayList<List<String>>();
        for (var index = 0; index < row.size(); index++) {
            result.add(wrap(row.get(index), font, widths[index] - 12, FONT_SIZE));
        }
        return result;
    }

    private static List<String> wrap(String value, PDType0Font font, float width, float size) throws IOException {
        var words = value.split("\\s+");
        var lines = new ArrayList<String>();
        var current = new StringBuilder();
        for (var word : words) {
            var candidate = current.isEmpty() ? word : current + " " + word;
            if (!current.isEmpty() && font.getStringWidth(candidate) / 1000 * size > width) {
                lines.add(current.toString());
                current = new StringBuilder(word);
            } else {
                current = new StringBuilder(candidate);
            }
        }
        if (!current.isEmpty()) {
            lines.add(current.toString());
        }
        return lines.isEmpty() ? List.of("") : lines;
    }

    private static String fit(String value, PDType0Font font, float size, float width) throws IOException {
        var result = value;
        while (!result.isEmpty() && font.getStringWidth(result) / 1000 * size > width) {
            result = result.substring(0, result.length() - 1);
        }
        return result.equals(value) ? result : result + "…";
    }

    private static void write(PDPageContentStream stream, PDType0Font font, String value, float x, float y, float size, Color color) throws IOException {
        stream.beginText();
        stream.setNonStrokingColor(color);
        stream.setFont(font, size);
        stream.newLineAtOffset(x, y);
        stream.showText(value);
        stream.endText();
    }

    private static Path findFont() throws IOException {
        var candidates = List.of(
            Path.of("C:/Windows/Fonts/arial.ttf"),
            Path.of("C:/Windows/Fonts/segoeui.ttf"),
            Path.of("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
            Path.of("/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf"),
            Path.of("/System/Library/Fonts/Supplemental/Arial.ttf")
        );
        return candidates.stream().filter(Files::isRegularFile).findFirst().orElseThrow(() -> new IOException("A font with Ukrainian glyphs was not found on this computer."));
    }
}
