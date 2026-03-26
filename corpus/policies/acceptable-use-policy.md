# Acceptable Use Policy

**Policy ID:** VT-POL-001
**Effective Date:** March 1, 2024
**Last Reviewed:** March 1, 2024
**Next Review Date:** March 1, 2025
**Policy Owner:** James Park, VP of Information Security
**Approved By:** Executive Leadership Team

## 1. Purpose

This policy defines the acceptable use of Vortex Technologies computing resources, networks, and systems. It applies to all employees, contractors, interns, and third-party personnel who access Vortex Technologies infrastructure, whether on-premises or cloud-based. This includes all environments used to develop, deploy, and operate the [Aurora Analytics Platform](../products/aurora-analytics-platform.md), [Nebula Cloud Storage](../products/nebula-cloud-storage.md), and [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md).

All personnel are expected to read and acknowledge this policy within 30 days of hire or contract start date. Annual re-acknowledgment is required.

## 2. Acceptable Use of Compute Resources

Vortex Technologies provides computing resources — including cloud infrastructure, development workstations, CI/CD pipelines, and Kubernetes clusters — for authorized business purposes. Reasonable personal use is permitted (e.g., occasional personal email, web browsing during breaks), provided it does not interfere with work responsibilities, consume excessive resources, or violate any other provision of this policy.

All usage of compute resources is subject to monitoring and logging as described in Section 7.

## 3. Prohibited Activities

The following activities are strictly prohibited on Vortex Technologies systems and networks:

### 3.1 Cryptocurrency Mining

Running cryptocurrency mining software (including but not limited to Bitcoin, Ethereum, Monero miners) on any Vortex-owned or Vortex-provisioned compute resource is prohibited. This includes mining on development workstations, CI runners, Kubernetes pods, and cloud instances. Cryptocurrency mining consumes significant compute and energy resources and creates abnormal load patterns that interfere with [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) anomaly detection baselines.

### 3.2 Personal File Hosting

Using Vortex infrastructure to host personal file sharing services, media servers (e.g., Plex, Jellyfin), or personal backup storage is prohibited. Company storage, including [Nebula Cloud Storage](../products/nebula-cloud-storage.md) buckets, must be used exclusively for business data. Personal data stored on company systems is subject to the [Data Retention Policy](../policies/data-retention-policy.md) and may be deleted without notice.

### 3.3 Unauthorized Network Scanning

Running port scans, vulnerability scans, or penetration tests against Vortex Technologies systems or third-party systems from Vortex networks is prohibited unless explicitly authorized in writing by the VP of Information Security. Authorized security testing must be conducted in designated testing environments and coordinated with the SRE team to avoid triggering false positive alerts in the [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md).

### 3.4 Other Prohibited Activities

- Distributing malware, viruses, or ransomware
- Accessing systems or data without authorization
- Sharing credentials or API keys with unauthorized individuals
- Circumventing access controls, firewalls, or content filters
- Using company resources for illegal activities
- Sending unsolicited bulk email (spam) from company systems
- Installing unauthorized software on production systems

## 4. Bring Your Own Device (BYOD) Policy

Employees who use personal devices (laptops, phones, tablets) to access Vortex Technologies systems must comply with the following requirements:

### 4.1 Mobile Device Management (MDM)

All personal devices used for work must be enrolled in the company's MDM solution (currently Microsoft Intune). The MDM agent enforces the following:

- Remote wipe capability in case of device loss or employee separation
- Automatic screen lock after 5 minutes of inactivity
- Minimum OS version requirements (iOS 16+, Android 13+, Windows 11, macOS 13+)

### 4.2 Encrypted Storage

All personal devices must have full-disk encryption enabled. On macOS, FileVault must be active. On Windows, BitLocker must be active. On mobile devices, device encryption must be enabled in system settings.

### 4.3 No Jailbroken or Rooted Devices

Jailbroken iOS devices and rooted Android devices are prohibited from accessing Vortex Technologies systems. The MDM solution actively detects jailbreak and root status and will automatically revoke access if detected.

### 4.4 Application Restrictions

Company data may only be accessed through approved applications. Downloading company data to unapproved personal applications (e.g., personal Dropbox, personal Google Drive) is prohibited.

## 5. Remote Access and Security Requirements

### 5.1 VPN Requirement

All remote access to internal Vortex Technologies systems must be conducted through the company VPN (currently WireGuard-based). Direct access to internal services, databases, and administrative interfaces from the public internet is prohibited. The VPN connection is required for accessing internal tools, staging environments, and production infrastructure management consoles.

### 5.2 Multi-Factor Authentication (MFA)

MFA is mandatory for all Vortex Technologies accounts, including:

- SSO / Identity Provider (Okta)
- VPN access
- Cloud provider consoles (AWS, GCP)
- Source code repositories (GitHub)
- Production database access
- [Nebula Cloud Storage](../products/nebula-cloud-storage.md) administrative console
- [Aurora Analytics Platform](../products/aurora-analytics-platform.md) administrative console

Approved MFA methods include hardware security keys (preferred), authenticator apps (Authy, Google Authenticator), and push notifications via Okta Verify. SMS-based MFA is not permitted due to SIM-swapping risks.

### 5.3 Session Management

VPN sessions time out after 12 hours of continuous use and must be re-authenticated. Web application sessions expire after 1 hour of inactivity, consistent with the JWT token expiration policy documented in the [API Style Guide](../engineering/api-style-guide.md).

## 6. Consequences of Violations

Violations of this policy will result in disciplinary action proportional to the severity and frequency of the violation:

| Violation Level | Action | Examples |
|----------------|--------|----------|
| **First minor violation** | Verbal warning documented in HR file | Excessive personal use, minor software installation |
| **Second minor violation or first major violation** | Written warning with corrective action plan | Unauthorized scanning, BYOD non-compliance |
| **Repeated violations or severe first offense** | Termination of employment or contract | Cryptocurrency mining, data exfiltration, credential sharing |

In cases involving potential criminal activity, Vortex Technologies reserves the right to involve law enforcement. Violations that result in a security incident must be reported and handled according to the [Incident Response Plan](../policies/incident-response-plan.md).

## 7. Monitoring and Enforcement

Vortex Technologies monitors network traffic, system logs, and resource usage to enforce this policy and detect security threats. Monitoring is conducted in accordance with applicable privacy laws. The [Pulse Monitoring Suite](../products/pulse-monitoring-suite.md) is used to detect anomalous resource usage patterns that may indicate policy violations, such as sustained high GPU utilization (potential mining) or unusual egress traffic volumes.

Employees should have no expectation of privacy when using company-provided systems and networks.

## 8. Related Documents

- [Data Retention Policy](../policies/data-retention-policy.md)
- [Incident Response Plan](../policies/incident-response-plan.md)
- [On-Call Runbook](../engineering/on-call-runbook.md)
- [API Style Guide — Authentication](../engineering/api-style-guide.md)
