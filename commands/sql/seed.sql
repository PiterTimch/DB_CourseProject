INSERT INTO customer (customer_id, customer_type, first_name, last_name, phone)
VALUES
    (1, 'individual', 'Іван', 'Петренко', '+380501112233'),
    (2, 'business', 'Олена', 'Коваль', '+380671234567')
ON CONFLICT (customer_id) DO NOTHING;

INSERT INTO employee (employee_id, first_name, last_name, role, login, password_hash)
VALUES
    (1, 'Марія', 'Шевченко', 'manager', 'demo.manager', '!'),
    (2, 'Андрій', 'Мельник', 'manager', 'demo.mechanic', '!'),
    (3, 'Оксана', 'Бондар', 'director', 'demo.director', '!')
ON CONFLICT (employee_id) DO NOTHING;

INSERT INTO car (car_id, customer_id, vin, license_plate, model)
VALUES
    (1, 1, 'WVWZZZ1JZXW000001', 'BO1234AB', 'Volkswagen Golf'),
    (2, 1, 'WAUZZZ8K9AA000002', 'BO5678CD', 'Audi A4'),
    (3, 2, 'TMBJG7NE7F0000003', 'BO9012EF', 'Škoda Octavia')
ON CONFLICT (car_id) DO NOTHING;

INSERT INTO service (service_id, name, price)
VALUES
    (1, 'Заміна моторної оливи', 1200.00),
    (2, 'Діагностика ходової', 850.00),
    (3, 'Заміна гальмівних колодок', 1600.00)
ON CONFLICT (service_id) DO NOTHING;

INSERT INTO part (part_id, name, article, price)
VALUES
    (1, 'Моторна олива 5W-30', 'OIL-5W30-1L', 420.00),
    (2, 'Масляний фільтр', 'FILTER-OIL-01', 310.00),
    (3, 'Гальмівні колодки передні', 'BRAKE-PAD-F01', 2450.00)
ON CONFLICT (part_id) DO NOTHING;

INSERT INTO orders (order_id, car_id, employee_id, order_date, deadline, status, discount, total_cost, mileage)
VALUES
    (1, 1, 1, DATE '2026-10-05', DATE '2026-10-05', 'closed', 0, 3190.00, 84500),
    (2, 3, 1, DATE '2026-11-03', DATE '2026-11-04', 'in_progress', 5, 4655.00, 126300)
ON CONFLICT (order_id) DO NOTHING;

INSERT INTO order_service (order_id, service_id, employee_id, quantity, price)
VALUES
    (1, 1, 2, 1, 1200.00),
    (2, 2, 2, 1, 850.00),
    (2, 3, 2, 1, 1600.00)
ON CONFLICT (order_id, service_id) DO NOTHING;

INSERT INTO order_part (order_id, part_id, quantity, price)
VALUES
    (1, 1, 4, 420.00),
    (1, 2, 1, 310.00),
    (2, 3, 1, 2450.00)
ON CONFLICT (order_id, part_id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('customer', 'customer_id'), GREATEST((SELECT COALESCE(MAX(customer_id), 1) FROM customer), 1));
SELECT setval(pg_get_serial_sequence('employee', 'employee_id'), GREATEST((SELECT COALESCE(MAX(employee_id), 1) FROM employee), 1));
SELECT setval(pg_get_serial_sequence('car', 'car_id'), GREATEST((SELECT COALESCE(MAX(car_id), 1) FROM car), 1));
SELECT setval(pg_get_serial_sequence('service', 'service_id'), GREATEST((SELECT COALESCE(MAX(service_id), 1) FROM service), 1));
SELECT setval(pg_get_serial_sequence('part', 'part_id'), GREATEST((SELECT COALESCE(MAX(part_id), 1) FROM part), 1));
SELECT setval(pg_get_serial_sequence('orders', 'order_id'), GREATEST((SELECT COALESCE(MAX(order_id), 1) FROM orders), 1));
