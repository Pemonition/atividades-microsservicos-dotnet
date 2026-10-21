param([switch]$IncludeOrders)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$workspace = Split-Path $root -Parent
$logs = Join-Path $root 'artifacts'
New-Item $logs -ItemType Directory -Force | Out-Null
$processes = [Collections.Generic.List[System.Diagnostics.Process]]::new()
$results = [Collections.Generic.List[string]]::new()
$headers = @{ Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('guest:guest')) }

function Wait-Check([scriptblock]$Check, [string]$Description) {
    for ($i = 0; $i -lt 60; $i++) {
        try { if (& $Check) { return } } catch { }
        Start-Sleep -Seconds 1
    }
    throw "Tempo esgotado: $Description"
}
function Start-App([string]$Project, [string]$Name, [int]$Port = 0, [int]$SmtpPort = 1025) {
    $dll = Join-Path $Project "bin/Debug/net8.0/$Name.dll"
    if (!(Test-Path $dll)) { throw "Compile o projeto antes: $Project" }
    $arguments = "`"$dll`""
    if ($Port) { $arguments += " --urls http://127.0.0.1:$Port --environment Development" }
    $environment = @{
        RabbitMQ__Uri = 'amqp://guest:guest@localhost:5672/'
        Smtp__Host = 'localhost'; Smtp__Port = "$SmtpPort"; Smtp__Security = 'None'
        Smtp__User = ''; Smtp__Password = ''; Smtp__From = 'cadastro@loja.local'
    }
    $process = Start-Process dotnet -ArgumentList $arguments -WorkingDirectory $Project -Environment $environment `
        -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs "$Name-$SmtpPort.log") `
        -RedirectStandardError (Join-Path $logs "$Name-$SmtpPort-error.log")
    $processes.Add($process)
    return $process
}
function Stop-App($Process) {
    if (!$Process.HasExited) { Stop-Process -Id $Process.Id; $Process.WaitForExit() }
}
function Request([string]$Url, [string]$Method, [object]$Body, [int]$Expected) {
    $response = Invoke-WebRequest $Url -Method $Method -ContentType application/json `
        -Body ($Body | ConvertTo-Json -Depth 10) -SkipHttpErrorCheck -TimeoutSec 20
    if ([int]$response.StatusCode -ne $Expected) {
        throw "$Method $Url retornou $($response.StatusCode); esperado $Expected. $($response.Content)"
    }
    if ($response.Content) { return ($response.Content | ConvertFrom-Json) }
}
function Check-Email([string]$Address, [string[]]$Text) {
    Wait-Check {
        $script:emailSummary = (Invoke-RestMethod http://127.0.0.1:8025/api/v1/messages -TimeoutSec 5).messages |
            Where-Object { @($_.To | ForEach-Object { $_.Address }) -contains $Address } | Select-Object -First 1
        $null -ne $script:emailSummary
    } "email para $Address"
    $message = Invoke-RestMethod "http://127.0.0.1:8025/api/v1/message/$($script:emailSummary.ID)" -TimeoutSec 5
    foreach ($part in $Text) {
        if (!$message.Text.Contains($part)) { throw "Email sem dado esperado: $part" }
    }
    return $message
}
function Queue-Count([string]$Queue) {
    return (Invoke-RestMethod "http://127.0.0.1:15672/api/queues/%2F/$Queue" -Headers $headers -TimeoutSec 5).messages
}
function Passed([string]$Description) { $results.Add($Description); Write-Output "PASSOU: $Description" }

try {
    Wait-Check { (Invoke-RestMethod http://127.0.0.1:15672/api/overview -Headers $headers -TimeoutSec 5).rabbitmq_version } 'RabbitMQ'
    Wait-Check { (Invoke-WebRequest http://127.0.0.1:8025/api/v1/messages -TimeoutSec 5).StatusCode -eq 200 } 'Mailpit'
    $user = Start-App (Join-Path $root 'UserService') 'UserService' 15104
    $email = Start-App (Join-Path $root 'EmailService') 'EmailService' 15289
    Wait-Check { (Invoke-WebRequest http://127.0.0.1:15104/swagger/v1/swagger.json).StatusCode -eq 200 } 'UserService'
    Wait-Check { (Invoke-WebRequest http://127.0.0.1:15289/swagger/v1/swagger.json).StatusCode -eq 200 } 'EmailService'
    $address = "cadastro-$([guid]::NewGuid().ToString('N'))@example.com"
    $created = Request http://127.0.0.1:15104/api/Usuario POST @{ nome = 'Aluno Integracao'; email = $address } 202
    $null = Check-Email $address @('Aluno Integracao', $created.id, 'UTC')
    $null = Request http://127.0.0.1:15104/api/Usuario POST @{ nome = ''; email = 'invalido' } 400
    Passed 'AuthApp: HTTP 202 -> RabbitMQ -> SMTP -> email com nome, ID, data e destinatario; dados invalidos retornam 400.'
    $direct = @{ id = [guid]::NewGuid().ToString(); name = 'Envio Direto'; email = "direto-$address"; dataCriacao = [DateTime]::UtcNow.ToString('O') }
    $null = Request http://127.0.0.1:15289/api/Email/boas-vindas POST $direct 200
    $null = Check-Email $direct.email @('Envio Direto', $direct.id)
    Passed 'AuthApp: endpoint direto retorna 200 somente apos envio SMTP.'
    Stop-App $email
    $before = Queue-Count 'fila-usuario-criado-v2-falhas'
    $email = Start-App (Join-Path $root 'EmailService') 'EmailService' 15289 1
    Wait-Check { (Invoke-WebRequest http://127.0.0.1:15289/swagger/v1/swagger.json).StatusCode -eq 200 } 'EmailService com SMTP indisponivel'
    $null = Request http://127.0.0.1:15104/api/Usuario POST @{ nome = 'Falha SMTP'; email = "falha-$address" } 202
    Wait-Check { (Queue-Count 'fila-usuario-criado-v2-falhas') -gt $before } 'evento na fila de falhas AuthApp'
    $null = Request http://127.0.0.1:15289/api/Email/boas-vindas POST $direct 502
    Passed 'AuthApp: falha SMTP preserva evento na fila de falhas e endpoint direto retorna 502.'
    Stop-App $email
    Stop-App $user

    if ($IncludeOrders) {
        $ordersRoot = Join-Path $workspace 'EcommereceMicroservices'
        $api = Start-App (Join-Path $ordersRoot 'OrderApi') 'OrderApi' 15109
        $worker = Start-App (Join-Path $ordersRoot 'EmailWorker') 'EmailWorker'
        Wait-Check { (Invoke-WebRequest http://127.0.0.1:15109/swagger/v1/swagger.json).StatusCode -eq 200 } 'OrderApi'
        $orderAddress = "pedido-$address"
        $order = Request http://127.0.0.1:15109/pedidos POST @{ clienteNome = 'Cliente Integracao'; clienteEmail = $orderAddress; valorTotal = 149.90 } 202
        $id = $order.pedido.pedidoId
        $null = Request "http://127.0.0.1:15109/pedidos/$id" PUT @{ status = 'Enviado'; valorTotal = 159.90 } 200
        $null = Check-Email $orderAddress @('Cliente Integracao', $id, 'Criado', 'Enviado', '159,90', 'UTC')
        $null = Request "http://127.0.0.1:15109/pedidos/$([guid]::NewGuid())" PUT @{ status = 'Pago'; valorTotal = 1 } 404
        $null = Request "http://127.0.0.1:15109/pedidos/$id" PUT @{ status = ''; valorTotal = -1 } 400
        Passed 'Pedidos: criacao 202, atualizacao 200, email com status anterior/atual e valor; inexistente 404 e invalido 400.'
        Stop-App $worker
        $before = Queue-Count 'pedidos-atualizados-email-falhos'
        $worker = Start-App (Join-Path $ordersRoot 'EmailWorker') 'EmailWorker' 0 1
        $null = Request "http://127.0.0.1:15109/pedidos/$id" PUT @{ status = 'Entregue'; valorTotal = 159.90 } 200
        Wait-Check { (Queue-Count 'pedidos-atualizados-email-falhos') -gt $before } 'evento na fila de falhas de pedidos'
        Passed 'Pedidos: SMTP indisponivel aciona tentativas limitadas e preserva evento na fila de falhas.'
    }
    @{ checkedAt = [DateTimeOffset]::Now.ToString('O'); passed = $results.ToArray() } |
        ConvertTo-Json -Depth 5 | Set-Content (Join-Path $logs 'integration-result.json') -Encoding utf8
} finally {
    foreach ($process in $processes) { Stop-App $process }
}
