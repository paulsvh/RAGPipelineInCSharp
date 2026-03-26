# API Style Guide

**Document Owner:** Platform Team
**Last Updated:** February 2024
**Classification:** Internal (see [Data Retention Policy](../policies/data-retention-policy.md))

## Overview

This document defines the engineering standards for designing and implementing REST APIs at Vortex Technologies. All public-facing APIs for the [Aurora Analytics Platform](../products/aurora-analytics-platform.md), [Nebula Cloud Storage](../products/nebula-cloud-storage.md), and [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) must conform to these standards. Internal service-to-service communication uses gRPC as decided in [ADR-002](../engineering/architecture-decision-records.md); this guide applies only to external REST APIs.

## URL Naming Conventions

### General Rules

- Use **lowercase kebab-case** for all URL path segments: `/data-sources`, not `/dataSources` or `/DataSources`.
- Use **plural nouns** for resource collections: `/users`, `/dashboards`, `/alert-rules`.
- Use resource identifiers as path segments: `/users/{user_id}/dashboards/{dashboard_id}`.
- Avoid verbs in URLs. Use HTTP methods to express actions:
  - `GET /dashboards` — list dashboards
  - `POST /dashboards` — create a dashboard
  - `GET /dashboards/{id}` — retrieve a dashboard
  - `PUT /dashboards/{id}` — replace a dashboard
  - `PATCH /dashboards/{id}` — partially update a dashboard
  - `DELETE /dashboards/{id}` — delete a dashboard
- For actions that do not map cleanly to CRUD operations, use a sub-resource: `POST /deployments/{id}/rollback`.

### Naming Examples

| Good | Bad | Reason |
|------|-----|--------|
| `/v1/alert-rules` | `/v1/alertRules` | Use kebab-case |
| `/v1/data-sources` | `/v1/getDataSources` | No verbs in paths |
| `/v1/users/{id}/api-keys` | `/v1/users/{id}/apikeys` | Separate compound words with hyphens |
| `/v1/metrics` | `/v1/metric` | Use plural nouns |

## API Versioning

APIs are versioned using **URL path prefixes**. The version number is placed immediately after the base path:

```
https://api.vortextech.io/v1/dashboards
https://api.vortextech.io/v2/dashboards
```

### Versioning Rules

- **Major versions** (`v1`, `v2`) indicate breaking changes. A new major version is required when removing fields, changing field types, altering response structure, or modifying authentication requirements.
- **Non-breaking changes** (adding optional fields, adding new endpoints) do not require a version bump.
- **Deprecated versions** are supported for a minimum of 12 months after the successor version reaches General Availability. Deprecation is announced via the `Sunset` HTTP header and documented on the developer portal.
- The current stable versions are `v1` for Aurora and Pulse APIs, and `v2` for Nebula APIs (v1 was sunset in June 2023).

## Pagination

All list endpoints must support **cursor-based pagination**. Offset-based pagination is not permitted because it performs poorly at scale and produces inconsistent results when data is modified between pages.

### Request Parameters

| Parameter | Type   | Required | Default | Description |
|-----------|--------|----------|---------|-------------|
| `cursor`  | string | No       | (none)  | Opaque cursor string from a previous response. Omit for the first page. |
| `limit`   | integer| No       | 25      | Number of items per page. Minimum: 1, Maximum: 100. |

### Response Format

```json
{
  "data": [
    { "id": "dash_01", "name": "Revenue Overview" },
    { "id": "dash_02", "name": "Error Rate by Service" }
  ],
  "pagination": {
    "next_cursor": "eyJpZCI6ImRhc2hfMDIiLCJjcmVhdGVkX2F0IjoiMjAyNC0wMS0xNVQxMDozMDowMFoifQ==",
    "has_more": true
  }
}
```

- The `next_cursor` field is a Base64-encoded opaque string. Clients must not parse or construct cursors — they are an implementation detail that may change without notice.
- When `has_more` is `false`, the `next_cursor` field is omitted.
- The `data` array may contain fewer items than `limit` on the last page.

### Implementation Notes

Cursors should encode the sort key(s) and the unique identifier of the last item. For most endpoints, this is `(created_at, id)`. Use `WHERE (created_at, id) > ($cursor_created_at, $cursor_id) ORDER BY created_at, id LIMIT $limit` in PostgreSQL (see [ADR-001](../engineering/architecture-decision-records.md) for datastore context).

## Error Response Format

All APIs must return errors in the **RFC 7807 Problem Details** format. This provides a consistent, machine-readable error structure across all Vortex products.

### Error Response Schema

```json
{
  "type": "https://api.vortextech.io/errors/rate-limit-exceeded",
  "title": "Rate Limit Exceeded",
  "status": 429,
  "detail": "You have exceeded the maximum request rate of 1000 requests per minute. Please retry after the period indicated in the Retry-After header.",
  "instance": "/v1/dashboards",
  "extensions": {
    "retry_after_seconds": 32,
    "limit": 1000,
    "remaining": 0,
    "reset_at": "2024-03-15T14:32:00Z"
  }
}
```

### Required Fields

| Field    | Type    | Description |
|----------|---------|-------------|
| `type`   | string  | A URI reference that identifies the error type. Should link to documentation. |
| `title`  | string  | A short, human-readable summary of the error type. |
| `status` | integer | The HTTP status code. |
| `detail` | string  | A human-readable explanation specific to this occurrence. |

### Optional Fields

| Field        | Type   | Description |
|--------------|--------|-------------|
| `instance`   | string | The request path that generated the error. |
| `extensions` | object | Additional machine-readable context specific to the error type. |

### Standard HTTP Status Codes

| Code | Meaning | When to Use |
|------|---------|-------------|
| 400  | Bad Request | Malformed request body, invalid parameters |
| 401  | Unauthorized | Missing or invalid authentication credentials |
| 403  | Forbidden | Valid credentials but insufficient permissions |
| 404  | Not Found | Resource does not exist |
| 409  | Conflict | Resource state conflict (e.g., duplicate creation) |
| 422  | Unprocessable Entity | Syntactically valid but semantically invalid request |
| 429  | Too Many Requests | Rate limit exceeded |
| 500  | Internal Server Error | Unexpected server failure (triggers [Incident Response](../policies/incident-response-plan.md)) |
| 503  | Service Unavailable | Planned maintenance or temporary overload |

## Authentication

All API requests must be authenticated using **OAuth 2.0 with JWT bearer tokens**.

### Token Lifecycle

| Token Type     | Expiration | Refresh Mechanism |
|---------------|------------|-------------------|
| Access Token  | 1 hour     | Use refresh token to obtain a new access token |
| Refresh Token | 30 days    | Re-authenticate with credentials |

### Authentication Flow

1. Client authenticates with the Vortex Identity Service using client credentials or authorization code flow.
2. Identity Service returns an access token (JWT) and a refresh token.
3. Client includes the access token in the `Authorization` header: `Authorization: Bearer <access_token>`.
4. When the access token expires, the client uses the refresh token to obtain a new access token without re-authenticating.

### JWT Claims

Access tokens include the following claims:

```json
{
  "sub": "user_abc123",
  "org": "org_xyz789",
  "scope": "aurora:read aurora:write nebula:read",
  "iat": 1710500000,
  "exp": 1710503600,
  "iss": "https://auth.vortextech.io"
}
```

The `scope` claim determines which APIs and operations the token can access. Scopes follow the pattern `{product}:{permission}` (e.g., `aurora:read`, `nebula:write`, `pulse:admin`).

Session expiration aligns with the security requirements in the [Acceptable Use Policy](../policies/acceptable-use-policy.md), which mandates 1-hour timeout for web application sessions.

## Rate Limiting

All API endpoints enforce rate limiting to ensure fair usage and protect service stability.

### Rate Limits by Plan

| Plan       | Rate Limit          | Burst Allowance |
|------------|--------------------:|-----------------|
| Standard   | 1,000 req/min       | 50 extra        |
| Enterprise | 5,000 req/min       | 200 extra       |

Rate limits are applied per API key (not per IP address). When the rate limit is exceeded, the API returns a `429 Too Many Requests` response with the following headers:

| Header          | Description |
|-----------------|-------------|
| `X-RateLimit-Limit` | Maximum requests allowed per window |
| `X-RateLimit-Remaining` | Requests remaining in the current window |
| `X-RateLimit-Reset` | Unix timestamp when the window resets |
| `Retry-After` | Seconds to wait before retrying |

### Example Rate Limit Response

```
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1710501600
Retry-After: 32

{
  "type": "https://api.vortextech.io/errors/rate-limit-exceeded",
  "title": "Rate Limit Exceeded",
  "status": 429,
  "detail": "You have exceeded the maximum request rate of 1000 requests per minute."
}
```

Clients should implement exponential backoff when receiving 429 responses. The [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) custom metrics API and the [Nebula Cloud Storage](../products/nebula-cloud-storage.md) S3-compatible API both follow these same rate limiting conventions.

## Related Documents

- [Architecture Decision Records](../engineering/architecture-decision-records.md) — ADR-001 (PostgreSQL), ADR-002 (gRPC)
- [On-Call Runbook](../engineering/on-call-runbook.md) — Remediation for API-related alerts
- [Incident Response Plan](../policies/incident-response-plan.md) — Escalation for 5xx errors
- [Acceptable Use Policy](../policies/acceptable-use-policy.md) — Session and authentication requirements
