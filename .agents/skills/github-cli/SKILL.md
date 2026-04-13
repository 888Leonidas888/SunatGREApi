---
name: github-cli
description: Expertise in managing GitHub repositories, PRs, and issues using the GitHub CLI (`gh`).
---

# GitHub CLI Skill

This skill allows the agent to interact with GitHub repositories directly from the command line using the official GitHub CLI (`gh`).

## Common Commands

### Authentication
Check authentication status:
```powershell
gh auth status
```

### Pull Requests
List open PRs:
```powershell
gh pr list
```

View a specific PR:
```powershell
gh pr view <number-or-url>
```

Create a new PR:
```powershell
gh pr create --title "Title" --body "Body"
```

Merge a PR:
```powershell
gh pr merge <number-or-url> --merge
```

### Issues
List open issues:
```powershell
gh issue list
```

Create an issue:
```powershell
gh issue create --title "Title" --body "Body"
```

### Repository
View repository information:
```powershell
gh repo view
```

## Guidelines for Agents
- Always check `gh auth status` if you suspect authentication issues.
- Use descriptive titles and bodies for PRs and Issues.
- When creating a PR, prefer using `--title` and `--body` flags to avoid interactive prompts.
- To push a new branch and create a PR in one flow:
  ```powershell
  git push -u origin <branch-name>; gh pr create --fill
  ```
