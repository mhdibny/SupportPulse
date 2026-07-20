# 🔐 SECURITY – The SupportPulse Fortress
### *A Zero-Trust, Defense-in-Depth Architecture for Real-Time Systems*

**Version:** 1.0  
**Scope:** ASP.NET Core 9, SignalR WebSockets, JWT, and Data Persistence

---

## Table of Contents

- [1. Introduction & Security Philosophy](#1-introduction--security-philosophy)
- [2. Layer 1: Authentication & Credential Storage](#2-layer-1-authentication--credential-storage-the-foundation)
  - [2.1. Password Hashing: Argon2id + Pepper](#21-password-hashing-argon2id--pepper)
- [3. Layer 2: Session Management & Token Strategy](#3-layer-2-session-management--token-strategy)
  - [3.1. Dual-Token Architecture (JWT + Refresh Token)](#31-dual-token-architecture-jwt--refresh-token)
  - [3.2. Silent Token Renewal (Frontend)](#32-silent-token-renewal-frontend)
- [4. Layer 3: Fortifying the WebSocket Pipeline](#4-layer-3-fortifying-the-websocket-pipeline-the-heavy-lifting)
  - [4.1. Filter 1: TokenExpirationHubFilter](#41-filter-1-tokenexpirationhubfilter)
  - [4.2. Filter 2: NonceValidationHubFilter (Anti-Replay)](#42-filter-2-noncevalidationhubfilter-anti-replay)
  - [4.3. Filter 3: SecurityStampHubFilter (Real-Time Session Invalidation)](#43-filter-3-securitystamphubfilter-real-time-session-invalidation)
  - [4.4. Filter 4: PermissionCheckerHubFilter (Fine-Grained RBAC)](#44-filter-4-permissioncheckerhubfilter-fine-grained-rbac)
- [5. Layer 4: Authorization & The In-Memory Cache](#5-layer-4-authorization--the-in-memory-cache)
- [6. Layer 5: File & Data Access Control](#6-layer-5-file--data-access-control)
  - [6.1. Secure File Download Middleware](#61-secure-file-download-middleware)
  - [6.2. Multi-Relational Access Check](#62-multi-relational-access-check-ifileaccessservice)
- [7. Layer 6: Concurrency Security (Race Conditions)](#7-layer-6-concurrency-security-race-conditions)
- [8. Summary: The Security Matrix](#8-summary-the-security-matrix)

---

## 1. Introduction & Security Philosophy

In modern web applications, **WebSockets** represent the largest attack surface. Unlike standard HTTP requests (which are stateless and short-lived), WebSockets maintain persistent, stateful connections. If an attacker compromises a WebSocket token, they effectively own a "permanent backdoor" into your system until the token naturally expires.

SupportPulse does not rely on "security by obscurity." We implement a **Defense-in-Depth** strategy, ensuring that even if one layer is breached (e.g., an XSS vulnerability steals a JWT), the subsequent layers immediately neutralize the threat.

We follow the **Zero-Trust** principle: 
> *"Never trust the client request, always validate the context and permission at the exact moment of execution."*

---

## 2. Layer 1: Authentication & Credential Storage (The Foundation)

### 2.1. Password Hashing: Argon2id + Pepper
**Implementation:** `SupportUnit.Core.Security.Password.PasswordHasher`

We do not store plaintext or weakly hashed passwords. We utilize **Argon2id**, the winner of the Password Hashing Competition, configured with memory-hard parameters to resist GPU/ASIC brute-force attacks.

**The Algorithm:**
```csharp
// Configuration from appsettings
Argon2MemoryKb = 64 * 1024  // 64 MB RAM per hash
Argon2Iterations = 3
Argon2DegreeOfParallelism = 4
HashSize = 32 bytes
```

**The "Pepper" Pattern:**
We concatenate a server-side secret (`Pepper` stored in Environment Variables) to the plaintext password *before* hashing. 
- **Why?** If an attacker compromises the database (SQL Injection), they only get the hashes and salts. Without the Pepper (which is never stored in the DB), they cannot even begin a brute-force attack, rendering the dumped data useless.

**Timing Attack Prevention:**
We strictly use `CryptographicOperations.FixedTimeEquals` for hash verification. This ensures the comparison takes a constant amount of time regardless of whether the first byte matches, preventing side-channel timing attacks.

---

## 3. Layer 2: Session Management & Token Strategy

### 3.1. Dual-Token Architecture (JWT + Refresh Token)
We decouple short-term access from long-term session persistence.

- **Access Token (JWT):** Short-lived (**15 minutes**). Contains the User ID, `SecurityStamp`, and `Nonce`. Used for API calls and initial WebSocket handshake.
- **Refresh Token:** Stored in a **`HttpOnly, Secure, SameSite=Strict`** Cookie.
  - **HttpOnly:** Inaccessible to JavaScript, completely immune to XSS attacks.
  - **Secure:** Only transmitted over HTTPS.
  - **Rotation on Use:** When a client requests a new token (`/api/token/auto-renew`), we **revoke the old Refresh Token** and issue a new one *in the same database transaction*. This prevents replay attacks; if an attacker steals a refresh token, using it invalidates the legitimate user's token, forcing them to re-authenticate and alerting them to the intrusion.

### 3.2. Silent Token Renewal (Frontend)
The frontend (`admin.js` / `chat.js`) constantly monitors the JWT expiry. 
- **Logic:** **1 minute before the token expires**, the client sends the `HttpOnly` Refresh Token to the server.
- The server validates the refresh token, issues a new JWT, and returns it in the response body, while setting a *new* HttpOnly Refresh Token in the response cookie.

**Why 15 minutes?** 
This is a deliberate trade-off: short enough to limit the damage of token theft, but long enough to avoid overwhelming the server with renewal requests during active usage.

---

## 4. Layer 3: Fortifying the WebSocket Pipeline (The "Heavy Lifting")

WebSocket connections are established via an HTTP Upgrade request. Since standard `Authorization` headers are often problematic during the WebSocket handshake in browsers, we pass the JWT via the `access_token` query string.

**The Vulnerability:** Query strings are often logged in server logs, which is a risk. **Our Defense:** The JWT is short-lived (**15 minutes**), limiting the window of opportunity. Furthermore, we apply **Four (4) consecutive security filters** on the Hub pipeline to validate every single packet.

### 4.1. Filter 1: `TokenExpirationHubFilter`
- **Execution:** Triggers immediately upon connection.
- **Logic:** Decodes the JWT and checks the `ValidTo` property against `DateTime.UtcNow`.
- **Action:** If expired, the Hub context is immediately aborted (`context.Abort()`). No further processing occurs.

### 4.2. Filter 2: `NonceValidationHubFilter` (Anti-Replay)
- **The Threat:** If a hacker intercepts the JWT via a man-in-the-middle attack, they could attempt to open their own WebSocket connection using the same token.
- **The Defense:** Every JWT contains a unique GUID claim called `Nonce`.
- **Implementation:** The server maintains an `IMemoryCache` of used Nonces (scoped to the specific Hub path). 
- **Flow:** 
  1. Client connects with `Nonce = X`.
  2. Server checks cache. If `X` exists → **Abort** (Replay attack detected).
  3. Server stores `X` in cache with a **TTL of 15 minutes**.
  4. Since the JWT expires in **15 minutes**, the Nonce TTL perfectly matches the token lifetime. This means a token cannot be replayed even within its valid window. The slight overlap ensures that even if clocks are skewed, the token is already dead before the Nonce expires.

### 4.3. Filter 3: `SecurityStampHubFilter` (Real-Time Session Invalidation)

**The Threat:**  
An admin is banned, or their permissions are downgraded, but they remain connected to the WebSocket and continue operating with old privileges.

**The Defense:**  
Every user has a `SecurityStamp` (a random GUID) stored in the SQL database. This stamp is embedded inside the JWT claim at login time.

**Execution:**  
This filter executes on **EVERY SINGLE INVOCATION** of a Hub method (e.g., `SendMessage`, `LockChat`).

**The Query:**
```csharp
string? stamp = await _db.Users
    .AsNoTracking()
    .Where(u => u.Id == userId)
    .Select(s => s.SecurityStamp)
    .SingleOrDefaultAsync();
```

**Why this is cheap:**
- **Index:** `Id` is the Primary Key → Clustered Index lookup – **O(log n)**.
- **Data size:** Only one column (GUID → **~36 bytes**) – minimal network traffic.
- **Tracking:** `AsNoTracking()` → zero Change Tracker overhead.
- **Rows touched:** Exactly **1** row.
- **Execution time:** **< 1ms** on a standard SQL Server instance.

**Cost for 1,000 requests:**  
`1,000 × 0.001s = 1 second` of total database CPU time.

**The Trade-off:**
- **Without this filter:** Banned admin stays connected for up to **15 minutes** (JWT lifetime) – a **critical security gap**.
- **With this filter:** Banned admin is disconnected **instantly** – cost: **~1 second per 1,000 requests**.

**Action:**  
If the stamps mismatch, the Hub invokes `Context.Abort()`, immediately disconnecting the user and forcing them to re-authenticate. This is **true real-time revocation**.

> **"For the cost of 1 second per 1,000 requests, we eliminate a 15‑minute security window – that's a 900x improvement for a fraction of a penny."**

### 4.4. Filter 4: `PermissionCheckerHubFilter` (Fine-Grained RBAC)
- **The Threat:** A frontend UI could be manipulated to call a Hub method the user shouldn't have access to.
- **The Defense:** Hub methods are decorated with `[HubPermissionChecker(AdminPermission.X)]`.
- **Execution:** The filter uses Reflection (cached to prevent performance hits) to read the required permission ID.
- **Logic:** It injects `IAdminUserService` and checks `UserHasPermissionAsync`. 
- **Performance:** It leverages the **In-Memory Permission Cache** (see Layer 4), meaning this check hits the `ConcurrentDictionary` and costs **O(1) without touching the database** on every call.

---

## 5. Layer 4: Authorization & The In-Memory Cache

Checking database permissions on every WebSocket packet (which could be hundreds per second) is a death sentence for performance.

**Our Solution:** `IAdminPermissionCacheService`.

- **Data Structure:** `ConcurrentDictionary<int, HashSet<int>>` mapping `PermissionId` -> `List of Admin UserIds`.
- **Build Strategy:** Rebuilt on application startup.
- **Update Strategy (Incremental):** We *never* drop the cache to rebuild it completely.
  - When an admin edits a role (`RoleService.EditRoleAsync`), we calculate the `added` and `removed` permissions.
  - We call `AddPermissionsToUser` and `RemovePermissionsToUser` directly on the cache.
  - This ensures that **permission changes are reflected in the WebSocket pipeline within milliseconds**.

> **Horizontal Scaling Note:** The current in-memory cache works perfectly for a single-instance deployment (e.g., Docker Compose). If you need to scale SupportPulse **horizontally** across multiple server instances, replace the `ConcurrentDictionary` with a distributed cache like **Redis** (using `IDistributedCache` or a Redis backplane for SignalR). This guarantees that permission updates are immediately synchronized across all nodes, maintaining consistent zero-trust authorization in a load-balanced environment.

---

## 6. Layer 5: File & Data Access Control

### 6.1. Secure File Download Middleware
The `DownloadController` handles file requests.
- **Path Traversal Prevention:** We explicitly check for `..` or `/` in the filename parameter. If found, we return `BadRequest` immediately.
- **Physical Security:** Files are stored outside the `wwwroot` directory. They cannot be accessed directly via URL. They *must* go through the authorized download endpoint.

### 6.2. Multi-Relational Access Check (`IFileAccessService`)
A user cannot download a file just because they know the GUID filename. The service checks **4 distinct paths** to ensure authorization:
1. **Is the user the sender?** (They own the file).
2. **Is the user the creator of the chat?** (They have a right to see files sent by admins).
3. **Is the user the admin who currently has the chat locked?** (They are actively managing it).
4. **Is the chat free (`LockedByAdminId == null`) AND is the user an admin assigned to this Support Category?** (If a chat is free, any admin in that category has read-access to continue the conversation).

---

## 7. Layer 6: Concurrency Security (Race Conditions)

While not traditional "security," race conditions can lead to data corruption and privilege escalation (e.g., two admins locking the same chat).

**The Solution:** `SemaphoreSlim` per `SupportCategoryId` in the `AssignChatService`.

```csharp
private static readonly ConcurrentDictionary<int, SemaphoreSlim> _categoryLocks = new();
// ...
await categoryLock.WaitAsync();
try {
    // Re-check the database state (Double-Check Locking Pattern)
    if (chat.LockedByAdminId != null) return Error; 
    // Proceed with assignment...
} finally { categoryLock.Release(); }
```
This ensures that even if 10 requests hit the server simultaneously for the same category, they are processed sequentially, guaranteeing that only one admin gets the chat.

---

## 8. Summary: The Security Matrix

| Threat Vector | Defense Mechanism | Code Reference | Time Parameters |
| :--- | :--- | :--- | :--- |
| **Database Leak (Passwords)** | Argon2id + Pepper + FixedTimeEquals | `PasswordHasher.cs` | N/A |
| **XSS (Token Theft)** | HttpOnly Cookies for Refresh Tokens | `TokenService.cs` | N/A |
| **Replay Attacks (MITM)** | JWT Nonce + Memory Cache (TTL 15 min) | `NonceValidationHubFilter.cs` | **15 min** |
| **Session Hijacking** | Short-lived JWTs + Rotation | `TokenService.cs` | **15 min JWT** |
| **Privilege Escalation** | SecurityStamp Validation (Real-time) | `SecurityStampHubFilter.cs` | Real-time |
| **Unauthorized Hub Calls** | Attribute-based RBAC + O(1) Cache | `PermissionCheckerHubFilter.cs` | Real-time |
| **File Access Bypass** | 4-Layer Relationship Check | `FileAccessService.cs` | N/A |
| **Concurrency/Data Corruption** | SemaphoreSlim Locks | `AssignChatService.cs` | N/A |

---

> **Final Security Postulate:** 
> SupportPulse treats every incoming WebSocket packet as a potential threat. By combining cryptographic hardness (Argon2id) with session-layer hardening (Nonce/Stamp) and execution-layer validation (Permission Cache), we ensure that **even an attacker with a valid JWT cannot perform malicious actions** without passing the real-time permission gauntlet. The **15-minute JWT expiry** paired with a **15-minute Nonce cache TTL** creates a tight, secure window that balances user experience with ironclad defense against replay attacks.

---

> *This document is part of SupportPulse's commitment to transparency and serves as both a security whitepaper and a practical guide for .NET developers building real-time, zero-trust systems. Built by a developer who believes that security is not an afterthought — it's the foundation.*

