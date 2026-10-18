Write-Host "Configurando entorno OCPP Gateway..."
try {
    docker ps > $null 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker no esta ejecutandose."
        exit
    }
}
catch {
    Write-Error "Error al verificar Docker."
    exit
}

Write-Host "Iniciando contenedores..."
docker-compose up -d

Write-Host "Esperando inicio de la base de datos..."
Start-Sleep -Seconds 15

Write-Host "Compilando solucion..."
dotnet build ..\OcppGateway.sln
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error de compilacion."
    exit
}

dotnet ef database update --project ..\OcppGateway.Infrastructure --startup-project .
Write-Host "Configuracion completada."
