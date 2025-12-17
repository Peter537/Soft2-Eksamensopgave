# GitHub Actions Workflows

Denne mappe indeholder GitHub Actions workflows til CI/CD, deploy og sikkerhedsscanning.

## Workflows

| Workflow                                           | Beskrivelse                                     |
| -------------------------------------------------- | ----------------------------------------------- |
| [cicd.yml](./cicd.yml)                             | Hoved CI/CD pipeline til build, test og deploy  |
| [azure_deploy.yml](./azure_deploy.yml)             | Manuel deploy til Azure (Terraform plan/apply)  |
| [azure_destroy.yml](./azure_destroy.yml)           | Manuel destroy af Azure infrastruktur           |
| [azure_staging_test.yml](./azure_staging_test.yml) | Deploy staging + kør tests + destroy igen       |
| [labeler.yml](./labeler.yml)                       | Automatisk PR-labeling baseret på ændrede filer |
| [security.yml](./security.yml)                     | Sikkerhedsscanning ved pull requests            |
| [weekly_codeql.yml](./weekly_codeql.yml)           | Ugentlig CodeQL-analyse for sårbarheder         |
| [weekly_trivy.yml](./weekly_trivy.yml)             | Ugentlig Trivy container sikkerhedsscanning     |
