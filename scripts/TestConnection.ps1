# Script de prueba de conexión para SICOE API
# Prueba el endpoint de solicitar descarga

$baseUrl = "https://localhost:7001"  # Ajustar según el puerto que use la aplicación
$endpoint = "$baseUrl/api/descarga/solicitar"

# Datos de prueba
$body = @{
    ClienteId = 1
    FechaInicial = "2025-01-01T00:00:00"
    FechaFinal = "2025-01-31T23:59:59"
    TipoComprobante = $null
    RfcEmisor = $null
    RfcReceptor = $null
} | ConvertTo-Json

Write-Host "Probando conexión a SICOE API..." -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint" -ForegroundColor Yellow
Write-Host "Body: $body" -ForegroundColor Gray
Write-Host ""

try {
    # Ignorar certificados SSL para desarrollo local
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    
    $response = Invoke-RestMethod -Uri $endpoint -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    
    Write-Host "✅ ÉXITO - Respuesta recibida:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor White
}
catch {
    Write-Host "❌ ERROR:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Respuesta del servidor:" -ForegroundColor Yellow
        Write-Host $responseBody -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Para verificar Hangfire Dashboard, navega a: $baseUrl/hangfire" -ForegroundColor Cyan

