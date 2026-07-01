# DOMAIN.md

# Taksaka V2 — Background Processing Platform

---

# Vision

Taksaka V2 is the centralized Background Processing Platform for MyHospital.

Its purpose is to execute asynchronous workloads reliably, continuously monitor platform health, detect operational problems early, and provide an operational control center for Hospital IT (EDP).

Taksaka is not a Hospital Information System.

Taksaka is not a Hospital Operational Dashboard.

Taksaka is an Operational Control Center dedicated to background processing.

---

# Mission

Provide a reliable, observable, extensible, and self-monitoring platform for all asynchronous processing within MyHospital.

---

# Problem Statement

Modern Hospital Information Systems execute many operations that should not run synchronously inside the user interface.

Examples include

- Accounting Journal Generation
- BPJS Integration
- SATUSEHAT Integration
- Legacy Projection
- Dashboard Projection
- Notification Delivery
- Cache Refresh
- Data Repair

Executing these operations directly during user interaction increases response time and negatively affects user experience.

Therefore these operations are delegated to Taksaka.

---

# Goals

## Functional Goals

- Execute background jobs
- Execute scheduled jobs
- Execute queued jobs
- Retry failed jobs
- Replay failed jobs
- Execute projection workers
- Execute integration workers
- Execute maintenance workers

## Operational Goals

- Monitor platform health
- Detect abnormal conditions
- Notify operators before failures become incidents
- Provide execution history
- Provide execution visibility
- Provide diagnostic capability

---

# Non Goals

Taksaka does NOT

- Replace HIS modules
- Replace Hospital Operational Dashboard
- Replace Business Intelligence
- Perform hospital operational analytics
- Manage hospital workflow

Business systems create work.

Taksaka executes work.

---

# Core Concepts

## Job

A Job is a unit of background work.

Examples

- Generate Accounting Journal
- Upload SATUSEHAT Data
- Generate Legacy Projection
- Refresh Cache

A Job has

- identity
- payload
- priority
- status
- execution history

---

## Worker

A Worker is a capability that executes one type of Job.

Examples

- Accounting Worker
- Projection Worker
- SATUSEHAT Worker
- Notification Worker

Workers encapsulate business processing.

---

## Queue

A Queue represents pending work waiting for execution.

The Queue guarantees eventual execution.

---

## Scheduler

The Scheduler creates Jobs according to predefined schedules.

Examples

- Every minute
- Every midnight
- Every Sunday
- Every 30 seconds

---

## Dispatcher

The Dispatcher assigns queued Jobs to the appropriate Worker.

---

## Alert

An Alert represents an operational warning requiring operator attention.

Alerts exist to prevent unnoticed failures.

Alerts are generated from Operational Health.

---

## Operator Console

The Operator Console is the application used by Hospital IT (EDP).

It provides

- Monitoring
- Diagnostics
- Replay
- Retry
- Configuration
- Alert Management

The Operator Console never performs business processing.

---

# Worker Categories

## Projection

Generate DDD models from legacy systems.

Examples

- Registration Projection
- Billing Projection
- Patient Projection

---

## Integration

Communicate with external systems.

Examples

- SATUSEHAT
- BPJS
- LIS
- RIS

---

## Business

Execute asynchronous hospital business processes.

Examples

- Accounting Journal
- Notification
- Dashboard Update

---

## Maintenance

Maintain platform integrity.

Examples

- Cleanup
- Data Repair
- Cache Refresh
- Rebuild Index

---

# Job Lifecycle

Created

↓

Queued

↓

Running

↓

Completed

or

↓

Failed

↓

Retry Waiting

↓

Running

↓

Completed

or

↓

Dead Letter

---

# Operational Health

Operational Health describes the current ability of Taksaka to execute background processing reliably.

Health is a business concept.

It represents the operational condition of the platform from the perspective of Hospital IT.

Operational Health is independent from implementation details.

---

## Health Dimensions

Taksaka continuously evaluates health across several dimensions.

### Platform Health

Overall condition of the Background Processing Platform.

Examples

- Platform Available
- Platform Degraded
- Platform Offline

---

### Queue Health

Measures workload waiting for execution.

Examples

- Queue backlog
- Queue growth
- Oldest pending job

---

### Worker Health

Measures whether Workers continue to process Jobs correctly.

Examples

- Worker available
- Worker delayed
- Worker offline

---

### Execution Health

Measures execution quality.

Examples

- Success rate
- Failure rate
- Retry rate
- Execution duration

---

### Integration Health

Measures connectivity with external systems.

Examples

- SATUSEHAT
- BPJS
- Email
- SMS
- WhatsApp

---

### Business Capability Health

Represents the operational condition of asynchronous business capabilities.

Examples

- Accounting Journal
- Registration Projection
- Billing Projection
- Notification Delivery

Business Capability Health allows operators to identify which business services are degraded without understanding technical implementation.

---

# Health States

Every Health Dimension reports one of the following states.

- Healthy
- Warning
- Critical
- Offline
- Disabled
- Unknown

These states form the ubiquitous language used throughout Taksaka.

---

# Alert Model

Alerts are generated when Operational Health transitions into abnormal states.

Alert severity

- Information
- Warning
- Critical

An Alert should answer

- What happened?
- Which capability is affected?
- Why is it unhealthy?
- What action is recommended?

---

# Operator Responsibilities

Taksaka assists Hospital IT by providing early warning before background processing failures become business incidents.

Typical operator actions include

- Investigate
- Retry failed Jobs
- Replay failed Jobs
- Restart Workers
- Escalate infrastructure failures
- Notify vendor support

---

# Future Direction

Taksaka is intended to become the universal Background Processing Platform for MyHospital.

Every new asynchronous capability should integrate by introducing new Workers.

As MyHospital evolves, the platform itself should remain stable while new capabilities are introduced through plugins.

The success of Taksaka is measured by its ability to execute, observe, diagnose, and recover background processing without requiring changes to the platform itself.