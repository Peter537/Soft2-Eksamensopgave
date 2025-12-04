# Version Control

Dette dokument beskriver vores version control praksis, så teamet kan arbejde effektivt, sikkert og med høj kvalitet.

## Branching Strategy

Vi anvender **GitHub Flow** med en altid-deployable **main**. Strategien understøtter hyppig integration og hurtig feedback.

**Branches**

- `main`: Altid release‑ready. Beskyttet med branch protection (kræver Pull Request, min. 1 reviewer, grønne status checks). Ingen direkte pushes.
- `feature/<issueid>-<kort-issue-navn>`: Kortlevende branches (1–3 dage) for nyt arbejde.
- `hotfix/<issueid>-<kort-issue-navn>`: Akutte rettelser. Laves fra main, merges direkte tilbage efter godkendt PR og grøn CI.

## Branch Protection Rules

For `main` gælder følgende:

- Require PR med min. 1 reviewer.
- Status checks required: build, test og security skal være grønne.
- Require up‑to‑date branch før merge.

## Issue Management

Vi styrer arbejde via GitHub Issues og GitHub Projects.

**Templates**:

- Pull Request: [pull_request_template.md](../.github/PULL_REQUEST_TEMPLATE/pull_request_template.md)
- Issue: [feature_request.yml](../.github/ISSUE_TEMPLATE/feature_request.yml)
- Bug Report: [bug_report.yml](../.github/ISSUE_TEMPLATE/bug_report.yml)

**Labels**:

- **Auto labels**: bug, documentation, feature, refactor.

**Fields**:

- **Automatically**: start date når rykket til In Progress; done date når rykket til Done.
- **Manually**: priority (High, Medium, Low); size (XL, L, M, S, XS).

## Commit Guidelines

Vi følger Conventional Commits for læsbar historik og auto‑changelogs.

### Format

type(scope): short description

### Types

- docs, feat, fix, refactor, test, devops

Eksempler:

- feat(order): add fee calculation logic
- fix(payment): handle failed callback
- test(api): add authZ boundary tests

Skriv små, meningsfulde commits; undgå "work-in-progress" og "update".
