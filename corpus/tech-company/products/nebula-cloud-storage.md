# Nebula Cloud Storage

**Product Category:** Object Storage
**Current Version:** 3.8.0
**Last Updated:** January 2024

## Overview

Nebula Cloud Storage is Vortex Technologies' enterprise-grade object storage service designed for durability, security, and cost efficiency. With an S3-compatible API, Nebula allows teams to migrate existing workloads seamlessly while gaining access to advanced features like intelligent tiering, geo-replication across six global regions, and built-in compliance controls for regulated industries.

Nebula currently stores over 45 petabytes of customer data and serves more than 8 billion API requests per month. It is the storage backbone for several Vortex products, including the [Aurora Analytics Platform](../products/aurora-analytics-platform.md), which uses Nebula to persist historical event archives and dashboard snapshots.

## Storage Tiers

Nebula offers three storage tiers optimized for different access patterns. Objects can be assigned to a tier at upload time or transitioned automatically via lifecycle policies.

| Tier    | Price (per GB/month) | Access Latency   | Retrieval Fee   | Use Case                          |
|---------|---------------------|------------------|-----------------|-----------------------------------|
| Hot     | $0.023              | < 10 ms          | None            | Frequently accessed data          |
| Warm    | $0.012              | < 100 ms         | $0.01 per GB    | Infrequent access, quick retrieval|
| Archive | $0.004              | 1 - 12 hours     | $0.03 per GB    | Long-term retention, compliance   |

### Intelligent Tiering

Nebula's intelligent tiering engine monitors object access patterns over a rolling 30-day window. Objects that have not been accessed in 30 days are automatically transitioned from Hot to Warm. Objects untouched for 90 days move to Archive. Customers can override these defaults with custom lifecycle rules. Transition events are logged and can be monitored via the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md).

## Security and Encryption

### Encryption at Rest

All data stored in Nebula is encrypted at rest using AES-256 encryption. Customers can choose between Vortex-managed keys (default) or bring-your-own-key (BYOK) via integration with AWS KMS, Azure Key Vault, or HashiCorp Vault. Key rotation is performed automatically every 90 days for Vortex-managed keys.

### Encryption in Transit

All API communication with Nebula is encrypted using TLS 1.3. Older TLS versions (1.0 and 1.1) are not supported. TLS 1.2 is supported but will be deprecated in Q3 2024.

### Access Control

Nebula supports bucket-level and object-level access control via IAM policies. Presigned URLs can be generated for temporary access with configurable expiration (minimum 60 seconds, maximum 7 days). All access events are logged to an immutable audit trail retained for 12 months.

## Geo-Replication

Nebula replicates data across six global regions to ensure durability and low-latency access:

| Region Code | Location            | Status     |
|-------------|---------------------|------------|
| US-EAST-1   | Virginia, USA       | Active     |
| US-WEST-2   | Oregon, USA         | Active     |
| EU-WEST-1   | Dublin, Ireland     | Active     |
| EU-CENTRAL-1| Frankfurt, Germany  | Active     |
| AP-SOUTH-1  | Mumbai, India       | Active     |
| AP-EAST-1   | Tokyo, Japan        | Active     |

Cross-region replication can be configured per bucket. By default, data resides in the region selected at bucket creation. Enabling multi-region replication increases storage costs by 1.5x but provides 99.999999999% (11 nines) durability and automatic failover if a region becomes unavailable.

## S3-Compatible API

Nebula's API is fully compatible with the AWS S3 API specification (v2 and v4 signature). Existing tools such as the AWS CLI, Terraform S3 backend, and popular SDKs (boto3, aws-sdk-js, AWS SDK for .NET) work with Nebula by simply changing the endpoint URL. Our API design follows the conventions described in the Vortex [API Style Guide](../engineering/api-style-guide.md), including cursor-based pagination for listing operations and RFC 7807 error responses.

Supported operations include `PutObject`, `GetObject`, `DeleteObject`, `ListObjectsV2`, `CreateMultipartUpload`, `HeadObject`, `CopyObject`, and `PutBucketLifecycleConfiguration`, among others. Batch operations for bulk deletion and tagging are also available.

## Compliance

Nebula Cloud Storage meets the following compliance standards:

- **SOC 2 Type II** -- Audited annually by an independent third party. Latest report available upon request.
- **HIPAA** -- Business Associate Agreements (BAAs) available for healthcare customers. PHI can be stored in Nebula with appropriate access controls and encryption.
- **GDPR** -- Data residency controls ensure EU customer data remains within EU regions. Right-to-erasure requests are honored within 72 hours via the compliance API.

All data stored in Nebula is subject to the Vortex Technologies [Data Retention Policy](../policies/data-retention-policy.md). Customers should review the retention schedule to understand how classification levels affect minimum retention periods and deletion procedures, particularly for data classified as Confidential or Restricted.

## Monitoring and Observability

Nebula publishes storage metrics (total objects, total bytes, request counts, error rates, and latency percentiles) to the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) every 15 seconds. The [on-call runbook](../engineering/on-call-runbook.md) includes procedures for handling storage quota exhaustion alerts triggered by Pulse.

## Pricing Summary

Customers are billed monthly based on storage consumed and API requests made. There are no minimum commitments. Volume discounts are available for customers storing more than 100 TB.

| Metric               | Price                      |
|-----------------------|----------------------------|
| Hot storage           | $0.023 per GB / month      |
| Warm storage          | $0.012 per GB / month      |
| Archive storage       | $0.004 per GB / month      |
| PUT/POST requests     | $0.005 per 1,000 requests  |
| GET/HEAD requests     | $0.0004 per 1,000 requests |
| Data transfer (egress)| $0.09 per GB (first 10 TB) |

For questions about billing or to discuss Enterprise pricing, contact billing@vortextech.io.
