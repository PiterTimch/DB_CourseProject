package ua.zunu.reports.data;

import org.junit.jupiter.api.Test;

import java.lang.reflect.Method;

import static org.junit.jupiter.api.Assertions.assertEquals;

final class ReportNumberParsingTest {
    private static final Method PARSE_NUMBER = parserMethod();

    @Test
    void parses_numbers_with_spaces_commas_and_units() throws Exception {
        assertEquals(1234.5, parse("1 234,5 грн"));
        assertEquals(42.0, parse("42 км"));
        assertEquals(-7.25, parse("-7,25"));
    }

    @Test
    void returns_zero_for_non_numeric_text() throws Exception {
        assertEquals(0.0, parse("немає даних"));
    }

    private static double parse(String value) throws Exception {
        return (double) PARSE_NUMBER.invoke(null, value);
    }

    private static Method parserMethod() {
        try {
            var method = JdbcReportRepository.class.getDeclaredMethod("parseNumber", String.class);
            method.setAccessible(true);
            return method;
        } catch (ReflectiveOperationException exception) {
            throw new ExceptionInInitializerError(exception);
        }
    }
}
