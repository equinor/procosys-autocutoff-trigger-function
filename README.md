# procosys-autocutoff-trigger-function

Trigger ProCoSys AutoCutoff — an Azure Function App that runs on a timer schedule.

## CI/CD

| Workflow | Trigger | Environment |
|---|---|---|
| 🤖 Build & run tests | `pull_request` | — |
| ✏️ Verify formatting | `pull_request` | — |
| 🗃️ Verify PR Title | `pull_request` | — |
| 🚀🧪 Deploy to test | `pull_request` / manual | `test` |
| 🚀🏭 Deploy to prod | `push` to `main` | `prod` |
| 🚀🏭 Deploy rollback to prod | manual (`ref` input) | `prod-rollback` |

> **Note:** Merging to `main` automatically deploys to **prod**. There is no manual gate.

### PR flow

1. Open a PR — CI checks run and the app is deployed to **test**
2. Review, approve, and merge to `main`
3. Merge automatically deploys to **prod**

### Manual deployment

- **Test**: trigger the *Deploy to test* workflow manually via `workflow_dispatch`
- **Prod rollback**: trigger *Deploy rollback to prod* with a branch name, commit SHA, or relative ref (e.g. `HEAD~1`)

## Local development

Use settings like these in `secrets.json` to test locally (trigger every 15 seconds):

```json
{
  "Schedule": "*/15 * * * * *"
}
```
