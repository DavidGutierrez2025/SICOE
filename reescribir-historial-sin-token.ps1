# reescribir-historial-sin-token.ps1
# Reescribe el historial de Git para eliminar el token que quedó en commit 77c48c6,
# crea un único commit limpio y hace force-push a origin/development.
#
# EJECUTAR EN POWERSHELL EXTERNA (Win+X -> Windows PowerShell), desde esta carpeta.

Set-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

Write-Host "=== Reescribir historial (sin token) y force-push ===" -ForegroundColor Cyan

# 1. Rama huérfana (sin historial previo)
Write-Host "1. Creando rama huérfana 'temp'..." -ForegroundColor Yellow
git checkout --orphan temp
if ($LASTEXITCODE -ne 0) { exit 1 }

# 2. Añadir todo el árbol actual (archivos ya sin token)
Write-Host "2. Añadiendo archivos..." -ForegroundColor Yellow
git add -A

# 3. Un único commit limpio
Write-Host "3. Creando commit limpio..." -ForegroundColor Yellow
git commit -m "Initial commit: SICOE - Sistema de Integración CFDI con SAT"
if ($LASTEXITCODE -ne 0) { exit 1 }

# 4. Reemplazar development por esta rama
Write-Host "4. Reemplazando rama 'development'..." -ForegroundColor Yellow
git branch -D development
git branch -m development

# 5. Force-push
Write-Host "5. Force-push a origin/development..." -ForegroundColor Yellow
git push -u origin development --force
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en push. Revisa token/credenciales y que no haya reglas que bloqueen --force." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Listo. Historial reescrito y subido sin secretos." -ForegroundColor Green
Write-Host "Revoca el token expuesto en https://github.com/settings/tokens y crea uno nuevo." -ForegroundColor Yellow
