# Taksaka V2 — Architecture

Version: **2.0**

---

# Purpose

This document defines the architecture of Taksaka V2.

It is the authoritative architecture specification for the repository.

All implementations MUST conform to this document.

If implementation conflicts with this document, this document has authority.

---

# Vision

Taksaka is a Background Processing Platform.

It executes work.

It does not define work.

Business capabilities evolve independently by introducing new Worker Plugins.

The platform itself should remain stable.

---

# Architecture Philosophy

Taksaka is **not** a Hospital Information System.

Taksaka is **not** a Business Framework.

Taksaka is an execution platform.

The platform understands only:

* Job
* Queue
* Worker
* Scheduler
* Dispatcher
* Health
* Alert
* Plugin

Nothing else.

Hospital business knowledge belongs entirely inside Worker Plugins.

---

# Core Principle

> **The Worker Plugin is the deployment unit, development unit, ownership unit, and extension unit of the platform.**

Everything required to execute a business capability belongs inside its Worker Plugin.

---

# Architecture Style

Taksaka adopts:

* Modular Monolith
* Plugin Architecture
* Pipeline Execution
* Event-driven Background Processing

The platform is designed to support hundreds of independent Worker Plugins without requiring platform modification.

---

# High-Level Architecture

```
                 Operator Console
                        │
                 REST / gRPC API
                        │
               ┌──────────────────┐
               │   Taksaka Host    │
               └────────┬──────────┘
                        │
               ┌──────────────────┐
               │  Taksaka Engine   │
               └────────┬──────────┘
                        │
          discovers Worker Plugins
                        │
        ┌───────────────┼────────────────┐
        │               │                │
     Worker A       Worker B        Worker C
        │               │                │
        └───────────────┼────────────────┘
                        │
            Database / APIs / Files
```

---

# Design Principles

## 1.

Engine contains no business rules.

Never.

---

## 2.

Workers contain business rules.

Always.

---

## 3.

Workers are Plugins.

They are discovered automatically.

They are never registered manually.

---

## 4.

Everything executes through Jobs.

Workers are never invoked directly.

---

## 5.

Workers are isolated.

Each Worker owns its implementation.

Workers do not share business code.

---

## 6.

Workers are stateless.

Execution state belongs to Jobs.

---

## 7.

Platform projects remain stable.

Business capabilities evolve through Worker Plugins.

---

## 8.

Adding a new Worker must never require modifying the platform.

---

# Solution Structure

```
src/

    Taksaka.Core

    Taksaka.Abstractions

    Taksaka.Engine

    Taksaka.Hosting

    Taksaka.Persistence

    Taksaka.Infrastructure

    Taksaka.ConsoleApi

plugins/

    Accounting/

    SatuSehat/

    Dashboard/

    Notification/

    Maintenance/

tests/
```

---

# Platform Responsibilities

## Taksaka.Core

Contains platform domain only.

Examples

* Job
* Queue
* Scheduler
* Dispatcher
* ExecutionHistory
* Health

Never contains:

* SQL
* Hospital rules
* Business entities

---

## Taksaka.Abstractions

Contains stable platform contracts only.

Examples

* IWorker
* IJobContext
* ILogger
* IPlugin

Never contains

* Worker DTO
* Worker Repository
* Worker Options
* Worker SQL
* Business Model

---

## Taksaka.Engine

Responsible for

* Scheduling
* Dispatching
* Retry
* Timeout
* Pipeline
* Worker Discovery

Contains no hospital business logic.

---

## Taksaka.Persistence

Stores platform state only.

Examples

* Jobs
* Queue
* History
* Alerts
* Scheduler

Never stores hospital business entities.

---

## Taksaka.Infrastructure

Reusable technical services only.

Examples

* Logging
* SMTP
* HTTP
* Redis
* SQL Connection Factory
* Metrics

Never contains

* Business SQL
* Business Repository
* Business DTO
* Business Mapping

---

## Taksaka.Hosting

Application bootstrap.

Responsible for

* Plugin Loading
* Host Lifecycle
* Engine Startup

Contains no Worker registration.

---

## Taksaka.ConsoleApi

Provides operator APIs.

Never contains business processing.

---

# Worker Plugin

A Worker Plugin is completely self-contained.

Typical structure:

```
plugins/

    RegistrationProjection/

        RegistrationProjection.csproj

        Worker/

        Repository/

        Queries/

        Models/

        Configuration/

        Helpers/

        Tests/

        README.md

        plugin.json
```

The Worker owns everything required to execute its business capability.

---

# Worker Ownership

Each Worker owns:

* Business Logic
* Repository
* SQL
* DTO
* Model
* Mapping
* Configuration
* Validation
* Tests
* Documentation

Nothing is shared unless it is truly platform infrastructure.

---

# Plugin Manifest

Every Worker Plugin provides metadata.

Example

```
Name

Version

Worker Type

Description

Dependencies

Configuration
```

The platform discovers this automatically.

---

# Worker Discovery

The Host scans the plugin directory.

```
plugins/

    *.dll
```

For every assembly:

```
Load Assembly

↓

Find IWorker

↓

Validate

↓

Register

↓

Ready
```

No manual registration is permitted.

---

# Configuration

Worker configuration belongs to the Worker.

Platform configuration belongs to the Platform.

Worker configuration is never added to platform projects.

---

# Dependency Rules

Allowed

```
Worker

↓

Taksaka.Abstractions
```

Allowed

```
Worker

↓

Platform Infrastructure Services
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

Worker
```

Forbidden

```
Worker

↓

Another Worker
```

Forbidden

```
Worker

↓

Platform Business Code
```

Forbidden

```
Platform

↓

Worker-specific Repository
```

Forbidden

```
Platform

↓

Worker-specific DTO
```

---

# Extensibility Rules

Adding a new business capability requires:

* New Worker Plugin

It must not require:

* Engine modification
* Hosting modification
* Infrastructure modification
* Persistence modification
* Abstractions modification
* ConsoleApi modification

If one of these projects must change, the implementation is introducing a platform capability rather than a Worker.

---

# Worker Lifecycle

```
Plugin discovered

↓

Validated

↓

Loaded

↓

Idle

↓

Executing

↓

Idle
```

Workers never retain execution state.

---

# Execution Pipeline

Every Job executes through:

```
Load Job

↓

Acquire Lock

↓

Resolve Worker

↓

Execute

↓

Collect Metrics

↓

Persist History

↓

Release Lock
```

Retry, timeout, metrics, logging, transactions, and exception handling are platform responsibilities.

---

# Architecture Invariants

The following statements are always true.

1. A Worker Plugin is self-contained.

2. A Worker owns all of its business implementation.

3. The platform owns only execution.

4. Platform projects remain stable.

5. Business capabilities evolve through Plugins.

6. Worker Plugins are independently deployable.

7. Creating a new Worker modifies only the Worker Plugin project.

8. Copying a new Worker Plugin into the `plugins` directory is sufficient to extend the platform.

---

# Architecture Violations

The following are violations.

❌ Adding DTOs to `Taksaka.Abstractions` for a Worker.

❌ Adding repositories to `Taksaka.Infrastructure`.

❌ Adding SQL to platform projects.

❌ Adding Worker Options to platform projects.

❌ Editing Hosting to register a Worker.

❌ Editing Engine because a new Worker is introduced.

❌ Worker directly invoking another Worker.

❌ Business rules inside Engine.

❌ Business rules inside Infrastructure.

❌ Platform projects changing because a Worker is added.

---

# AI Agent Rules

When implementing code:

1. Treat every Worker as an independent plugin.

2. Keep every Worker self-contained.

3. Never add Worker-specific code to platform projects.

4. Never modify Hosting to register a Worker.

5. Never modify Infrastructure for Worker-specific repositories or SQL.

6. Never add Worker DTOs or Options to `Taksaka.Abstractions`.

7. Never modify Engine when introducing a new Worker.

8. If implementing a Worker requires modifying another project, stop and explain why.

9. Platform projects may change only when introducing a new platform capability.

10. The success criterion is simple:

> **A new Worker can be added by copying its plugin into the `plugins` directory without modifying any existing platform project.**

---

# Long-Term Vision

Taksaka becomes a stable execution platform.

The Engine changes rarely.

Platform capabilities evolve deliberately.

Business capabilities evolve continuously by introducing new Worker Plugins.

The architecture is successful when hundreds of Workers can coexist while the platform itself remains largely unchanged.
