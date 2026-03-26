# Pulse Monitoring Suite

**Product Category:** Infrastructure and Application Monitoring
**Current Version:** 5.1.3
**Last Updated:** March 2024

## Overview

Pulse Monitoring Suite is Vortex Technologies' comprehensive observability platform for infrastructure and application monitoring. Pulse provides lightweight agents, flexible alerting rules, distributed tracing, uptime monitoring, and a custom metrics API — everything engineering teams need to maintain reliability and respond to incidents quickly.

Pulse is used internally at Vortex Technologies to monitor the [Aurora Analytics Platform](../products/aurora-analytics-platform.md) and [Nebula Cloud Storage](../products/nebula-cloud-storage.md) production environments. The same platform that keeps our own products reliable is available to every Vortex customer.

## Lightweight Agents

The Pulse agent is a single statically-linked binary (< 15 MB) that runs on Linux, macOS, and Windows. It collects system metrics (CPU, memory, disk, network), process-level metrics, and container metrics (Docker, containerd) with minimal resource overhead — typically under 0.5% CPU and 30 MB of memory.

Agent installation is a single command:

```bash
curl -sL https://install.vortextech.io/pulse | bash -s -- --api-key YOUR_API_KEY
```

For Kubernetes environments, Pulse provides a Helm chart that deploys the agent as a DaemonSet. The Helm chart also installs the OpenTelemetry Collector sidecar for distributed tracing collection. Deployment procedures and configuration are maintained in our internal [Architecture Decision Records](../engineering/architecture-decision-records.md) — see ADR-003 for context on our Kubernetes migration.

## Metric Resolution

Pulse collects and stores metrics at **15-second resolution** by default. This granularity enables teams to detect transient spikes and micro-outages that would be invisible at 1-minute or 5-minute intervals. Historical metrics are downsampled after 30 days (to 1-minute resolution) and after 90 days (to 5-minute resolution) to manage storage costs.

Custom metrics submitted via the API inherit the same resolution and retention policies.

## Alerting Rules

Pulse supports three types of alerting rules, which can be combined for sophisticated detection:

### Threshold Alerts

Classic threshold-based alerts trigger when a metric crosses a static boundary. Example: "Alert when CPU usage exceeds 85% for 5 consecutive minutes." Thresholds can be configured as above, below, or outside-range. The [on-call runbook](../engineering/on-call-runbook.md) documents standard remediation steps for common threshold alerts such as high CPU and database connection pool exhaustion.

### Anomaly Alerts

Anomaly alerts use statistical models to learn the normal behavior of a metric and alert when values deviate significantly. This is particularly useful for metrics with strong daily or weekly seasonality, such as request volume or user sign-ups. When used with the [Aurora Analytics Platform](../products/aurora-analytics-platform.md), anomaly alerts can also incorporate Aurora's ML anomaly detection output for richer signal correlation.

### Composite Alerts

Composite alerts combine multiple conditions using boolean logic (AND, OR, NOT). Example: "Alert when CPU > 85% AND memory > 90% AND deployment occurred in the last 30 minutes." Composite alerts reduce noise by filtering out conditions that are expected or benign in isolation.

## Integrations

Pulse routes alerts and notifications to the tools your team already uses:

| Integration      | Supported Actions                        | Configuration       |
|------------------|------------------------------------------|---------------------|
| PagerDuty        | Create incident, resolve incident        | API key             |
| Slack            | Post to channel, DM on-call engineer     | OAuth app or webhook |
| OpsGenie         | Create alert, add tags, set priority     | API key             |
| Microsoft Teams  | Post adaptive card to channel            | Incoming webhook     |
| Email            | Send alert summary with charts           | SMTP or native       |
| Generic Webhook  | POST JSON payload to any URL             | URL + optional auth  |

Alert routing rules support time-based overrides (e.g., page PagerDuty during business hours, Slack-only after hours) and severity-based routing aligned with the [Incident Response Plan](../policies/incident-response-plan.md) severity levels (SEV1 through SEV4).

## Custom Metrics API

Teams can submit custom application metrics to Pulse via a simple HTTP API. The API accepts metrics in StatsD, Prometheus exposition, or Pulse's native JSON format.

```bash
curl -X POST https://api.vortextech.io/v1/metrics \
  -H "Authorization: Bearer $PULSE_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "metric": "checkout.latency_ms",
    "value": 142,
    "tags": {"service": "payments", "region": "us-east-1"},
    "timestamp": 1709145600
  }'
```

The API follows the Vortex [API Style Guide](../engineering/api-style-guide.md) conventions, including OAuth2 + JWT authentication and standard rate limiting (1,000 requests per minute for standard plans, 5,000 for Enterprise).

## Distributed Tracing

Pulse includes full distributed tracing support built on the OpenTelemetry standard. Traces are collected via the Pulse agent's embedded OpenTelemetry Collector and visualized in the Pulse UI as flame graphs and service maps.

Key tracing features:

- **Automatic instrumentation** for popular frameworks (Express.js, Spring Boot, ASP.NET Core, Django, FastAPI)
- **Trace-to-log correlation** links trace spans to structured log entries
- **Service dependency maps** automatically generated from trace data
- **Tail-based sampling** to capture 100% of error and high-latency traces while sampling normal traffic at configurable rates

Trace data is retained for 15 days on standard plans and 30 days on Enterprise plans. The decision to adopt gRPC for inter-service trace transport is documented in [ADR-002](../engineering/architecture-decision-records.md).

## Uptime Monitoring

Pulse can perform synthetic HTTP, TCP, and ICMP checks against your endpoints from all six [Nebula geo-replication regions](../products/nebula-cloud-storage.md). Checks run every 15 seconds, and downtime is detected within 30 seconds from at least two independent regions before an alert is fired (to prevent false positives from transient network issues).

Uptime reports are available as monthly SLA compliance dashboards, which Enterprise customers use to verify adherence to the 99.99% uptime guarantees offered by Aurora and Nebula.

## Pricing

| Plan       | Price         | Hosts   | Custom Metrics | Trace Retention |
|------------|---------------|---------|----------------|-----------------|
| Free       | $0/mo         | 5       | 100            | 1 day           |
| Team       | $15/host/mo   | 50      | 1,000          | 7 days          |
| Business   | $23/host/mo   | 500     | 10,000         | 15 days         |
| Enterprise | Custom        | Unlimited | Unlimited    | 30 days         |

All plans include unlimited alerting rules, integrations, and uptime checks. For Enterprise pricing, contact sales@vortextech.io.
