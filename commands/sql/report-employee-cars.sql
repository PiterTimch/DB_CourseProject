SELECT
    concat_ws(' ', e.last_name, e.first_name) AS "Працівник",
    COALESCE(string_agg(DISTINCT concat(c.model, ', ', c.license_plate), ', ' ORDER BY concat(c.model, ', ', c.license_plate)), '') AS "Автомобілі"
FROM employee e
LEFT JOIN orders o ON o.employee_id = e.employee_id
LEFT JOIN car c ON c.car_id = o.car_id
GROUP BY e.employee_id, e.last_name, e.first_name
ORDER BY e.last_name, e.first_name;
