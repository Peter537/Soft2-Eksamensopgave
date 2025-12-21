# Statisk kodeanalyse (StyleCop)

Vi har brugt **StyleCop Analyzers** (via .NET analyzere under build) til at finde advarsler og potentielle kvalitetsproblemer i koden.
Konfigurationen ligger i [MToGo/stylecop.json](../../MToGo/stylecop.json), som styrer hvilke StyleCop-regler der gælder (fx dokumentationsregler).

## Sådan kørte vi analysen

For at sikre at analyzere (inkl. StyleCop) bliver kørt under build, kørte vi følgende kommando fra repo-root i PowerShell:

```powershell
dotnet build .\MToGo\ --no-restore /p:RunAnalyzersDuringBuild=true > static-analysis-report.txt 2>&1
```

- `--no-restore`: vi undgår restore på hver kørsel (forudsætter at dependencies allerede er restored).
- `/p:RunAnalyzersDuringBuild=true`: tvinger analyzere til at blive kørt som del af build.
- `> static-analysis-report.txt 2>&1`: gemmer både standard output og fejloutput i én fil, så resultatet kan dokumenteres.

## Resultat og udbedring

Før rettelser indeholdt analysen advarsler (bl.a. nullability, ubrugte felter og obsolet API-brug). Dette output er gemt i:

- [resources/static-code-analysis/static-analysis-report-errors.txt](resources/static-code-analysis/static-analysis-report-errors.txt)

Herefter rettede vi de rapporterede problemer i kodebasen. Det endelige output er gemt i:

- [resources/static-code-analysis/static-analysis-report-fixed.txt](resources/static-code-analysis/static-analysis-report-fixed.txt)

Efter rettelser rapporterer analyzeren `0 Warning(s)` og `0 Error(s)`.

## CI/CD kvalitetssikring

For at sikre at statisk analyse også håndhæves i vores CI/CD, har vi tilføjet et separat job i pipeline-filen [cicd.yml](../../.github/workflows/cicd.yml), som kører build med analyzere aktiveret.

- Hvis analyzere rapporterer **errors**, fejler jobbet og dermed workflowet.
- Hvis der kun er **warnings**, fortsætter pipeline (warnings behandles ikke som errors).

Derudover er job'et **Save build outputs** sat til at afhænge af StyleCop/analysis-jobbet, så vores security-workflow (se [security.yml](../../.github/workflows/security.yml)) ikke går videre, før den statiske analyse er kørt og bestået.
