package ua.zunu.reports.data;

import ua.zunu.reports.domain.DateRange;
import ua.zunu.reports.domain.ReportPoint;
import ua.zunu.reports.domain.ReportResult;
import ua.zunu.reports.domain.ReportType;
import ua.zunu.reports.infrastructure.DatabaseConnectionFactory;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.List;

public final class JdbcReportRepository implements ReportRepository {
    private final DatabaseConnectionFactory connections;

    public JdbcReportRepository(DatabaseConnectionFactory connections) {
        this.connections = connections;
    }

    @Override
    public ReportResult load(ReportType type, DateRange range) throws SQLException {
        var query = queryFor(type);
        var rows = new ArrayList<List<String>>();
        var points = new ArrayList<ReportPoint>();
        try (var connection = connections.open(); var statement = connection.prepareStatement(query.sql())) {
            statement.setObject(1, range.from());
            statement.setObject(2, range.to());
            try (var result = statement.executeQuery()) {
                while (result.next()) {
                    rows.add(readRow(result, query.columns().size()));
                    if (query.chartColumn() >= 0) {
                        var label = rows.getLast().get(query.labelColumn());
                        var value = rows.getLast().get(query.chartColumn());
                        points.add(new ReportPoint(label, parseNumber(value)));
                    }
                }
            }
        }
        return new ReportResult(type, range, query.columns(), rows, points, query.chartSeriesName());
    }

    private static List<String> readRow(ResultSet result, int columnCount) throws SQLException {
        var row = new ArrayList<String>(columnCount);
        for (var index = 1; index <= columnCount; index++) {
            var value = result.getString(index);
            row.add(value == null || value.isBlank() ? "—" : value);
        }
        return row;
    }

    private static double parseNumber(String value) {
        try {
            return Double.parseDouble(value.replace(',', '.').replaceAll("[^0-9.\\-]", ""));
        } catch (NumberFormatException exception) {
            return 0;
        }
    }

    private static Query queryFor(ReportType type) {
        return switch (type) {
            case ORDERS -> new Query("""
                SELECT to_char(o.order_date, 'DD.MM.YYYY'),
                       '№ ' || c.car_id || ' · ' || c.model || ' · ' || c.license_plate,
                       concat_ws(' ', cu.last_name, cu.first_name),
                       concat_ws(' ', e.last_name, e.first_name),
                       CASE o.status WHEN 'open' THEN 'Нове' WHEN 'in_progress' THEN 'У роботі' WHEN 'closed' THEN 'Закрито' ELSE o.status END,
                       COALESCE(to_char(o.deadline, 'DD.MM.YYYY'), '—'),
                       o.mileage,
                       to_char(o.total_cost, 'FM999G999G990D00') || ' ₴'
                FROM orders o
                JOIN car c ON c.car_id = o.car_id
                JOIN customer cu ON cu.customer_id = c.customer_id
                JOIN employee e ON e.employee_id = o.employee_id
                WHERE o.order_date BETWEEN ? AND ?
                ORDER BY o.order_date DESC, o.order_id DESC
                """, List.of("Дата", "Автомобіль", "Клієнт", "Менеджер", "Статус", "Термін", "Пробіг, км", "Сума"), -1, -1, "");
            case EMPLOYEE_CARS -> new Query("""
                SELECT concat_ws(' ', e.last_name, e.first_name),
                       COALESCE(string_agg(DISTINCT '№ ' || c.car_id || ' · ' || c.model || ' · ' || c.license_plate, ', ' ORDER BY '№ ' || c.car_id || ' · ' || c.model || ' · ' || c.license_plate), '—'),
                       count(DISTINCT o.order_id),
                       COALESCE(sum(os.quantity) FILTER (WHERE o.order_id IS NOT NULL), 0)
                FROM employee e
                LEFT JOIN order_service os ON os.employee_id = e.employee_id
                LEFT JOIN orders o ON o.order_id = os.order_id AND o.status = 'closed' AND o.order_date BETWEEN ? AND ?
                LEFT JOIN car c ON c.car_id = o.car_id
                GROUP BY e.employee_id, e.last_name, e.first_name
                ORDER BY e.last_name, e.first_name
                """, List.of("Працівник", "Автомобілі", "Закриті замовлення", "Роботи виконано"), -1, -1, "");
            case SERVICE_COUNTS -> new Query("""
                SELECT s.name, COALESCE(sum(os.quantity), 0)::bigint
                FROM service s
                LEFT JOIN order_service os ON os.service_id = s.service_id
                LEFT JOIN orders o ON o.order_id = os.order_id AND o.status = 'closed' AND o.order_date BETWEEN ? AND ?
                GROUP BY s.service_id, s.name
                ORDER BY COALESCE(sum(os.quantity) FILTER (WHERE o.order_id IS NOT NULL), 0) DESC, s.name
                """, List.of("Назва послуги", "Кількість виконань"), 1, 0, "Виконання");
            case MONTHLY_REVENUE -> new Query("""
                SELECT to_char(date_trunc('month', o.order_date), 'YYYY-MM'),
                       to_char(sum(o.total_cost), 'FM999G999G990D00'),
                       count(*)
                FROM orders o
                WHERE o.status = 'closed' AND o.order_date BETWEEN ? AND ?
                GROUP BY date_trunc('month', o.order_date)
                ORDER BY date_trunc('month', o.order_date)
                """, List.of("Місяць", "Дохід, грн", "Замовлень закрито"), 1, 0, "Дохід, грн");
            case ORDER_STATUSES -> new Query("""
                SELECT CASE status WHEN 'open' THEN 'Нове' WHEN 'in_progress' THEN 'У роботі' WHEN 'closed' THEN 'Закрито' ELSE status END,
                       count(*),
                       to_char(sum(total_cost), 'FM999G999G990D00')
                FROM orders
                WHERE order_date BETWEEN ? AND ?
                GROUP BY status
                ORDER BY count(*) DESC
                """, List.of("Статус", "Замовлень", "Сума, грн"), 1, 0, "Замовлення");
            case TOP_SERVICES -> new Query("""
                SELECT s.name, sum(os.quantity), count(DISTINCT o.order_id), to_char(sum(os.quantity * os.price), 'FM999G999G990D00')
                FROM service s
                JOIN order_service os ON os.service_id = s.service_id
                JOIN orders o ON o.order_id = os.order_id
                WHERE o.status = 'closed' AND o.order_date BETWEEN ? AND ?
                GROUP BY s.service_id, s.name
                ORDER BY sum(os.quantity) DESC, sum(os.quantity * os.price) DESC
                LIMIT 20
                """, List.of("Послуга", "Виконань", "Замовлень", "Виручка, грн"), 1, 0, "Виконання");
            case PART_USAGE -> new Query("""
                SELECT p.name, p.article, sum(op.quantity), count(DISTINCT o.order_id), to_char(sum(op.quantity * op.price), 'FM999G999G990D00')
                FROM part p
                JOIN order_part op ON op.part_id = p.part_id
                JOIN orders o ON o.order_id = op.order_id
                WHERE o.status = 'closed' AND o.order_date BETWEEN ? AND ?
                GROUP BY p.part_id, p.name, p.article
                ORDER BY sum(op.quantity) DESC, p.name
                """, List.of("Запчастина", "Артикул", "Кількість", "Замовлень", "Вартість, грн"), 2, 0, "Використано");
            case EMPLOYEE_PERFORMANCE -> new Query("""
                SELECT concat_ws(' ', e.last_name, e.first_name), count(DISTINCT o.order_id),
                       sum(os.quantity), to_char(sum(os.quantity * os.price), 'FM999G999G990D00')
                FROM employee e
                JOIN order_service os ON os.employee_id = e.employee_id
                JOIN orders o ON o.order_id = os.order_id
                WHERE e.role = 'manager' AND o.status = 'closed' AND o.order_date BETWEEN ? AND ?
                GROUP BY e.employee_id, e.last_name, e.first_name
                ORDER BY sum(os.quantity * os.price) DESC
                """, List.of("Працівник", "Замовлень", "Робіт", "Виручка, грн"), 2, 0, "Робіт виконано");
            case CUSTOMER_ACTIVITY -> new Query("""
                SELECT concat_ws(' ', cu.last_name, cu.first_name), count(DISTINCT o.order_id),
                       count(DISTINCT c.car_id), to_char(sum(o.total_cost), 'FM999G999G990D00'),
                       to_char(max(o.order_date), 'DD.MM.YYYY')
                FROM customer cu
                JOIN car c ON c.customer_id = cu.customer_id
                JOIN orders o ON o.car_id = c.car_id
                WHERE o.status = 'closed' AND o.order_date BETWEEN ? AND ?
                GROUP BY cu.customer_id, cu.last_name, cu.first_name
                ORDER BY sum(o.total_cost) DESC
                LIMIT 30
                """, List.of("Клієнт", "Візитів", "Автомобілів", "Витрати, грн", "Останній візит"), 1, 0, "Візитів");
            case OVERDUE_ORDERS -> new Query("""
                SELECT o.order_id, to_char(o.order_date, 'DD.MM.YYYY'),
                       '№ ' || c.car_id || ' · ' || c.model || ' · ' || c.license_plate,
                       concat_ws(' ', cu.last_name, cu.first_name),
                       to_char(o.deadline, 'DD.MM.YYYY'), CURRENT_DATE - o.deadline,
                       to_char(o.total_cost, 'FM999G999G990D00') || ' ₴'
                FROM orders o
                JOIN car c ON c.car_id = o.car_id
                JOIN customer cu ON cu.customer_id = c.customer_id
                WHERE o.status IN ('open', 'in_progress') AND o.deadline < CURRENT_DATE AND o.order_date BETWEEN ? AND ?
                ORDER BY o.deadline, o.order_id
                """, List.of("№", "Дата", "Автомобіль", "Клієнт", "Термін", "Днів прострочення", "Сума"), -1, -1, "");
            case MILEAGE_PROGRESS -> new Query("""
                WITH mileage AS (
                    SELECT o.car_id, o.mileage, o.order_date, c.model, c.license_plate,
                           row_number() OVER (PARTITION BY o.car_id ORDER BY o.order_date, o.order_id) AS first_order,
                           row_number() OVER (PARTITION BY o.car_id ORDER BY o.order_date DESC, o.order_id DESC) AS last_order,
                           count(*) OVER (PARTITION BY o.car_id) AS visits
                    FROM orders o JOIN car c ON c.car_id = o.car_id
                    WHERE o.order_date BETWEEN ? AND ?
                )
                SELECT '№ ' || car_id || ' · ' || model || ' · ' || license_plate,
                       max(mileage) FILTER (WHERE first_order = 1),
                       max(mileage) FILTER (WHERE last_order = 1),
                       max(mileage) FILTER (WHERE last_order = 1) - max(mileage) FILTER (WHERE first_order = 1),
                       max(visits)
                FROM mileage
                GROUP BY car_id, model, license_plate
                HAVING max(visits) > 1
                ORDER BY max(mileage) FILTER (WHERE last_order = 1) - max(mileage) FILTER (WHERE first_order = 1) DESC
                """, List.of("Автомобіль", "Перший пробіг", "Останній пробіг", "Зміна, км", "Візитів"), 3, 0, "Зміна пробігу, км");
        };
    }

    private record Query(String sql, List<String> columns, int chartColumn, int labelColumn, String chartSeriesName) {
    }
}
