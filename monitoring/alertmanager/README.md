# Alertmanager

Alertmanager håndterer routing og afsendelse af alerts fra Prometheus til Discord.

## Formål

Alertmanager modtager alerts fra Prometheus og:

- **Grupperer**: Samler relaterede alerts for at reducere støj
- **Router**: Sender alerts til de rigtige modtagere baseret på severity
- **Deduplicerer**: Forhindrer gentagne notifikationer
- **Silencer**: Mulighed for at mute alerts midlertidigt

## Filer

| Fil                                                      | Beskrivelse                                           |
| -------------------------------------------------------- | ----------------------------------------------------- |
| [alertmanager.yml.template](./alertmanager.yml.template) | Konfigurationsskabelon med miljøvariable-placeholders |
| [entrypoint.sh](./entrypoint.sh)                         | Script der substituerer miljøvariabler ved opstart    |

## Konfiguration

### Miljøvariabler

| Variabel                | Beskrivelse                    |
| ----------------------- | ------------------------------ |
| `DISCORD_WEBHOOK_ALERT` | Discord webhook URL til alerts |

### Routing

Alerts routes baseret på severity:

| Severity   | Opførsel                              |
| ---------- | ------------------------------------- |
| `critical` | Hurtig gruppe (5s wait, 15m repeat)   |
| `info`     | Langsom gruppe (5m wait, 24h repeat)  |
| Default    | Standard gruppe (30s wait, 4h repeat) |

### Inhibit Rules

Følgende regler forhindrer redundante alerts:

- `NoActivePartners` inhiberer `LowActivePartners`
- `NoActiveAgents` inhiberer `LowActiveAgents`

## Discord Integration

Alerts sendes til Discord med formaterede beskeder:

- 🔔 **Firing**: Alert er aktiv
- ✅ **Resolved**: Alert er løst
