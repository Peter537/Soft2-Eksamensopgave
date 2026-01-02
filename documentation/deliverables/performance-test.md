# MToGo Performance Tests

Performance-test ved hjælp af **NBomber** som er et .NET load testing-bibliotek.

## Hurtig start

```bash
# Kør kommandoerne fra repository root

# 1. Start hele systemet (alle services + gateway)
docker compose up -d --build

# 2. Vent på at services bliver sunde (healthy)
# Ex. tjek http://localhost:8080/health

# 3. Kør performance-tests
cd ./MToGo/tests/MToGo.Performance.Tests
dotnet test -e RUN_PERFORMANCE_TESTS=true
```

**Bemærk**: Testene rammer rigtige API-endpoints, så hele MToGo-systemet skal være oppe at køre. Testene springes over som standard, så et normalt `dotnet test` kører dem ikke.

## Testtyper

| Test          | Hvad den gør                      | Belastning                                  | Endpoint                       |
| ------------- | --------------------------------- | ------------------------------------------- | ------------------------------ |
| **Smoke**     | Tjekker at systemet er i live     | 1 req/s i 10s                               | `/health`                      |
| **Load**      | Normal læsetrafik ved spidsbelast | ~6 req/s, 1 min ramp-up + 5 min steady      | `/api/v1/orders/customer/{id}` |
| **Stress**    | Finder bristepunkt for læsning    | ~6 -> ~12 -> ~30 req/s, 30s på hvert niveau | `/api/v1/orders/customer/{id}` |
| **OrderFlow** | Opretter ordrer (med auth)        | ~3 req/s, 30s warm-up + 5 min steady        | `/api/v1/orders/order`         |

## Realistisk trafik-simulering

Alle tests bruger **randomiserede brugere og data** for at komme tættere på realistisk brug:

- **Forudgenererede JWT-tokens** for en stor pulje af kunde-ID'er (1-1000), genbrugt på tværs af requests for at efterligne rigtige klienter, der beholder et session-token
- **Tilfældig kundevælgning** fra puljen for at sprede load på mange brugere og reducere cache-bias
- **Varierede ordre-payloads** med tilfældige varer, antal og priser
- **Forskellige adresser/gebyrer** for hver request

Opsætningen balancerer realisme (token-genbrug, gentagne brugere) med variation (mange forskellige kunder og ordrer).

## Trafikberegning (simpel køteori)

Vi beregner test-load ud fra forretningskrav:

```
Business target: 18.000.000 orders/year
Daily average:   18.000.000 / 365 = 49.315 orders/day
Peak hours:      7 hours (lunch + dinner)
Peak multiplier: 3x

Peak RPS = (49.315 × 3) / (7 × 3.600) ≈ 6 requests/second
```

### Nøglebegreber

**Utilization** = Arrival Rate / Service Rate

```
0-50%   -> Systemet er afslappet
50-70%  -> Normal drift
70-85%  -> Høj belastning
85%+    -> Fare! Svartider eksploderer
```

Derfor tester vi ved 6 req/s – det er vores beregnede spidsbelastning.

## Kørsel af tests

```bash
# Normal testkørsel (springer performance-tests over)
dotnet test

# Kør performance-tests
dotnet test -e RUN_PERFORMANCE_TESTS=true

# Kør en specifik test
dotnet test -e RUN_PERFORMANCE_TESTS=true --filter "SmokeTest"
```

## Rapporter

NBomber genererer HTML-rapporter i mappen `bin/Debug/net8.0/reports/`:

- `smoke/` - Basal connectivity-check
- `load/` - Performance for læse-API under normal load
- `stress/` - Performance for læse-API under stress (finder bristepunkter)
- `order-flow/` - Performance for skrive-API (ordreoprettelse)

Åbn `index.html` i en hvilken som helst mappe for at se grafer og statistik. Rapporterne viser realistiske performance-data fra faktiske API-kald med randomiserede trafikmønstre.

## Eksempelrapporter

Da mappen `bin/Debug/net8.0/reports/` genereres ved runtime og ikke er committed til Git, er her eksempler på de rapporter, som vores performance-tests genererer:

### Rapportstruktur

[resources/performance-test/reports/](./resources/performance-test/reports/) # (Kopieret fra bin/Debug/net8.0/reports/)  
├── load/  
│ ├── [index.html](./resources/performance-test/reports/load/index.html) # Interaktivt dashboard  
│ ├── [statistics.csv](./resources/performance-test/reports/load/statistics.csv) # Rå performance-data  
│ ├── [statistics.md](./resources/performance-test/reports/load/statistics.md) # Markdown-opsummering  
│ ├── [statistics.txt](./resources/performance-test/reports/load/statistics.txt) # Tekst-opsummering  
│ └── [nbomber-load-test-log.txt](./resources/performance-test/reports/load/nbomber-load-test-log.txt)  
├── order-flow/  
│ ├── [index.html](./resources/performance-test/reports/order-flow/index.html)  
│ ├── [statistics.csv](./resources/performance-test/reports/order-flow/statistics.csv)  
│ ├── [statistics.md](./resources/performance-test/reports/order-flow/statistics.md)  
│ ├── [statistics.txt](./resources/performance-test/reports/order-flow/statistics.txt)  
│ └── [nbomber-order-flow-test-log.txt](./resources/performance-test/reports/order-flow/nbomber-order-flow-test-log.txt)  
├── smoke/  
│ ├── [index.html](./resources/performance-test/reports/smoke/index.html)  
│ ├── [statistics.csv](./resources/performance-test/reports/smoke/statistics.csv)  
│ ├── [statistics.md](./resources/performance-test/reports/smoke/statistics.md)  
│ ├── [statistics.txt](./resources/performance-test/reports/smoke/statistics.txt)  
│ └── [nbomber-smoke-test-log.txt](./resources/performance-test/reports/smoke/nbomber-smoke-test-log.txt)  
├── stress/  
│ ├── [index.html](./resources/performance-test/reports/stress/index.html)  
│ ├── [statistics.csv](./resources/performance-test/reports/stress/statistics.csv)  
│ ├── [statistics.md](./resources/performance-test/reports/stress/statistics.md)  
│ ├── [statistics.txt](./resources/performance-test/reports/stress/statistics.txt)  
│ └── [nbomber-stress-test-log.txt](./resources/performance-test/reports/stress/nbomber-stress-test-log.txt)

#### Load .html-eksempel

![Load Test Report Example](./resources/performance-test/reports/load/image.png)

#### Order Flow .html-eksempel

![Order Flow Test Report Example](./resources/performance-test/reports/order-flow/image.png)

#### Smoke .html-eksempel

![Smoke Test Report Example](./resources/performance-test/reports/smoke/image.png)

#### Stress .html-eksempel

![Stress Test Report Example](./resources/performance-test/reports/stress/image.png)
