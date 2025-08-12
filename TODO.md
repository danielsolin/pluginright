# PluginRight CLI — Local E2E Test

## Goal
Simulate the full flow **order → generate → review → deliver/OOS** locally with
a 24-hour SLA.

## Directory Layout
- [ ] `orders/` incoming JSON
- [ ] `work/<orderId>/` temp build
- [ ] `out/` delivered ZIPs
- [ ] `review/` notes/decisions
- [ ] `logs/` events + timings

## CLI (contract only)
- [ ] `pr order:new <file.json>` — validate & enqueue
- [ ] `pr gen:run <orderId> [--stub|--llm]` — create `work/<id>/src`
- [ ] `pr review:open <orderId>` — open summary + notes in `$EDITOR`
- [ ] `pr review:approve <orderId>` — package ZIP → `out/`
- [ ] `pr review:reject <orderId> --reason "<text>" --offer-consult <rate>`
- [ ] `pr status <orderId>` — show state/timestamps
- [ ] `pr queue` — list by SLA deadline

States: `queued → generated → under_review → approved | rejected`

## Order JSON (minimum)
- [ ] `orderId` (string)
- [ ] `customer.name`, `customer.email`
- [ ] `plugin.name`, `entity`, `triggers[]`, `stage`
- [ ] `fieldsOfInterest[]`, `logic`, `constraints[]`
- [ ] `submittedAt` (ISO)

## “Working” Definition
- [ ] Offline run with `--stub` produces compilable skeleton
- [ ] Artifacts: ZIP or rejection note
- [ ] Timestamps for each phase
- [ ] Logs with durations

## Review Checklist
- [ ] Compiles cleanly
- [ ] Registration matches order (entity, stage, steps)
- [ ] Safe queries/paging; minimal columns
- [ ] Guards (nulls, recursion, depth)
- [ ] Naming/style meets standard

Decision: **Approve** (ZIP) or **Reject/OOS** (email template + consult option)

## SLA (24h)
- [ ] On `order:new`: set `receivedAt`
- [ ] On decision: set `completedAt`
- [ ] `queue` sorts by `deadline = receivedAt + 24h`
- [ ] Flag risk after 16h elapsed

## Test Orders (examples)
- [ ] In-scope: Contact phone validation (pre-op create/update)
- [ ] In-scope: Account name change → update related Contacts’ `new_timestamp` (post-op)
- [ ] OOS: External API push (SAP) with retries
- [ ] OOS: Batch/migration over ~1M rows

## Now vs Later
**Now**
- [ ] JSON validation
- [ ] Stub generator (Plugin.cs, .csproj, props, registration.json)
- [ ] Packaging to ZIP
- [ ] State machine + logs + notes

**Later**
- [ ] `--llm` path
- [ ] Compile check hook + static lint
- [ ] Email templates
- [ ] Consult quote helper