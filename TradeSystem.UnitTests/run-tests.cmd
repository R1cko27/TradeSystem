@echo off
rem Запуск юнит-тестов через штатный раннер MSTest (Microsoft.Testing.Platform).
set TESTDIR=%~dp0
set RESULTS=%TESTDIR%TestResults
if not exist "%RESULTS%" mkdir "%RESULTS%"
"%TESTDIR%bin\Debug\net8.0-windows\TradeSystem.UnitTests.exe" --output Detailed --report-trx --results-directory "%RESULTS%" > "%RESULTS%\mstest-output.txt" 2>&1
type "%RESULTS%\mstest-output.txt"
exit /b %ERRORLEVEL%
