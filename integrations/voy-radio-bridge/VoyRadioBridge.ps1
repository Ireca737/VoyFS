# ============================================================
# VOY Radio Bridge - FlyLab
# POC 0.1 - GitHub-safe baseline
# JoinFS COM Webhook -> TeamSpeak ServerQuery
# ============================================================

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ConfigPath = Join-Path $Root "config\radio-bridge.json"
$CredentialsPath = Join-Path $Root "config\credentials.json"

if (-not (Test-Path $ConfigPath)) { throw "Missing config: $ConfigPath" }
if (-not (Test-Path $CredentialsPath)) { throw "Missing local credentials: $CredentialsPath" }

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$credentials = Get-Content $CredentialsPath -Raw | ConvertFrom-Json

$ListenUrl  = [string]$config.listen_url
$TSHost     = [string]$config.teamspeak.host
$TSPort     = [int]$config.teamspeak.port
$TSServerID = [int]$config.teamspeak.server_id
$TSUser     = [string]$credentials.teamspeak.username
$TSPassword = [string]$credentials.teamspeak.password

function Connect-TeamSpeak {
    Write-Host "Connessione TeamSpeak ServerQuery..." -ForegroundColor Cyan
    $script:tcp = New-Object System.Net.Sockets.TcpClient($TSHost, $TSPort)
    $script:stream = $script:tcp.GetStream()
    $script:reader = New-Object System.IO.StreamReader($script:stream)
    $script:writer = New-Object System.IO.StreamWriter($script:stream)
    $script:writer.NewLine = "`r`n"
    $script:writer.AutoFlush = $true
    Start-Sleep -Milliseconds 200
    while ($script:stream.DataAvailable) { $null = $script:reader.ReadLine() }
    Invoke-TSQuery "login $TSUser $TSPassword" | Out-Null
    Invoke-TSQuery "use sid=$TSServerID" | Out-Null
    Write-Host "TeamSpeak ServerQuery connesso." -ForegroundColor Green
}

function Invoke-TSQuery {
    param([string]$Command)
    $script:writer.WriteLine($Command)
    $lines = @()
    while ($true) {
        $line = $script:reader.ReadLine()
        if ($null -eq $line) { throw "Connessione TeamSpeak ServerQuery chiusa." }
        if ($line -match '^error id=') {
            if ($line -notmatch '^error id=0 ') { throw "TeamSpeak ServerQuery: $line" }
            break
        }
        if ($line -ne "") { $lines += $line }
    }
    return $lines
}

function ConvertFrom-TSEscape {
    param([string]$Text)
    if ($null -eq $Text) { return "" }
    $Text = $Text.Replace('\s', ' ')
    $Text = $Text.Replace('\p', '|')
    $Text = $Text.Replace('\/', '/')
    $Text = $Text.Replace('\\', '\')
    return $Text
}

function Get-TSClientId {
    param([string]$Nickname)
    $response = Invoke-TSQuery "clientlist"
    foreach ($line in $response) {
        foreach ($entry in ($line -split '\|')) {
            $clid = $null; $name = $null; $type = $null
            foreach ($field in ($entry -split ' ')) {
                if ($field -match '^clid=(.+)$') { $clid = $Matches[1] }
                if ($field -match '^client_nickname=(.*)$') { $name = ConvertFrom-TSEscape $Matches[1] }
                if ($field -match '^client_type=(.+)$') { $type = $Matches[1] }
            }
            if (($type -eq "0") -and ($name -ceq $Nickname)) { return $clid }
        }
    }
    return $null
}

function Get-TSChannelId {
    param([string]$Frequency)
    $response = Invoke-TSQuery "channellist -topic"
    foreach ($line in $response) {
        foreach ($entry in ($line -split '\|')) {
            $cid = $null; $topic = $null
            foreach ($field in ($entry -split ' ')) {
                if ($field -match '^cid=(.+)$') { $cid = $Matches[1] }
                if ($field -match '^channel_topic=(.*)$') { $topic = ConvertFrom-TSEscape $Matches[1] }
            }
            if ($topic -eq $Frequency) { return $cid }
        }
    }
    return $null
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($ListenUrl)

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " VOY Radio Bridge - FlyLab - POC 0.1" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

Connect-TeamSpeak
$listener.Start()

Write-Host ""
Write-Host "Webhook JoinFS: $ListenUrl" -ForegroundColor Green
Write-Host "In attesa di variazioni COM1..." -ForegroundColor Green
Write-Host "CTRL+C per terminare."
Write-Host ""

while ($listener.IsListening) {
    $context = $listener.GetContext()
    try {
        $request = $context.Request
        $readerHTTP = New-Object System.IO.StreamReader($request.InputStream,$request.ContentEncoding)
        $body = $readerHTTP.ReadToEnd()
        $readerHTTP.Close()
        $data = $body | ConvertFrom-Json

        foreach ($update in $data.comsupdate) {
            $nickname = [string]$update.nickname
            $frequency = ([string]$update.com1).Replace(',', '.')

            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
            Write-Host "$nickname " -ForegroundColor Yellow -NoNewline
            Write-Host "COM1 = $frequency"

            $clid = Get-TSClientId $nickname
            if (-not $clid) {
                Write-Host "  TeamSpeak: client '$nickname' non trovato." -ForegroundColor DarkYellow
                continue
            }

            $cid = Get-TSChannelId $frequency
            if (-not $cid) {
                Write-Host "  Nessun canale con Topic $frequency" -ForegroundColor DarkYellow
                continue
            }

            Write-Host "  TS client: clid=$clid"
            Write-Host "  TS channel: cid=$cid"
            Invoke-TSQuery "clientmove clid=$clid cid=$cid" | Out-Null
            Write-Host "  MOVE OK -> $frequency" -ForegroundColor Green
        }

        $buffer = [System.Text.Encoding]::UTF8.GetBytes("OK")
        $context.Response.StatusCode = 200
        $context.Response.ContentLength64 = $buffer.Length
        $context.Response.OutputStream.Write($buffer, 0, $buffer.Length)
        $context.Response.OutputStream.Close()
    }
    catch {
        Write-Host "ERRORE: $($_.Exception.Message)" -ForegroundColor Red
        try {
            $context.Response.StatusCode = 500
            $context.Response.Close()
        } catch {}
    }
}
