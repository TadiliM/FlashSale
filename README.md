# FlashSale

FlashSale is a ticket booking platform designed to explore the challenges of handling high demand for events and concurrent seat reservations.

The project focuses on:

- **Booking consistency:** preventing double bookings when multiple users attempt to reserve the same seat.
- **Temporary reservations:** holding seats for a limited time and releasing them when reservations expire.
- **Real-time updates:** notifying users of seat availability changes using WebSockets.
- **System Design:** exploring concurrency, data consistency, caching, scalability, and asynchronous processing.
- **Testing and performance:** validating booking behavior under concurrent requests and measuring performance under load.

## Planned technologies

- **Backend:** C#, .NET, ASP.NET Core
- **Frontend:** React, TypeScript
- **Database:** PostgreSQL
- **Real-time communication:** SignalR / WebSockets
- **Infrastructure:** Redis, Kafka
- **Testing:** xUnit, integration tests, k6
- **CI/CD and containers:** GitHub Actions, Docker
- **Observability:** OpenTelemetry, Prometheus, Grafana

The goal is to design and build a reliable booking system, with a focus on concurrency, real-time communication, and software architecture.
