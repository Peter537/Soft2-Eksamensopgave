# 🍕 Event-Driven Food Delivery System
### Bachelor Exam Demo - Microservices with Apache Kafka

---

## 📋 The Services

| Service | Port | Responsibility |
|---------|------|----------------|
| **KafkaOrderDemo** | 7066 | Blazor UI - Customer + Kitchen interface |
| **CentralHub.API** | 5288 | Order gateway - receives customer orders |
| **PartnerService** | 5220 | Restaurant operations - accept/prepare/ready |
| **NotificationService** | 5230 | Customer notifications (console logs) |
| **LocationService** | 5240 | Real-time GPS tracking simulation |
| **Apache Kafka** | 9092 | Event message broker |

---

## 🤔 The Problem: Traditional Architecture

### How it works WITHOUT Kafka (REST/Polling):

```
Customer Order → CentralHub → HTTP POST → PartnerService
                                              ↓
                                         HTTP POST → NotificationService
                                              ↓
                                         HTTP GET (polling) ← Customer checks status
```

### Problems:
- ❌ **Tight Coupling** - CentralHub needs to know URLs of PartnerService and NotificationService
- ❌ **Cascading Failures** - If NotificationService is down, PartnerService request fails
- ❌ **Polling Overhead** - Customer constantly checks "Is my order ready yet?"
- ❌ **Network Dependencies** - Services must all be online simultaneously
- ❌ **Code Modification** - Adding new features means **MODIFYING existing services**

### Adding a feature (e.g., SMS notifications):
```diff
// PartnerService - MUST MODIFY existing code
public async Task AcceptOrder(string orderId) {
    // existing code
    await httpClient.PostAsync("http://notification-service/notify", ...);
+   await httpClient.PostAsync("http://sms-service/send", ...);  // New dependency!
}
```

**You modify the network to add functionality** 🔧

---

## ✨ The Solution: Event-Driven with Kafka

### How it works WITH Kafka:

```
Customer Order → CentralHub → Publishes Event → Kafka Topic
                                                     ↓
                                        ┌────────────┼────────────┐
                                        ↓            ↓            ↓
                                  PartnerSvc   NotificationSvc  (Future: SMS)
                                  (listening)    (listening)    (listening)
```

### Benefits:
- ✅ **Loose Coupling** - Services don't know about each other
- ✅ **Resilience** - One service down doesn't break others
- ✅ **Real-time** - No polling needed, events push automatically
- ✅ **Scalability** - Add services without changing existing code
- ✅ **Code Addition** - New features = **ADD new consumers**, don't modify existing

### Adding the SAME feature (SMS notifications):
```csharp
// NEW SmsService - ZERO changes to existing code!
public class OrderAcceptedConsumer : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken ct) {
        await foreach (var msg in kafkaConsumer.ConsumeAsync("order-accepted", ct)) {
            await SendSMS(msg.Value.OrderId);  // Just listen and react!
        }
    }
}
```

**You extend the network by adding listeners** �

---

## 💡 The Shower Thought

### Modular Monolith:
> "To add a feature, you **MODIFY** existing code"

### Event-Driven:
> "To add a feature, you **ADD** new listeners"

**You don't need to modify a network to understand a flow.**

---

## 🔄 Complete Order Flow

https://mermaid.live/edit#pako:eNp9VM1q20AQfpXJQiGB2Fhy9GMdAqnTxmDjmDq9FF820tgRlrTqSkrqhEAvLfSQQltKIRQCPfVY6Bv0UfIEeYTOSpZjx0p0kLT7fd98M7MjXTBXeMgcluDbDCMX930-kTwcRUBXzGXqu37MoxTawBNoZ0kqQpSw-Tzg50JuVfA6ORGjVPKgkx2vM7qK0OXjKV_HBkMFDmgnQjlEeeq7uM7q56y-SP2x7_LUF9Gj1F5O7YkHtILYru3utjsODALuIhxKD-Uc6BDSdUCorZorkafoFVCXkMHQgbubb7-gj2eFbKMAi_tguKzmrovxirxfynMp7K0Q-sOaysqB258fYMQKShljY8QeN4olUuV-NKlwGqxiC5O7my-_b99f3d1c_SWzl0J44CdwjMSEIh7l9YQndcabVfi9ut9f8rr6vqgoF8JYSIh9d5rFTxZGDPSIU1FXDsHrJaxXYsOU5gAOaKSOJHenVcVfX1NC-9I_pYxEBOkJwhmfPehyIEQML4gyAw0SdEXkwaamQ-qHmMy_AHX15kkH82GrZbFHc3NPoIFFEMpL5Xigpsgw6qZlav_-9OH249d8aZsWLQsVRt5yJqWDl2dc45Ke6qQ2eQp249lWRYP2glAkKRydoMT18n_cl78IlggRPehAb-U4PAyUgtqufLVGtfF-yVpz_fypHLVFJPJj22wifY85qcxwm9FPJuRqyS5UgBGjswlxxBx69bicqgQvSUMf-RshwlImRTY5Yc6YBwmtigOY_9IWu5K6irItsihljtVs5kGYc8HeMUczm_VmU7dt29Is0zQUOmOOXm_ZO7pp2DuGtdNotXTjcpud576NesuyTaNltpp6o2Fp-uV_wp-0-A

---

## 📊 System Architecture (ASCII)

```
┌─────────────────────────────────────────────────────────────────┐
│                        Apache Kafka (Port 9092)                 │
│  ┌─────────────┬──────────────┬─────────────┬─────────────────┐ │
│  │order-created│order-accepted│order-ready  │location-update  │ │
│  └─────────────┴──────────────┴─────────────┴─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
         ▲                    ▲                           ▲
         │ Publishes          │ Publishes                 │ Publishes
         │                    │                           │
    ┌────┴─────┐        ┌─────┴──────┐          ┌────────┴────────┐
    │CentralHub│        │Partner     │          │Location         │
    │   :5288  │        │Service     │          │Service          │
    │          │        │   :5220    │          │   :5240         │
    └──────────┘        └────────────┘          └─────────────────┘
                              │                          │
                              │                          │
                         Subscribes                 Subscribes
                              │                          │
                              ▼                          ▼
                    ┌──────────────────────────────────────┐
                    │      NotificationService             │
                    │            :5230                     │
                    │  - OrderAcceptedConsumer             │
                    │  - OrderPreparingConsumer            │
                    │  - OrderReadyConsumer                │
                    │  - DriverArrivingConsumer            │
                    │  - OrderDeliveredConsumer            │
                    └──────────────────────────────────────┘
                                    │
                                    │ Updates UI
                                    ▼
                            ┌───────────────┐
                            │ Blazor UI     │
                            │    :7066      │
                            └───────────────┘
```

---

## 📢 Kafka Topics & Events

| Topic | Published By | Consumed By | Purpose |
|-------|--------------|-------------|---------|
| `order-created` | CentralHub | PartnerService | New order notification |
| `order-accepted` | PartnerService | NotificationService | Restaurant confirmed |
| `order-preparing` | PartnerService | NotificationService | Cooking started |
| `order-ready` | PartnerService | NotificationService | Food ready for pickup |
| `order-pickedup` | PartnerService | NotificationService, LocationService | Driver collected order |
| `location-update` | LocationService | *(Future: Map UI)* | GPS coordinates (12 updates) |
| `driver-arriving` | LocationService | NotificationService | 80% progress reached |
| `order-delivered` | LocationService | NotificationService | Delivery complete |

---

## 🔧 How Kafka Integration Works

### 1. Shared Event Models
```csharp
// Shared/Events/OrderCreatedEvent.cs
public class OrderCreatedEvent {
    public string OrderId { get; set; }
    public string RestaurantId { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 2. Publishing Events
```csharp
// CentralHub publishes
await kafkaProducer.PublishAsync(
    KafkaTopics.OrderCreated,  // "order-created"
    orderId,
    new OrderCreatedEvent { OrderId = orderId, ... }
);
```

### 3. Consuming Events
```csharp
// PartnerService listens
public class OrderCreatedConsumer : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken ct) {
        await foreach (var msg in kafkaConsumer.ConsumeAsync<OrderCreatedEvent>(
            KafkaTopics.OrderCreated, ct)) {
            
            Console.WriteLine($"📦 New order: {msg.Value.OrderId}");
            // React to event!
        }
    }
}
```

---

## 🎯 Key Demo Points

### 1. **Independence**
- Kill NotificationService → Orders still flow through kitchen
- Restart LocationService → GPS tracking resumes from last position

### 2. **Extensibility**
- Want SMS notifications? Add new SmsService listening to same topics
- Want analytics? Add AnalyticsService counting events
- **No modifications to existing services needed!**

### 3. **Real-time Streaming**
- LocationService publishes GPS updates every 1 second (12 total)
- NotificationService reacts instantly
- Demonstrates continuous event streams, not just one-off messages

### 4. **Decoupled Communication**
- PartnerService has no idea NotificationService exists
- LocationService doesn't know who's listening to GPS updates
- Services only know: "I publish to Kafka" and "I listen to Kafka"

---

## 🎓 Exam Presentation Script

**"Our bachelor project uses microservices architecture. Today I'll demonstrate why we chose event-driven design with Apache Kafka."**

### Problem Statement:
*"In traditional systems, services call each other directly. This creates tight coupling. If I want to add a new feature - like SMS notifications - I have to MODIFY existing code in multiple services. The network becomes rigid."*

### Our Solution:
*"With Kafka, services publish events to topics. Other services subscribe to topics they care about. If I want SMS notifications, I just ADD a new service that listens. Zero changes to existing code. The network grows organically."*

### Live Demo:
1. Show 5 terminal windows + Blazor UI
2. Place order → Watch events flow through console logs
3. Accept → Prepare → Ready → Pickup
4. **Highlight LocationService:** 12 GPS updates over 12 seconds
5. "Driver arriving" at 80% → "Delivered" at 100%

### Conclusion:
*"This is event-driven architecture. Services don't know about each other. They just emit and listen. It's scalable, resilient, and extensible. That's why modern systems like Uber, Netflix, and LinkedIn use Kafka."*

---

## 🚀 Running the Demo

```powershell
# 1. Start Kafka (Docker)
docker-compose up -d

# 2. Start services in 5 terminals
cd CentralHub.API     ; dotnet run
cd PartnerService     ; dotnet run
cd NotificationService; dotnet run
cd LocationService    ; dotnet run
cd KafkaOrderDemo     ; dotnet run

# 3. Open browser → http://localhost:7066
```

**Watch the console logs flow! 🌊**
```

**Watch the console logs flow! 🌊**

### Without Kafka:
```
CentralHub → calls → PartnerService → calls → NotificationService
  ❌ If one service is down, everything breaks
  ❌ Services are tightly coupled
```

### With Kafka:
```
CentralHub → publishes event → Kafka
                                 ↓
                    ┌────────────┴────────────┐
                    ↓                         ↓
            PartnerService              NotificationService
         (listens independently)      (listens independently)
         
  ✅ Services don't know about each other
  ✅ One service down doesn't break others
  ✅ Easy to add new services
```

Think for example adding a new **SMSNotificationService** - All it does is perhaps also just listen to the same events that get emitted, or it can react to other services emitting something or perhaps it just emits its own events that other services can listen to.

Its so easy to add functionality because each service just listens to the event THEY want to listen to. Super loose coupling!

---

## 🔄 Order Flow Example

**Customer orders a pizza:**

1. **Customer places order** → CentralHub receives it
2. CentralHub publishes `order-created` event to Kafka
3. **PartnerService hears the event** → Shows order in Kitchen UI
4. Chef clicks "Accept" → PartnerService publishes `order-accepted` event
5. **NotificationService hears it** → Creates toast: "✅ Order accepted!"
6. Chef clicks "Start Preparing" → PartnerService publishes `order-preparing` event
7. **NotificationService hears it** → Shows toast: "👨‍🍳 Food is being prepared!"
8. Chef clicks "Ready" → PartnerService publishes `order-ready` event
9. **NotificationService hears it** → Shows toast: "🍕 Order ready!"
10. Chef clicks "Picked Up" → PartnerService publishes `order-pickedup` event
11. **NotificationService hears it** → Shows toast: "🚚 Driver on the way!"

**Key point:** PartnerService and NotificationService never talk to each other directly. They both just listen to Kafka topics they care about.

---

## � Kafka Topics (Event Categories)

| Topic Name | When It's Published | Who Publishes | Who Listens |
|------------|---------------------|---------------|-------------|
| `order-created` | Customer places order | CentralHub | PartnerService |
| `order-accepted` | Restaurant accepts | PartnerService | NotificationService |
| `order-preparing` | Chef starts cooking | PartnerService | NotificationService |
| `order-ready` | Food is ready | PartnerService | NotificationService |
| `order-pickedup` | Driver picks up | PartnerService | NotificationService |

---

## 💡 Why This Matters

1. **Independence** - NotificationService can be down, order still flows through PartnerService
2. **Scalability** - Easy to add new services (e.g., SMS notifications) without changing existing code
3. **Resilience** - Kafka stores events, so if a service restarts, it can replay missed events
4. **Async** - Services don't wait for each other, everything happens in parallel

---

## 🎓 For Your Exam

**Explain it like this:**

"In traditional systems, services call each other directly - if one crashes, the whole chain breaks. 

With Kafka, services publish events to a message broker. Other services subscribe to events they care about. They work independently.

For example: when a chef accepts an order, PartnerService publishes an 'order-accepted' event. NotificationService hears it and creates a toast notification. If NotificationService is down, the kitchen still works fine - it just publishes events without caring who's listening.

This is called **event-driven architecture** - services react to events, not direct calls."



