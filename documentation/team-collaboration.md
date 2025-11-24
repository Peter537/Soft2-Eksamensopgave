# Team Collaboration

## Formål

At have høj kvalitet gennem en enkel og disciplinerede proces, der kombinerer Scrum's ceremonier med Kanban's flow-styring og transparens.

## Ceremonier og struktur

- **Daily Stand-ups** (max 15 min): Møde hver morgen for status og koordinering.
- **Weekly Planning** (max 2 timer): Planlægning af ugens arbejde og prioritering af backlog items.
- **Weekly Review og Retrospective** (max 30 min): Gennemgang af færdigt arbejde med demo til interessenter.
- **Weekly Retrospective** (max 30 min): Intern evaluering af teamets processer og samarbejde.
- **Kanban board**: GitHub Projects bruges som fælles board for issues, auto-flow fra "Backlog" til "Done".

## Flow-regler for Kanban

- **Work-In-Progress-limit**: Maksimalt 1 ny "In Progress" issue pr. udvikler.
- **Pull-princippet**: Man trækker næste højeste prioritet fra Backlog.
- **Definition of Ready (DoR)**: Klart formål, acceptance criteria, estimeret, labels sat.
- **Definition of Done (DoD)**: Grøn CI, tests tilføjet/opdateret, kode reviewet, sikkerheds-checks OK, dokumentation opdateret, issue lukket og flyttet til Done.

## Arbejdsgang for et backlog item

1. **Flyt issue** fra Backlog til In Progress.
2. **Opret branch** som hedder `feature/<issueid>-<kort-issue-navn>`.
3. **Udvikling** med små commits, tests løbende.
4. **Opret PR** med mindst 1 reviewer, og backlog item rykkkes automatisk til Review.
5. **Review og godkendelse** af PR fra reviewer.
6. **Issue assignee merger** til main, sletter branch, issue og flyttes automatisk Done.

Se [version-control.md](version-control.md#issue-management) for detaljer om issue management.
