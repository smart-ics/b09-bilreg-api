# Taksaka V2 — Architecture

Version: **2.0**

---

# Architecture Vision

Taksaka is not merely a Job Scheduler.

Taksaka is an **Operational Background Processing Platform**.

Its responsibilities are to:

* Execute background workloads
* Coordinate execution
* Allocate execution resources
* Monitor platform health
* Observe platform behavior
* Provide operational visibility
* Support diagnosis and recovery

The platform exists so Hospital IT can **understand, control and recover** background processing.

Business systems create work.

Taksaka manages its execution.

---

# Architecture Style

Taksaka adopts:

* Modular Monolith
* Hexagonal Architecture
* Plugin Architecture
* Pipeline Architecture
* Event Driven Architecture
* Resource Oriented Scheduling

The platform is designed so it can evolve into distributed execution in the future without redesign.

---

# High Level Architecture

```text
                    Browser
                       │
                SignalR + REST
                       │
        ┌────────────────────────────────┐
        │        Taksaka Server          │
        │────────────────────────────────│
        │                                │
        │ REST API                       │
        │ SignalR Hub                    │
        │ Plugin Loader                  │
        │ Configuration                  │
        │ Dependency Injection           │
        │                                │
        └───────────────┬────────────────┘
                        │
        ┌────────────────────────────────┐
        │         Taksaka Engine         │
        │────────────────────────────────│
        │                                │
        │ Scheduler                      │
        │ Queue Manager                  │
        │ Dispatcher                     │
        │ Resource Manager               │
        │ Execution Pipeline             │
        │ Retry Manager                  │
        │ Dead Letter Manager            │
        │ Health Monitor                 │
        │ Metrics                        │
        │ Event Publisher                │
        │                                │
        └───────────────┬────────────────┘
                        │
                    Workers
                        │
                Infrastructure
```

---

# Solution Structure

Worker plugins are organized by implementation, not by architectural category.

```text
plugins/

    RegistrationProjection/

    SatusehatUpload/

    AccountingJournal/

    EmailNotification/

    CacheRefresh/
```

Or, generically:

```text
plugins/

    <WorkerPlugin1>/

    <WorkerPlugin2>/

    ...
```

There are no category folders such as Projection, Integration, Business, or Maintenance.

Each plugin references only `Taksaka.Abstractions`. The Engine never references plugins.

---

# Core Runtime Components

The Engine consists of independent runtime services.

## Scheduler

Creates Jobs.

Scheduler never executes Jobs.

Example

```
Every Minute

↓

Create Health Check Job
```

---

## Queue Manager

Owns all queued Jobs.

Responsibilities

* Persistent queue
* Priority ordering
* Queue statistics
* Queue visibility

Queue Manager never executes Jobs.

---

## Dispatcher

Owns execution decisions.

Responsibilities

* Select next Job
* Resolve Worker
* Start execution
* Coordinate retry
* Publish execution events

Dispatcher never checks CPU, memory or concurrency.

Those belong to Resource Manager.

---

## Resource Manager

Resource Manager owns execution capacity.

Responsibilities

* Global concurrency
* Worker concurrency
* Resource allocation
* Resource locks
* Fair scheduling
* Starvation prevention

Dispatcher asks

```
May this Job execute?
```

Resource Manager answers

```
Yes

or

Wait
```

---

## Retry Manager

Retry belongs to Engine.

Workers never retry themselves.

Retry policy is configurable.

---

## Dead Letter Manager

Owns permanently failed Jobs.

Operators decide replay.

---

## Health Monitor

Produces platform health.

Never executes business logic.

---

# Execution Flow

```
Scheduler

↓

Create Job

↓

Queue

↓

Dispatcher

↓

Resource Manager

↓

Worker

↓

Execution Pipeline

↓

Metrics

↓

History

↓

Completed
```

Every Job follows exactly this lifecycle.

---

# Worker Philosophy

Workers are capabilities.

Workers are not schedulers.

Workers are not threads.

Workers are not services.

Workers execute one Job at a time.

Workers never

* schedule themselves
* retry themselves
* create threads
* allocate resources

Workers only execute business logic.

---

# Worker Policy

Every Worker declares an execution policy.

Example

```
Accounting Journal Worker

Priority             High

Max Concurrency      2

Retry                5

Timeout              5 minutes

Circuit Breaker      Enabled
```

Registration Projection Worker

```
Priority             Background

Max Concurrency      4

Retry                Infinite

Timeout              None
```

Dispatcher reads policy automatically.

---

# Queue Model

There is one logical queue.

Jobs have priority.

Priority determines execution order.

Example

```
Critical

High

Normal

Low

Background
```

Jobs from every source become identical once they enter Queue.

Sources include

* Scheduler
* Business Modules
* Operator Replay
* Immediate Recovery
* Retry Manager

Dispatcher does not distinguish Job origin.

---

# Immediate Execution

Immediate execution never bypasses Queue.

Instead

```
Business Module

↓

Create Critical Job

↓

Queue

↓

Dispatcher

↓

Execute
```

Immediate Jobs therefore

* appear in history
* support retry
* support replay
* generate metrics
* generate alerts

---

# Execution Pipeline

Every Job executes through identical middleware.

```
Acquire Resource

↓

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

↓

Publish Events

↓

Persist History

↓

Release Resource
```

Workers never implement these concerns.

---

# Operator Console

Operator Console is a Web Application.

It never executes business processing.

It communicates exclusively through

* REST API
* SignalR

The Console is an operational control center.

---

# Live Monitoring

The platform continuously publishes runtime events.

Examples

```
Queue Changed

Worker Started

Worker Completed

Worker Failed

Health Changed

Alert Raised

Resource Allocated
```

SignalR pushes updates immediately.

No polling required.

---

# Observable Runtime

Every runtime component is observable.

## Scheduler

Current schedules

Upcoming Jobs

Execution history

---

## Queue

Queue length

Priority distribution

Oldest Job

Estimated wait time

---

## Dispatcher

Current activity

Running Jobs

Dispatch rate

---

## Resource Manager

Global concurrency

Worker concurrency

Resource utilization

Allocation history

---

## Worker

Status

Idle

Running

Offline

Disabled

Current Job

---

## Retry

Retry count

Retry queue

Retry delay

---

## Dead Letter

Dead Letter Jobs

Failure reason

Replay history

---

# Operator Dashboards

## Queue Dashboard

Shows

```
Critical

High

Normal

Low

Background
```

Every priority displays

* Job count
* Oldest Job
* Estimated waiting time

---

## Worker Dashboard

Shows every Worker

* Running
* Idle
* Disabled
* Current Job
* Average Duration
* Failure Rate

---

## Resource Dashboard

Displays

```
Global Slots

16 / 20
```

Worker utilization

```
Registration Projection

2 / 4

Accounting Journal

1 / 2

Email Notification

8 / 16
```

---

## Dispatcher Dashboard

Displays

Current dispatch decisions

Current running Jobs

Dispatch throughput

---

## Execution Timeline

Live execution stream

```
10:01

Accounting Journal Completed

10:01

Registration Projection Started

10:02

SATUSEHAT Upload Retry

Running
```

---

## Health Dashboard

Platform

Queue

Workers

Infrastructure

Business Capability

All updated in real time.

---

# AI Agent Rules

When implementing Taksaka

Always remember

1. Scheduler creates Jobs only.
2. Queue owns pending Jobs.
3. Dispatcher decides execution.
4. Resource Manager decides capacity.
5. Workers execute business logic only.
6. Retry belongs to Engine.
7. Every Job passes through Queue.
8. Immediate Jobs never bypass Queue.
9. Every runtime component must be observable.
10. Prefer adding a Worker over modifying Engine.
11. Keep Workers stateless.
12. Engine must remain business-agnostic.
13. The Operator Console must expose the platform's runtime state, not just job lists.
14. Every new runtime feature should publish events for monitoring and SignalR updates.

---

## My Final Observation

The architecture of Taksaks V2 resembles an **operating system for background workloads**:

* **Scheduler** creates work.
* **Queue Manager** owns work.
* **Dispatcher** selects work.
* **Resource Manager** allocates execution capacity.
* **Execution Pipeline** provides reliability.
* **Workers** provide capabilities.
* **Health Monitor** evaluates operational state.
* **Operator Console** visualizes the entire runtime.

That separation of responsibilities is what will allow the platform to scale from **3 workers today** to **50+ workers in the future** without becoming difficult to reason about or maintain. I believe this is a much stronger architectural foundation than the original version and aligns well with the vision described in your domain artifact.
