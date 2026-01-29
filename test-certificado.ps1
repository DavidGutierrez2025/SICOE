# Script de prueba para extraer RFC del certificado real
# Certificado: C:\FIEL\00001000000704874250.cer

$certPath = "C:\FIEL\00001000000704874250.cer"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Prueba de Extraccion de RFC del Certificado" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que el archivo existe
if (-not (Test-Path $certPath)) {
    Write-Host "ERROR: El archivo no existe: $certPath" -ForegroundColor Red
    exit 1
}

Write-Host "Archivo encontrado: $certPath" -ForegroundColor Green
Write-Host ""

# Leer el archivo y convertirlo a Base64
try {
    $certBytes = [System.IO.File]::ReadAllBytes($certPath)
    $certBase64 = [System.Convert]::ToBase64String($certBytes)
    
    Write-Host "Certificado leido correctamente" -ForegroundColor Green
    Write-Host "  Tamano: $($certBytes.Length) bytes" -ForegroundColor Gray
    Write-Host ""
    
    # Verificar que el API este corriendo
    $apiUrl = "https://localhost:7052/api/autenticacion/extraer-rfc"
    
    Write-Host "Enviando solicitud al API..." -ForegroundColor Yellow
    Write-Host "URL: $apiUrl" -ForegroundColor Gray
    Write-Host ""
    
    # Crear el body del request
    $body = @{
        certificadoCer = $certBase64
    } | ConvertTo-Json
    
    # Hacer la solicitud HTTP (ignorar certificado SSL en desarrollo)
    try {
        $response = Invoke-RestMethod -Uri $apiUrl `
            -Method Post `
            -ContentType "application/json" `
            -Body $body `
            -SkipCertificateCheck `
            -ErrorAction Stop
        
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "RESULTADO DE LA PRUEBA" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        
        if ($response.Success) {
            Write-Host "EXITO: RFC extraido correctamente" -ForegroundColor Green
            Write-Host ""
            Write-Host "RFC Extraido: " -NoNewline
            Write-Host $response.Data.Rfc -ForegroundColor Yellow -BackgroundColor DarkBlue
            Write-Host ""
        } else {
            Write-Host "ERROR: No se pudo extraer el RFC" -ForegroundColor Red
            Write-Host "Mensaje: $($response.Message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "ERROR AL COMUNICARSE CON EL API" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Asegurate de que:" -ForegroundColor Yellow
        Write-Host "  1. El API este corriendo (SICOE.API)" -ForegroundColor Yellow
        Write-Host "  2. El puerto sea 7052 (HTTPS)" -ForegroundColor Yellow
        Write-Host "  3. El certificado SSL de desarrollo este configurado" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Error detallado:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        Write-Host ""
        
        # Mostrar tambien el certificado directamente usando .NET
        Write-Host "Intentando extraer RFC directamente del certificado..." -ForegroundColor Yellow
        Write-Host ""
        
        try {
            Add-Type -AssemblyName System.Security
            $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)
            
            Write-Host "Informacion del Certificado:" -ForegroundColor Cyan
            Write-Host "  Subject: $($cert.Subject)" -ForegroundColor Gray
            Write-Host "  Issuer: $($cert.Issuer)" -ForegroundColor Gray
            Write-Host "  NotBefore: $($cert.NotBefore)" -ForegroundColor Gray
            Write-Host "  NotAfter: $($cert.NotAfter)" -ForegroundColor Gray
            Write-Host ""
            
            # Intentar extraer RFC del Subject usando regex simple
            $subject = $cert.Subject
            if ($subject -match '2\.5\.4\.45\s*=\s*([A-Z0-9]{12,13})') {
                $rfc = $matches[1]
                Write-Host "RFC encontrado en Subject: " -NoNewline -ForegroundColor Green
                Write-Host $rfc -ForegroundColor Yellow -BackgroundColor DarkBlue
            } else {
                Write-Host "No se encontro RFC en formato OID 2.5.4.45" -ForegroundColor Yellow
                Write-Host "  Subject completo: $subject" -ForegroundColor Gray
            }
        }
        catch {
            Write-Host "Error al leer el certificado directamente: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}
catch {
    Write-Host "ERROR: No se pudo leer el archivo del certificado" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
