# Taksaka V2 — Architecture

Version: 1.0

---

# Purpose

This document defines the architecture of Taksaka V2.

It is the authoritative architecture specification for the repository.

All implementations MUST conform to this document.

If implementation conflicts with this document, this document has authority.

---

# Architecture Philosophy

Taksaka is **not** a Hospital Information System.

Taksaka is a generic Background Processing Platform.

The Engine executes work.

Business Modules define work.

Therefore:

Business knowledge MUST NEVER exist inside the Engine.

The Engine understands only:

- Job
- Queue
- Worker
- Scheduler
- Dispatcher
- Health
- Alert
- Plugin

Nothing else.

---

# Architecture Style

Taksaka adopts:

- Modular Monolith
- Hexagonal Architecture
- Plugin Architecture
- Pipeline Execution Architecture
- Dependency Injection
- Event-driven Background Processing

The platform is designed so it can evolve into distributed execution without redesign.

---

# High-Level Architecture

```
                Operator Console
                       │
                REST / gRPC API
                       │
               ┌──────────────────┐
               │    Taksaka Host   │
               └────────┬──────────┘
                        │
              ┌──────────────────────┐
              │   Taksaka Engine      │
              └──────────────────────┘
                        │
        ┌───────────────┼────────────────┐
        │               │                │
 Projection        Integration      Business
  Workers            Workers         Workers
        │               │                │
        └───────────────┼────────────────┘
                        │
               Infrastructure
```

---

# Design Principles

## 1.

Engine contains no business rules.

Never.

---

## 2.

Workers contain business logic.

Always.

---

## 3.

Engine discovers Workers.

Workers never register themselves manually.

---

## 4.

Everything executes through Jobs.

Never call Worker directly.

---

## 5.

Everything is observable.

Every execution generates

- log
- metrics
- duration
- execution history

---

## 6.

Everything is replayable.

Any failed Job can be replayed independently.

---

## 7.

Workers are stateless.

State belongs to Jobs.

---

## 8.

Infrastructure is replaceable.

Business logic must never depend on SQL Server, Redis, Windows Service, or HTTP.

---

# Solution Structure

```
src/

    Taksaka.Core

    Taksaka.Abstractions

    Taksaka.Engine

    Taksaka.Infrastructure

    Taksaka.Persistence

    Taksaka.Hosting

    Taksaka.ConsoleApi

plugins/

    Projection/

    Integration/

    Business/

    Maintenance/

tests/

shared/
```

---

# Project Responsibilities

## Taksaka.Core

Contains platform domain.

Allowed:

- Job
- Queue
- Worker
- Scheduler
- Dispatcher
- Alert
- Health
- ExecutionHistory

Forbidden:

- SQL
- HTTP
- File System
- Business Rules

---

## Taksaka.Abstractions

Contains interfaces only.

Examples

- IWorker
- IQueue
- IDispatcher
- IScheduler
- IHealthRule
- IAlertRule
- IPlugin

Contains no implementation.

---

## Taksaka.Engine

Contains orchestration.

Responsible for

- Dispatch
- Scheduling
- Retry
- Dead Letter
- Worker Discovery
- Execution Pipeline

Must never contain hospital business logic.

---

## Taksaka.Persistence

Responsible for storing platform state.

Examples

Jobs

Schedules

Execution History

Alerts

Configuration

Never stores hospital business entities.

---

## Taksaka.Infrastructure

Contains technical implementation.

Examples

SQL Server

Redis

Cron

SMTP

Logging

SignalR

Windows Service

REST Client

Infrastructure is replaceable.

---

## Taksaka.Hosting

Application bootstrap.

Responsible for

- Dependency Injection
- Plugin Loading
- Configuration
- Host Lifecycle

Contains no business logic.

---

## Taksaka.ConsoleApi

API consumed by Operator Console.

Console never accesses database directly.

---

# Plugin Architecture

Workers are plugins.

Plugins may be added without modifying Engine.

Example

```
plugins/

    AccountingWorker

    ProjectionWorker

    NotificationWorker

    BPJSWorker

    SatuSehatWorker
```

Plugins reference only

```
Taksaka.Abstractions
```

Plugins must never reference Engine internals.

---

# Dependency Rules

Allowed

```
Plugin

↓

Abstractions

↓

Core
```

Allowed

```
Infrastructure

↓

Abstractions
```

Allowed

```
Hosting

↓

Engine
```

Forbidden

```
Engine

↓

Plugin
```

Forbidden

```
Core

↓

Infrastructure
```

Forbidden

```
Core

↓

SQL Server
```

Forbidden

```
Worker

↓

Another Worker
```

Workers communicate only through Jobs.

---

# Job Lifecycle

```
Created

↓

Queued

↓

Dispatched

↓

Running

↓

Completed
```

or

```
Running

↓

Failed

↓

Retry Waiting

↓

Running

↓

Completed
```

or

```
Running

↓

Failed

↓

Dead Letter
```

---

# Worker Lifecycle

Worker is

Discovered

↓

Validated

↓

Registered

↓

Idle

↓

Executing

↓

Idle

Workers must never retain execution state.

---

# Execution Pipeline

Every Job executes through the same pipeline.

```
Load Job

↓

Acquire Lock

↓

Deserialize Payload

↓

Resolve Worker

↓

Execute

↓

Collect Metrics

↓

Persist History

↓

Publish Events

↓

Release Lock
```

Retry

Timeout

Logging

Metrics

Exception Handling

must be middleware.

Workers should never implement these concerns.

---

# Middleware Pipeline

Standard execution order

```
Logging

↓

Metrics

↓

Timeout

↓

Retry

↓

Transaction

↓

Worker
```

Every Job executes through this pipeline.

---

# Queue Rules

Queue is persistent.

Queue guarantees eventual execution.

Queue ordering is configurable.

Queue must support priority.

Queue implementation is replaceable.

---

# Retry Rules

Retry policy belongs to Engine.

Workers must not retry themselves.

Retry strategy must be configurable.

---

# Dead Letter

Permanent failures enter Dead Letter.

Dead Letter items are never deleted automatically.

Operator decides replay.

---

# Health Monitoring

Health checks include

Queue

Worker

Infrastructure

Execution

Scheduler

Alerts

Health checks never execute business logic.

---

# Configuration

Everything configurable.

Examples

Worker Enabled

Retry Count

Timeout

Polling Interval

Concurrency

Queue Priority

Alert Threshold

No magic numbers.

---

# Logging

Every execution produces

Correlation Id

Job Id

Worker

Duration

Status

Error

Timestamp

Logging is automatic.

Workers should log business information only.

---

# Metrics

Minimum metrics

Execution Count

Success Rate

Failure Rate

Average Duration

Retry Count

Queue Length

Worker Utilization

---

# Error Handling

Workers throw exceptions.

Engine catches exceptions.

Engine decides

Retry

Dead Letter

Alert

Workers must never swallow exceptions.

---

# Concurrency

Only one Worker may own one Job.

Job ownership is enforced by Engine.

Workers must assume concurrent execution.

Workers must therefore be stateless.

---

# Extensibility Rules

Adding a new capability should require

- new Worker

NOT

- Engine modification

Engine changes are reserved for platform evolution.

---

# Architecture Violations

The following are architecture violations.

❌ Engine references Accounting

❌ Engine references Pharmacy

❌ Engine references Registration

❌ Worker directly invokes another Worker

❌ Worker modifies Queue directly

❌ Business rules inside Infrastructure

❌ SQL inside Core

❌ HTTP inside Core

❌ Infrastructure referenced by Core

❌ Static global state

❌ Singleton mutable business objects

---

# AI Agent Rules

When implementing code:

1. Never place hospital business logic inside Engine.

2. Prefer adding a new Worker over modifying Engine.

3. Respect dependency direction.

4. Never bypass Dispatcher.

5. Never execute Worker directly.

6. Never introduce circular dependencies.

7. Keep Workers stateless.

8. Keep Infrastructure replaceable.

9. Keep Core framework-independent.

10. If uncertain where code belongs, choose the higher-level abstraction rather than leaking infrastructure into Core.

---

# Long-Term Vision

Taksaka is intended to become the universal execution platform for asynchronous processing across MyHospital.

Business capabilities evolve by introducing new Workers.

The Engine should remain stable over time.

The architecture is successful when new features are added without modifying the Engine.