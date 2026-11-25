package ua.zunu.reports.infrastructure;

import org.junit.jupiter.api.Test;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.Base64;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

final class PasswordHashVerifierTest {
    private final PasswordHashVerifier verifier = new PasswordHashVerifier();

    @Test
    void verifies_correct_password_and_rejects_wrong_password() {
        var hash = verifier.hash("secret-password");

        assertTrue(verifier.verify("secret-password", hash));
        assertFalse(verifier.verify("wrong-password", hash));
    }

    @Test
    void verifies_hash_in_the_aspnet_identity_v3_format() {
        var webApplicationHash = "AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg/6pg3Oh12zx1bld1/jl9yDZ4HvL3IXbKzzllNoGSd1KQ==";

        assertTrue(verifier.verify("web-password", webApplicationHash));
        assertFalse(verifier.verify("wrong-password", webApplicationHash));
    }

    @Test
    void rejects_malformed_hash_and_out_of_range_parameters() {
        assertFalse(verifier.verify("password", "not-base64!"));
        assertFalse(verifier.verify("password", Base64.getEncoder().encodeToString(new byte[8])));

        var invalidIterations = ByteBuffer.allocate(13 + 16 + 32).order(ByteOrder.BIG_ENDIAN);
        invalidIterations.put((byte) 1).putInt(2).putInt(1_000_001).putInt(16);
        assertFalse(verifier.verify("password", Base64.getEncoder().encodeToString(invalidIterations.array())));
    }
}
