# ⚙️ SMART CHAT ORCHESTRATION ENGINE – TECHNICAL WHITEPAPER
### *Fair Workload Distribution & Deadlock Prevention in Real-Time Support Systems*

**Version:** 1.0  
**Scope:** .NET 9, SignalR, Concurrent Processing, and Algorithmic Fairness

---

## Table of Contents

- [1. Introduction & The Fairness Problem](#1-introduction--the-fairness-problem)
- [2. The Core Algorithm: Weighted Scoring Formula](#2-the-core-algorithm-weighted-scoring-formula)
- [3. The Assignment Pipeline (Channel-Based Decoupling)](#3-the-assignment-pipeline-channel-based-decoupling)
- [4. The Auto-Lock Flow (Execution Sequence)](#4-the-auto-lock-flow-execution-sequence)
- [5. The Auto-Unlock Flow (The Self-Healing Sweeper)](#5-the-auto-unlock-flow-the-self-healing-sweeper)
- [6. Manual Operations & The Double-Check Locking Pattern](#6-manual-operations--the-double-check-locking-pattern)
- [7. Performance & Complexity Analysis](#7-performance--complexity-analysis)
- [8. Race Condition Defense Matrix](#8-race-condition-defense-matrix)
- [9. Configuration Reference (The Tunable Knobs)](#9-configuration-reference-the-tunable-knobs)

---

## 1. Introduction & The Fairness Problem

In traditional support systems, chat assignments are often handled via **Round-Robin** or simple **First-Come-First-Served**. These naive approaches share a critical flaw: they ignore the *current state* of the admin.

- **Admin A** might be handling 8 complex chats while **Admin B** is idle. Round-Robin will still assign the next chat to Admin A, leading to burnout and delayed responses.
- Conversely, an admin might step away from their desk while a chat remains locked, leaving the user waiting indefinitely.

**SupportPulse** solves this with a **hybrid algorithmic engine** that combines:
1. **Real-Time Scoring** – Dynamically evaluates admin fitness based on capacity, efficiency, and responsiveness.
2. **Asynchronous Queuing** – Decouples chat creation from assignment using `Channel<T>` to prevent user-side latency.
3. **Self-Healing Auto-Unlock** – Automatically recovers "zombie" chats using a background sweeper.
4. **Race Condition Immunity** – Utilizes `SemaphoreSlim` to guarantee atomicity in multi-threaded environments.

---

## 2. The Core Algorithm: Weighted Scoring Formula

At the heart of the system lies the `IScoringService`. When a chat needs to be assigned, we calculate a dynamic score for every eligible admin.

### The Mathematical Formula
```
Score = (MaxActiveChats - ActiveChats) × W₁ 
       - (EndedToday) × W₂ 
       + (IdleMinutes) × W₃
```

### Parameter Breakdown

| Variable | Description | Data Source |
| :--- | :--- | :--- |
| **MaxActiveChats** | Auto-assign capacity limit (default: 5). | `ChatAutoLockSettings` |
| **ActiveChats** | Current chats locked by this admin (where `!IsEnded`). | DB Query (Count) |
| **EndedToday** | Chats the admin completed today. Acts as a **penalty** to encourage closure. | DB Query (Count) |
| **IdleMinutes** | Minutes since the *last user message* in their active chats. | DB Query (Max Time) |
| **W₁ (Capacity)** | Weight for free capacity (default: **1000**). | `ScoreWeightCapacity` |
| **W₂ (Efficiency)** | Weight penalty for low completion (default: **10**). | `ScoreWeightEfficiency` |
| **W₃ (Idle)** | Weight bonus for being ready (default: **5**). | `ScoreWeightIdleMinutes` |

### Why These Weights?
- **W₁ (1000)** is deliberately massive. Ensuring high capacity is the **primary objective**—we want to avoid overloading an admin at all costs.
- **W₂ (10)** acts as a gentle nudge. Closing tickets is good, but we don't punish heavily because some chats take longer.
- **W₃ (5)** rewards admins who haven't received a message recently, ensuring they get fresh chats and users get quicker responses.

> ⚙️ **Fully customizable to your strategy:** Every weight, the capacity limits, and all timeout values are stored in `appsettings.json` (section `ChatAutoLockSettings`). You can fine‑tune them at any time to perfectly match your organization’s support strategy – no code changes. Just adjust the numbers and restart the application.

### Tie-Breaking
If multiple admins yield the exact same score, we use **`Guid.NewGuid()`** to select a random candidate. This prevents deterministic bias.

**Code Reference:** `ScoringService.cs`
```csharp
public double CalculateScore(int activeChats, int endedToday, double idleMinutes)
{
    return (_settings.MaxActiveChatsPerAdmin - activeChats) * _settings.ScoreWeightCapacity
           - endedToday * _settings.ScoreWeightEfficiency
           + idleMinutes * _settings.ScoreWeightIdleMinutes;
}
```

---

## 3. The Assignment Pipeline (Channel-Based Decoupling)

To ensure the user does not wait for the assignment logic to execute (which involves multiple DB queries and calculations), we use an **asynchronous queue**.

**The Flow:**
1. **User Action:** User creates a chat. The chat is saved to the DB with `LockedByAdminId = null`.
2. **Queueing:** The service writes an `AssignChatDto` into a `Channel<AssignChatDto>` (Bounded capacity: 1000).
3. **Background Processing:** `AutoAssignBackgroundService` continuously reads from this channel.
4. **Processing:** It creates a new DI scope, resolves `IAssignChatService`, and executes the assignment logic without blocking the user's HTTP/WebSocket request.

**Why This Matters:**
This pattern ensures that user-facing operations (chat creation) remain **sub-50ms**, even if the assignment logic takes 200-300ms.

**Code Reference:** `ChatService.cs` → `CreateChatAsync` & `AutoAssignBackgroundService.cs`.

---

## 4. The Auto-Lock Flow (Execution Sequence)

When `AutoAssignBackgroundService` picks a chat from the queue, `AssignChatService.AssignChatAsync` executes a **strictly atomic** process:

1. **Acquire the Category Lock:**  
   `SemaphoreSlim` for the specific `SupportCategoryId` is awaited. This ensures no two threads process assignments for the same category simultaneously.
2. **Filter Online Admins:**  
   Queries `IOnlineAdminTracker` (a `ConcurrentDictionary` updated by SignalR `OnConnected`/`OnDisconnected`) to get online admins.
3. **Filter Eligibility:**  
   Joins with `UserSupportCategories` to only include admins who belong to this specific category. Excludes the `ExcludedAdminId` (if provided, usually the previous admin who just unlocked it).
4. **Load Calculation:**  
   For each eligible admin, we query:
   - `ActiveChats` (where `LockedByAdminId == adminId` and `!IsEnded`).
   - `EndedToday` (where `LockedByAdminId == adminId` and `IsEnded` and `CreatedTime.Date == Today`).
   - `IdleMinutes` (the maximum `Time` of messages sent by the user in chats this admin locked).
5. **Scoring & Selection:**  
   The `ScoringService` calculates the score for each admin. The one with the highest score is selected.
6. **Double-Check Lock (Atomic Commit):**  
   *Crucially*, within the `SemaphoreSlim` lock, we re-query the chat entity. We verify that `LockedByAdminId` is **still null**. If another thread (perhaps a manual lock) grabbed it in the split second, we abort.
7. **Update & Dispatch:**  
   We update `LockedByAdminId`, `LockedAt`, and `ChatStatusId`. We dispatch a `ChatAutoAssigned` event to notify the assigned admin via SignalR.

---

## 5. The Auto-Unlock Flow (The Self-Healing Sweeper)

`AutoUnlockBackgroundService` runs every `UnlockCheckIntervalMinutes` (default: **1 minute**) using a `PeriodicTimer`.

**The Query Logic:**
The system identifies "zombie" chats (locked but inactive) based on the following SQL logic:

```csharp
var unlockCandidates = await db.Chats
    .Where(c => c.LockedByAdminId != null && !c.IsEnded && c.ChatStatusId == (int)ChatStatusEnum.Responding)
    .Where(c =>
        // Case 1: Last unseen user message is older than the timeout
        c.Messages
            .Where(m => m.SenderId == c.CreatorId && !m.IsSeen)
            .OrderByDescending(m => m.Time)
            .Select(m => (DateTime?)m.Time)
            .FirstOrDefault() < timeout
        ||
        // Case 2: User never sent a message, and the lock time is older than timeout
        (!c.Messages.Any(m => m.SenderId == c.CreatorId) && c.LockedAt < timeout))
    .Select(c => new { c.Id, c.SupportCategoryId, c.Subject })
    .ToListAsync();
```

**Why this logic?**
- **Case 1:** If the admin hasn't seen the user's latest message, it means they aren't paying attention.
- **Case 2:** If the user is waiting for the admin to initiate, but the admin hasn't responded and the lock is too old.

**The Healing Process:**
For each candidate found:
1. Store the `previousAdminId`.
2. Atomically set `LockedByAdminId = null` and `LockedAt = null`.
3. Dispatch `ChatAutoUnlocked` event to notify all online admins.
4. Re-queue the chat with `ExcludedAdminId = previousAdminId` into the `Channel`.

**Result:** The chat is instantly re-assigned to a **different** available admin, ensuring the user never gets ignored.

---

## 6. Manual Operations & The Double-Check Locking Pattern

### 6.1. Manual Lock (`ManualLockChatAsync`)
- **Validation:** Checks if the chat is free (`LockedByAdminId == null`).
- **Capacity Check:** Ensures the admin hasn't exceeded `MaxTotalActiveChatsPerAdmin`.
- **The Lock:** Acquires the `SemaphoreSlim` for the category.
- **The Double-Check:** Inside the lock, it re-verifies that `LockedByAdminId` is still null. This is the **critical defense** against race conditions where two admins try to lock the same free chat simultaneously.
- **Commit:** Updates the DB.

### 6.2. Manual Unlock (`ManualUnlockChatAsync`)
- **Validation:** Checks that `LockedByAdminId == adminId` (only the owner can unlock).
- **Action:** Sets `LockedByAdminId = null`.
- **Note:** *Does NOT* re-queue the chat for auto-assignment. This gives the admin the freedom to release a chat they can't handle, leaving it free for other admins to pick up manually.

---

## 7. Performance & Complexity Analysis

### Time Complexity
- **Assignment (`AssignChatAsync`):** `O(n)`, where `n` is the number of online admins in that support category.
- **Scoring Calculations:** `O(1)` per admin (simple arithmetic).
- **Database Queries:** 1 query for online admins + `n` queries for active chat counts (or optimized to 1-2 queries using EF Core grouping).

### Cost Analysis (1000 Admins Scenario)
Even if we scale to 1000 admins in a single category:
- We filter by `SupportCategoryId` early, reducing `n` drastically.
- Realistically, a support category has **< 50 admins**. 
- `50 admins × < 1ms (DB query per admin)` = **~50ms** for the assignment loop.

**Compared to the cost of a user waiting idle (5+ minutes):** This algorithm is **infinitely cheaper** in terms of user satisfaction.

### Horizontal Scaling Note
The `SemaphoreSlim` and in-memory `IOnlineAdminTracker` are designed for a single-instance deployment. To scale SupportPulse horizontally across multiple servers:
- Replace `SemaphoreSlim` with a distributed lock (e.g., **Redis RedLock**).
- Replace the in-memory online tracker with a **Redis backplane** (or SignalR’s built-in Redis scale-out).
This preserves the same atomicity and real-time visibility across all nodes.

---

## 8. Race Condition Defense Matrix

| Threat | Defense Mechanism | Code Reference |
| :--- | :--- | :--- |
| **Two admins lock the same free chat** | `SemaphoreSlim` per Category + Double-Check DB state inside lock. | `AssignChatService.cs` |
| **Auto-Unlock and Manual Lock collide** | `SemaphoreSlim` ensures sequential execution. | `AssignChatService.cs` |
| **Stale data in concurrent threads** | All writes are wrapped in `using var transaction` where needed (Ban/Unban) and `await _db.SaveChangesAsync()` ensures ACID. | `AssignChatService.cs` |
| **Excluded Admin Re-assignment** | `ExcludedAdminId` is passed in the DTO and filtered early in `AssignChatAsync`. | `AssignChatService.cs` |

---

## 9. Configuration Reference (The Tunable Knobs)

All parameters are loaded from `appsettings.json` into `ChatAutoLockSettings`.

> 🎛️ **These knobs are yours.** Every value in the table below is a configuration parameter that you can freely adjust to align with your organization’s unique support strategy – without touching a single line of code. Modify `appsettings.json`, restart the application, and the entire orchestration engine adapts instantly. Tailor weights, timeouts, and limits to exactly what your team needs.

| Parameter | Type | Default | Scientific Rationale |
| :--- | :--- | :--- | :--- |
| **MaxActiveChatsPerAdmin** | int | 5 | Prevents cognitive overload (auto-assign limit). |
| **MaxTotalActiveChatsPerAdmin** | int | 10 | Hard cap (manual + auto) to prevent edge-case overload. |
| **AutoUnlockTimeoutMinutes** | int | 5 | Standard SLA for initial response time (users expect <5 min). |
| **UnlockCheckIntervalMinutes** | double | 1 | Sweeps frequently to avoid 5+ minute delays in detection. |
| **ScoreWeightCapacity** | int | 1000 | **Primary objective:** Keep admins balanced. |
| **ScoreWeightEfficiency** | int | 10 | Encourages closing tickets but doesn't punish hard. |
| **ScoreWeightIdleMinutes** | int | 5 | Rewards readiness. Prevents "chat hoarding." |

---

> **Final Engineering Postulate:**
> This algorithm transforms a chaotic support queue into a **predictable, fair, and self-healing** system. By weighting capacity heavily, we ensure user queries land on the admin with the most time to help them, while the Auto-Unlock sweeper guarantees no user is ever truly abandoned. The use of `SemaphoreSlim` and `Channel<T>` ensures this fairness scales gracefully under heavy concurrent load, and the entire mechanism is tunable to match any organization’s operational rhythm.

---

> *This document is part of SupportPulse’s core engineering blueprint – a marriage of queuing theory, concurrency control, and real-world operational empathy. Built to be studied, adapted, and trusted in production.*

