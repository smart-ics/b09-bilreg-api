# Patient Tracker — Admission Queue Operations SOP

**Artifact status:** Canonical target operational specification

**Bounded context:** Patient Tracker

**Business specification:** [Admission Queue Operations Domain](./TRACKER-ADMISSION-QUEUE-DOMAIN.md)

**Bahasa Indonesia companion:** [SOP Operasi Antrean Admisi](./TRACKER-ADMISSION-QUEUE-SOP-ID.md)

**Application terminology status:** `Kiosk`, `Queue Display`, `Admission Module`, and `Call` are established by the approved operational concept. Other controls are described by their operational action because their visible UI labels have not yet been approved.

## 1. Purpose

Provide one repeatable end-to-end procedure for issuing an admission Queue Label, calling the Queue Entry to a workstation-configured Loket, starting Registration Assistance, resolving the applicable Patient Journey, recording the registration outcome, and completing the queue service.

This SOP applies to Walk-In Patients and Booking Patients whose Self-Registration requires assistance.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Visitor | Human | Chooses a locally displayed active Service Point, retains the issued Queue Label, responds to a Queue Call, and presents available evidence. |
| Admission Officer | Human | Operates the workstation-configured Loket, may select any active Service Point, calls Queue Entries, starts Registration Assistance, resolves the applicable Patient Journey, and records the registration outcome. |
| Queue Operations Supervisor | Human | Resolves authorization, no-show, display outage, and Service Point transfer exceptions that require accountable approval. |
| Kiosk | Application | Displays locally configured Service Points, requests one Queue Entry with optional ClientRequestId, shows the issued Queue Label, and reports printing status. |
| Queue Ticket Printer | Device | Prints or reprints the Queue Label issued by the Kiosk. |
| Admission Module | Application | Uses the workstation-configured Loket, presents active Service Points and the Work List, records queue actions, supports Journey Resolution and registration, and displays observable outcomes. |
| Queue Display | Application | Presents the current Queue Call and destination Loket and, when enabled, announces the call through audio. |
| Patient Tracker Queue Service | Subsystem | Provides Queue Session, Queue Entry, Queue Label, Queue Call, identity-association, and queue-state results to the participating applications. |
| Admisi Rajal Registration Service | Subsystem | Provides Journey Resolution context and the authoritative Outpatient Registration outcome. |

## 3. Preconditions

1. The required Admission Queue Operations capabilities have been released for operational use.

2. At least one active admission Service Point is present in local Kiosk configuration.

3. The Service Point is active and has a recognizable name and Queue Prefix. Its daily Queue Session may be established lazily by the first valid intake.

4. The Kiosk can request a Queue Entry and display the resulting Queue Label.

5. The Queue Ticket Printer is ready, or the approved procedure for recording and communicating an unprinted Queue Label is available.

6. The Queue Display is available for the applicable Patient waiting area, or the Queue Operations Supervisor has approved a temporary manual-calling procedure.

7. The Admission Officer has signed in to the Admission Module.

8. The Admission Module obtains a valid Loket identifier from trusted workstation configuration.

9. Every configured Loket may serve every active admission Service Point in V1.

10. The Patient or Visitor requires assisted outpatient registration as a Walk-In or after Booking Self-Registration requires assistance.

## 4. Operational Steps

### 4.1 Issue the Queue Label at the Kiosk

1. **Patient or Visitor** approaches the Kiosk and reviews the admission Service Points displayed by **Kiosk**.

2. **Patient or Visitor** chooses the applicable Service Point, such as BPJS Admission or General Admission.

3. **Kiosk** submits one queue-intake request for the chosen Service Point without supplying an authoritative Business Date and shows a processing indication.

4. **Patient Tracker Queue Service** resolves the server Business Date, verifies that the submitted ServicePointId is active, loads or lazily establishes the dedicated Queue Session for that Service Point and Business Date, atomically advances LastQueueNumber, and returns one Queue Entry with its Queue Label. When ClientRequestId is supplied, the same identifier returns the existing result.

5. **Kiosk** displays the Queue Label and sends that same Queue Label to **Queue Ticket Printer**.

6. **Queue Ticket Printer** prints the Queue Label; **Kiosk** displays whether printing succeeded.

7. **Patient or Visitor** retains the printed Queue Label, or records the Queue Label shown by **Kiosk** when an approved unprinted-ticket procedure is used.

8. **Patient or Visitor** waits in the area served by the applicable Queue Display.

### 4.2 Prepare the Admission Loket

9. **Admission Officer** opens the Admission Module and verifies the workstation-configured Loket before serving a queue.

10. **Admission Module** displays active Service Points; V1 applies no per-Loket authorization filter.

11. **Admission Officer** selects the Service Point to serve.

12. **Admission Module** displays the active enriched Admisi Rajal Work List for the selected Service Point by composing the Patient Tracker queue-only Admission Queue Worklist Projection with Booking, identity, Registration, and administrative context. Queue membership, state, call state, LoketKey, timestamps, Queue Label, Priority indicator, and optional TrackerId remain Patient Tracker truth. Priority may affect display/sorting but never auto-selects an entry.

13. **Admission Officer** verifies that no other Queue Entry is currently outstanding or In Service at the configured Loket before calling another Queue Entry.

### 4.3 Call the Patient or Visitor

14. **Admission Officer** selects one Waiting Queue Entry from the Work List and invokes **Call**.

15. **Patient Tracker Queue Service** conditionally records the Queue Call, increments CallCount, updates `BILRG_AdmLoketCurrentCall`, and increments AnnouncementVersion in the same transaction when audio is required.

16. **Admission Module** displays the successful Queue Call with the Queue Label and destination Loket.

17. After a post-commit SignalR refresh hint or periodic poll, **Queue Display** reloads current state. It presents the Queue Label and destination Loket and plays audio only for a newly observed AnnouncementVersion.

18. **Patient or Visitor** compares the announced Queue Label with the retained Queue Label and approaches the announced Loket.

19. **Admission Officer** compares the presented Queue Label with the outstanding Queue Call.

### 4.4 Start Registration Assistance

20. **Admission Officer** invokes the action for starting service only after the Patient or Visitor presents at the configured Loket.

21. **Patient Tracker Queue Service** acknowledges the outstanding Queue Call and changes the Queue Entry result from `Waiting` to `In Service`.

22. **Admission Module** displays the Queue Entry as `In Service` and removes it from the Waiting Work List.

23. **Queue Display** stops presenting the call as outstanding after a refresh or periodic poll reloads the updated current state.

### 4.5 Resolve the Patient Journey and perform Registration Assistance

24. **Admission Officer** asks the Patient or Visitor for available identity, booking, and administrative evidence.

25. **Admission Officer** identifies whether the assistance follows the Booking path or the Walk-In path.

26. For the Booking path, **Admission Officer** searches the available Journey Resolution context using the Patient-supplied evidence and reviews the returned candidate evidence.

27. For the Booking path, **Admission Officer** selects the applicable existing Booking Patient Tracker; **Admisi Rajal Registration Service** and **Patient Tracker Queue Service** display whether the Queue Entry association succeeded.

28. For the Walk-In path, **Admission Officer** searches for an applicable existing Patient Journey using the available evidence and reviews all returned candidates.

29. For the Walk-In path with an applicable existing journey, **Admission Officer** selects that Patient Tracker; **Patient Tracker Queue Service** displays whether the Queue Entry association succeeded.

30. For the Walk-In path without an applicable existing journey, **Admission Officer** continues Registration Assistance without creating a Tracker from the Queue Number alone.

31. **Admission Officer** enters or confirms the registration information required by the Admission Module and submits the registration attempt.

32. For a Walk-In registration that establishes a new journey, **Admisi Rajal Registration Service** establishes the Outpatient Registration and its Patient Tracker association; **Patient Tracker Queue Service** attaches the existing Queue Entry to that Tracker.

33. **Admisi Rajal Registration Service** persists and displays one final Registration Outcome: `Established` with RegId, or `NotEstablished` with a required ReasonCode. The outcome retains OutcomeId, QueueEntryId, Explanation when supplied, DecidedAt, and DecidedBy.

34. **Admission Officer** verifies the displayed registration outcome and completes any remaining operational communication with the Patient or Visitor.

### 4.6 Complete the queue service

35. **Admission Officer** invokes the action for completing queue service only after the final Registration Outcome has been persisted and displayed and no further Registration Assistance remains at the Loket.

36. **Patient Tracker Queue Service** changes the Queue Entry result from `In Service` to `Done`.

37. **Admission Module** displays the Queue Entry as `Done` and removes it from the active Work List.

38. **Admission Officer** verifies that the Loket no longer has an outstanding or In Service Queue Entry before calling the next Queue Entry.

## 5. Operational Exceptions

### 5.1 No Service Point is available at the Kiosk

- **Kiosk** displays that no admission service is currently available and does not issue a Queue Label.
- **Patient Tracker Queue Service** rejects intake when the submitted ServicePointId is not active. Kiosk-local offering configuration is not server authority.
- **Patient or Visitor** requests direction from an Admission Officer or Queue Operations Supervisor.
- **Queue Operations Supervisor** directs the Patient or Visitor to an available Kiosk, Service Point, or approved manual intake procedure.

### 5.2 Queue Label is issued but printing fails

- **Kiosk** continues to display the already issued Queue Label and reports the print failure.
- **Patient or Visitor** does not request another Queue Number for the same attempt.
- **Queue Operations Supervisor** or an authorized Admission Officer invokes the approved reprint action when available; **Queue Ticket Printer** prints the same Queue Label.
- If reprinting is unavailable, **Patient or Visitor** records the displayed Queue Label and follows the approved unprinted-ticket procedure.

### 5.3 Queue intake result is uncertain

- When ClientRequestId was supplied, **Kiosk** requests the result using that same identifier instead of initiating a second attempt. Without it, retry-safe recovery is not guaranteed.
- **Patient Tracker Queue Service** returns the previously issued Queue Label when the original attempt succeeded.
- **Kiosk** displays or reprints that Queue Label; **Patient or Visitor** does not obtain a duplicate Queue Number for the same attempt.

### 5.4 Loket workstation configuration is unavailable

- **Admission Module** blocks Call and Recall when LoketKey is missing or known to be duplicated.
- **Admission Officer** does not call a Queue Entry until configuration is valid.
- **Queue Operations Supervisor** corrects the controlled workstation configuration or directs the Admission Officer to another uniquely configured Loket. Renaming a PC requires a controlled configuration update.

### 5.5 Queue Entry was already called or claimed elsewhere

- **Patient Tracker Queue Service** rejects the conflicting call result.
- **Admission Module** displays that the Queue Entry is no longer available and refreshes the Work List.
- **Admission Officer** verifies the refreshed Work List and selects another available Queue Entry; the officer does not continue serving the rejected claim.

### 5.6 Patient or Visitor does not present after Call

- **Admission Officer** invokes Recall for the same Queue Entry when another call is appropriate.
- **Patient Tracker Queue Service** retains the same Queue Label, increments CallCount, updates `BILRG_AdmLoketCurrentCall`, and increments AnnouncementVersion when the Recall requires audio. No detailed Call Attempt history is retained in V1.
- **Queue Display** presents and optionally announces the Recall with the same Queue Label and Loket.
- CallCount is informational only. The system does not calculate thresholds, mark No-Show, postpone the entry, apply a “next five patients” rule, or choose the next entry.
- **Admission Officer** manually requests disposition from **Queue Operations Supervisor** according to hospital policy.
- **Queue Operations Supervisor** selects the approved disposition: retain the Queue Entry as `Waiting` for later service or conclude it as `Withdrawn`/No-Show.
- **Admission Module** displays the resulting disposition; **Admission Officer** does not mark the Queue Entry `Done` merely because the Patient or Visitor did not present.

### 5.7 Patient or Visitor chose an inapplicable Service Point

- **Admission Officer** stops before starting Registration Assistance when the mismatch is recognized while the Queue Entry is still `Waiting`.
- **Admission Officer** requests redirection to another Service Point from **Queue Operations Supervisor**.
- **Queue Operations Supervisor** approves or rejects the redirection using available evidence. On approval, the original entry receives an explicit non-active disposition and a new destination entry is created with Priority, CreationReason `Redirected`, and required SourceAntrianEntryId.
- When approved, **Patient Tracker Queue Service** displays the original Queue Entry as `Withdrawn` and issues a replacement Queue Entry and Queue Label for the applicable Service Point.
- **Admission Module** shows the Priority indicator and relationship to the original Queue Entry. Priority does not force calling order; **Admission Officer** retains selection authority and communicates the replacement Queue Label and waiting destination.

### 5.8 Queue Display or audio is unavailable

- **Queue Display** shows its unavailable condition when observable.
- **Queue Display** reloads `BILRG_AdmLoketCurrentCall` after reconnect and every configured polling interval; SignalR is only a refresh trigger.
- **Admission Officer** pauses new Queue Calls unless **Queue Operations Supervisor** has approved a temporary manual-calling procedure.
- Under an approved manual procedure, **Admission Officer** communicates the same Queue Label and Loket without changing the Queue Entry or issuing another Queue Number.
- **Queue Operations Supervisor** restores normal display-based calling when the Queue Display becomes available.

### 5.9 Booking evidence does not establish the applicable Patient Tracker

- **Admission Officer** asks for additional Patient-supplied evidence and reviews every applicable Journey Candidate.
- **Admission Officer** does not associate the Queue Entry from the Booking QR alone and does not create a replacement Patient Tracker for the Booking path.
- If the applicable Booking Patient Tracker cannot be resolved, **Admission Officer** keeps the Queue Entry `In Service` while escalating the identity-resolution exception under the approved registration procedure.

### 5.10 Tracker association conflict occurs

- **Patient Tracker Queue Service** rejects the losing association attempt and preserves the association that succeeded first.
- **Admission Module** displays the conflict and refreshes the Queue Entry and Journey Resolution context.
- **Admission Officer** verifies the refreshed association and does not automatically retry with another Patient Tracker.

### 5.11 Registration data is correctable

- **Admisi Rajal Registration Service** displays the validation problem without reporting an established Registration.
- **Admission Officer** corrects the displayed data while the Queue Entry remains `In Service` and resubmits the registration attempt.
- The validation problem does not create a Registration Outcome or OutcomeId.
- **Admission Officer** does not complete queue service until an accountable registration outcome is displayed.

### 5.12 Registration is not established after accountable resolution

- **Admission Officer** explicitly decides the final `NotEstablished` result and selects the required ReasonCode after accountable resolution; it is not inferred from an error, timeout, or absent Registration.
- **Admisi Rajal Registration Service** persists OutcomeId, QueueEntryId, Result `NotEstablished`, ReasonCode, Explanation when supplied, DecidedAt, and DecidedBy without a RegId.
- **Admisi Rajal Registration Service** displays the persisted `Registration Not Established` outcome and its available operational explanation.
- **Admission Officer** communicates the outcome and any approved next action to the Patient or Visitor.
- When no further Registration Assistance remains, **Admission Officer** completes the queue service using that OutcomeId; **Admission Module** displays the Queue Entry as `Done` without presenting an Outpatient Registration as established.
- The Queue Entry may remain Anonymous when no Patient Journey was established; the outcome and queue completion do not create one.

### 5.13 Queue Session has allocated Queue Number 9999

- **Patient Tracker Queue Service** rejects any further Queue Number allocation from that Queue Session after Queue Number 9999 has been allocated.
- **Patient Tracker Queue Service** does not wrap the counter, issue a number above 9999, change the Queue Label format, or create a second session for the same Service Point and Business Date.
- **Kiosk** displays that the selected Service Point cannot accept further intake on the current Business Date and does not automatically retry with a caller-selected date.
- **Queue Operations Supervisor** directs the Patient or Visitor to another active Service Point when operationally applicable; otherwise intake resumes only on the next server-resolved Business Date.
- Existing Queue Labels from the exhausted Queue Session remain unchanged and valid for their existing Queue Entries.

## 6. Completion Criteria

The procedure is complete when all applicable observable results are present:

1. The Patient or Visitor received one Queue Label for the selected Service Point.

2. The Queue Call displayed the same Queue Label and the destination Loket.

3. The Queue Entry changed to `In Service` only after the Patient or Visitor presented and Registration Assistance started.

4. The Booking path reused the applicable existing Booking Patient Tracker, or the Walk-In path selected an applicable existing Patient Tracker or associated the entry with the Tracker established by Registration.

5. The Admission Module displayed a persisted final Registration Outcome with stable OutcomeId: `Established` with RegId, or `NotEstablished` with ReasonCode.

6. The Queue Entry displays `Done` after Registration Assistance ends, or displays `Withdrawn` when the approved procedure ended participation before service start.

7. The completed or withdrawn Queue Entry is absent from the active Waiting and In Service Work Lists.

8. The configured Loket has no remaining outstanding or In Service Queue Entry from this procedure.

## 7. References

| Authority | Document |
|---|---|
| Admission queue business specification | [TRACKER-ADMISSION-QUEUE-DOMAIN.md](./TRACKER-ADMISSION-QUEUE-DOMAIN.md) |
| Indonesian admission queue business companion | [TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md](./TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md) |
| Parent Patient Tracker business specification | [TRACKER-DOMAIN.md](./TRACKER-DOMAIN.md) |
| Admisi Rajal business specification | [admisi-rajal-domain.md](../admisi-rajal/admisi-rajal-domain.md) |
| Operational event interpretation | [operational-events.md](../../concepts/operational-events.md) |
| Current implementation-gap evidence | [tracker-admission-queue-late-identification-gap-analysis.md](./tracker-admission-queue-late-identification-gap-analysis.md) |
| Target technical architecture | [TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md) |

The target architecture distinguishes currently implemented behavior from the missing multi-Service-Point, Loket, Kiosk, Queue Call, Queue Display, no-show, and transfer capabilities.
