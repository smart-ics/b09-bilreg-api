# Admisi Rajal High-Density Worklist — Phase 2 Implementation Summary

**Status:** Implementation complete; production enablement depends on the Phase 1 paged-worklist contract
and standard frontend verification
**Date:** 2026-07-25
**Source roadmap:** `c012_myhospital_web/docs/_notes/admisi/admisi-rajal-high-density-worklist-implementation-roadmap.md`
**Scope:** Dense, read-only officer worklist presentation. No queue lifecycle, Registration, database, or
backend mutation contract changes.

## 1. Result

The enabled Admission Queue interface is now a localized worklist workspace rather than a fixed
one-card-per-row sidebar. It contains a workstation/service-point toolbar, a pinned Current Loket
Session strip, and a row-virtualized active worklist. The existing Registration form remains
unchanged and queue tile Preview selection does not load, modify, or save Registration data.

The legacy `SidebarAntrianPasien` remains the rollback path. The existing runtime configuration is
the only cutover boundary:

```json
"admissionQueue": {
  "enabled": true,
  "useLegacySidebarFallback": false
}
```

Set `useLegacySidebarFallback` to `true` to return to the legacy workflow after clients refresh.
No new feature flag or database migration is required.

## 2. Frontend behavior

- The workspace requests the Phase 1 `officer-worklist` page with `activeOnly=true` and a fixed
  100-entry limit. It shows `"{loaded} antrean dimuat"`; when `hasMore` is true, it adds
  `"masih ada antrean lain"` and never treats loaded items as a server total.
- Waiting and In Service entries are rendered as compact 104-pixel tiles. The grid chooses five,
  four, three, or two columns from the measured pane width, groups items into fixed-height rows,
  and virtualizes those rows with `@vueuse/core`.
- Queue Label is the tile hero. Booking/Walk-In, Waiting/Called/In Service, ownership, Priority,
  recall count, and unresolved identity have text or icon cues in addition to color.
- Tile Preview and Call/Recall are separate buttons. Selection stores only the stable
  `antrianId:noUrut` key and resolves the latest item from the refreshed worklist, so polling does
  not replace a selected object or modify Registration state.
- Tile-level Call and Recall use the existing mutations, workstation headers, conflict handling,
  invalidation, and authoritative refetch behavior. Start Processing and Return to Waiting are not
  exposed by this phase.
- The Current Loket Session remains visible even if its entry is absent from the selected service
  point or page. Its Outstanding state retains Recall; In Service is status-only until Phase 5.

## 3. Compatibility and deployment

1. Deploy the additive Phase 1 backend paging contract before enabling the dense workspace in a
   production environment.
2. The UI is selected only when `admissionQueue.enabled=true` and
   `useLegacySidebarFallback=false`; the fallback preserves the prior sidebar and Registration
   behavior.
3. Phase 2 deliberately has no append or infinite paging. A partial active page is disclosed but
   not presented as a complete queue.
4. Rollback changes only runtime configuration. Queue state and existing Call/Recall history stay
   authoritative and require no data reversal.

## 4. Verification and deferred work

Focused tests cover presentation grouping/cues, action eligibility, dense tile interaction, and
workspace state rendering. The required release verification remains targeted Vitest, frontend
type-check, production build, lint, and Full-HD/125%-zoom/tablet/mobile viewport checks.

Phase 3 will extract the Registration Assistance Panel and make Preview an explicit, comprehensive
read-only mode. Phase 4 adds Return to Waiting; Phase 5 moves Start/Resume Processing into that
panel and adds dirty-state/conflict recovery. None of those behaviors are implied by this phase.
