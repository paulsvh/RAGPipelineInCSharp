# Data Retention Policy

**Policy ID:** VT-POL-003
**Effective Date:** January 15, 2024
**Last Reviewed:** January 15, 2024
**Next Review Date:** January 15, 2025
**Policy Owner:** Maria Chen, Chief Data Officer
**Approved By:** Executive Leadership Team

## 1. Purpose

This policy establishes the requirements for retaining, archiving, and disposing of data created, collected, or managed by Vortex Technologies. It ensures that data is retained for the minimum period required by legal, regulatory, and business obligations, and that it is deleted securely when no longer needed.

This policy applies to all Vortex Technologies employees, contractors, and third-party service providers who handle company data. It governs data across all systems, including the [Aurora Analytics Platform](../products/aurora-analytics-platform.md), [Nebula Cloud Storage](../products/nebula-cloud-storage.md), internal databases, email systems, and physical records.

## 2. Data Classification Levels

All data managed by Vortex Technologies must be assigned one of four classification levels. The classification determines the retention period, access controls, and deletion procedures.

| Classification | Description | Examples | Access Control |
|---------------|-------------|----------|----------------|
| **Public** | Information intended for public consumption | Marketing materials, published blog posts, open-source code, public API documentation | No restrictions |
| **Internal** | Non-sensitive business information | Internal meeting notes, project plans, non-sensitive Slack messages, engineering [architecture decision records](../engineering/architecture-decision-records.md) | Vortex employees and authorized contractors |
| **Confidential** | Sensitive business or customer information | Customer data, financial reports, product roadmaps, sales pipeline data, [incident response](../policies/incident-response-plan.md) post-mortems | Named individuals or teams with explicit approval |
| **Restricted** | Highly sensitive data subject to regulatory requirements | PII, PHI, payment card data, encryption keys, credentials, SOC 2 audit evidence | Strict need-to-know, MFA required, access logged |

When in doubt, data should be classified at the higher level. Reclassification requests must be submitted to the Data Governance team and approved by the data owner.

## 3. Retention Periods

Data must be retained for the minimum period specified below, measured from the date of creation or last modification, whichever is later.

| Classification | Minimum Retention Period | Maximum Retention Period | Storage Requirements |
|---------------|-------------------------|-------------------------|---------------------|
| Public | Indefinite | None | Any approved storage system |
| Internal | 3 years | 5 years | Encrypted storage recommended |
| Confidential | 7 years | 10 years | Encrypted storage required; Nebula Warm or Archive tier |
| Restricted | 10 years | 15 years | AES-256 encryption required; Nebula Archive tier with geo-replication |

Customer data stored in [Nebula Cloud Storage](../products/nebula-cloud-storage.md) is subject to both this policy and the customer's contractual retention terms. Where a customer contract specifies a longer retention period, the contractual term takes precedence.

Product telemetry data ingested through the [Aurora Analytics Platform](../products/aurora-analytics-platform.md) follows the retention limits of the customer's pricing tier (30 days for Starter, 1 year for Pro, custom for Enterprise).

## 4. Deletion Procedures

When data reaches the end of its retention period and is not subject to a legal hold, it must be deleted according to the procedure appropriate for its classification level.

### Standard Deletion (Public, Internal, Confidential)

Standard deletion involves removing all copies of the data from production systems, backups, and caches. For data stored in Nebula, this means issuing a `DeleteObject` API call followed by verification that the object is no longer accessible. Backup copies must be purged within 30 days of the primary deletion.

### Cryptographic Erasure (Restricted)

Restricted data must be deleted using cryptographic erasure. This involves destroying all copies of the encryption key used to protect the data, rendering the encrypted data permanently unreadable. The key destruction event must be logged in the audit trail with the following details:

- Data asset identifier
- Encryption key identifier
- Timestamp of key destruction
- Identity of the authorized individual who initiated the deletion
- Confirmation hash from the key management system

For Restricted data encrypted with Vortex-managed keys in Nebula, the key destruction is initiated through the Key Management API. For BYOK customers, Vortex will issue a formal notification that the customer must destroy the key on their side.

## 5. Legal Hold Obligations

A legal hold suspends all deletion activities for data that may be relevant to pending or anticipated litigation, regulatory investigations, or audits. When a legal hold is issued by the Legal department:

- All automated lifecycle policies on affected data must be paused immediately.
- Affected data must be tagged with the legal hold identifier in Nebula metadata.
- No copies of the data may be deleted, modified, or moved until the hold is released.
- The hold applies across all systems, including backups and disaster recovery replicas.

Failure to comply with a legal hold may result in disciplinary action as outlined in the [Acceptable Use Policy](../policies/acceptable-use-policy.md) and may expose Vortex Technologies to legal liability.

## 6. Annual Audit Requirements

The Data Governance team must conduct an annual audit of data retention compliance. The audit must include:

1. **Inventory review** -- Verify that all data assets are classified and that classifications are current.
2. **Retention compliance check** -- Confirm that data has been retained for the required minimum period and that expired data has been deleted.
3. **Deletion verification** -- For a random sample of at least 50 deleted assets, verify that deletion was performed correctly (standard or cryptographic, as appropriate).
4. **Legal hold review** -- Confirm that all active legal holds are documented and that affected data has not been modified or deleted.
5. **Access control audit** -- Verify that access to Confidential and Restricted data is limited to authorized individuals.

Audit results must be reported to the Chief Data Officer and retained as Confidential records for 7 years.

## 7. Exceptions

Exceptions to this policy must be requested in writing to the Chief Data Officer and approved by the VP of Legal. Approved exceptions are valid for one year and must be renewed annually. All exceptions must be documented with a business justification and a risk assessment.

## 8. Related Documents

- [Acceptable Use Policy](../policies/acceptable-use-policy.md)
- [Incident Response Plan](../policies/incident-response-plan.md)
- [Nebula Cloud Storage — Compliance](../products/nebula-cloud-storage.md)
- [Aurora Analytics Platform — Data Retention by Tier](../products/aurora-analytics-platform.md)
