# Service Fee, Delivery Fee og Bonus Model

## Serviceafgift

Serviceafgiften er en procentdel af ordreværdien på en glidende skala og rundes ned til 2 decimaler. Denne afgift går til MToGo.

C#:

```csharp
public static decimal ServiceFee(decimal orderValue) => Math.Floor(orderValue * (Math.Max(3m, 6m - (orderValue / 1000m) * 3m) / 100m) * 100m) / 100m;
```

Regler:

- Ordre < 101 DKK => 6%
- Ordre 101-1.000 DKK => glidende fra 6% til 3%
- Ordre > 1.000 DKK => 3%

## Fordeling af leveringsafgift

- Agent: 75%
- MToGo: 10%
- Bonuspulje: 15% (akkumuleres og udbetales månedligt)

## Afgift eksempler

Antag deliveryFee = 35 DKK

- Ordre 89 DKK

  - Agent: 35 \* 0.75 = 26,25 DKK
  - MToGo: 35 \* 0.10 = 3,50 DKK
  - Bonuspulje: 35 \* 0.15 = 5,25 DKK
  - Kommission: 89 \* 0,06 = 5,34 DKK
  - MToGo samlet: 5,34 + 3,50 = 8,84 DKK

- Ordre 210 DKK

  - Agent: 26,25 DKK
  - MToGo: 3,50 DKK
  - Bonuspulje: 5,25 DKK
  - Kommission: 210 \* 0,0537 ≈ 11,28 DKK
  - MToGo samlet: 11,28 + 3,50 = 14,78 DKK

## Bonuspulje

### Regler og kvalifikation

- Puljen = sum af 15% fra alle leveringsafgifter i måneden.
- Minimum 20 leverancer pr. agent pr. måned for at kvalificere.
- Udbetales månedligt den første hverdag. Resterende penge går til MToGo.
- Fordeling baseres på objektive KPI'er: arbejdstimer, tidlige/sene vagter, antal leverancer, ordre‑værdi og kundevurderinger.
- Gennemsnitlig rating er 3.0 hvis man har mindre end 5 ratings.

### Udregning

1. Indsamling pr. agent: contribution = sum(deliveryFee \* 0.15), hours, early/late hours, avg_rating.
2. Del-score (normaliseret 0–1): TimeScore, ReviewScore.
3. Performance = vægtet sum (w_time = 0.5, w_review = 0.5).
4. Earned = contribution \* Performance.

C#-skitse:

```csharp
decimal contribution = orders.Sum(o => o.DeliveryFee * 0.15m);
decimal weightedHours = earlyHours*1.2m + lateHours*1.2m + (hoursTotal - earlyHours - lateHours)*1.0m;
decimal timeScore = hoursTotal > 0 ? weightedHours / (hoursTotal * 1.2m) : 0m;
decimal reviewScore = avgRating / 5m;
decimal performance = 0.5m*timeScore + 0.5m*reviewScore;
decimal earned = Math.Round(contribution * performance, 2, MidpointRounding.ToZero);
```
