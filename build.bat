@echo off

echo Building for .NET 3.5
call bundle exec rake all BUILD_CONFIG_KEY=NET35 %1
IF NOT %ERRORLEVEL% == 0 goto FAILED

echo Building for .NET 4.0
call bundle exec rake unclean %1
IF NOT %ERRORLEVEL% == 0 goto FAILED

echo Creating NuGet package
call bundle exec rake nuget %1
IF NOT %ERRORLEVEL% == 0 goto FAILED

echo Create ZIP package
call bundle exec rake package

:FAILED
