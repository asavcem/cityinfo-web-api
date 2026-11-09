param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5199
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$apiRoot = Join-Path $projectRoot 'CityInfo.API'
$checkDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('cityinfo-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkDirectory | Out-Null
$baseUrl = "http://127.0.0.1:$Port"
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousConnection = $env:ConnectionStrings__CityInfoDBConnectionString
$apiProcess = $null
$script:checks = 0

function Assert-Response {
    param([string]$Method, [string]$Path, [int]$Status, [string]$Body, [string]$ContentType = 'application/json')
    $request = @{
        Uri = $baseUrl + $Path
        Method = $Method
        UseBasicParsing = $true
        TimeoutSec = 10
    }
    if ($Body) {
        $request.Body = $Body
        $request.ContentType = $ContentType
    }
    try {
        $response = Invoke-WebRequest @request
    }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        $actualStatus = [int]$_.Exception.Response.StatusCode
        if ($actualStatus -ne $Status) { throw "$Method $Path expected $Status, received $actualStatus." }
        $script:checks++
        return
    }
    if ([int]$response.StatusCode -ne $Status) {
        throw "$Method $Path expected $Status, received $($response.StatusCode)."
    }
    $script:checks++
    return $response
}

Push-Location $projectRoot
try {
    # Refuse to exercise an unrelated process already listening on this port.
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    try { $listener.Start() } finally { $listener.Stop() }

    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Local EF tool restore failed.' }
    dotnet build CityInfo.slnx
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $databasePath = Join-Path $checkDirectory 'CityInfo.db'
    $env:ConnectionStrings__CityInfoDBConnectionString = "Data Source=$databasePath"
    Push-Location $apiRoot
    try {
        dotnet ef database update --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }
    }
    finally { Pop-Location }

    $startOptions = @{
        FilePath = 'dotnet'
        ArgumentList = @('bin/Debug/net10.0/CityInfo.API.dll', '--urls', $baseUrl)
        WorkingDirectory = $apiRoot
        PassThru = $true
        RedirectStandardOutput = (Join-Path $checkDirectory 'stdout.log')
        RedirectStandardError = (Join-Path $checkDirectory 'stderr.log')
    }
    if ($env:OS -eq 'Windows_NT') { $startOptions.WindowStyle = 'Hidden' }
    $apiProcess = Start-Process @startOptions

    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($apiProcess.HasExited) { throw 'API exited before becoming ready. Inspect the temporary logs.' }
        try {
            $null = Invoke-WebRequest -Uri "$baseUrl/api/cities" -UseBasicParsing -TimeoutSec 1
            $ready = $true
            break
        }
        catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw 'API did not become ready.' }

    $cities = (Assert-Response GET '/api/cities' 200).Content | ConvertFrom-Json
    if (($cities.name -join ',') -ne 'Antwerp,New York City,Paris') { throw 'Unexpected seed cities or order.' }

    $cases = @(
        @{ Path = '?name=Paris'; Expected = 'Paris' },
        @{ Path = '?name=%20Paris%20'; Expected = 'Paris' },
        @{ Path = '?searchQuery=park'; Expected = 'New York City' },
        @{ Path = '?searchQuery=Antw'; Expected = 'Antwerp' },
        @{ Path = '?searchQuery=%20tower%20'; Expected = 'Paris' },
        @{ Path = '?name=Paris&searchQuery=tower'; Expected = 'Paris' },
        @{ Path = '?name=Antwerp&searchQuery=tower'; Expected = '' },
        @{ Path = '?searchQuery=no-such-city'; Expected = '' }
    )
    foreach ($case in $cases) {
        $items = @((Assert-Response GET ('/api/cities' + $case.Path) 200).Content | ConvertFrom-Json)
        if (($items.name -join ',') -ne $case.Expected) { throw "Unexpected search/filter response: $($case.Path)" }
    }

    $city = (Assert-Response GET '/api/cities/3?includePointsOfInterest=true' 200).Content | ConvertFrom-Json
    if (@($city.pointsOfInterest).Count -ne 2) { throw 'Expected two seeded points of interest for Paris.' }
    $null = Assert-Response GET '/api/cities/999' 404
    $null = Assert-Response GET '/api/cities/999/pointsofinterest' 404
    $null = Assert-Response GET '/openapi/v1.json' 200
    $null = Assert-Response POST '/api/cities/3/pointsofinterest' 400 '{"name":""}'

    $bulk = (Assert-Response PUT '/api/cities/3/pointsofinterest/bulk' 200 '{"newDescription":"Smoke bulk description"}').Content | ConvertFrom-Json
    if ($bulk.updatedCount -ne 2) { throw 'Expected two bulk-updated rows.' }
    $points = (Assert-Response GET '/api/cities/3/pointsofinterest' 200).Content | ConvertFrom-Json
    if (@($points | Where-Object description -ne 'Smoke bulk description').Count -ne 0) { throw 'Bulk descriptions were not persisted.' }
    $null = Assert-Response PUT '/api/cities/999/pointsofinterest/bulk' 404 '{"newDescription":"Not applied"}'

    $created = Assert-Response POST '/api/cities/3/pointsofinterest' 201 '{"name":"Smoke attraction","description":"Smoke description"}'
    $newPoint = $created.Content | ConvertFrom-Json
    if (-not $created.Headers['Location']) { throw 'Create response has no Location header.' }
    $pointPath = "/api/cities/3/pointsofinterest/$($newPoint.id)"
    $null = Assert-Response GET $pointPath 200
    $null = Assert-Response PUT $pointPath 204 '{"name":"Updated attraction","description":"Updated description"}'
    $null = Assert-Response PATCH $pointPath 204 '[{"op":"replace","path":"/name","value":"Patched attraction"}]' 'application/json-patch+json'
    $updated = (Assert-Response GET $pointPath 200).Content | ConvertFrom-Json
    if ($updated.name -ne 'Patched attraction' -or $updated.description -ne 'Updated description') { throw 'PUT/PATCH values were not persisted.' }
    $null = Assert-Response DELETE $pointPath 204
    $null = Assert-Response GET $pointPath 404

    Write-Output "PASS: $script:checks HTTP checks against a fresh migrated SQLite database."
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id
        $null = $apiProcess.WaitForExit(5000)
    }
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:ConnectionStrings__CityInfoDBConnectionString = $previousConnection
    Pop-Location
    Write-Output "Temporary database and logs: $checkDirectory"
}

