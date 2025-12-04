# Architecture Documentation

Denne mappe indeholder systemarkitektur og design dokumentation.

## Dokumenter

| Dokument                                           | Beskrivelse                                 |
| -------------------------------------------------- | ------------------------------------------- |
| [system-architecture.md](./system-architecture.md) | Overordnet systemarkitektur med C4-modeller |
| [frontend-wireframe.md](./frontend-wireframe.md)   | Frontend wireframes og UI design            |

## Services

MToGo består af følgende services:

- Order Service
- Partner Service
- Customer Service
- Agent Service
- Agent Bonus Service
- Notification Service
- Feedback Hub Service
- WebSocket Services (Customer, Partner, Agent)

## Ressourcer

Følgende C4 diagrammer er tilgængelige i [/resources](./resources/)-mappen:

- `system-context-diagram.png` - System Context Diagram
- `container-diagram.png` - Container Diagram
- `*-component-diagram.png` - Component Diagrams for alle services
