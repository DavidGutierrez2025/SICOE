# Subir SICOE a https://github.com/DavidGutierrez2025/SICOE
# Ejecutar en PowerShell desde esta carpeta. Cierra VS/otras apps que usen el repo antes.

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "=== Subir SICOE a GitHub ===" -ForegroundColor Cyan

git config user.email "david.gutierrez73@gmail.com"
git config user.name "DavidGutierrez2025"
git remote set-url origin https://github.com/DavidGutierrez2025/SICOE.git

$branch = git branch --show-current
if ($branch -ne "development") {
    if ((git branch -a 2>$null) -match "development") { git checkout development }
    else { git checkout -b development }
}

git add .
git status --short

$msg = "Actualización SICOE: integración SAT, descargas, UI."
$count = (git diff --cached --name-only 2>$null).Count
if ($count -gt 0) {
    git commit -m $msg
}

Write-Host "`nPush a origin/development (pedirá usuario/token si no hay credenciales)..." -ForegroundColor Yellow
git push -u origin development

Write-Host "`nListo." -ForegroundColor Green
