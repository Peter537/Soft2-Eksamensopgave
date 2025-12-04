# MToGo Agent Bonus Service

Microservice ansvarlig for beregning og administration af agentpræstationsbonusser.

## Formål

Agent Bonus Service håndterer bonusberegninger for leveringsagenter:

- **Præstationssporing**: Spor leveringer, timing og kundeanmeldelser
- **Bonusberegning**: Beregn månedlige bonusser baseret på præstationsmetrikker
- **Bonushistorik**: Se historiske bonusdata

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Integration**: Gateway API til agent- og ordredata

## Bonuskriterier

Bonusser beregnes baseret på:

- **Antal Leveringer**: Totale gennemførte leveringer i perioden
- **Til-Tids Præstation**: Procentdel af leveringer gennemført til tiden
- **Kundeanmeldelser**: Gennemsnitlig rating fra kundefeedback

## API Endpoints

| Metode | Endpoint                                | Beskrivelse                 |
| ------ | --------------------------------------- | --------------------------- |
| GET    | `/api/v1/agent-bonus/{agentId}`         | Hent agentbonusinfo         |
| GET    | `/api/v1/agent-bonus/{agentId}/preview` | Forhåndsvis potentiel bonus |
