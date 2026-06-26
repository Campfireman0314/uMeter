---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-01-source-continuity-reconfiguration
last_verified_at: 2026-06-26
last_verified_by: opsx-archive
verification_basis: archive
applies_to:
  - src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp
superseded_by:
---

# Publish Stop Mode Before Worker Exit

**Tags:** [cpp, concurrency, lifecycle, audio]

## Symptom
Restarting a sender with the same stable handle can still emit a `close` packet if the worker thread exits before it observes the intended close-suppression mode.

## Root Cause
The controlling thread set `active` to false before publishing `sendCloseOnStop`. The worker checks its shutdown mode after leaving the loop, so a timeout or wake between those two stores can read the old default and send the packet that the restart path meant to suppress.

## Before Fix

```diff
-    if (! active.exchange (false, std::memory_order_acq_rel))
-    {
-        return;
-    }
-
-    sendCloseOnStop.store (sendClose, std::memory_order_release);
```

## After Fix

```diff
+    sendCloseOnStop.store (sendClose, std::memory_order_release);
+    if (! active.exchange (false, std::memory_order_acq_rel))
+    {
+        sendCloseOnStop.store (true, std::memory_order_release);
+        return;
+    }
```

## Key Takeaway
Publish shutdown-mode flags before waking or stopping worker threads that read them on exit.

## Source
commit: pending (2026-06-26)
