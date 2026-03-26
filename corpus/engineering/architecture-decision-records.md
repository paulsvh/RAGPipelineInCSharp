# Architecture Decision Records

This document collects approved Architecture Decision Records (ADRs) for Vortex Technologies. ADRs capture significant technical decisions, their context, and their consequences. They serve as a historical record for current and future team members.

All ADRs are classified as Internal per the [Data Retention Policy](../policies/data-retention-policy.md) and retained for a minimum of 3 years.

---

## ADR-001: Choosing PostgreSQL over MongoDB for Primary Datastore

**Date:** 2023-06-14
**Status:** Accepted
**Deciders:** Anika Patel (VP Engineering), David Liu (Staff Engineer), Sarah Okonkwo (Database Team Lead)

### Context

Vortex Technologies needed to select a primary datastore for the [Aurora Analytics Platform](../products/aurora-analytics-platform.md) and supporting internal services. The two leading candidates were PostgreSQL (relational) and MongoDB (document-oriented). The decision needed to account for:

- The need to store both structured metadata (user accounts, billing, configurations) and semi-structured event data (analytics payloads with variable schemas)
- ACID transaction requirements for billing and account management
- Query complexity — Aurora's SQL query engine requires join support, window functions, and aggregations
- Operational maturity and ecosystem tooling
- Team expertise — the engineering team had deeper experience with relational databases

### Decision

We chose **PostgreSQL** (version 15+) as our primary datastore.

### Rationale

1. **Relational integrity.** PostgreSQL provides full ACID compliance, foreign keys, and constraints. These guarantees are essential for financial data (billing records, subscription management) and user account management. MongoDB's multi-document transactions, while improved in recent versions, remain less mature and carry performance overhead.

2. **JSONB for flexibility.** PostgreSQL's JSONB column type provides document-store flexibility within a relational database. Analytics event payloads with variable schemas are stored as JSONB and queried efficiently using GIN indexes. This eliminates the primary advantage of a document database while retaining relational benefits.

3. **Strong ecosystem.** PostgreSQL has broad support across ORMs, migration tools (Flyway, Alembic), monitoring solutions, and managed hosting providers. The [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) includes built-in PostgreSQL metric collectors for connection pool utilization, query latency, and replication lag.

4. **Query power.** Aurora's SQL query engine translates user queries into PostgreSQL-dialect SQL. Features like CTEs, window functions, lateral joins, and materialized views are used extensively. MongoDB's aggregation pipeline, while powerful, would require a translation layer and does not support standard SQL.

### Trade-offs

- **Schema migrations** require more planning than MongoDB's schema-less approach. We mitigate this with versioned migrations and a staging environment.
- **Horizontal scaling** is more complex with PostgreSQL. We use read replicas for query distribution and Citus for sharding high-volume event tables.

### Consequences

- All new services default to PostgreSQL unless a specific use case justifies an alternative (documented via a new ADR).
- Database connection pooling via PgBouncer is mandatory. Connection pool exhaustion alerts are configured in Pulse and documented in the [On-Call Runbook](../engineering/on-call-runbook.md).
- Database backup procedures follow the retention periods defined in the [Data Retention Policy](../policies/data-retention-policy.md).

---

## ADR-002: Adopting gRPC for Inter-Service Communication

**Date:** 2023-09-22
**Status:** Accepted
**Deciders:** Anika Patel (VP Engineering), Marcus Webb (Platform Team Lead), Li Wei (Senior Engineer)

### Context

As Vortex Technologies transitioned from a monolithic architecture to microservices, we needed to standardize the communication protocol between internal services. The candidates were:

- **REST over HTTP/1.1** — simple, well-understood, broad tooling support
- **gRPC over HTTP/2** — high performance, strong typing, streaming support
- **GraphQL** — flexible querying, good for frontend-backend communication

External-facing APIs (customer-facing) would continue to use REST following the [API Style Guide](../engineering/api-style-guide.md). This decision only affects internal service-to-service communication.

### Decision

We adopted **gRPC** as the standard protocol for internal service-to-service communication.

### Rationale

1. **Performance.** gRPC uses Protocol Buffers for serialization, which is significantly more compact and faster to parse than JSON. Benchmarks showed 3-5x throughput improvement over REST+JSON for our typical payloads. This matters particularly for the high-volume data path between Aurora's ingestion layer and storage services.

2. **Strong typing.** Protocol Buffer schemas (.proto files) serve as a machine-readable contract between services. Code generation in Go, Java, Python, and C# eliminates serialization bugs and provides compile-time type safety. Schema evolution rules (field numbering, backward compatibility) are enforced by the protobuf compiler.

3. **Streaming support.** gRPC supports server-side, client-side, and bidirectional streaming. This is used extensively by the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) for real-time metric streaming from agents to the collection backend, and for distributed tracing data transport.

4. **Deadline propagation.** gRPC's built-in deadline/timeout propagation prevents cascading failures in service chains. If a downstream service cannot respond within the caller's deadline, the request is cancelled automatically.

### Trade-offs

- **Harder debugging.** Binary Protocol Buffer payloads are not human-readable. We mitigate this by requiring all services to support gRPC reflection (for tools like `grpcurl`) and by maintaining a JSON transcoding proxy for development environments.
- **Browser incompatibility.** gRPC is not natively supported in browsers. External APIs remain REST-based, and we use grpc-web for internal admin UIs where needed.
- **Load balancer complexity.** gRPC requires HTTP/2-aware load balancers. Our Kubernetes ingress controllers (see ADR-003) are configured with gRPC health checking and connection balancing.

### Consequences

- All new internal services must expose gRPC interfaces. REST interfaces may be added in addition for debugging but are not required.
- Proto files are maintained in a central `vortex-protos` repository with CI validation for backward compatibility.
- The SRE team maintains gRPC-specific Pulse dashboards for latency, error rates, and connection counts.

---

## ADR-003: Migrating from Amazon ECS to Kubernetes

**Date:** 2024-01-10
**Status:** Accepted
**Deciders:** Anika Patel (VP Engineering), James Park (VP Information Security), Marcus Webb (Platform Team Lead), Ops Team

### Context

Vortex Technologies had been running all production services on Amazon ECS (Elastic Container Service) since 2021. As the company grew, several pain points emerged:

- **Vendor lock-in.** ECS is AWS-proprietary. Expanding to multiple cloud providers for [Nebula Cloud Storage](../products/nebula-cloud-storage.md) geo-replication required a portable orchestration layer.
- **Limited ecosystem.** ECS has a smaller ecosystem of community tools compared to Kubernetes. Service meshes, progressive delivery, and GitOps tooling are predominantly Kubernetes-native.
- **Cost at scale.** ECS Fargate pricing became less competitive as our container fleet grew beyond 500 tasks. Self-managed Kubernetes on EC2 (and potentially on other clouds) offered significant savings.

### Decision

We migrated production workloads from Amazon ECS to **Kubernetes**, running on Amazon EKS for the primary region and with the option to deploy on GKE or AKS for additional regions.

### Rationale

1. **Vendor portability.** Kubernetes runs identically across AWS (EKS), GCP (GKE), and Azure (AKS). This supports our multi-region strategy for Nebula and positions us to negotiate better cloud pricing by avoiding vendor lock-in.

2. **Community tooling.** The Kubernetes ecosystem provides battle-tested solutions for service mesh (Istio), GitOps (ArgoCD), secrets management (External Secrets Operator), progressive delivery (Argo Rollouts), and observability (Prometheus, OpenTelemetry). The [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) agent is deployed as a DaemonSet, and our distributed tracing pipeline uses the OpenTelemetry Collector Helm chart.

3. **Cost at scale.** Migrating from Fargate to self-managed node groups with Karpenter autoscaling reduced our compute costs by approximately 35%. Spot instances are used for non-critical workloads (batch processing, CI runners).

### Trade-offs

- **Operational complexity.** Kubernetes has a steeper learning curve than ECS. We invested in a 3-month training program for all engineering teams and hired two additional SRE engineers.
- **Upgrade burden.** Kubernetes releases new versions quarterly, and clusters must be upgraded regularly. We allocated one sprint per quarter for cluster upgrades.
- **Security surface.** Kubernetes introduces additional security considerations (RBAC, network policies, pod security standards). Security requirements are enforced via policy-as-code (OPA Gatekeeper) and reviewed per the [Acceptable Use Policy](../policies/acceptable-use-policy.md).

### Consequences

- All production services now run on Kubernetes (EKS). The migration completed in Q2 2024.
- ECS is decommissioned. No new ECS task definitions are permitted.
- On-call engineers must complete Kubernetes troubleshooting certification. Runbook procedures are documented in the [On-Call Runbook](../engineering/on-call-runbook.md).
- Certificate management in Kubernetes is handled via cert-manager, with renewal procedures documented in the runbook.
