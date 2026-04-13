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
- **Branching Rule:** ALWAYS start new feature branches from a clean and updated `main` branch.
  ```powershell
  git checkout main; git pull origin main; git checkout -b feat/your-feature-name
  ```
- **PR Workflow:** When a PR is created, wait for the CI (GitHub Actions) to pass (Build/Test) before requesting a human review.
- **Descriptive PRs:** Use `--title` and `--body` flags to explain the "why" of the changes.
- **Authentication:** Always check `gh auth status` if you suspect authentication issues.
- **Atomic Commits:** Prefer small, focused commits over large, monolithic ones.
