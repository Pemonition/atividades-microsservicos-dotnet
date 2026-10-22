$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (!(Test-Path python/.venv/Scripts/python.exe)) {
    python -m venv python/.venv
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar ambiente Python.' }
}
& python/.venv/Scripts/python.exe -m pip install --only-binary=:all: -r python/requirements.txt
if ($LASTEXITCODE -ne 0) { throw 'Falha ao instalar dependencias Python.' }
& python/.venv/Scripts/python.exe -m grpc_tools.protoc -Icontratos --python_out=python --grpc_python_out=python contratos/produto.proto
if ($LASTEXITCODE -ne 0) { throw 'Falha na geracao do contrato Python.' }
dotnet restore Api/StudioCursos.Api.csproj
if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar pacotes .NET.' }
dotnet build Api/StudioCursos.Api.csproj --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar API.' }
Write-Output 'Pronto. Inicie python/server.py e a API nos terminais indicados no README.'
