# GitHub Actions Workflows

Denne mappe indeholder GitHub Actions workflows til CI/CD og sikkerhedsscanning.

## Workflows

| Workflow                                 | Beskrivelse                                     |
| ---------------------------------------- | ----------------------------------------------- |
| [cicd.yml](./cicd.yml)                   | Hoved CI/CD pipeline til build, test og deploy  |
| [labeler.yml](./labeler.yml)             | Automatisk PR-labeling baseret på ændrede filer |
| [security.yml](./security.yml)           | Sikkerhedsscanning ved pull requests            |
| [weekly_codeql.yml](./weekly_codeql.yml) | Ugentlig CodeQL-analyse for sårbarheder         |
| [weekly_trivy.yml](./weekly_trivy.yml)   | Ugentlig Trivy container sikkerhedsscanning     |
