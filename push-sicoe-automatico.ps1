# push-sicoe-automatico.ps1
# Configura Git, rama development, y hace push automático usando tu token.
# Ejecutar desde la raíz del proyecto SICOE.
#
# IMPORTANTE: Cierra Cursor/Visual Studio y ejecuta este script en PowerShell
# externo (Win+X -> Windows PowerShell). Si lo ejecutas desde el terminal
# integrado, puede fallar con "Permission denied" en .git.
#
# Uso:
#   .\push-sicoe-automatico.ps1 -Token "github_pat_xxxx"
#   o definir variable de entorno: $env:GITHUB_TOKEN = "github_pat_xxxx"
#   y luego: .\push-sicoe-automatico.ps1

param(
    [Parameter(Mandatory = $false)]
    [string]$Token = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Continue"
$remoteBase = "https://github.com/DavidGutierrez2025/SICOE.git"
$remoteUrl = "https://DavidGutierrez2025:$Token@github.com/DavidGutierrez2025/SICOE.git"

Write-Host "=== SICOE - Configuración y push automático a development ===" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrWhiteSpace($Token)) {
    Write-Host "ERROR: No se proporcionó token." -ForegroundColor Red
    Write-Host ""
    Write-Host "Ejecuta una de estas opciones:" -ForegroundColor Yellow
    Write-Host '  1. .\push-sicoe-automatico.ps1 -Token "tu_token"' -ForegroundColor White
    Write-Host '  2. $env:GITHUB_TOKEN = "tu_token"; .\push-sicoe-automatico.ps1' -ForegroundColor White
    Write-Host ""
    Write-Host "Genera un token en: https://github.com/settings/tokens" -ForegroundColor Gray
    exit 1
}

# 1. Git user (omitir si falla; remoto ya suele estar bien)
Write-Host "1. Configurando usuario Git..." -ForegroundColor Yellow
git config user.email "david.gutierrez73@gmail.com" 2>$null
git config user.name "DavidGutierrez2025" 2>$null
if ($LASTEXITCODE -ne 0) { Write-Host "   (Omitido: no se pudo escribir .git/config.)" -ForegroundColor Gray }
else { Write-Host "   Listo." -ForegroundColor Green }

# 2. Repo
if (-not (Test-Path ".git")) {
    Write-Host "2. Inicializando repositorio..." -ForegroundColor Yellow
    git init
    Write-Host "   Listo." -ForegroundColor Green
} else {
    Write-Host "2. Repositorio Git encontrado." -ForegroundColor Green
}

# 3. Rama development
$branch = git branch --show-current 2>$null
if ($branch -eq "development") {
    Write-Host "3. Rama 'development' activa." -ForegroundColor Green
} else {
    Write-Host "3. Configurando rama 'development'..." -ForegroundColor Yellow
    if ($branch -eq "main") { git branch -M development }
    else { git checkout -b development 2>$null; if ($LASTEXITCODE -ne 0) { git branch -M development } }
    Write-Host "   Listo." -ForegroundColor Green
}

# 4. Remoto con token (uso automático)
Write-Host "4. Configurando remoto (con token)..." -ForegroundColor Yellow
$exists = git remote get-url origin 2>$null
if ($exists) { git remote set-url origin $remoteUrl 2>$null }
else { git remote add origin $remoteUrl 2>$null }
if ($LASTEXITCODE -ne 0) { Write-Host "   (Omitido: no se pudo escribir .git/config. Usa remoto actual.)" -ForegroundColor Gray }
else { Write-Host "   Listo." -ForegroundColor Green }

# 5. Staging y commit
Write-Host "5. Agregando archivos y creando commit..." -ForegroundColor Yellow
git add .
$status = git status --porcelain 2>$null
if ($status) {
    git commit -m "Initial commit: SICOE - Sistema de Integración CFDI con SAT" 2>$null
    if ($LASTEXITCODE -eq 0) { Write-Host "   Commit creado." -ForegroundColor Green }
    else { Write-Host "   Sin cambios para commitear." -ForegroundColor Gray }
} else {
    $last = git log -1 --oneline 2>$null
    if ($last) { Write-Host "   Ya hay commit; nada nuevo que agregar." -ForegroundColor Gray }
    else {
        git commit -m "Initial commit: SICOE - Sistema de Integración CFDI con SAT" --allow-empty 2>$null
        Write-Host "   Commit inicial creado." -ForegroundColor Green
    }
}

# 6. Push
Write-Host "6. Haciendo push a origin/development..." -ForegroundColor Yellow
git push -u origin development

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Listo. Código subido a https://github.com/DavidGutierrez2025/SICOE (rama development)." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Error en push. Revisa token, permisos y conexión." -ForegroundColor Red
    exit 1
}

Write-Host "7. Token guardado en remoto local (.git/config). Los push futuros serán automáticos." -ForegroundColor Green
Write-Host ""
Write-Host "Para más push automáticos más adelante:" -ForegroundColor Cyan
Write-Host "  git push origin development" -ForegroundColor White
Write-Host ""
