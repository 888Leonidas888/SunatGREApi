param(
    [string]$Query,
    [string]$File
)

$DATABASE_FILE = "C:\Users\jescriba\Desktop\development\SunatGREApi\guias_sunat.db"

if ($Query) {
    C:/Users/jescriba/AppData/Local/SQlite/sqlite-tools-win-x64-3510200/sqlite3.exe "$DATABASE_FILE" "$Query"
}
elseif ($File) {
    C:/Users/jescriba/AppData/Local/SQlite/sqlite-tools-win-x64-3510200/sqlite3.exe "$DATABASE_FILE" ".read `"$File`""
}
else {
    Write-Host "Please provide -Query or -File parameter"
}
