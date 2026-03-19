---
name: github-actions-pipeline-migration
description: Migrates Azure DevOps pipelines to GitHub Actions, or sets up new GitHub Actions CI/CD for ProCoSys repositories. Use when asked to set up GitHub Actions, replace Azure DevOps pipelines, add CI/CD workflows, or configure dev/test/prod deployment environments. Covers Azure Function Apps and Web Apps deployed via Docker container or zip package, using OIDC authentication and private NuGet feeds.
compatibility: Requires GitHub Actions and Azure CLI. Docker deployments additionally require Azure Container Registry access.
---

# GitHub Actions Pipeline Migration Skill

When invoked to set up or migrate GitHub Actions for a ProCoSys repository, perform the following steps:

## Migration Workflow

1. **Analyze the repository**
   - Report that the github-actions-pipeline-migration skill is being used
   - Identify all projects in the `.sln` file (for formatting matrix)
   - Identify all private NuGet feed names from `nuget.config`
   - Determine the **deployment method**: Docker (container image) or zip (package deploy)
   - Determine the **app type**: Azure Function App or Web App (App Service)
   - Determine which environments exist: `dev` (optional), `test`, `prod`, `prod-rollback`
   - Identify resource group names and app names for each environment
   - **If Docker**: identify the Dockerfile path, build context, ACR name, and image repository name

2. **Create composite actions**
   - Create `.github/actions/get-devops-token/action.yml`
   - Create `.github/actions/nuget-config/action.yml`

3. **Create CI workflows**
   - Create `.github/workflows/ci-build-and-test.yml`
   - Create `.github/workflows/ci-verify-formatting.yml`
   - Create `.github/workflows/ci-verify-pr-title.yml`

4. **Create deploy workflows** (based on which environments exist)
   - `deploy-dev.yml` — if a `dev` environment exists
   - `deploy-test.yml` — always
   - `deploy-prod.yml` — always
   - `deploy-prod-rollback.yml` — always

5. **Update repository documentation**
   - Create/update `README.md` — keep concise with brief description, CI/CD workflow section (including important note about auto-deploy to prod), PR/Production flow, and manual deployment instructions
   - Create/update `CONTRIBUTING.md` — keep concise with trunk-based development intro, contribution checklist, and mermaid flowchart showing dev/test/prod deployment flow
   - **Style**: Follow ProCoSys convention of concise, focused documentation (see procosys-fam-feeder-function as reference)

6. **Remove old pipeline files**
   - Delete the `pipelines/` directory if migrating from Azure DevOps

---

## Environment & Trigger Convention

| Environment | Workflow | Automatic trigger | Manual trigger |
|---|---|---|---|
| `dev` (optional) | `deploy-dev.yml` | `pull_request` | `workflow_dispatch` |
| `test` | `deploy-test.yml` | `push: branches: main` + `pull_request` (if no dev) | `workflow_dispatch` |
| `prod` | `deploy-prod.yml` | `push: branches: main` | — |
| `prod-rollback` | `deploy-prod-rollback.yml` | — | `workflow_dispatch` with `ref` input |

- If a **`dev` environment exists**: `deploy-dev.yml` triggers on `pull_request`; `deploy-test.yml` triggers on `push: branches: main`
- If **no `dev` environment**: `deploy-test.yml` triggers on both `pull_request` and `push: branches: main`
- `deploy-prod.yml` and `deploy-prod-rollback.yml` are always created alongside `deploy-test.yml`
- **`deploy-prod.yml` has NO manual trigger** — prod deployment only happens automatically on merge to main for stricter production control. Use `deploy-prod-rollback.yml` for any manual production interventions

---

See [references/REFERENCE.md](references/REFERENCE.md) for complete file templates.

---

## Key Rules

### Naming Conventions
- CI workflows are prefixed with `ci-`: `ci-build-and-test.yml`, `ci-verify-formatting.yml`, `ci-verify-pr-title.yml`
- Use full words in filenames: `ci-verify-formatting.yml` (not `ci-verify-format.yml`)
- Deploy workflows: `deploy-dev.yml`, `deploy-test.yml`, `deploy-prod.yml`, `deploy-prod-rollback.yml`
- Workflow names use emojis: 🤖 CI, ✏️ formatting, 🗃️ PR title, 🚀🔬 dev deploy, 🚀🧪 test deploy, 🚀🏭 prod deploy

### CI Workflows
- **ci-build-and-test.yml**: If multiple test projects exist (e.g., `Core.Tests`, `Infrastructure.Tests`), use a matrix strategy with `fail-fast: false` to run tests in parallel. Build solution once, then run `dotnet test ./${{ matrix.test-project }}` for each test project.
- **ci-verify-formatting.yml**: Use matrix strategy with `fail-fast: false` to check all projects independently

### GitHub Variables vs Secrets
- `vars.AZURE_CLIENT_ID` — variable, not a secret
- `vars.AZURE_TENANT_ID` — variable, not a secret
- `vars.AZURE_SUBSCRIPTION_ID` — variable, not a secret
- Only use `secrets.*` for genuinely secret values (passwords, keys)
- These must be configured per GitHub environment

### Authentication
- OIDC / Workload Identity Federation throughout — no stored credentials
- CI workflows (NuGet only): use `allow-no-subscriptions: true` in `get-devops-token` action
- Deploy workflows: use `subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}` (required for `az functionapp`/`az webapp` commands)
- NuGet token in deploy workflows is obtained inline via `az account get-access-token` after the main Azure login

### Deployment Artifact Versioning
- **Docker**: tag images with short SHA (7 chars): `git rev-parse --short HEAD`
  - Regular deploys: step with `id: sha`, referenced as `steps.sha.outputs.short`
  - Rollback: compute after checking out the target ref with `id: commit`, referenced as `steps.commit.outputs.sha`
  - **Do not use `github.sha`** for rollback — it is the workflow trigger commit, not the checked-out ref
- **Zip**: the artifact is ephemeral (built and deployed inline); no image registry involved. Rollback follows the same git ref checkout pattern but rebuilds the zip from source.

### App Type and Deployment Method
- **Docker — Function App**: `az functionapp config container set --registry-server ... --image ...`
- **Docker — Web App**: `az webapp config container set --registry-server ... --container-image-name ...`
- **Zip — Function App**: `az functionapp deployment source config-zip --src deploy.zip`
- **Zip — Web App**: `az webapp deploy --src-path deploy.zip --type zip`
- OIDC login and NuGet token steps are identical regardless of deployment method

### Deprecated `az functionapp`/`az webapp` flags (Docker deployments only)
- Use `--registry-server` (not deprecated `--docker-registry-server-url`)
- Use `--image` for function apps (not deprecated `--docker-custom-image-name`)
- Use `--container-image-name` for web apps (not deprecated `--docker-custom-image-name`)

### NuGet Configuration
- Use `dotnet nuget update source` to inject credentials into existing named sources from `nuget.config`
- Do **not** remove and re-add sources
- Always include `--store-password-in-clear-text` (required for ADO token auth)

### Runners
- All workflows: `ubuntu-latest`
- Only use `windows-latest` if the project has a Windows-specific dependency (rare)

### Required GitHub Environments
Create environments in repository settings before workflows can run. Each needs federated credentials on the managed identity for the OIDC subject matching that environment:
- `dev` (if applicable)
- `test`
- `prod` (if applicable)
- `prod-rollback` (if applicable)

---

See [references/EXAMPLE.md](references/EXAMPLE.md) for detailed before/after examples.
