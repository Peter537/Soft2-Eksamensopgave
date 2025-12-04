# MToGo Website

Blazor Server frontend-applikation der leverer brugerfladen til kunder, partnere, agenter og administration.

## Formål

Websitet er en Blazor Server-applikation der betjener alle brugerflader for MToGo-platformen:

- **Kunde UI**: Gennemse restauranter, afgiv ordrer, spor leveringer
- **Partner UI**: Administrer menu, accepter/afvis ordrer
- **Agent UI**: Se tilgængelige ordrer, opdater leveringsstatus
- **Management UI**: Admin-dashboard, opret konti, se analyser

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0 Blazor Server
- **UI**: Bootstrap 5, Bootstrap Icons
- **Lokalisering**: Flersproget support (Engelsk, Dansk)
- **Realtid**: SignalR til WebSocket-forbindelser

## Funktioner

- Rollebaseret navigation og adgangskontrol
- Realtids ordre-statusopdateringer via WebSockets
- Responsivt design til mobil og desktop
- Internationalisering (i18n) support

## Kørsel

```bash
dotnet run
```

**Port**: `8081`
