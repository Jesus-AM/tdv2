# Pure parsing/mapping helpers. No network, database access, environment evaluation or writes.
# Deliberately imports only the supported literal settings, never Laravel's DB_* or APP_KEY.
function Read-LaravelSettings([string]$Path) {
    $allowed = @('MSGRAPH_TENANT_ID','MSGRAPH_CLIENT_ID','MSGRAPH_SECRET_ID',
        'NEXO_APP_KEY','NEXO_DB_HOST','NEXO_DB_PORT','NEXO_DB_DATABASE','NEXO_DB_USERNAME','NEXO_DB_PASSWORD','NEXO_DB_SSLMODE','NEXO_DB_SSLROOTCERT',
        'MSSQL_HOST','MSSQL_PORT','MSSQL_DATABASE','MSSQL_USERNAME','MSSQL_PASSWORD','MSSQL_ENCRYPT','MSSQL_TRUST_SERVER_CERTIFICATE','SII_TABLA_UR',
        'ILDA_DB_HOST','ILDA_DB_PORT','ILDA_DB_DATABASE','ILDA_DB_USERNAME','ILDA_DB_PASSWORD','ILDA_DB_SSL_CA')
    $settings = @{}
    foreach ($line in [IO.File]::ReadAllLines($Path)) {
        if ($line -notmatch '^\s*(?:export\s+)?([A-Z][A-Z0-9_]*)\s*=(.*)$') { continue }
        $key = $Matches[1]; $raw = $Matches[2].Trim()
        if ($key -cnotin $allowed) { continue }
        if ($settings.ContainsKey($key)) { throw ('Importacion: clave duplicada: ' + $key) }
        $quote = if ($raw.Length) { $raw[0] } else { [char]0 }
        if ($quote -eq '"' -or $quote -eq "'") {
            $buffer = [Text.StringBuilder]::new(); $closed = $false
            for ($i=1; $i -lt $raw.Length; $i++) {
                $ch = $raw[$i]
                if ($ch -eq $quote) { $closed=$true; break }
                if ($quote -eq '"' -and $ch -eq '\') {
                    $i++
                    if ($i -ge $raw.Length) { throw ('Importacion: escape incompleto en ' + $key) }
                    $ch = switch -CaseSensitive ($raw[$i]) {
                        '"' { '"' }; '\' { '\' }; '$' { '$' }
                        'n' { "`n" }; 'r' { "`r" }; 't' { "`t" }; 'f' { [char]12 }; 'v' { [char]11 }
                        default { throw ('Importacion: escape no admitido en ' + $key) }
                    }
                } elseif ($quote -eq '"' -and $ch -eq '$' -and ($i+1) -lt $raw.Length -and $raw[$i+1] -eq '{') {
                    throw ('Importacion: interpolacion requiere revision manual en ' + $key)
                }
                $null = $buffer.Append($ch)
            }
            if (-not $closed -or $raw.Substring($i+1).Trim() -notmatch '^(#.*)?$') { throw ('Importacion: comillas o sufijo no admitidos en ' + $key) }
            $value = $buffer.ToString()
        } else {
            $value = ($raw -split '#',2)[0].Trim()
            if ($value -match '\s|\$\{') { throw ('Importacion: valor sin comillas o interpolacion requiere revision en ' + $key) }
            # Laravel env() treats these unquoted literals as null/empty.
            if ($value -match '^(null|\(null\)|empty|\(empty\))$') { $value = '' }
        }
        $settings[$key] = $value
    }
    return $settings
}
function Convert-LaravelSettings([hashtable]$Settings) {
    $required = @('MSGRAPH_TENANT_ID','MSGRAPH_CLIENT_ID','MSGRAPH_SECRET_ID',
        'NEXO_DB_HOST','NEXO_DB_DATABASE','NEXO_DB_USERNAME','NEXO_DB_PASSWORD',
        'MSSQL_HOST','MSSQL_DATABASE','MSSQL_USERNAME','MSSQL_PASSWORD',
        'ILDA_DB_HOST','ILDA_DB_DATABASE','ILDA_DB_USERNAME','ILDA_DB_PASSWORD')
    $missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace($Settings[$_]) })
    if ($missing.Count) { throw ('Importacion: faltan claves requeridas: ' + ($missing -join ', ')) }
    $guid = [Guid]::Empty
    foreach ($key in @('MSGRAPH_TENANT_ID','MSGRAPH_CLIENT_ID')) {
        if (-not [Guid]::TryParse($Settings[$key],[ref]$guid)) { throw ('Importacion: GUID no valido en ' + $key) }
    }
    if ($Settings.ContainsKey('NEXO_APP_KEY') -and $Settings.NEXO_APP_KEY -cne 'tdv2') { throw 'Importacion: NEXO_APP_KEY debe resolver tdv2; no se amplio el contrato.' }
    if ($Settings.ILDA_DB_DATABASE -cne 'ilda_db') { throw 'Importacion: ILDA_DB_DATABASE no coincide con el contrato ilda_db.' }
    if ($Settings.ContainsKey('SII_TABLA_UR') -and $Settings.SII_TABLA_UR -cne 'poa.UNIDADES_RESPONSABLES_POA') { throw 'Importacion: SII_TABLA_UR no coincide con la unica tabla admitida.' }
    $ports = @{}
    foreach ($spec in @(@('NEXO_DB_PORT',5432),@('MSSQL_PORT',1439),@('ILDA_DB_PORT',3306))) {
        $port = [int]$spec[1]
        if ($Settings.ContainsKey($spec[0]) -and (-not [int]::TryParse($Settings[$spec[0]],[ref]$port) -or $port -lt 1 -or $port -gt 65535)) { throw ('Importacion: puerto no valido en ' + $spec[0]) }
        $ports[$spec[0]]=$port
    }
    $sslModes = @{ disable='Disable'; allow='Allow'; prefer='Prefer'; require='Require'; 'verify-ca'='VerifyCA'; 'verify-full'='VerifyFull' }
    $ssl = if ($Settings.ContainsKey('NEXO_DB_SSLMODE')) { $Settings.NEXO_DB_SSLMODE.ToLowerInvariant() } else { 'require' }
    if (-not $sslModes.ContainsKey($ssl)) { throw 'Importacion: NEXO_DB_SSLMODE no admitido.' }
    $encrypt = if ($Settings.ContainsKey('MSSQL_ENCRYPT')) { $Settings.MSSQL_ENCRYPT.ToLowerInvariant() } else { 'yes' }
    $encryptModes = @{ yes='True'; true='True'; '1'='True'; mandatory='True'; no='False'; false='False'; '0'='False'; optional='False'; strict='Strict' }
    if (-not $encryptModes.ContainsKey($encrypt)) { throw 'Importacion: MSSQL_ENCRYPT no admitido.' }
    $trust = if ($Settings.ContainsKey('MSSQL_TRUST_SERVER_CERTIFICATE')) { $Settings.MSSQL_TRUST_SERVER_CERTIFICATE.ToLowerInvariant() } else { 'false' }
    if ($trust -notin @('true','false','1','0','')) { throw 'Importacion: MSSQL_TRUST_SERVER_CERTIFICATE requiere un booleano explicito.' }
    # Builders quote embedded semicolons, quotes and spaces in credentials correctly.
    $nexo = [System.Data.Common.DbConnectionStringBuilder]::new()
    $nexo['Host']=$Settings.NEXO_DB_HOST; $nexo['Port']=$ports.NEXO_DB_PORT
    $nexo['Database']=$Settings.NEXO_DB_DATABASE; $nexo['Username']=$Settings.NEXO_DB_USERNAME; $nexo['Password']=$Settings.NEXO_DB_PASSWORD
    $nexo['SSL Mode']=$sslModes[$ssl]; $nexo['Search Path']='public'; $nexo['Timeout']=5
    $nexo['Include Error Detail']=$false; $nexo['Log Parameters']=$false
    if ($Settings.NEXO_DB_SSLROOTCERT) { $nexo['Root Certificate']=$Settings.NEXO_DB_SSLROOTCERT }
    $sii = [System.Data.Common.DbConnectionStringBuilder]::new()
    $sii['Data Source']=$Settings.MSSQL_HOST + ',' + $ports.MSSQL_PORT; $sii['Initial Catalog']=$Settings.MSSQL_DATABASE
    $sii['User ID']=$Settings.MSSQL_USERNAME; $sii['Password']=$Settings.MSSQL_PASSWORD
    $sii['Encrypt']=$encryptModes[$encrypt]; $sii['TrustServerCertificate']=($trust -in @('true','1'))
    $sii['ApplicationIntent']='ReadOnly'; $sii['Persist Security Info']=$false; $sii['Connect Timeout']=5
    $ilda = [System.Data.Common.DbConnectionStringBuilder]::new()
    $ilda['Server']=$Settings.ILDA_DB_HOST; $ilda['Port']=$ports.ILDA_DB_PORT; $ilda['Database']='ilda_db'
    $ilda['User ID']=$Settings.ILDA_DB_USERNAME; $ilda['Password']=$Settings.ILDA_DB_PASSWORD
    $ilda['Connection Timeout']=5; $ilda['Persist Security Info']=$false; $ilda['AllowLoadLocalInfile']=$false
    $ilda['SslMode']=if ($Settings.ILDA_DB_SSL_CA) { 'VerifyFull' } else { 'Preferred' }
    if ($Settings.ILDA_DB_SSL_CA) { $ilda['SslCa']=$Settings.ILDA_DB_SSL_CA }
    foreach ($key in @('NEXO_DB_SSLROOTCERT','ILDA_DB_SSL_CA')) {
        if ($Settings[$key] -and (-not [IO.Path]::IsPathRooted($Settings[$key]) -or -not [IO.File]::Exists($Settings[$key]))) { throw ('Importacion: certificado requiere ruta absoluta existente en ' + $key) }
    }
    return [PSCustomObject]@{
        TenantId=$Settings.MSGRAPH_TENANT_ID; ClientId=$Settings.MSGRAPH_CLIENT_ID
        ClientSecret=(ConvertTo-SecureString $Settings.MSGRAPH_SECRET_ID -AsPlainText -Force)
        Nexo=(ConvertTo-SecureString $nexo.ConnectionString -AsPlainText -Force)
        Sii=(ConvertTo-SecureString $sii.ConnectionString -AsPlainText -Force)
        Ilda=(ConvertTo-SecureString $ilda.ConnectionString -AsPlainText -Force)
    }
}
