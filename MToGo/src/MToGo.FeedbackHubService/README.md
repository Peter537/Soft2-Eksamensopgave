# MToGo Feedback Hub Service

Microservice ansvarlig for indsamling og administration af kundeanmeldelser og feedback.

## Formål

Feedback Hub Service håndterer alle feedback-relaterede operationer:

- **Ordreanmeldelser**: Indsaml anmeldelser for gennemførte ordrer
- **Agentanmeldelser**: Kundefeedback på leveringsagenter
- **Partneranmeldelser**: Kundefeedback på restauranter
- **Ratingaggregering**: Beregn gennemsnitlige ratings

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL

## API Endpoints

| Metode | Endpoint                                    | Beskrivelse                  |
| ------ | ------------------------------------------- | ---------------------------- |
| POST   | `/api/v1/feedback-hub/reviews`              | Indsend ny anmeldelse        |
| GET    | `/api/v1/feedback-hub/reviews`              | List alle anmeldelser        |
| GET    | `/api/v1/feedback-hub/reviews/{id}`         | Hent specifik anmeldelse     |
| GET    | `/api/v1/feedback-hub/reviews/order/{id}`   | Hent anmeldelser for ordre   |
| GET    | `/api/v1/feedback-hub/reviews/agent/{id}`   | Hent anmeldelser for agent   |
| GET    | `/api/v1/feedback-hub/reviews/partner/{id}` | Hent anmeldelser for partner |
| DELETE | `/api/v1/feedback-hub/reviews/{id}`         | Slet anmeldelse              |

## Feedbacktyper

- **Ordrefeedback**: Samlet ordreoplevelse
- **Agentfeedback**: Leveringsoplevelse og agentadfærd
- **Partnerfeedback**: Madkvalitet og restaurantservice
