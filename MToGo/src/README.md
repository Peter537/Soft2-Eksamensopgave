# MToGo Services

Denne mappe indeholder alle microservices der udgør den nye MToGo-platform.

## Arkitektur

Alle services (undtagen Gateway og Website) er interne og tilgås gennem Gateway. Services kommunikerer via:

- **HTTP/REST**: Synkrone API-kald
- **WebSockets**: Realtids tovejskommunikation
- **Kafka**: Asynkron event-drevet messaging

## Services

| Service                                                             | Beskrivelse                         |
| ------------------------------------------------------------------- | ----------------------------------- |
| [MToGo.Gateway](./MToGo.Gateway/)                                   | API Gateway med YARP reverse proxy  |
| [MToGo.Website](./MToGo.Website/)                                   | Blazor Server frontend-applikation  |
| [MToGo.OrderService](./MToGo.OrderService/)                         | Ordreoprettelse og -administration  |
| [MToGo.CustomerService](./MToGo.CustomerService/)                   | Kundeautentificering og profiler    |
| [MToGo.PartnerService](./MToGo.PartnerService/)                     | Restaurant/partner-administration   |
| [MToGo.AgentService](./MToGo.AgentService/)                         | Leveringsagent-administration       |
| [MToGo.AgentBonusService](./MToGo.AgentBonusService/)               | Agentbonusberegninger               |
| [MToGo.ManagementService](./MToGo.ManagementService/)               | Admin-administrationsfunktionalitet |
| [MToGo.FeedbackHubService](./MToGo.FeedbackHubService/)             | Anmeldelser og feedbackindsamling   |
| [MToGo.NotificationService](./MToGo.NotificationService/)           | Notifikationshåndtering via Kafka   |
| [MToGo.WebSocketCustomerService](./MToGo.WebSocketCustomerService/) | Realtidsopdateringer til kunder     |
| [MToGo.WebSocketPartnerService](./MToGo.WebSocketPartnerService/)   | Realtidsopdateringer til partnere   |
| [MToGo.WebSocketAgentService](./MToGo.WebSocketAgentService/)       | Realtidsopdateringer til agenter    |
| [MToGo.Shared](./MToGo.Shared/)                                     | Delte modeller, DTOs og utilities   |
