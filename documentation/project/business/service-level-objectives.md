# Service Level Objectives (SLO)

Dette dokument definerer Service Level Objectives (SLO'er) for MToGo food delivery platformen.

## Udvalgte SLO'er

Da vi kun har begrænset tid til dette projekt, har vi valgt at fokusere på nogle få nøgle-SLO'er for at demonstrere vores tilgang til service level management.

| SLO                              | Beskrivelse                                                                                             | Mål                  |
| -------------------------------- | ------------------------------------------------------------------------------------------------------- | -------------------- |
| **Order Service Uptime**         | Procentdel af tid systemet er tilgængeligt og fungerer korrekt                                          | 99.9%                |
| **Order Creation Latency (p95)** | Tiden det tager fra man har betalt til at kafka-topic bliver oprettet                                   | < 400 ms             |
| **Order Creation Success Rate**  | Procentdel af ordrer der oprettes uden fejl                                                             | 99.9%                |
| **Order Service Request Rate**   | Systemet skal understøtte mindst 1500 samtidige ordreanmodninger pr. minut uden performance degradation | >= 1500 requests/min |

---

## SLO Oversigt

SLOs er interne engineering targets. Disse er strengere end eksterne SLAs for at give et error budget og early warning system.

### OrderService

| Metric                           | SLO Target | Measurement Window |
| -------------------------------- | ---------- | ------------------ |
| **Uptime**                       | >= 99.5%   | 30-day rolling     |
| **Order Creation Latency (p95)** | < 400ms    | 24-hour rolling    |
| **Order Creation Success Rate**  | >= 99.8%   | 30-day rolling     |
| **Kafka Event Publish Success**  | >= 99.95%  | 30-day rolling     |

### Partner Services

#### PartnerService

| Metric                                | SLO Target | Measurement Window |
| ------------------------------------- | ---------- | ------------------ |
| **Uptime**                            | >= 99.5%   | 30-day rolling     |
| **Order Accept/Reject Latency (p95)** | < 800ms    | 24-hour rolling    |
| **API Success Rate**                  | >= 99.5%   | 30-day rolling     |

#### WebSocketPartnerService

| Metric                                | SLO Target  | Measurement Window |
| ------------------------------------- | ----------- | ------------------ |
| **WebSocket Connection Uptime**       | >= 99.5%    | 30-day rolling     |
| **Order Notification Delivery (p95)** | < 2 seconds | 24-hour rolling    |
| **Message Delivery Success**          | >= 98%      | 30-day rolling     |

### Agent Services

#### AgentService

| Metric                                | SLO Target | Measurement Window |
| ------------------------------------- | ---------- | ------------------ |
| **Uptime**                            | >= 99.5%   | 30-day rolling     |
| **Delivery Assignment Latency (p95)** | < 800ms    | 24-hour rolling    |
| **API Success Rate**                  | >= 99.5%   | 30-day rolling     |

#### WebSocketAgentService

| Metric                                 | SLO Target  | Measurement Window |
| -------------------------------------- | ----------- | ------------------ |
| **WebSocket Connection Uptime**        | >= 99.5%    | 30-day rolling     |
| **Delivery Update Notification (p95)** | < 2 seconds | 24-hour rolling    |
| **Message Delivery Success**           | >= 98%      | 30-day rolling     |

### Gateway

| Metric                     | SLO Target           | Measurement Window |
| -------------------------- | -------------------- | ------------------ |
| **Gateway Uptime**         | >= 99.9%             | 30-day rolling     |
| **Routing Overhead (p95)** | < 30ms added latency | 24-hour rolling    |
| **Gateway Success Rate**   | >= 99.95%            | 30-day rolling     |

### Notification Service

| Metric                         | SLO Target  | Measurement Window |
| ------------------------------ | ----------- | ------------------ |
| **Notification Delivery Rate** | >= 97%      | 30-day rolling     |
| **Notification Latency (p95)** | < 5 seconds | 24-hour rolling    |
| **Service Uptime**             | >= 99%      | 30-day rolling     |

### Kafka Infrastructure

| Metric                    | SLO Target | Measurement Window |
| ------------------------- | ---------- | ------------------ |
| **Broker Uptime**         | >= 99.9%   | 30-day rolling     |
| **Consumer Lag (p99)**    | < 800ms    | 24-hour rolling    |
| **Event Publish Success** | >= 99.95%  | 30-day rolling     |
