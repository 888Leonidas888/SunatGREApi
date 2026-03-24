---
name: SQL Server CLI
description: How to query the SQL Server database using CLI
---
# SQL Server CLI Skill

This skill allows the agent to execute queries on the SQL Server database using a predefined PowerShell script.

## Connection Configuration
The connection parameters (server, port, user, password, database) are located in `run-query.ps1`.
**Note for User:** Please edit `run-query.ps1` with the real credentials for the SQL Server test database.

## Usage
To execute a query, run the following command using the `run_command` tool:

```powershell
.agents\skills\sqlserver-cli\run-query.ps1 -Query "SELECT TOP 10 * FROM lg_proveedor;"
```

To execute a SQL file, run:
```powershell
.agents\skills\sqlserver-cli\run-query.ps1 -File "path\to\script.sql"
```
