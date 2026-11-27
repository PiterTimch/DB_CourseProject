package ua.zunu.reports.ui;

import javafx.application.Platform;
import javafx.beans.property.ReadOnlyStringWrapper;
import javafx.collections.FXCollections;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.Scene;
import javafx.scene.chart.BarChart;
import javafx.scene.chart.CategoryAxis;
import javafx.scene.chart.LineChart;
import javafx.scene.chart.NumberAxis;
import javafx.scene.chart.XYChart;
import javafx.scene.control.Alert;
import javafx.scene.control.Button;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Label;
import javafx.scene.control.ScrollPane;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.Tooltip;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.Priority;
import javafx.scene.layout.VBox;
import javafx.stage.FileChooser;
import javafx.stage.Stage;
import ua.zunu.reports.ReportApplication;
import ua.zunu.reports.domain.Director;
import ua.zunu.reports.domain.ReportResult;
import ua.zunu.reports.domain.ReportType;
import ua.zunu.reports.export.ReportPdfExporter;
import ua.zunu.reports.service.ReportService;

import java.awt.Desktop;
import java.io.IOException;
import java.nio.file.Path;
import java.sql.SQLException;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public final class DashboardView {
    private final ReportService reports;
    private final ReportPdfExporter pdfExporter;
    private final ExecutorService worker = Executors.newSingleThreadExecutor(Thread.ofPlatform().daemon().factory());
    private final VBox navigation = new VBox(7);
    private final VBox chartBox = new VBox(10);
    private final TableView<java.util.List<String>> table = new TableView<>();
    private final Label title = new Label();
    private final Label description = new Label();
    private final Label rowCount = new Label();
    private final Label status = new Label();
    private final DatePicker fromDate = new DatePicker(LocalDate.now().withDayOfYear(1));
    private final DatePicker toDate = new DatePicker(LocalDate.now());
    private final Button exportButton = new Button("↓  Експорт PDF");
    private ReportType selected = ReportType.ORDERS;
    private ReportResult currentReport;

    public DashboardView(ReportService reports, ReportPdfExporter pdfExporter) {
        this.reports = reports;
        this.pdfExporter = pdfExporter;
    }

    public void show(Stage stage, Director director) {
        var shell = new BorderPane();
        shell.getStyleClass().add("app-shell");
        shell.setLeft(buildSidebar(stage, director));
        shell.setCenter(buildContent(director));
        var scene = new Scene(shell, 1440, 900);
        scene.getStylesheets().add(getClass().getResource("/ua/zunu/reports/ui/theme.css").toExternalForm());
        stage.setTitle("Garage · Звіти");
        stage.setMinWidth(1120);
        stage.setMinHeight(720);
        stage.setScene(scene);
        stage.centerOnScreen();
        stage.show();
        stage.setOnCloseRequest(event -> worker.shutdownNow());
        selectReport(selected);
    }

    private VBox buildSidebar(Stage stage, Director director) {
        var sidebar = new VBox(18);
        sidebar.getStyleClass().add("sidebar");
        sidebar.setPadding(new Insets(28, 18, 18, 18));
        sidebar.setPrefWidth(275);
        var brand = new HBox(12, new Label("G"), new VBox(1, new Label("Garage"), new Label("ГЕНЕРАТОР ЗВІТІВ")));
        brand.getStyleClass().add("brand-block");
        brand.getChildren().getFirst().getStyleClass().add("brand-mark");
        brand.getChildren().get(1).getStyleClass().add("brand-copy");
        var section = new Label("АНАЛІТИКА");
        section.getStyleClass().add("side-caption");
        navigation.getStyleClass().add("report-navigation");
        for (var report : ReportType.values()) {
            var button = new Button(report.title());
            button.setMaxWidth(Double.MAX_VALUE);
            button.setAlignment(Pos.CENTER_LEFT);
            button.getStyleClass().add("report-nav-button");
            button.setOnAction(event -> selectReport(report));
            navigation.getChildren().add(button);
        }
        var spacer = new VBox();
        VBox.setVgrow(spacer, Priority.ALWAYS);
        var directorLabel = new Label(director.firstName() + " " + director.lastName());
        directorLabel.getStyleClass().add("director-name");
        var roleLabel = new Label("КЕРІВНИК");
        roleLabel.getStyleClass().add("director-role");
        var userCard = new VBox(4, directorLabel, roleLabel);
        userCard.getStyleClass().add("user-card");
        var logout = new Button("↪  Вийти із системи");
        logout.getStyleClass().add("logout-button");
        logout.setMaxWidth(Double.MAX_VALUE);
        logout.setOnAction(event -> {
            worker.shutdownNow();
            new LoginView(ReportApplication.authentication()).show(stage, next -> new DashboardView(reports, pdfExporter).show(stage, next));
        });
        sidebar.getChildren().addAll(brand, section, navigation, spacer, userCard, logout);
        return sidebar;
    }

    private VBox buildContent(Director director) {
        var content = new VBox(22);
        content.getStyleClass().add("main-content");
        content.setPadding(new Insets(34, 42, 36, 42));
        var eyebrow = new Label("ЦЕНТР КЕРУВАННЯ  /  " + director.login().toUpperCase());
        eyebrow.getStyleClass().add("eyebrow");
        title.getStyleClass().add("page-title");
        description.getStyleClass().add("page-description");
        var heading = new VBox(7, eyebrow, title, description);
        var startLabel = new Label("Від");
        var endLabel = new Label("До");
        startLabel.getStyleClass().add("field-label");
        endLabel.getStyleClass().add("field-label");
        fromDate.getStyleClass().add("date-input");
        toDate.getStyleClass().add("date-input");
        var apply = new Button("Оновити звіт");
        apply.getStyleClass().addAll("button", "button-primary");
        apply.setOnAction(event -> loadSelectedReport());
        exportButton.getStyleClass().addAll("button", "button-secondary");
        exportButton.setDisable(true);
        exportButton.setOnAction(event -> exportCurrentReport());
        var filters = new HBox(12, field(startLabel, fromDate), field(endLabel, toDate), apply, exportButton);
        filters.setAlignment(Pos.BOTTOM_LEFT);
        filters.getStyleClass().add("filter-panel");
        var chartHeader = new HBox(new Label("ДИНАМІКА ПОКАЗНИКІВ"));
        chartHeader.getStyleClass().add("chart-heading");
        chartBox.getStyleClass().add("chart-card");
        chartBox.getChildren().addAll(chartHeader, new Label("Оберіть звіт для побудови графіка."));
        var tableHeading = new HBox(10, new Label("ДЕТАЛІЗАЦІЯ"), rowCount);
        tableHeading.getStyleClass().add("table-heading");
        table.getStyleClass().add("report-table");
        table.setPlaceholder(new Label("Немає записів за обраний період"));
        VBox.setVgrow(table, Priority.ALWAYS);
        status.getStyleClass().add("status-message");
        var scroll = new ScrollPane(new VBox(22, heading, filters, chartBox, tableHeading, table, status));
        scroll.setFitToWidth(true);
        scroll.getStyleClass().add("content-scroll");
        VBox.setVgrow(scroll, Priority.ALWAYS);
        content.getChildren().add(scroll);
        return content;
    }

    private void selectReport(ReportType report) {
        selected = report;
        var index = 0;
        for (var node : navigation.getChildren()) {
            node.getStyleClass().remove("active");
            if (index++ == report.ordinal()) {
                node.getStyleClass().add("active");
            }
        }
        loadSelectedReport();
    }

    private void loadSelectedReport() {
        try {
            selectedRange();
        } catch (IllegalArgumentException exception) {
            showError(exception.getMessage());
            return;
        }
        title.setText(selected.title());
        description.setText(selected.description());
        status.setText("Формуємо звіт…");
        exportButton.setDisable(true);
        var type = selected;
        var from = fromDate.getValue();
        var to = toDate.getValue();
        CompletableFuture.supplyAsync(() -> {
            try {
                return reports.load(type, from, to);
            } catch (SQLException exception) {
                throw new RuntimeException(exception);
            }
        }, worker).whenComplete((report, error) -> Platform.runLater(() -> {
            if (error != null) {
                status.setText("Не вдалося отримати дані з бази.");
                showError("Не вдалося сформувати звіт. Перевір підключення до PostgreSQL.");
                return;
            }
            currentReport = report;
            renderTable(report);
            renderChart(report);
            rowCount.setText(report.rows().size() + " записів");
            status.setText("Оновлено " + LocalDate.now().format(DateTimeFormatter.ofPattern("dd.MM.yyyy")));
            exportButton.setDisable(false);
        }));
    }

    private void renderTable(ReportResult report) {
        table.getColumns().clear();
        table.getItems().clear();
        for (var index = 0; index < report.columns().size(); index++) {
            var columnIndex = index;
            var column = new TableColumn<java.util.List<String>, String>(report.columns().get(index));
            column.setCellValueFactory(cell -> new ReadOnlyStringWrapper(cell.getValue().get(columnIndex)));
            column.setPrefWidth(Math.max(115, 850.0 / report.columns().size()));
            column.setMinWidth(90);
            table.getColumns().add(column);
        }
        table.setItems(FXCollections.observableArrayList(report.rows()));
    }

    private void renderChart(ReportResult report) {
        chartBox.getChildren().removeIf(node -> node instanceof javafx.scene.chart.Chart || node.getStyleClass().contains("chart-empty"));
        if (report.type().chartType() == ua.zunu.reports.domain.ChartType.NONE) {
            chartBox.getChildren().add(new Label("Цей звіт зручно переглядати у таблиці."));
            chartBox.getChildren().getLast().getStyleClass().add("chart-empty");
            return;
        }
        if (report.chartPoints().isEmpty()) {
            chartBox.getChildren().add(new Label("За обраний період немає даних для графіка."));
            chartBox.getChildren().getLast().getStyleClass().add("chart-empty");
            return;
        }
        var xAxis = new CategoryAxis();
        xAxis.setTickLabelRotation(20);
        var yAxis = new NumberAxis();
        yAxis.setForceZeroInRange(true);
        var chart = report.type().chartType() == ua.zunu.reports.domain.ChartType.LINE
            ? new LineChart<String, Number>(xAxis, yAxis)
            : new BarChart<String, Number>(xAxis, yAxis);
        chart.setLegendVisible(false);
        chart.setAnimated(false);
        chart.setTitle(report.chartSeriesName());
        if (chart instanceof LineChart<String, Number> lineChart) {
            lineChart.setCreateSymbols(true);
        }
        var series = new XYChart.Series<String, Number>();
        series.setName(report.chartSeriesName());
        report.chartPoints().stream().limit(16).forEach(point -> series.getData().add(new XYChart.Data<>(point.label(), point.value())));
        chart.getData().add(series);
        chart.setPrefHeight(280);
        chartBox.getChildren().add(chart);
    }

    private void exportCurrentReport() {
        if (currentReport == null) {
            return;
        }
        var chooser = new FileChooser();
        chooser.setTitle("Зберегти звіт у PDF");
        chooser.setInitialFileName("garage-" + selected.name().toLowerCase() + ".pdf");
        chooser.getExtensionFilters().add(new FileChooser.ExtensionFilter("PDF document", "*.pdf"));
        var selectedFile = chooser.showSaveDialog(table.getScene().getWindow());
        if (selectedFile == null) {
            return;
        }
        var file = selectedFile.getName().toLowerCase().endsWith(".pdf")
            ? selectedFile
            : new java.io.File(selectedFile.getParentFile(), selectedFile.getName() + ".pdf");
        status.setText("Створюємо PDF…");
        exportButton.setDisable(true);
        var report = currentReport;
        CompletableFuture.runAsync(() -> {
            try {
                pdfExporter.export(report, Path.of(file.toURI()));
                if (Desktop.isDesktopSupported()) {
                    Desktop.getDesktop().open(file);
                }
            } catch (IOException exception) {
                throw new RuntimeException(exception);
            }
        }, worker).whenComplete((ignored, error) -> Platform.runLater(() -> {
            exportButton.setDisable(false);
            if (error != null) {
                status.setText("Не вдалося створити PDF.");
                showError("Не вдалося експортувати PDF. Перевір обрану папку та наявність шрифту з українськими літерами.");
                return;
            }
            status.setText("PDF збережено: " + file.getAbsolutePath());
        }));
    }

    private javafx.scene.Node field(Label label, DatePicker picker) {
        var box = new VBox(6, label, picker);
        box.getStyleClass().add("date-field");
        return box;
    }

    private ua.zunu.reports.domain.DateRange selectedRange() {
        return new ua.zunu.reports.domain.DateRange(fromDate.getValue(), toDate.getValue());
    }

    private static void showError(String message) {
        var alert = new Alert(Alert.AlertType.ERROR, message);
        alert.setHeaderText("Потрібна перевірка");
        alert.setTitle("Garage · Повідомлення");
        alert.showAndWait();
    }
}
