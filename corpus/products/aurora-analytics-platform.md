# Aurora Analytics Platform

**Product Category:** Real-Time Data Analytics
**Current Version:** 4.2.1
**Last Updated:** February 2024

## Overview

Aurora Analytics Platform is Vortex Technologies' flagship real-time data analytics solution. Built for teams that need to transform raw data streams into actionable insights within milliseconds, Aurora combines a high-throughput streaming ingestion engine, an interactive SQL query engine, customizable dashboards, and built-in machine learning anomaly detection into a single unified platform.

Aurora is trusted by over 2,000 organizations worldwide, processing more than 14 billion events per day across all customer deployments. Whether you are monitoring e-commerce transaction flows, analyzing IoT sensor telemetry, or running financial risk models, Aurora provides the speed and flexibility to keep pace with your data.

## Core Capabilities

### Streaming Ingestion Engine

Aurora's ingestion layer accepts data from a wide variety of sources with sub-second latency. The engine supports both push-based (webhooks, SDKs) and pull-based (polling connectors) ingestion patterns. Data is validated, enriched, and indexed on arrival, making it queryable almost immediately.

Supported throughput scales linearly with provisioned capacity. Starter tier customers can ingest up to 50,000 events per second, while Enterprise deployments have been validated at over 2 million events per second.

### Interactive SQL Query Engine

Aurora ships with a fully ANSI SQL-compliant query engine optimized for analytical workloads. The engine uses columnar storage with vectorized execution to deliver sub-second query response times across terabytes of data. Window functions, CTEs, and lateral joins are fully supported. Users can query both real-time streaming data and historical archives through a single SQL interface.

### Dashboards and Visualization

The built-in dashboard builder supports over 30 chart types, including time series, heatmaps, scatter plots, geo-maps, and funnel visualizations. Dashboards refresh in real time via WebSocket connections and can be shared publicly, embedded in external applications via iframe, or exported as PDF reports on a scheduled basis.

### ML Anomaly Detection

Aurora includes a pre-trained anomaly detection module that continuously monitors configured metrics and flags statistical outliers. The module supports seasonal decomposition, z-score thresholding, and isolation forest models. Detected anomalies generate alerts that can be routed to the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) for centralized alerting and escalation through your existing incident response workflows. See the [Incident Response Plan](../policies/incident-response-plan.md) for how Vortex Technologies handles anomaly-triggered incidents internally.

## Supported Data Sources

| Data Source   | Connector Type | Minimum Version | Real-Time Support |
|---------------|---------------|-----------------|-------------------|
| PostgreSQL    | Pull (CDC)    | 12.0+           | Yes               |
| MySQL         | Pull (CDC)    | 8.0+            | Yes               |
| Snowflake     | Pull (Batch)  | N/A             | Scheduled only    |
| Amazon S3     | Pull (Event)  | N/A             | Yes (via SQS)     |
| Apache Kafka  | Push (Stream) | 2.8+            | Yes               |

Additional connectors for MongoDB, Redis Streams, Google BigQuery, and Azure Event Hubs are available in beta. Custom data sources can be integrated through Aurora's REST Ingestion API, which is documented in our [API Style Guide](../engineering/api-style-guide.md) conventions.

## Pricing Tiers

| Feature                        | Starter ($99/mo) | Pro ($499/mo)     | Enterprise (Custom) |
|--------------------------------|-------------------|-------------------|---------------------|
| Events per month               | 10 million        | 100 million       | Unlimited           |
| Dashboards                     | 5                 | 50                | Unlimited           |
| Data retention                 | 30 days           | 1 year            | Custom              |
| SQL query concurrency          | 5 concurrent      | 25 concurrent     | Unlimited           |
| Anomaly detection              | Basic (z-score)   | Advanced (all models) | Advanced + custom |
| SSO / SAML                     | No                | Yes               | Yes                 |
| SLA uptime guarantee           | Best effort       | 99.9%             | 99.99%              |
| Pulse Monitoring integration   | No                | Yes               | Yes                 |
| Support                        | Email (48h)       | Email + Chat (4h) | Dedicated TAM (1h) |

Enterprise customers may also negotiate custom data retention periods. All retention handling follows the Vortex Technologies [Data Retention Policy](../policies/data-retention-policy.md) to ensure compliance with regulatory obligations.

## SLA Guarantees

Pro tier customers receive a 99.9% monthly uptime SLA, measured as the percentage of minutes in which the Aurora query engine and ingestion API return successful responses. Enterprise tier customers receive a 99.99% SLA with financial credits issued automatically for any breach. SLA exclusions include scheduled maintenance windows (announced 72 hours in advance) and force majeure events.

Uptime is independently monitored by the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md), which runs synthetic checks against Aurora endpoints every 15 seconds across all six Nebula geo-replication regions.

## Integration with Pulse Monitoring Suite

Aurora integrates natively with the Pulse Monitoring Suite. Anomaly detection alerts, query latency degradation warnings, and ingestion pipeline health metrics are forwarded to Pulse in real time. From Pulse, these alerts can be routed to PagerDuty, Slack, OpsGenie, or Microsoft Teams according to your team's escalation preferences. This integration is enabled by default for Pro and Enterprise customers and requires no additional configuration.

## Getting Started

Sign up for a free 14-day trial of the Pro tier at [aurora.vortextech.io](https://aurora.vortextech.io). No credit card is required. All data ingested during the trial is retained for 30 days after trial expiration. For Enterprise evaluations, contact sales@vortextech.io to schedule an architecture review with our solutions engineering team.
