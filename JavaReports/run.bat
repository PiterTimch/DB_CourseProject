@echo off
setlocal
for %%I in ("%~dp0..") do set "REPOSITORY_ROOT=%%~fI"
cd /d "%REPOSITORY_ROOT%"
if not defined JAVA_HOME (
    echo JAVA_HOME must point to JDK 25.
    exit /b 1
)
"%JAVA_HOME%\bin\java.exe" -version
if errorlevel 1 exit /b 1
mvn -f JavaReports\pom.xml javafx:run
