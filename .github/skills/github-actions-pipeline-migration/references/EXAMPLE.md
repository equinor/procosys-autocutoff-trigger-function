# GitHub Actions Pipeline Migration - Examples

## Example 1: Function App with test + prod environments (no dev), Docker

This example shows a complete migration for a Docker-based Azure Function App with `test` and `prod` environments. No `dev` environment exists, so `deploy-test.yml` handles both PR and main branch deployments.

### Before: Azure DevOps (`pipelines/azure-pipelines.yml`)

```yaml
trigger:
  branches:
    include:
      - master

variables:
  - template: variables/global-variables.yml
  - template: variables/app-variables.yml

stages:
  - stage: CI
    jobs:
      - template: jobs/build-and-test.yml

  - stage: common
    dependsOn: CI
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/master'))
    jobs:
      - template: jobs/dockerbuild.yml
        parameters:
          repository: myapp/my-function

  - stage: test
    dependsOn: common
    jobs:
      - template: jobs/deploy-function.yml
        parameters:
          functionApp: pcs-myapp-test-func
          rgName: pcs-myapp-non-prod-rg
          keyVaultUrl: pcs-myapp-non-prod-kv
          env: test

  - stage: prod
    dependsOn: common
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/master'))
    jobs:
      - template: jobs/deploy-function.yml
        parameters:
          functionApp: pcs-myapp-prod-func
          rgName: pcs-myapp-prod-rg
          keyVaultUrl: pcs-myapp-prod-kv
          env: prod
```

### After: `.github/workflows/deploy-test.yml`

```yaml
name: 🚀🧪 Deploy to test

on:
  workflow_dispatch:
  pull_request:

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    name: Deploy to test
    runs-on: ubuntu-latest
    environment: test
    steps:
      - uses: actions/checkout@v4

      - name: Get short SHA
        id: sha
        run: echo "short=$(git rev-parse --short HEAD)" >> $GITHUB_OUTPUT

      - name: Login to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - name: Get DevOps token for NuGet
        id: devops-token
        shell: bash
        run: echo "token=$(az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)" >> $GITHUB_OUTPUT

      - name: Login to ACR
        run: az acr login --name procosys

      - name: Build Docker image
        run: |
          docker build \
            --build-arg FEED_ACCESSTOKEN=${{ steps.devops-token.outputs.token }} \
            -t procosys.azurecr.io/myapp/my-function:${{ steps.sha.outputs.short }} \
            -f MyFunction/Dockerfile \
            .

      - name: Push Docker image
        run: docker push procosys.azurecr.io/myapp/my-function:${{ steps.sha.outputs.short }}

      - name: Set container image
        run: |
          az functionapp config container set \
            --resource-group pcs-myapp-non-prod-rg \
            --name pcs-myapp-test-func \
            --registry-server https://procosys.azurecr.io \
            --image procosys.azurecr.io/myapp/my-function:${{ steps.sha.outputs.short }}
```

### After: `.github/workflows/deploy-prod.yml`

```yaml
name: 🚀🏭 Deploy to prod

on:
  workflow_dispatch:
  push:
    branches:
      - main

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    name: Deploy to prod
    runs-on: ubuntu-latest
    environment: prod
    steps:
      - uses: actions/checkout@v4

      - name: Get short SHA
        id: sha
        run: echo "short=$(git rev-parse --short HEAD)" >> $GITHUB_OUTPUT

      - name: Login to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - name: Get DevOps token for NuGet
        id: devops-token
        shell: bash
        run: echo "token=$(az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)" >> $GITHUB_OUTPUT

      - name: Login to ACR
        run: az acr login --name procosys

      - name: Build Docker image
        run: |
          docker build \
            --build-arg FEED_ACCESSTOKEN=${{ steps.devops-token.outputs.token }} \
            -t procosys.azurecr.io/myapp/my-function:${{ steps.sha.outputs.short }} \
            -f MyFunction/Dockerfile \
            .

      - name: Push Docker image
        run: docker push procosys.azurecr.io/myapp/my-function:${{ steps.sha.outputs.short }}

      - name: Set container image
        run: |
          az functionapp config container set \
            --resource-group pcs-myapp-prod-rg \
            --name pcs-myapp-prod-func \
            --registry-server https://procosys.azurecr.io \
            --image procosys.azurecr.io/myapp/my-function:${{ steps.sha.outputs.short }}
```

**Key Changes from Azure DevOps:**
- Docker build/push and deploy merged into a single job per environment (no separate `common` stage)
- Image tagged with short git SHA instead of `$(Build.BuildId)`
- OIDC authentication replaces service connection credentials
- `--docker-registry-server-url` / `--docker-custom-image-name` replaced with `--registry-server` / `--image`
- App settings step omitted — managed separately outside the pipeline

---

## Example 2: Web App with dev environment, Docker

This example shows a Web App (App Service) with a `dev` environment (PR-triggered) and a `test` environment (merge-triggered). Because `dev` exists, `deploy-test.yml` does NOT have a `pull_request` trigger. `deploy-prod.yml` and `deploy-prod-rollback.yml` follow the same structure (see Examples 1 and 3), substituting the prod resource group and app name.

### `.github/workflows/deploy-dev.yml`

```yaml
name: 🚀🔬 Deploy to dev

on:
  workflow_dispatch:
  pull_request:

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    name: Deploy to dev
    runs-on: ubuntu-latest
    environment: dev
    steps:
      - uses: actions/checkout@v4

      - name: Get short SHA
        id: sha
        run: echo "short=$(git rev-parse --short HEAD)" >> $GITHUB_OUTPUT

      - name: Login to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - name: Get DevOps token for NuGet
        id: devops-token
        shell: bash
        run: echo "token=$(az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)" >> $GITHUB_OUTPUT

      - name: Login to ACR
        run: az acr login --name procosys

      - name: Build Docker image
        run: |
          docker build \
            --build-arg FEED_ACCESSTOKEN=${{ steps.devops-token.outputs.token }} \
            -t procosys.azurecr.io/myapp/my-webapp:${{ steps.sha.outputs.short }} \
            -f MyWebApp/Dockerfile \
            .

      - name: Push Docker image
        run: docker push procosys.azurecr.io/myapp/my-webapp:${{ steps.sha.outputs.short }}

      - name: Set container image
        run: |
          az webapp config container set \
            --resource-group pcs-myapp-non-prod-rg \
            --name pcs-myapp-dev \
            --registry-server https://procosys.azurecr.io \
            --container-image-name procosys.azurecr.io/myapp/my-webapp:${{ steps.sha.outputs.short }}
```

### `.github/workflows/deploy-test.yml` (dev environment present — push to main only)

```yaml
name: 🚀🧪 Deploy to test

on:
  workflow_dispatch:
  push:
    branches:
      - main

# ... same steps as deploy-dev.yml, with test resource group and app name

      - name: Set container image
        run: |
          az webapp config container set \
            --resource-group pcs-myapp-non-prod-rg \
            --name pcs-myapp-test \
            --registry-server https://procosys.azurecr.io \
            --container-image-name procosys.azurecr.io/myapp/my-webapp:${{ steps.sha.outputs.short }}
```

**Key differences vs Function App:**
- `az webapp config container set` instead of `az functionapp config container set`
- `--container-image-name` instead of `--image`

---

## Example 3: Prod rollback, Docker

```yaml
name: 🚀🏭 Deploy rollback to prod

on:
  workflow_dispatch:
    inputs:
      ref:
        description: 'Branch name, commit SHA, or relative ref (e.g., HEAD~1 AKA previous commit)'
        required: true
        default: 'HEAD~1'

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    name: Rollback prod
    runs-on: ubuntu-latest
    environment: prod-rollback
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
          ref: ${{ github.event.inputs.ref }}

      - name: Get commit SHA
        id: commit
        run: echo "sha=$(git rev-parse --short HEAD)" >> $GITHUB_OUTPUT

      - name: Login to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - name: Get DevOps token for NuGet
        id: devops-token
        shell: bash
        run: echo "token=$(az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)" >> $GITHUB_OUTPUT

      - name: Login to ACR
        run: az acr login --name procosys

      - name: Build Docker image
        run: |
          docker build \
            --build-arg FEED_ACCESSTOKEN=${{ steps.devops-token.outputs.token }} \
            -t procosys.azurecr.io/myapp/my-function:${{ steps.commit.outputs.sha }} \
            -f MyFunction/Dockerfile \
            .

      - name: Push Docker image
        run: docker push procosys.azurecr.io/myapp/my-function:${{ steps.commit.outputs.sha }}

      - name: Set container image
        run: |
          az functionapp config container set \
            --resource-group pcs-myapp-prod-rg \
            --name pcs-myapp-prod-func \
            --registry-server https://procosys.azurecr.io \
            --image procosys.azurecr.io/myapp/my-function:${{ steps.commit.outputs.sha }}
```

**Key differences from `deploy-prod.yml`:**
- Takes a `ref` input instead of triggering on push
- Checks out the specified ref with `fetch-depth: 0`
- Uses `steps.commit.outputs.sha` (resolved from the checked-out ref) — not `steps.sha.outputs.short` which would resolve to the workflow trigger commit
- Uses `environment: prod-rollback` (separate environment for approval gates and audit trail)
- Does **not** re-apply app settings — only updates the container image

---

## Example 4: NuGet config with two private feeds

```yaml
# .github/actions/nuget-config/action.yml
    - name: Configure NuGet sources
      shell: bash
      run: |
        dotnet nuget update source ProCoSysOfficial \
            --username statoildeveloper \
            --password ${{ steps.get-devops-token.outputs.token }} \
            --store-password-in-clear-text

        dotnet nuget update source TiePublic \
            --username equinor-ti \
            --password ${{ steps.get-devops-token.outputs.token }} \
            --store-password-in-clear-text
```

If the repository has only one private feed, include only one block. Feed names must match the `<add key="...">` names in `nuget.config` exactly.

---

## Example 5: Function App with zip deployment (no Docker)

This example shows a Function App deployed via zip package. There is no Docker image, no ACR, and no short SHA step. NuGet is configured inline after the Azure login so credentials are available before `dotnet publish`.

### `.github/workflows/deploy-test.yml`

```yaml
name: 🚀🧪 Deploy to test

on:
  workflow_dispatch:
  pull_request:

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    name: Deploy to test
    runs-on: ubuntu-latest
    environment: test
    steps:
      - uses: actions/checkout@v4

      - name: Login to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - name: Get DevOps token for NuGet
        id: devops-token
        shell: bash
        run: echo "token=$(az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)" >> $GITHUB_OUTPUT

      - name: Configure NuGet sources
        shell: bash
        run: |
          dotnet nuget update source ProCoSysOfficial \
            --username statoildeveloper \
            --password ${{ steps.devops-token.outputs.token }} \
            --store-password-in-clear-text

      - name: Build and publish
        run: dotnet publish MyFunction/MyFunction.csproj --configuration Release --output ./publish

      - name: Deploy zip
        run: |
          cd publish && zip -r ../deploy.zip .
          az functionapp deployment source config-zip \
            --resource-group pcs-myapp-non-prod-rg \
            --name pcs-myapp-test-func \
            --src ../deploy.zip
```

**Key Changes from Docker deployment:**
- No ACR login, Docker build, or image push steps
- No short SHA step — artifact is ephemeral (built and deployed inline)
- `dotnet publish` replaces `docker build`
- NuGet configured inline after Azure login (needed before `dotnet publish`)
- `az functionapp deployment source config-zip` replaces `az functionapp config container set`
- For Web App: `az webapp deploy --src-path deploy.zip --type zip`
