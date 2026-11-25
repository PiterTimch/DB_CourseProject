\pset pager off
\pset null '(NULL)'
BEGIN READ ONLY;
\echo
\echo '============================================================'
\echo ' DATABASE INTEGRITY REPORT'
\echo '============================================================'
SELECT current_database() AS database_name,
       current_user AS checked_as,
       to_char(clock_timestamp(), 'YYYY-MM-DD HH24:MI:SS TZ') AS checked_at;

\echo
\echo '--- Declared constraints and validation state ---'
SELECT t.relname AS table_name,
       c.conname AS constraint_name,
       CASE c.contype
           WHEN 'p' THEN 'PRIMARY KEY'
           WHEN 'f' THEN 'FOREIGN KEY'
           WHEN 'u' THEN 'UNIQUE'
           WHEN 'c' THEN 'CHECK'
           WHEN 'x' THEN 'EXCLUSION'
           ELSE c.contype::text
       END AS constraint_type,
       CASE WHEN c.convalidated THEN 'VALIDATED' ELSE 'NOT VALIDATED' END AS state,
       pg_get_constraintdef(c.oid, true) AS definition
FROM pg_constraint c
JOIN pg_class t ON t.oid = c.conrelid
JOIN pg_namespace n ON n.oid = t.relnamespace
WHERE n.nspname = 'public'
ORDER BY t.relname, c.contype, c.conname;

\echo
\echo '--- Row counts in project tables ---'
SELECT table_name, row_count
FROM (
    SELECT 'customer' AS table_name, count(*) AS row_count FROM customer
    UNION ALL SELECT 'employee', count(*) FROM employee
    UNION ALL SELECT 'car', count(*) FROM car
    UNION ALL SELECT 'service', count(*) FROM service
    UNION ALL SELECT 'part', count(*) FROM part
    UNION ALL SELECT 'orders', count(*) FROM orders
    UNION ALL SELECT 'order_service', count(*) FROM order_service
    UNION ALL SELECT 'order_part', count(*) FROM order_part
) counts
ORDER BY table_name;

\echo
\echo '--- Data checks (PASS means no violations) ---'
WITH checks(check_name, violation_count) AS (
    SELECT 'customer: required fields are not empty', count(*) FROM customer
      WHERE customer_type IS NULL OR btrim(customer_type) = '' OR first_name IS NULL OR btrim(first_name) = '' OR last_name IS NULL OR btrim(last_name) = '' OR phone IS NULL OR btrim(phone) = ''
    UNION ALL SELECT 'customer: customer_type is allowed', count(*) FROM customer WHERE customer_type NOT IN ('individual', 'business')
    UNION ALL SELECT 'employee: required fields are not empty', count(*) FROM employee
      WHERE first_name IS NULL OR btrim(first_name) = '' OR last_name IS NULL OR btrim(last_name) = '' OR role IS NULL OR btrim(role) = '' OR login IS NULL OR btrim(login) = '' OR password_hash IS NULL OR btrim(password_hash) = ''
    UNION ALL SELECT 'employee: role is allowed', count(*) FROM employee WHERE role NOT IN ('admin', 'manager', 'director', 'guest')
    UNION ALL SELECT 'employee: duplicate logins', count(*) FROM (SELECT login FROM employee GROUP BY login HAVING count(*) > 1) d
    UNION ALL SELECT 'car: required fields are not empty', count(*) FROM car
      WHERE vin IS NULL OR btrim(vin) = '' OR license_plate IS NULL OR btrim(license_plate) = '' OR model IS NULL OR btrim(model) = ''
    UNION ALL SELECT 'car: duplicate VINs', count(*) FROM (SELECT vin FROM car GROUP BY vin HAVING count(*) > 1) d
    UNION ALL SELECT 'car: duplicate license plates', count(*) FROM (SELECT license_plate FROM car GROUP BY license_plate HAVING count(*) > 1) d
    UNION ALL SELECT 'car: customer reference exists', count(*) FROM car c LEFT JOIN customer x USING (customer_id) WHERE x.customer_id IS NULL
    UNION ALL SELECT 'service: name is not empty', count(*) FROM service WHERE name IS NULL OR btrim(name) = ''
    UNION ALL SELECT 'service: duplicate names', count(*) FROM (SELECT name FROM service GROUP BY name HAVING count(*) > 1) d
    UNION ALL SELECT 'service: negative prices', count(*) FROM service WHERE price IS NULL OR price < 0
    UNION ALL SELECT 'part: required fields are not empty', count(*) FROM part WHERE name IS NULL OR btrim(name) = '' OR article IS NULL OR btrim(article) = ''
    UNION ALL SELECT 'part: duplicate articles', count(*) FROM (SELECT article FROM part GROUP BY article HAVING count(*) > 1) d
    UNION ALL SELECT 'part: negative prices', count(*) FROM part WHERE price IS NULL OR price < 0
    UNION ALL SELECT 'orders: car reference exists', count(*) FROM orders o LEFT JOIN car c USING (car_id) WHERE c.car_id IS NULL
    UNION ALL SELECT 'orders: employee reference exists', count(*) FROM orders o LEFT JOIN employee e USING (employee_id) WHERE e.employee_id IS NULL
    UNION ALL SELECT 'orders: discount is between 0 and 100', count(*) FROM orders WHERE discount IS NULL OR discount < 0 OR discount > 100
    UNION ALL SELECT 'orders: total cost and mileage are nonnegative', count(*) FROM orders WHERE total_cost IS NULL OR total_cost < 0 OR mileage IS NULL OR mileage < 0
    UNION ALL SELECT 'orders: deadline is on or after order date', count(*) FROM orders WHERE deadline IS NOT NULL AND deadline < order_date
    UNION ALL SELECT 'order_service: order reference exists', count(*) FROM order_service x LEFT JOIN orders o USING (order_id) WHERE o.order_id IS NULL
    UNION ALL SELECT 'order_service: service reference exists', count(*) FROM order_service x LEFT JOIN service s USING (service_id) WHERE s.service_id IS NULL
    UNION ALL SELECT 'order_service: employee reference exists', count(*) FROM order_service x LEFT JOIN employee e USING (employee_id) WHERE e.employee_id IS NULL
    UNION ALL SELECT 'order_service: quantity is positive and price nonnegative', count(*) FROM order_service WHERE quantity IS NULL OR quantity <= 0 OR price IS NULL OR price < 0
    UNION ALL SELECT 'order_service: duplicate order/service pairs', count(*) FROM (SELECT order_id, service_id FROM order_service GROUP BY order_id, service_id HAVING count(*) > 1) d
    UNION ALL SELECT 'order_part: order reference exists', count(*) FROM order_part x LEFT JOIN orders o USING (order_id) WHERE o.order_id IS NULL
    UNION ALL SELECT 'order_part: part reference exists', count(*) FROM order_part x LEFT JOIN part p USING (part_id) WHERE p.part_id IS NULL
    UNION ALL SELECT 'order_part: quantity is positive and price nonnegative', count(*) FROM order_part WHERE quantity IS NULL OR quantity <= 0 OR price IS NULL OR price < 0
    UNION ALL SELECT 'order_part: duplicate order/part pairs', count(*) FROM (SELECT order_id, part_id FROM order_part GROUP BY order_id, part_id HAVING count(*) > 1) d
), results AS (
    SELECT check_name, violation_count,
           CASE WHEN violation_count = 0 THEN 'PASS' ELSE 'FAIL' END AS result
    FROM checks
)
SELECT result, check_name, violation_count
FROM results
ORDER BY CASE result WHEN 'FAIL' THEN 0 ELSE 1 END, check_name;

\echo
\echo '--- Overall result ---'
WITH checks(violation_count) AS (
    SELECT count(*) FROM customer WHERE customer_type IS NULL OR btrim(customer_type) = '' OR first_name IS NULL OR btrim(first_name) = '' OR last_name IS NULL OR btrim(last_name) = '' OR phone IS NULL OR btrim(phone) = '' OR customer_type NOT IN ('individual', 'business')
    UNION ALL SELECT count(*) FROM employee WHERE first_name IS NULL OR btrim(first_name) = '' OR last_name IS NULL OR btrim(last_name) = '' OR role IS NULL OR btrim(role) = '' OR role NOT IN ('admin', 'manager', 'director', 'guest') OR login IS NULL OR btrim(login) = '' OR password_hash IS NULL OR btrim(password_hash) = ''
    UNION ALL SELECT count(*) FROM (SELECT login FROM employee GROUP BY login HAVING count(*) > 1) d
    UNION ALL SELECT count(*) FROM car c LEFT JOIN customer x USING (customer_id) WHERE x.customer_id IS NULL OR vin IS NULL OR btrim(vin) = '' OR license_plate IS NULL OR btrim(license_plate) = '' OR model IS NULL OR btrim(model) = ''
    UNION ALL SELECT count(*) FROM (SELECT vin FROM car GROUP BY vin HAVING count(*) > 1) d
    UNION ALL SELECT count(*) FROM (SELECT license_plate FROM car GROUP BY license_plate HAVING count(*) > 1) d
    UNION ALL SELECT count(*) FROM service WHERE name IS NULL OR btrim(name) = '' OR price IS NULL OR price < 0
    UNION ALL SELECT count(*) FROM (SELECT name FROM service GROUP BY name HAVING count(*) > 1) d
    UNION ALL SELECT count(*) FROM part WHERE name IS NULL OR btrim(name) = '' OR article IS NULL OR btrim(article) = '' OR price IS NULL OR price < 0
    UNION ALL SELECT count(*) FROM (SELECT article FROM part GROUP BY article HAVING count(*) > 1) d
    UNION ALL SELECT count(*) FROM orders o LEFT JOIN car c USING (car_id) LEFT JOIN employee e USING (employee_id) WHERE c.car_id IS NULL OR e.employee_id IS NULL OR status IS NULL OR btrim(status) = '' OR discount IS NULL OR discount < 0 OR discount > 100 OR total_cost IS NULL OR total_cost < 0 OR mileage IS NULL OR mileage < 0 OR (deadline IS NOT NULL AND deadline < order_date)
    UNION ALL SELECT count(*) FROM order_service x LEFT JOIN orders o USING (order_id) LEFT JOIN service s USING (service_id) LEFT JOIN employee e USING (employee_id) WHERE o.order_id IS NULL OR s.service_id IS NULL OR e.employee_id IS NULL OR quantity IS NULL OR quantity <= 0 OR price IS NULL OR price < 0
    UNION ALL SELECT count(*) FROM (SELECT order_id, service_id FROM order_service GROUP BY order_id, service_id HAVING count(*) > 1) d
    UNION ALL SELECT count(*) FROM order_part x LEFT JOIN orders o USING (order_id) LEFT JOIN part p USING (part_id) WHERE o.order_id IS NULL OR p.part_id IS NULL OR quantity IS NULL OR quantity <= 0 OR price IS NULL OR price < 0
    UNION ALL SELECT count(*) FROM (SELECT order_id, part_id FROM order_part GROUP BY order_id, part_id HAVING count(*) > 1) d
), constraint_state AS (
    SELECT count(*) FILTER (WHERE NOT c.convalidated) AS unvalidated
    FROM pg_constraint c
    JOIN pg_class t ON t.oid = c.conrelid
    JOIN pg_namespace n ON n.oid = t.relnamespace
    WHERE n.nspname = 'public'
      AND t.relname IN ('customer','employee','car','service','part','orders','order_service','order_part')
)
SELECT CASE WHEN (SELECT coalesce(sum(violation_count), 0) FROM checks) = 0 AND constraint_state.unvalidated = 0
            THEN 'PASS — no detected data violations; constraints are validated'
            ELSE 'FAIL — see failed checks above; one or more constraints may be unvalidated'
       END AS overall_result,
       (SELECT coalesce(sum(violation_count), 0) FROM checks) AS detected_violations,
       constraint_state.unvalidated AS unvalidated_constraints,
       ((SELECT coalesce(sum(violation_count), 0) FROM checks) = 0 AND constraint_state.unvalidated = 0) AS overall_pass
FROM constraint_state
\gset integrity_
\echo Overall result: :integrity_overall_result
\echo Detected violations: :integrity_detected_violations
\echo Unvalidated constraints: :integrity_unvalidated_constraints
COMMIT;
\if :integrity_overall_pass
    \echo Integrity check completed successfully.
\else
    \echo Integrity check failed.
    \quit 1
\endif
