package ua.zunu.reports.domain;

public enum ReportType {
    ORDERS("Журнал замовлень", "Замовлення за обраний період із клієнтом, авто, пробігом і сумою.", ChartType.NONE),
    EMPLOYEE_CARS("Працівники та автомобілі", "Автомобілі й кількість виконаних замовлень кожного працівника.", ChartType.NONE),
    SERVICE_COUNTS("Виконані послуги", "Кількість виконань кожної послуги за період.", ChartType.BAR),
    MONTHLY_REVENUE("Дохід за місяцями", "Чистий дохід та кількість закритих замовлень у динаміці.", ChartType.LINE),
    ORDER_STATUSES("Статуси замовлень", "Розподіл замовлень і їх вартості за статусами.", ChartType.BAR),
    TOP_SERVICES("Популярні послуги", "Найпопулярніші роботи за кількістю та сумою.", ChartType.BAR),
    PART_USAGE("Використання запчастин", "Кількість і вартість запчастин, використаних у закритих замовленнях.", ChartType.BAR),
    EMPLOYEE_PERFORMANCE("Результативність працівників", "Виконані роботи, замовлення та виручка за виконавцями.", ChartType.BAR),
    CUSTOMER_ACTIVITY("Активність клієнтів", "Кількість відвідувань, витрати та дата останнього візиту.", ChartType.BAR),
    OVERDUE_ORDERS("Прострочені замовлення", "Відкриті замовлення, термін яких уже минув.", ChartType.NONE),
    MILEAGE_PROGRESS("Пробіг автомобілів", "Зміна зафіксованого пробігу між першим та останнім замовленням.", ChartType.BAR);

    private final String title;
    private final String description;
    private final ChartType chartType;

    ReportType(String title, String description, ChartType chartType) {
        this.title = title;
        this.description = description;
        this.chartType = chartType;
    }

    public String title() {
        return title;
    }

    public String description() {
        return description;
    }

    public ChartType chartType() {
        return chartType;
    }
}
