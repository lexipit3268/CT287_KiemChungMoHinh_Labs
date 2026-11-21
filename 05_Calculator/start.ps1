# Script khoi chay ung dung May tinh ca nhan (WPF Calculator)
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " KHOI CHAY MAY TINH CA NHAN (WPF + FSM + EXPRESSION PARSER) " -ForegroundColor Yellow
Write-Host " Mon hoc: Kiem chung mo hinh (CT287) - CTU               " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

Set-Location "$PSScriptRoot"
dotnet run --project ".\Calculator.UI\Calculator.UI.csproj"
