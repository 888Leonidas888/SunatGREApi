---
name: MySQL CLI
description: How to query the MySQL database using CLI
---
# MySQL CLI Skill

This skill allows the agent to execute queries on the MySQL database using a predefined PowerShell script. It uses `mysqlsh` for connections.

## Connection Configuration
The connection parameters (host, port, user, password, database) are located in `run-query.ps1`.
**Note for User:** Please edit `run-query.ps1` with the real credentials for the test database.

## Usage
To execute a query, run the following command using the `run_command` tool:

```powershell
.agents\skills\mysql-cli\run-query.ps1 -Query "SELECT * FROM fake_table LIMIT 10;"
```

To execute a SQL file, you can modify the script or run:
```powershell
.agents\skills\mysql-cli\run-query.ps1 -File "path\to\script.sql"
```
