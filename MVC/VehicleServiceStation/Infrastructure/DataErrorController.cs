using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace VehicleServiceStation.Infrastructure;

public abstract class DataErrorController : Controller
{
    protected void AddDataError(Exception exception)
    {
        var postgres = exception as PostgresException ?? exception.InnerException as PostgresException;
        var message = postgres?.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => "Такий запис уже існує. Перевірте унікальні значення.",
            PostgresErrorCodes.ForeignKeyViolation => "Запис використовується в замовленнях або інших даних і не може бути видалений.",
            PostgresErrorCodes.CheckViolation => "Значення не відповідає обмеженням бази даних.",
            PostgresErrorCodes.NotNullViolation => "Заповніть усі обов’язкові поля.",
            _ => exception is KeyNotFoundException or InvalidOperationException ? exception.Message : "Не вдалося зберегти зміни. Перевірте дані та спробуйте ще раз."
        };

        ModelState.AddModelError(string.Empty, message);
    }
}
