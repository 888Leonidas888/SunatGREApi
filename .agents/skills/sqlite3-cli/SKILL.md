---
name: SQLite3 CLI
description: How to query the SQLite3 database using CLI
---
# SQLite3 CLI Skill

This skill allows the agent to execute queries on the SQLite3 database using a predefined PowerShell script.

## Connection Configuration
The database file path is located in `run-query.ps1`.
**Note for User:** Please edit `run-query.ps1` with the real path to the `.sqlite3` or `.db` test database file.

## Usage
To execute a query, run the following command using the `run_command` tool:

```powershell
.agents\skills\sqlite3-cli\run-query.ps1 -Query "SELECT * FROM fake_table LIMIT 10;"
```

To execute a SQL file, run:
```powershell
.agents\skills\sqlite3-cli\run-query.ps1 -File "path\to\script.sql"
```
