@echo off
echo ==========================================================
echo  KHOI CHAY MAY TINH CA NHAN (WPF + FSM + EXPRESSION PARSER)
echo  Mon hoc: Kiem chung mo hinh (CT287) - CTU
echo ==========================================================
cd /d "%~dp0"
dotnet run --project .\Calculator.UI\Calculator.UI.csproj
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Khong the khoi chay. Vui long kiem tra dotnet SDK.
    pause
)
