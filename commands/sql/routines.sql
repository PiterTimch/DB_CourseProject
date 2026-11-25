CREATE OR REPLACE PROCEDURE create_service_order(
    IN p_car_id integer,
    IN p_employee_id integer,
    IN p_order_date date,
    IN p_deadline date,
    IN p_mileage integer,
    INOUT p_order_id integer
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    IF p_mileage < 0 THEN
        RAISE EXCEPTION 'Пробіг не може бути від’ємним.' USING ERRCODE = '22023';
    END IF;

    IF p_deadline IS NOT NULL AND p_deadline < p_order_date THEN
        RAISE EXCEPTION 'Термін замовлення не може бути раніше дати створення.' USING ERRCODE = '22023';
    END IF;

    INSERT INTO orders (car_id, employee_id, order_date, deadline, status, mileage)
    VALUES (p_car_id, p_employee_id, p_order_date, p_deadline, 'open', p_mileage)
    RETURNING order_id INTO p_order_id;
END;
$procedure$;

CREATE OR REPLACE PROCEDURE add_order_service(
    IN p_order_id integer,
    IN p_service_id integer,
    IN p_employee_id integer,
    IN p_quantity integer
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_status varchar(40);
    v_price numeric(12, 2);
BEGIN
    IF p_quantity <= 0 THEN
        RAISE EXCEPTION 'Кількість послуги має бути більшою за нуль.' USING ERRCODE = '22023';
    END IF;

    SELECT status INTO v_status
    FROM orders
    WHERE order_id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Замовлення не знайдено.' USING ERRCODE = 'P0002';
    END IF;
    IF v_status = 'closed' THEN
        RAISE EXCEPTION 'Закрите замовлення не можна змінити.' USING ERRCODE = '55000';
    END IF;

    SELECT price INTO v_price FROM service WHERE service_id = p_service_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Послугу не знайдено.' USING ERRCODE = 'P0002';
    END IF;

    INSERT INTO order_service (order_id, service_id, employee_id, quantity, price)
    VALUES (p_order_id, p_service_id, p_employee_id, p_quantity, v_price)
    ON CONFLICT (order_id, service_id) DO UPDATE
    SET quantity = order_service.quantity + EXCLUDED.quantity,
        employee_id = EXCLUDED.employee_id;

    UPDATE orders
    SET status = CASE WHEN status = 'open' THEN 'in_progress' ELSE status END
    WHERE order_id = p_order_id;

    PERFORM recalculate_service_order_total(p_order_id);
END;
$procedure$;

CREATE OR REPLACE PROCEDURE close_service_order(IN p_order_id integer)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_status varchar(40);
BEGIN
    SELECT status INTO v_status
    FROM orders
    WHERE order_id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Замовлення не знайдено.' USING ERRCODE = 'P0002';
    END IF;
    IF v_status = 'closed' THEN
        RAISE EXCEPTION 'Замовлення вже закрите.' USING ERRCODE = '55000';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM order_service WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Додайте щонайменше одну послугу перед закриттям замовлення.' USING ERRCODE = '55000';
    END IF;

    PERFORM recalculate_service_order_total(p_order_id);
    UPDATE orders SET status = 'closed' WHERE order_id = p_order_id;
END;
$procedure$;

CREATE OR REPLACE FUNCTION recalculate_service_order_total(p_order_id integer)
RETURNS void
LANGUAGE sql
AS $function$
    UPDATE orders o
    SET total_cost = ROUND((
        (SELECT COALESCE(SUM(quantity * price), 0) FROM order_service WHERE order_id = o.order_id)
        + (SELECT COALESCE(SUM(quantity * price), 0) FROM order_part WHERE order_id = o.order_id)
    ) * (100 - o.discount) / 100, 2)
    WHERE o.order_id = p_order_id;
$function$;

\getenv app_db_user APP_DB_USER
SELECT format('GRANT EXECUTE ON ALL ROUTINES IN SCHEMA public TO %I', :'app_db_user')
\gexec
SELECT format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON ROUTINES TO %I', :'app_db_user')
\gexec
