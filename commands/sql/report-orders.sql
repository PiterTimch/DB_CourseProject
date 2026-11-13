SELECT
    o.order_date AS "Дата замовлення",
    concat(c.model, ', ', c.license_plate) AS "Автомобіль",
    concat_ws(' ', cu.last_name, cu.first_name) AS "Клієнт",
    concat_ws(' ', e.last_name, e.first_name) AS "Працівник",
    o.status AS "Статус"
FROM orders o
JOIN car c ON c.car_id = o.car_id
JOIN customer cu ON cu.customer_id = c.customer_id
JOIN employee e ON e.employee_id = o.employee_id
ORDER BY o.order_date, o.order_id;
