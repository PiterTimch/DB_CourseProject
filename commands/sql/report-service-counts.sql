SELECT
    s.name AS "Назва послуги",
    COALESCE(sum(os.quantity), 0)::bigint AS "Кількість виконань"
FROM service s
LEFT JOIN order_service os ON os.service_id = s.service_id
GROUP BY s.service_id, s.name
ORDER BY s.name;
