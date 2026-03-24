param(
    [string]$Query,
    [string]$File
)

$SERVER = "192.168.30.22"
$PORT = "1433"
$DATABASE = "SIGE_STN_PROD"

if ($Query) {
    sqlcmd -S "$SERVER,$PORT" -E -d $DATABASE -Q "$Query"
} elseif ($File) {
    sqlcmd -S "$SERVER,$PORT" -E -d $DATABASE -i "$File"
} else {
    Write-Host "Please provide -Query or -File parameter"
}
