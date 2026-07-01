# DOMAIN.md

# Taksaka V2 — Background Processing Platform

## Vision

Taksaka V2 is the centralized Background Processing Platform for MyHospital.

Its purpose is to execute asynchronous workloads reliably, monitor their execution, detect failures early, and provide operational visibility for Hospital IT (EDP).

Taksaka is not a Hospital Operational Dashboard.

Taksaka is an Operational Control Center dedicated to background processing.

---

# Problem Statement

Hospital Information Systems perform many operations that should not execute synchronously inside the user interface.

Examples:

- Accounting Journal Generation
- BPJS Integration
- SATUSEHAT Integration
- Dashboard Projection
- Legacy Projection
- Cache Refresh
- Data Repair
- Notification Delivery

Executing these operations during user interaction increases response time and degrades user experience.

Therefore these operations are executed asynchronously by Taksaka.

---

# Mission

Provide a reliable, observable, and extensible platform for executing background workloads across MyHospital.

---

# Goals

## Functional

- Execute background jobs
- Schedule recurring jobs
- Execute queued jobs
- Retry failed jobs
- Replay jobs
- Execute projection workers
- Execute integration workers
- Execute maintenance workers

## Operational

- Monitor worker health
- Monitor queue health
- Detect failures automatically
- Generate alerts
- Display execution history
- Display execution progress

## Architectural

- Plugin architecture
- Worker isolation
- Independent deployment
- Extensible without modifying the engine

---

# Non Goals

Taksaka DOES NOT:

- Perform hospital operational analytics
- Replace Business Intelligence
- Replace EMR dashboard
- Replace Admission dashboard
- Replace Bed dashboard

Those systems consume data produced by Taksaka but are not part of Taksaka.

---

# Core Concepts

## Job

A unit of work.

Examples

- Generate Accounting Journal
- Upload SEP
- Update Dashboard
- Generate Projection

Every Job has

- identity
- status
- payload
- execution history

---

## Worker

A software component capable of executing one type of Job.

Examples

- AccountingWorker
- ProjectionWorker
- SatuSehatWorker
- NotificationWorker

Workers are plugins.

The platform discovers Workers dynamically.

---

## Queue

A persistent list of pending Jobs.

The Queue guarantees that work is eventually executed.

---

## Scheduler

Creates Jobs automatically based on schedule.

Examples

Every minute

Every midnight

Every Sunday

Every 30 seconds

---

## Dispatcher

Responsible for assigning queued Jobs to the appropriate Worker.

Dispatcher knows:

Job Type

↓

Matching Worker

Dispatcher does not know business logic.

---

## Health Monitor

Continuously evaluates platform health.

Examples

Worker Offline

Queue Growing

Retry Storm

Projection Delay

Database Unreachable

---

## Alert

A warning generated when Health Rules are violated.

Alerts exist to notify operators before users experience failures.

---

## Operator Console

Desktop application used by Hospital IT.

Provides

- Monitoring
- Diagnostics
- Replay
- Retry
- Configuration

The Console never performs business processing.

It communicates with the platform.

---

# Worker Categories

## Projection

Generate DDD read models from legacy data.

Examples

Registration Projection

Billing Projection

Patient Projection

---

## Integration

Communicate with external systems.

Examples

SATUSEHAT

BPJS

LIS

RIS

---

## Business

Execute hospital business processes asynchronously.

Examples

Accounting Journal

Notification

Dashboard Update

---

## Maintenance

Maintain platform integrity.

Examples

Cleanup

Rebuild Index

Repair Data

Cache Refresh

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

# Health Model

The platform continuously evaluates:

## Queue Health

- Pending Count
- Oldest Pending
- Queue Growth

---

## Worker Health

- Running
- Idle
- Offline
- Disabled
- Error

---

## Execution Health

- Success Rate
- Failure Rate
- Retry Count
- Average Duration

---

## Infrastructure Health

- Database
- Network
- External APIs
- Disk Space

---

# Design Principles

## 1

Never block UI.

Everything that can be asynchronous should become a Job.

---

## 2

Workers are isolated.

Failure of one Worker must not stop others.

---

## 3

Workers are stateless.

State belongs to Jobs.

---

## 4

Business logic belongs inside Workers.

Engine never contains business rules.

---

## 5

Engine knows nothing about Registration, Billing or Pharmacy.

It only understands Jobs and Workers.

---

## 6

Everything is observable.

Every Job execution produces:

- log
- duration
- status
- metrics

---

## 7

Everything is replayable.

Operators can replay:

- one Job
- one aggregate
- one failed execution

without affecting unrelated Jobs.

---

# Future Direction

Taksaka is intended to become the execution platform for all asynchronous processing inside MyHospital.

As new bounded contexts are migrated to DDD, they integrate with Taksaka by providing new Workers.

The platform itself should require little or no modification.

New capability is introduced by adding Workers, not by changing the Engine.

---

# Architecture Philosophy

Business Modules

↓

Create Jobs

↓

Background Processing Platform

↓

Workers

↓

Infrastructure / External Systems

The platform executes work.

Business modules define work.

This separation keeps business concerns independent from execution concerns.
