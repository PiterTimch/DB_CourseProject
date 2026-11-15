package ua.zunu.reports.infrastructure;

import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.PBEKeySpec;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.util.Arrays;
import java.util.Base64;

public final class PasswordHashVerifier {
    private static final int ITERATIONS = 100_000;
    private static final SecureRandom RANDOM = new SecureRandom();

    public boolean verify(String password, String encodedHash) {
        try {
            var decoded = Base64.getDecoder().decode(encodedHash);
            if (decoded.length < 17 || decoded[0] != 1) {
                return false;
            }
            var header = ByteBuffer.wrap(decoded, 1, 12).order(ByteOrder.BIG_ENDIAN);
            var prf = header.getInt();
            var iterations = header.getInt();
            var saltLength = header.getInt();
            var subkeyOffset = 13 + saltLength;
            var subkeyLength = decoded.length - subkeyOffset;
            if (iterations < 1 || iterations > 1_000_000 || saltLength < 16 || saltLength > 1024 || subkeyLength < 16 || subkeyLength > 128) {
                return false;
            }
            var algorithm = switch (prf) {
                case 0 -> "PBKDF2WithHmacSHA1";
                case 1 -> "PBKDF2WithHmacSHA256";
                case 2 -> "PBKDF2WithHmacSHA512";
                default -> null;
            };
            if (algorithm == null) {
                return false;
            }
            var salt = Arrays.copyOfRange(decoded, 13, subkeyOffset);
            var expected = Arrays.copyOfRange(decoded, subkeyOffset, decoded.length);
            var actual = derive(password, salt, iterations, expected.length * 8, algorithm);
            return MessageDigest.isEqual(expected, actual);
        } catch (RuntimeException | java.security.GeneralSecurityException exception) {
            return false;
        }
    }

    public String hash(String password) {
        var salt = new byte[16];
        RANDOM.nextBytes(salt);
        byte[] subkey;
        try {
            subkey = derive(password, salt, ITERATIONS, 256, "PBKDF2WithHmacSHA512");
        } catch (java.security.GeneralSecurityException exception) {
            throw new IllegalStateException("The password hashing algorithm is not available.", exception);
        }
        var buffer = ByteBuffer.allocate(13 + salt.length + subkey.length).order(ByteOrder.BIG_ENDIAN);
        buffer.put((byte) 1);
        buffer.putInt(2);
        buffer.putInt(ITERATIONS);
        buffer.putInt(salt.length);
        buffer.put(salt);
        buffer.put(subkey);
        return Base64.getEncoder().encodeToString(buffer.array());
    }

    private static byte[] derive(String password, byte[] salt, int iterations, int keyLength, String algorithm) throws java.security.GeneralSecurityException {
        var spec = new PBEKeySpec(password.toCharArray(), salt, iterations, keyLength);
        try {
            return SecretKeyFactory.getInstance(algorithm).generateSecret(spec).getEncoded();
        } finally {
            spec.clearPassword();
        }
    }
}
