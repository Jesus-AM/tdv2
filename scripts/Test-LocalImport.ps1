# Synthetic only: no Herd files, databases or network. No fixture values in output.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Import-LaravelSettings.ps1')
$testRoot = Join-Path (Join-Path $PSScriptRoot '..\.artifacts') ('import-tests-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $testRoot
$fixture = Join-Path $testRoot 'synthetic.env'
$checks = [Collections.Generic.List[object]]::new()
function Assert($condition,[string]$message) { if (-not $condition) { throw $message } }
function Plain-Test([Security.SecureString]$value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
function Check([string]$name,[scriptblock]$test) {
    & $test
    $checks.Add(@{ name=$name; passed=$true })
    Write-Output ('PASS ' + $name)
}
function Reject([scriptblock]$work,[string]$expected) {
    $failed=$false
    try { $null = & $work } catch { $failed=$true; Assert ($_.Exception.Message -match $expected) 'Unexpected rejection category' }
    Assert $failed 'Unsafe input accepted'
}
$sample = @'
DB_HOST=laravel.invalid
DB_PASSWORD=not-for-import
APP_KEY=not-for-import
MSGRAPH_TENANT_ID=11111111-1111-1111-1111-111111111111
MSGRAPH_CLIENT_ID=22222222-2222-2222-2222-222222222222
MSGRAPH_SECRET_ID=" quote\";# \\ \$() end "
MSGRAPH_OAUTH_URL=https://legacy.invalid/connect
NEXO_APP_KEY=tdv2
NEXO_APP_ID=999999
NEXO_DB_HOST=nexo.invalid
NEXO_DB_DATABASE=published
NEXO_DB_USERNAME=reader
NEXO_DB_PASSWORD=' pass;"#\ $() end '
NEXO_DB_SSLMODE=require
MSSQL_HOST=sii.invalid
MSSQL_DATABASE=synthetic
MSSQL_USERNAME="DOMAIN\\reader"
MSSQL_PASSWORD="synthetic;\" # end" # trailing comment
MSSQL_ENCRYPT=yes
MSSQL_TRUST_SERVER_CERTIFICATE=false
SII_TABLA_UR=poa.UNIDADES_RESPONSABLES_POA
ILDA_DB_HOST=ilda.invalid
ILDA_DB_DATABASE=ilda_db
ILDA_DB_USERNAME=reader
ILDA_DB_PASSWORD='mysql;#"literal'
ILDA_ENABLED=true
INSTITUCIONAL_SYNC_AUTOMATICA=true
'@
try {
    [IO.File]::WriteAllText($fixture,$sample)
    $settings = Read-LaravelSettings $fixture
    $personal = Convert-LaravelSettings $settings
    Check 'Literal parsing, escapes, quoted spaces and comments' {
        Assert ($settings.MSGRAPH_SECRET_ID -ceq ' quote";# \ $() end ') 'Double quote decoding'
        Assert ($settings.NEXO_DB_PASSWORD -ceq ' pass;"#\ $() end ') 'Single quote preservation'
        Assert ($settings.MSSQL_USERNAME -ceq 'DOMAIN\reader') 'Backslash preservation'
        Assert ($settings.MSSQL_PASSWORD -ceq 'synthetic;" # end') 'Quoted comment preservation'
    }
    Check 'Local DB, callback, ID and activation settings excluded' {
        foreach ($key in @('DB_HOST','DB_PASSWORD','APP_KEY','MSGRAPH_OAUTH_URL','NEXO_APP_ID','ILDA_ENABLED','INSTITUCIONAL_SYNC_AUTOMATICA')) { Assert (-not $settings.ContainsKey($key)) 'Excluded key imported' }
        Assert ($personal.PSObject.Properties.Name -notcontains 'Tdv2') 'Local database override'
    }
    Check 'Connection escaping preserves credentials and TLS flags' {
        foreach ($spec in @(@('Nexo','NEXO_DB_PASSWORD'),@('Sii','MSSQL_PASSWORD'),@('Ilda','ILDA_DB_PASSWORD'))) {
            $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
            $builder.set_ConnectionString((Plain-Test $personal.($spec[0])))
            Assert ($builder['Password'] -ceq $settings[$spec[1]]) ('Connection credential changed: ' + $spec[0])
        }
        $sql = [System.Data.SqlClient.SqlConnectionStringBuilder]::new((Plain-Test $personal.Sii))
        Assert ($sql.Encrypt -and -not $sql.TrustServerCertificate -and $sql.ApplicationIntent -eq 'ReadOnly') 'SQL security options changed'
    }
    Check 'DPAPI export and import roundtrip' {
        $encrypted = Join-Path $testRoot 'synthetic.clixml'
        $personal | Export-Clixml -LiteralPath $encrypted
        $restored = Import-Clixml -LiteralPath $encrypted
        Assert ((Plain-Test $restored.ClientSecret) -ceq $settings.MSGRAPH_SECRET_ID) 'DPAPI mismatch'
        foreach ($key in @('MSGRAPH_SECRET_ID','NEXO_DB_PASSWORD','MSSQL_PASSWORD','ILDA_DB_PASSWORD')) { Assert (-not [IO.File]::ReadAllText($encrypted).Contains($settings[$key])) 'Plaintext in encrypted store' }
    }
    Check 'Reject missing credentials without disclosing values' {
        $changed=$settings.Clone(); $changed.Remove('NEXO_DB_PASSWORD')
        Reject { Convert-LaravelSettings $changed } '^Importacion: faltan claves requeridas: NEXO_DB_PASSWORD$'
    }
    Check 'Reject incompatible application, table and database contracts' {
        foreach ($key in @('NEXO_APP_KEY','SII_TABLA_UR','ILDA_DB_DATABASE')) {
            $changed=$settings.Clone(); $changed[$key]='other'
            Reject { Convert-LaravelSettings $changed } ('^Importacion: ' + $key)
        }
    }
    Check 'Reject duplicates, interpolation and malformed quotes' {
        foreach ($suffix in @("`nMSGRAPH_CLIENT_ID=duplicate",'${MISSING}', '"unterminated')) {
            $text = if ($suffix.StartsWith("`n")) { $sample + $suffix } else { $sample.Replace('MSGRAPH_CLIENT_ID=22222222-2222-2222-2222-222222222222',('MSGRAPH_CLIENT_ID=' + $suffix)) }
            [IO.File]::WriteAllText($fixture,$text)
            Reject { Read-LaravelSettings $fixture } '^Importacion:'
        }
    }
    Check 'Reject unknown TLS, boolean and port settings' {
        foreach ($key in @('NEXO_DB_SSLMODE','MSSQL_ENCRYPT','MSSQL_TRUST_SERVER_CERTIFICATE','MSSQL_PORT')) {
            $changed=$settings.Clone(); $changed[$key]='invalid'
            Reject { Convert-LaravelSettings $changed } '^Importacion:'
        }
    }
    [ordered]@{ utc=[DateTime]::UtcNow.ToString('o'); synthetic=$true; networkAccess=$false; checks=$checks } | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $testRoot 'verification.json')
    Write-Output ('Verified: ' + $checks.Count + '. Evidence: ' + (Join-Path $testRoot 'verification.json'))
} catch {
    Write-Output ('FAIL synthetic import verification: ' + $_.Exception.Message)
    exit 1
}
