# Incident Response Plan

**Policy ID:** VT-POL-005
**Effective Date:** February 1, 2024
**Last Reviewed:** February 1, 2024
**Next Review Date:** August 1, 2024
**Policy Owner:** Anika Patel, VP of Engineering
**Approved By:** Executive Leadership Team

## 1. Purpose

This document defines the incident response process for Vortex Technologies. It covers severity classification, response time requirements, escalation paths, communication protocols, and post-mortem procedures. This plan applies to all production incidents affecting the [Aurora Analytics Platform](../products/aurora-analytics-platform.md), [Nebula Cloud Storage](../products/nebula-cloud-storage.md), [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md), and internal infrastructure.

All engineering personnel, SRE team members, and on-call responders must be familiar with this plan. On-call responders should also review the [On-Call Runbook](../engineering/on-call-runbook.md) for technical remediation procedures.

## 2. Severity Levels

Incidents are classified into four severity levels based on customer impact, scope, and urgency. The initial severity is assigned by the first responder and may be reclassified by the Incident Commander as more information becomes available.

| Severity | Definition | Response Time | Examples |
|----------|-----------|---------------|----------|
| **SEV1** | Complete outage of a production system affecting all customers | 15 minutes | Aurora ingestion API fully down; Nebula returns 5xx on all requests; complete loss of monitoring in Pulse |
| **SEV2** | Major feature degraded or unavailable for a significant subset of customers | 30 minutes | Aurora SQL queries timing out for 30%+ of users; Nebula replication lag > 1 hour in 2+ regions; Pulse alerting delayed by > 5 minutes |
| **SEV3** | Minor feature impacted with limited customer visibility | 2 hours | Single Aurora dashboard widget failing; Nebula lifecycle transitions delayed; Pulse agent losing connectivity in one region |
| **SEV4** | Cosmetic issue or low-impact bug with no significant customer effect | Next business day | UI rendering glitch; incorrect tooltip text; non-critical log warnings |

Response time is measured from the moment the alert fires in the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) or the moment a customer report is received, whichever is earlier.

## 3. Escalation Paths

### 3.1 Incident Roles

Every SEV1 and SEV2 incident must have the following roles assigned within the first 15 minutes:

**Incident Commander (IC)**
The IC has overall authority over the incident. They coordinate the response, make decisions about scope and severity, and ensure communication protocols are followed. The on-call Engineering Manager serves as the default IC. For SEV1 incidents lasting more than 1 hour, the IC role escalates to the VP of Engineering.

**Technical Lead (TL)**
The TL is responsible for diagnosing the root cause and implementing the fix. The on-call engineer from the affected service team serves as the default TL. The TL works directly with the codebase and infrastructure, referencing the [On-Call Runbook](../engineering/on-call-runbook.md) for standard remediation procedures and the [Architecture Decision Records](../engineering/architecture-decision-records.md) for system design context.

**Communications Lead (CL)**
The CL manages all external and internal communications during the incident. This includes updating the public status page, posting in the #incidents Slack channel, and notifying affected customers via email for SEV1 incidents. The on-call Customer Success Manager serves as the default CL.

### 3.2 On-Call Rotation

The on-call rotation ensures 24/7 coverage for incident response:

| Tier | Role | Responsibility | Escalation Trigger |
|------|------|---------------|-------------------|
| Primary | Engineering On-Call | First responder; initial diagnosis and remediation | Automatically paged via Pulse + PagerDuty |
| Secondary | SRE On-Call | Infrastructure expertise; assists with scaling, networking, and platform issues | Paged if primary does not acknowledge within 10 minutes |
| Tertiary | VP of Engineering | Executive escalation for SEV1 incidents exceeding 1 hour or requiring business decisions | Manually escalated by IC |

On-call schedules rotate weekly. Handoff occurs every Monday at 10:00 AM UTC. The on-call engineer must be reachable within 5 minutes and have laptop access to VPN (as required by the [Acceptable Use Policy](../policies/acceptable-use-policy.md) remote access provisions).

## 4. Incident Response Workflow

### 4.1 Detection

Incidents are detected through three channels:

1. **Automated alerts** from the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) — threshold, anomaly, and composite alerts that trigger PagerDuty pages.
2. **Customer reports** submitted via the support portal or email to support@vortextech.io.
3. **Internal observation** by engineering teams during normal operations.

### 4.2 Triage (First 15 Minutes)

1. Acknowledge the alert in PagerDuty.
2. Open an incident channel in Slack: `#inc-YYYYMMDD-short-description`.
3. Assign initial severity based on the table in Section 2.
4. Assign IC, TL, and CL roles (for SEV1/SEV2).
5. Begin diagnosis using the [On-Call Runbook](../engineering/on-call-runbook.md).

### 4.3 Containment and Remediation

The TL works to contain the impact and restore service. Common actions include:

- Rolling back a recent deployment
- Scaling infrastructure horizontally (see runbook for CLI commands)
- Failing over to a secondary region
- Blocking malicious traffic at the load balancer
- Increasing database connection pool limits

The IC must authorize any action that could cause additional customer impact (e.g., full service restart, data migration).

### 4.4 Resolution

An incident is considered resolved when the affected service returns to normal operation and the Pulse monitoring dashboards confirm metrics are within acceptable thresholds. The IC declares resolution in the incident Slack channel and updates the status page.

## 5. Communication Templates

### External Status Page Update (SEV1)

> **[Service Name] — Service Disruption**
> We are currently experiencing a disruption affecting [Service Name]. Our engineering team is actively investigating and working to restore normal operation. We will provide updates every 30 minutes. Last updated: [Timestamp UTC].

### Customer Email Notification (SEV1)

> Subject: [Vortex Technologies] Service Incident Affecting [Service Name]
>
> Dear [Customer Name],
>
> We are writing to inform you that [Service Name] is currently experiencing a service disruption that may affect your operations. Our engineering team identified the issue at [Time UTC] and is actively working on a resolution.
>
> **Current Impact:** [Brief description]
> **Estimated Resolution:** [ETA or "Under investigation"]
>
> We will send a follow-up notification when the issue is resolved. If you have questions, please contact support@vortextech.io.

## 6. Post-Mortem Requirements

### 6.1 Timeline

| Severity | Post-Mortem Required | Due Date |
|----------|---------------------|----------|
| SEV1 | Yes (mandatory) | Within 5 business days of resolution |
| SEV2 | Yes (mandatory) | Within 5 business days of resolution |
| SEV3 | Optional (recommended) | Within 10 business days if conducted |
| SEV4 | No | N/A |

### 6.2 Post-Mortem Document Contents

Every post-mortem must include:

1. **Incident summary** — What happened, in plain language.
2. **Timeline** — Minute-by-minute reconstruction from detection to resolution.
3. **Root cause analysis (RCA)** — The underlying cause, not just the trigger. Use the "5 Whys" technique.
4. **Impact assessment** — Number of affected customers, duration, financial impact if applicable.
5. **Action items** — Concrete, assignable tasks to prevent recurrence. Each action item must have an owner and a due date.
6. **Lessons learned** — What went well, what went poorly, and what was lucky.

Post-mortems are blameless. They focus on systemic improvements, not individual fault. Post-mortem documents are classified as Confidential per the [Data Retention Policy](../policies/data-retention-policy.md) and retained for 7 years.

### 6.3 RCA Review Meeting

A review meeting must be held within 7 business days of resolution for SEV1 and SEV2 incidents. Attendees include the IC, TL, CL, the affected team's engineering manager, and the VP of Engineering. Action items are tracked in Jira and reviewed in weekly SRE stand-ups.

## 7. Related Documents

- [On-Call Runbook](../engineering/on-call-runbook.md)
- [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md)
- [Acceptable Use Policy](../policies/acceptable-use-policy.md)
- [Data Retention Policy](../policies/data-retention-policy.md)
- [Architecture Decision Records](../engineering/architecture-decision-records.md)
