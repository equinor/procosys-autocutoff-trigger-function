# Contributing

This repository follows **trunk-based development**. All changes go through pull requests targeting `main`.

## Checklist

- [ ] PR title follows [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) (e.g. `feat: ...`, `fix: ...`)
- [ ] Code builds without errors
- [ ] Formatting passes (`dotnet format --verify-no-changes`)
- [ ] Changes have been verified in the **test** environment (deployed on PR)

## Deployment flow

```mermaid
graph LR
    PR[Pull Request] -->|CI + deploy| Test[test]
    Merge[Merge to main] -->|auto-deploy| Prod[prod]
    Rollback[Manual rollback] -->|workflow_dispatch| Prod
```
