# Script para configurar Git y subir a la rama development
# Ejecutar este script desde la raíz del proyecto SICOE

Write-Host "=== Configuración de Git para SICOE - Rama Development ===" -ForegroundColor Cyan
Write-Host ""

# 1. Configurar email de Git
Write-Host "1. Configurando email de Git..." -ForegroundColor Yellow
git config user.email "david.gutierrez73@gmail.com"
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Email configurado: david.gutierrez73@gmail.com" -ForegroundColor Green
} else {
    Write-Host "   ✗ Error al configurar email" -ForegroundColor Red
    exit 1
}

# 2. Verificar/Configurar nombre de usuario
Write-Host "2. Verificando nombre de usuario..." -ForegroundColor Yellow
$currentName = git config --get user.name
if ([string]::IsNullOrWhiteSpace($currentName)) {
    git config user.name "david.gutierrezc"
    Write-Host "   ✓ Nombre configurado: david.gutierrezc" -ForegroundColor Green
} else {
    Write-Host "   ✓ Nombre ya configurado: $currentName" -ForegroundColor Green
}

# 3. Verificar que estamos en un repositorio Git
Write-Host "3. Verificando repositorio Git..." -ForegroundColor Yellow
if (-not (Test-Path ".git")) {
    Write-Host "   ✗ No se encontró un repositorio Git. Ejecutando 'git init'..." -ForegroundColor Yellow
    git init
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ✗ Error al inicializar repositorio" -ForegroundColor Red
        exit 1
    }
    Write-Host "   ✓ Repositorio inicializado" -ForegroundColor Green
} else {
    Write-Host "   ✓ Repositorio Git encontrado" -ForegroundColor Green
}

# 4. Crear o cambiar a la rama development
Write-Host "4. Configurando rama development..." -ForegroundColor Yellow
$currentBranch = git branch --show-current 2>$null
if ($currentBranch -eq "development") {
    Write-Host "   ✓ Ya estás en la rama 'development'" -ForegroundColor Green
} else {
    if ($currentBranch -eq "main") {
        Write-Host "   Cambiando de 'main' a 'development'..." -ForegroundColor Yellow
        git branch -M development
    } else {
        Write-Host "   Creando rama 'development'..." -ForegroundColor Yellow
        git checkout -b development 2>$null
        if ($LASTEXITCODE -ne 0) {
            git branch -M development
        }
    }
    Write-Host "   ✓ Rama 'development' configurada" -ForegroundColor Green
}

# 5. Verificar que .gitignore existe
Write-Host "5. Verificando .gitignore..." -ForegroundColor Yellow
if (-not (Test-Path ".gitignore")) {
    Write-Host "   ✗ No se encontró .gitignore" -ForegroundColor Red
    Write-Host "   Por favor, asegúrate de que el archivo .gitignore existe" -ForegroundColor Yellow
} else {
    Write-Host "   ✓ .gitignore encontrado" -ForegroundColor Green
}

# 6. Agregar archivos al staging
Write-Host "6. Agregando archivos al staging area..." -ForegroundColor Yellow
git add .
if ($LASTEXITCODE -eq 0) {
    $stagedCount = (git diff --cached --name-only).Count
    Write-Host "   ✓ $stagedCount archivos agregados al staging" -ForegroundColor Green
} else {
    Write-Host "   ✗ Error al agregar archivos" -ForegroundColor Red
    exit 1
}

# 7. Crear commit inicial
Write-Host "7. Creando commit inicial..." -ForegroundColor Yellow
$commitMessage = "Initial commit: SICOE - Sistema de Integración CFDI con SAT"
git commit -m $commitMessage
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Commit creado exitosamente" -ForegroundColor Green
} else {
    Write-Host "   ⚠ No se pudo crear el commit (puede que no haya cambios)" -ForegroundColor Yellow
}

# 8. Configurar remoto
Write-Host "8. Configuración del repositorio remoto..." -ForegroundColor Yellow
$remoteUrl = "https://github.com/DavidGutierrez2025/SICOE.git"
Write-Host "   URL del repositorio: $remoteUrl" -ForegroundColor Cyan

$existingRemote = git remote get-url origin 2>$null
if ($existingRemote) {
    Write-Host "   Remoto 'origin' ya existe: $existingRemote" -ForegroundColor Yellow
    if ($existingRemote -ne $remoteUrl) {
        Write-Host "   Actualizando remoto a la nueva URL..." -ForegroundColor Yellow
        git remote set-url origin $remoteUrl
        Write-Host "   ✓ Remoto 'origin' actualizado" -ForegroundColor Green
    } else {
        Write-Host "   ✓ Remoto 'origin' ya está configurado correctamente" -ForegroundColor Green
    }
} else {
    git remote add origin $remoteUrl
    Write-Host "   ✓ Remoto 'origin' agregado" -ForegroundColor Green
}

# Mostrar información del remoto
Write-Host ""
Write-Host "   Información del remoto:" -ForegroundColor Cyan
git remote -v

# 9. Resumen final
Write-Host ""
Write-Host "=== Resumen de Configuración ===" -ForegroundColor Cyan
Write-Host "Email Git: $(git config --get user.email)" -ForegroundColor White
Write-Host "Nombre Git: $(git config --get user.name)" -ForegroundColor White
Write-Host "Rama actual: $(git branch --show-current)" -ForegroundColor White
Write-Host ""

# 10. Instrucciones para hacer push
Write-Host "=== Próximos Pasos ===" -ForegroundColor Green
Write-Host ""
Write-Host "Para subir el código a la rama 'development', ejecuta:" -ForegroundColor Yellow
Write-Host "  git push -u origin development" -ForegroundColor White
Write-Host ""
Write-Host "O si prefieres hacerlo ahora, presiona Enter para continuar o Ctrl+C para cancelar."
$continue = Read-Host

if ($continue -ne $null) {
    Write-Host ""
    Write-Host "Haciendo push a origin/development..." -ForegroundColor Yellow
    git push -u origin development
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "¡Éxito! El código ha sido subido a la rama 'development'." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "Error al hacer push. Verifica:" -ForegroundColor Red
        Write-Host "  1. Que la URL del remoto sea correcta" -ForegroundColor Yellow
        Write-Host "  2. Que el repositorio 'SICOE' exista en tu cuenta de GitHub/GitLab" -ForegroundColor Yellow
        Write-Host "  3. Que tengas permisos de escritura" -ForegroundColor Yellow
        Write-Host "  4. Que tengas credenciales configuradas (git credential o SSH)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Si el repositorio no existe, créalo primero en:" -ForegroundColor Cyan
        Write-Host "  - GitHub: https://github.com/new" -ForegroundColor White
        Write-Host "  - GitLab: https://gitlab.com/projects/new" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "=== Configuración completada ===" -ForegroundColor Cyan
