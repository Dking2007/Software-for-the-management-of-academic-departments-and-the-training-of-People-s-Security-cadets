@echo off
rem Khoi dong ung dung demo Nhom2-BTL.
rem Cach dung: start.cmd          -> chay profile https
rem            start.cmd http     -> chay profile http (khong can chung chi HTTPS)
setlocal
cd /d "%~dp0"

set "PROFILE=%~1"
if "%PROFILE%"=="" set "PROFILE=https"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [LOI] Khong tim thay lenh dotnet. Hay cai .NET 8 SDK.
    goto :loi
)

dotnet --list-sdks | findstr /r /c:"^8\." >nul
if errorlevel 1 (
    echo [LOI] May chua co .NET 8 SDK. Cai qua Visual Studio Installer
    echo       ^(workload "ASP.NET and web development"^) hoac tai tu dotnet.microsoft.com.
    goto :loi
)

rem Thu muc goc co ca .csproj va .slnx nen phai chi ro tep du an.
echo === Dang tai goi thu vien (dotnet restore) ===
dotnet restore Nhom2-BTL.csproj
if errorlevel 1 goto :loi

echo === Dang chay ung dung voi profile %PROFILE% (nhan Ctrl+C de dung) ===
dotnet run --project Nhom2-BTL.csproj --launch-profile %PROFILE%
if errorlevel 1 goto :loi
exit /b 0

:loi
echo.
echo Khoi dong that bai. Doc thong bao loi o tren.
pause
exit /b 1
