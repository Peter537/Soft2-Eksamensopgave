# MToGo

MToGo er en madleveringsplatform bygget med en microservices-arkitektur ved brug af .NET 8.

## Projektstruktur

| Mappe                                                  | Beskrivelse                         |
| ------------------------------------------------------ | ----------------------------------- |
| [src/](./src/)                                         | Kildekode for alle services         |
| [tests/](./tests/)                                     | Tests                               |
| [Directory.Packages.props](./Directory.Packages.props) | Centraliserede NuGet-pakkeversioner |
| [MToGo.sln](./MToGo.sln)                               | Visual Studio solution fil          |

## Arkitektur

Platformen består af flere microservices der kommunikerer via:

- **REST APIs** gennem en central Gateway (YARP)
- **WebSockets** til realtidsopdateringer
- **Kafka** til event-drevet kommunikation
