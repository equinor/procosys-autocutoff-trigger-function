# File Templates Reference

Complete YAML templates for all GitHub Actions workflow files and composite actions. Replace `<placeholder>` values with repository-specific details.

---

## `.github/actions/get-devops-token/action.yml`

```yaml
name: 🔑 Get DevOps token
description: 'Login to Azure and obtain a token for DevOps'
inputs:
  client-id:
    description: 'SP client id'
    required: true
  tenant-id:
    description: 'Equinor tenant id'
    required: true

outputs:
  token:
    description: 'DevOps token'
    value: ${{ steps.token.outputs.token }}

runs:
  using: 'composite'
  steps:
    - name: 'Login to Azure'
      uses: azure/login@v2
      with:
        client-id: ${{ inputs.client-id }}
        tenant-id: ${{ inputs.tenant-id }}
        allow-no-subscriptions: true

    - name: 'Obtain token for DevOps'
      id: token
      shell: bash
      run: echo "token=$(az account get-access-token --resource $AZURE_DEVOPS_APPLICATION_ID --query accessToken -o tsv)" >> $GITHUB_OUTPUT
      env:
        AZURE_DEVOPS_APPLICATION_ID: "499b84ac-1321-427f-aa17-267ca6975798"
```

---

## `.github/actions/nuget-config/action.yml`

Uses `dotnet nuget update source` to inject credentials into the named sources already defined in `nuget.config`. Add one block per private feed. The username for `ProCoSysOfficial` is `statoildeveloper`; derive other usernames from the organization name in the feed URL.

```yaml
name: 📦 Configure NuGet
description: 'Configure NuGet feeds'
inputs:
  client-id:
    description: 'SP client id'
    required: true
  tenant-id:
    description: 'Equinor tenant id'
    required: true

runs:
  using: 'composite'
  steps:
    - name: Get DevOps token
      id: get-devops-token
      uses: ./.github/actions/get-devops-token
      with:
        client-id: ${{ inputs.client-id }}
        tenant-id: ${{ inputs.tenant-id }}

    - name: Configure NuGet sources
      shell: bash
      run: |
        dotnet nuget update source <FeedName> \
            --username <username> \
            --password ${{ steps.get-devops-token.outputs.token }} \
            --store-password-in-clear-text
        # Repeat for each additional private feed
```

---

## `.github/workflows/ci-build-and-test.yml`

```yaml
name: 🤖 Build & run tests

on:
  pull_request:

permissions:
  id-token: write
  contents: read

jobs:
  build-test:
    name: Build & run tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Configure NuGet sources
        uses: ./.github/actions/nuget-config
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}

      - name: Build solution
        run: dotnet build --configuration Release

      - name: Run tests
        run: dotnet test --configuration Release --no-build
```

---

## `.github/workflows/ci-verify-formatting.yml`

List every project from the `.sln` file in the matrix. Use `fail-fast: false` so all projects are checked independently.

```yaml
name: ✏️ Verify formatting

on:
  pull_request:

permissions:
  id-token: write
  contents: read

jobs:
  format-check:
    name: Verify formatting
    runs-on: ubuntu-latest
    strategy:
      matrix:
        project: [
          ProjectA,
          ProjectB,
          # one entry per project in the .sln
        ]
      fail-fast: false
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Configure NuGet sources
        uses: ./.github/actions/nuget-config
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}

      - name: Verify ${{ matrix.project }} formatting
        run: dotnet format ./${{ matrix.project }} --verify-no-changes
```

---

## `.github/workflows/ci-verify-pr-title.yml`

Copy this file verbatim — it is fully generic and requires no project-specific values.

```yaml
name: 🗃️ Verify PR Title

on:
  pull_request:
    types:
      - edited
      - opened
      - reopened
      - ready_for_review
      - synchronize
    branches:
      - main

jobs:
  main:
    name: Verify title
    runs-on: ubuntu-latest
    permissions:
      pull-requests: write
    steps:
      - uses: amannn/action-semantic-pull-request@v5
        id: lint_pr_title
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          types: |
            build
            chore
            ci
            docs
            feat
            fix
            perf
            refactor
            revert
            style
            test
          requireScope: false

      - uses: marocchino/sticky-pull-request-comment@v2
        if: always() && (steps.lint_pr_title.outputs.error_message != null)
        with:
          header: pr-title-lint-error
          message: |
            Hey there and thank you for opening this pull request! 👋🏼
            
            We require pull request titles to follow the [Conventional Commits specification](https://www.conventionalcommits.org/en/v1.0.0/) and it looks like your proposed title needs to be adjusted.

            Details:
            
            ```
            ${{ steps.lint_pr_title.outputs.error_message }}
            ```

      - if: ${{ steps.lint_pr_title.outputs.error_message == null }}
        uses: marocchino/sticky-pull-request-comment@v2
        with:
          header: pr-title-lint-error
          delete: true
```

---

## `.github/workflows/deploy-dev.yml` (only if `dev` environment exists)

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

      # --- DOCKER DEPLOYMENT ---
      - name: Login to ACR
        run: az acr login --name <acr-name>

      - name: Build Docker image
        run: |
          docker build \
            --build-arg FEED_ACCESSTOKEN=${{ steps.devops-token.outputs.token }} \
            -t <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }} \
            -f <path/to/Dockerfile> \
            .

      - name: Push Docker image
        run: docker push <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }}

      - name: Set container image
        run: |
          # For Azure Function App:
          az functionapp config container set \
            --resource-group <dev-resource-group> \
            --name <dev-app-name> \
            --registry-server https://<acr-name>.azurecr.io \
            --image <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }}

          # For Web App (App Service), use instead:
          # az webapp config container set \
          #   --resource-group <dev-resource-group> \
          #   --name <dev-app-name> \
          #   --registry-server https://<acr-name>.azurecr.io \
          #   --container-image-name <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }}

      # --- ZIP DEPLOYMENT (alternative to Docker) ---
      # - name: Configure NuGet sources
      #   shell: bash
      #   run: |
      #     dotnet nuget update source <FeedName> \
      #       --username <username> \
      #       --password ${{ steps.devops-token.outputs.token }} \
      #       --store-password-in-clear-text
      #
      # - name: Build and publish
      #   run: dotnet publish <ProjectName>/<ProjectName>.csproj --configuration Release --output ./publish
      #
      # - name: Deploy zip
      #   run: |
      #     cd publish && zip -r ../deploy.zip .
      #     # For Azure Function App:
      #     az functionapp deployment source config-zip \
      #       --resource-group <dev-resource-group> \
      #       --name <dev-app-name> \
      #       --src ../deploy.zip
      #     # For Web App (App Service), use instead:
      #     # az webapp deploy \
      #     #   --resource-group <dev-resource-group> \
      #     #   --name <dev-app-name> \
      #     #   --src-path ../deploy.zip \
      #     #   --type zip
```

---

## `.github/workflows/deploy-test.yml`

If no `dev` environment exists, add `pull_request:` as an additional trigger and remove `push: branches: main`. If `dev` exists, keep only `push: branches: main`.

```yaml
name: 🚀🧪 Deploy to test

on:
  workflow_dispatch:
  push:
    branches:
      - main
  # pull_request:  # Add this (and remove push trigger) only if there is no dev environment

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

      # --- DOCKER DEPLOYMENT ---
      - name: Login to ACR
        run: az acr login --name <acr-name>

      - name: Build Docker image
        run: |
          docker build \
            --build-arg FEED_ACCESSTOKEN=${{ steps.devops-token.outputs.token }} \
            -t <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }} \
            -f <path/to/Dockerfile> \
            .

      - name: Push Docker image
        run: docker push <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }}

      - name: Set container image
        run: |
          # For Azure Function App:
          az functionapp config container set \
            --resource-group <test-resource-group> \
            --name <test-app-name> \
            --registry-server https://<acr-name>.azurecr.io \
            --image <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }}

          # For Web App (App Service), use instead:
          # az webapp config container set \
          #   --resource-group <test-resource-group> \
          #   --name <test-app-name> \
          #   --registry-server https://<acr-name>.azurecr.io \
          #   --container-image-name <acr-name>.azurecr.io/<image-repo>:${{ steps.sha.outputs.short }}

      # --- ZIP DEPLOYMENT (alternative to Docker) ---
      # - name: Configure NuGet sources
      #   shell: bash
      #   run: |
      #     dotnet nuget update source <FeedName> \
      #       --username <username> \
      #       --password ${{ steps.devops-token.outputs.token }} \
      #       --store-password-in-clear-text
      #
      # - name: Build and publish
      #   run: dotnet publish <ProjectName>/<ProjectName>.csproj --configuration Release --output ./publish
      #
      # - name: Deploy zip
      #   run: |
      #     cd publish && zip -r ../deploy.zip .
      #     # For Azure Function App:
      #     az functionapp deployment source config-zip \
      #       --resource-group <test-resource-group> \
      #       --name <test-app-name> \
      #       --src ../deploy.zip
      #     # For Web App (App Service), use instead:
      #     # az webapp deploy \
      #     #   --resource-group <test-resource-group> \
      #     #   --name <test-app-name> \
      #     #   --src-path ../deploy.zip \
      #     #   --type zip
```

---

## `.github/workflows/deploy-prod.yml`

Identical structure to `deploy-test.yml`. Use `push: branches: main` + `workflow_dispatch` as triggers, `environment: prod`, and the prod resource group and app name.

---

## `.github/workflows/deploy-prod-rollback.yml`

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

      # --- DOCKER DEPLOYMENT ---
      - name: Login to ACR
        run: az acr login --name <acr-name>

      - name: Build Docker image
        run: |
          docker build \
            --build-arg FEED_ACCESSTOKEN=${{ steps.devops-token.outputs.token }} \
            -t <acr-name>.azurecr.io/<image-repo>:${{ steps.commit.outputs.sha }} \
            -f <path/to/Dockerfile> \
            .

      - name: Push Docker image
        run: docker push <acr-name>.azurecr.io/<image-repo>:${{ steps.commit.outputs.sha }}

      - name: Set container image
        run: |
          # For Azure Function App:
          az functionapp config container set \
            --resource-group <prod-resource-group> \
            --name <prod-app-name> \
            --registry-server https://<acr-name>.azurecr.io \
            --image <acr-name>.azurecr.io/<image-repo>:${{ steps.commit.outputs.sha }}

          # For Web App (App Service), use instead:
          # az webapp config container set \
          #   --resource-group <prod-resource-group> \
          #   --name <prod-app-name> \
          #   --registry-server https://<acr-name>.azurecr.io \
          #   --container-image-name <acr-name>.azurecr.io/<image-repo>:${{ steps.commit.outputs.sha }}

      # --- ZIP DEPLOYMENT (alternative to Docker) ---
      # - name: Configure NuGet sources
      #   shell: bash
      #   run: |
      #     dotnet nuget update source <FeedName> \
      #       --username <username> \
      #       --password ${{ steps.devops-token.outputs.token }} \
      #       --store-password-in-clear-text
      #
      # - name: Build and publish
      #   run: dotnet publish <ProjectName>/<ProjectName>.csproj --configuration Release --output ./publish
      #
      # - name: Deploy zip
      #   run: |
      #     cd publish && zip -r ../deploy.zip .
      #     # For Azure Function App:
      #     az functionapp deployment source config-zip \
      #       --resource-group <prod-resource-group> \
      #       --name <prod-app-name> \
      #       --src ../deploy.zip
      #     # For Web App (App Service), use instead:
      #     # az webapp deploy \
      #     #   --resource-group <prod-resource-group> \
      #     #   --name <prod-app-name> \
      #     #   --src-path ../deploy.zip \
      #     #   --type zip
```
