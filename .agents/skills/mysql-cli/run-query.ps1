param(
    [string]$Query,
    [string]$File
)

$HOST_MYSQL = "localhost"
$PORT_MYSQL = "3306"
$USER_MYSQL = "root"
$PASSWORD_MYSQL = ""
$DATABASE_MYSQL = "datamart"

$uri = if ([string]::IsNullOrEmpty($PASSWORD_MYSQL)) {
    "mysql://${USER_MYSQL}@${HOST_MYSQL}:${PORT_MYSQL}/${DATABASE_MYSQL}"
} else {
    "mysql://${USER_MYSQL}:${PASSWORD_MYSQL}@${HOST_MYSQL}:${PORT_MYSQL}/${DATABASE_MYSQL}"
}

if ($Query) {
    mysqlsh $uri --sql -e "$Query"
}
elseif ($File) {
    mysqlsh $uri --sql -f "$File"
}
else {
    Write-Host "Please provide -Query or -File parameter"
}
