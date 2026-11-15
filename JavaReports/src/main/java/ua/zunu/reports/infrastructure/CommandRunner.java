package ua.zunu.reports.infrastructure;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.nio.file.Path;
import java.time.Duration;
import java.util.concurrent.TimeUnit;

public final class CommandRunner {
    private final Path repositoryRoot;

    public CommandRunner(Path repositoryRoot) {
        this.repositoryRoot = repositoryRoot;
    }

    public void run(String commandName) throws IOException, InterruptedException {
        var windows = System.getProperty("os.name").toLowerCase().contains("win");
        var commandFile = repositoryRoot.resolve("commands").resolve(windows ? "windows" : "linux").resolve(commandName + (windows ? ".ps1" : ".sh"));
        if (!java.nio.file.Files.isRegularFile(commandFile)) {
            throw new IOException("Command file not found: " + commandFile);
        }
        var builder = windows
            ? new ProcessBuilder("powershell.exe", "-ExecutionPolicy", "Bypass", "-NoProfile", "-NonInteractive", "-File", commandFile.toString())
            : new ProcessBuilder("bash", commandFile.toString());
        builder.directory(repositoryRoot.toFile());
        builder.redirectErrorStream(true);
        var process = builder.start();
        var output = new ByteArrayOutputStream();
        var reader = Thread.ofVirtual().start(() -> {
            try {
                process.getInputStream().transferTo(output);
            } catch (IOException ignored) {
            }
        });
        if (!process.waitFor(Duration.ofMinutes(2).toMillis(), TimeUnit.MILLISECONDS)) {
            process.destroyForcibly();
            throw new IOException("Database command timed out: " + commandName);
        }
        reader.join();
        if (process.exitValue() != 0) {
            throw new IOException("Database command failed: " + commandName + System.lineSeparator() + output.toString(java.nio.charset.StandardCharsets.UTF_8));
        }
    }
}
