# PluginRight v1 – Step 1 Plan

## 1. Login / Registration
- Frontend in e.g. Next.js (hosted on Vercel for quick setup).
- Auth via Google and LinkedIn (Auth0 or Firebase Auth → Postgres/Supabase).
- Simple user profile:
  - ID
  - Name / email
  - Number of available plugin generations (default = 1 free)
  - History (date, input, generated code)

## 2. Free Plugin Generation
- API endpoint `/generate` (Python + OpenAI integration).
- Accepts:
  - Name / namespace
  - Description of logic
  - Step type (Create, Update, Delete)
  - Entity name
- Returns:
  - `.zip` containing:
    - `.csproj` (4.6.2 targeting)
    - Plugin class(es)
    - Minimal README (“how to compile”)
- Backend logs that the free token has been used.

## 3. Payment Solution
- Stripe integration (Checkout/Payments API).
- Packages:
  - **1 generation** – $X
  - **5 generations** – $Y
  - **Unlimited for 1 month** – $Z
- On payment: increase the user's token balance in the database.

## 4. Hosting
- **Frontend:** Vercel/Netlify.
- **Backend:** Railway/Fly.io/Azure Functions.
- **DB:** Supabase (Postgres + Auth sync).
- SSL and `pluginright.com` points to frontend.

---

✅ **Roadmap item:** *“Deliver compiled, signed DLL”* goes into **v2**, when we build the Windows build infrastructure.

---

## 5. Validation & Spec Refinement (v1)
**Goal:** Turn messy natural language into a clear, buildable plugin spec — or politely refuse.

### 5.1 Guided input (reduce ambiguity)
Use a guided form plus an optional free-text field:
- **Trigger:** Create / Update / Delete / Retrieve / Associate / Disassociate
- **Entity:** (autocomplete when we later pull Dataverse metadata; free text in v1)
- **Stage:** PreValidate / PreOperation / PostOperation
- **Execution:** Synchronous / Asynchronous (PostOperation)
- **Conditions:** field changes, attribute filters, simple comparisons
- **Actions:** set fields, throw exception, simple branching, optional HTTP (paid tier later)
- **Isolation:** Sandbox (default)

### 5.2 Multi-layer validation
- **Syntactic:** required fields, enum ranges, string limits.
- **Semantic:** incompatible combos (e.g., Update + PreValidate writing to target), missing/unknown attributes (when metadata available).
- **Safety/policy:** block secrets, unrestricted HTTP, file I/O, infinite loops, profanity/abuse.
- **Complexity guardrails:** compute a score; free tier allows only simple single-step flows.

Return a machine-readable result:
```json
{
  "ok": false,
  "score": 46,
  "errors": [{"code": "UNSUPPORTED_DOMAIN", "msg": "Only Microsoft Dataverse plugins are supported."}],
  "suggestions": [
    "Pick a Dataverse entity (e.g., account).",
    "Select a trigger (Create/Update)."
  ],
  "clarifying_questions": [
    "Which entity should this run on?",
    "Which field(s) must change to trigger it?"
  ],
  "normalized_spec": null
}
```

### 5.3 Auto-refinement loop (wizard UX)
If `ok == false` but fixable, present one clarifying question at a time and offer **Quick-Fix** buttons derived from `suggestions`. When enough info is present, produce a normalized spec (our DSL).

### 5.4 Normalized spec (our DSL)
Example (YAML):
```yaml
plugin:
  name: Contoso.UpdateAccountCreditLimit
  targetFramework: net462
  isolationMode: Sandbox
  steps:
    - message: Update
      entity: account
      stage: PreOperation
      execution: Synchronous
      conditions:
        - fieldChanged: creditlimit
      actions:
        - setField: { name: creditonhold, value: true }
        - if:
            when: "${context.Depth} > 1"
            then:
              - throw: "Prevent recursive update"
```

### 5.5 Minimal JSON Schema for the DSL (v1)
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "PluginRight DSL v1",
  "type": "object",
  "required": ["plugin"],
  "properties": {
    "plugin": {
      "type": "object",
      "required": ["name", "targetFramework", "isolationMode", "steps"],
      "properties": {
        "name": { "type": "string", "minLength": 1 },
        "targetFramework": { "type": "string", "enum": ["net462"] },
        "isolationMode": { "type": "string", "enum": ["Sandbox"] },
        "steps": {
          "type": "array",
          "minItems": 1,
          "items": {
            "type": "object",
            "required": ["message", "entity", "stage", "execution", "actions"],
            "properties": {
              "message": {
                "type": "string",
                "enum": ["Create","Update","Delete","Retrieve","Associate","Disassociate"]
              },
              "entity": { "type": "string", "minLength": 1 },
              "stage": { "type": "string", "enum": ["PreValidate","PreOperation","PostOperation"] },
              "execution": { "type": "string", "enum": ["Synchronous","Asynchronous"] },
              "conditions": {
                "type": "array",
                "items": {
                  "type": "object",
                  "oneOf": [
                    { "properties": { "fieldChanged": { "type": "string" } }, "required": ["fieldChanged"], "additionalProperties": false },
                    { "properties": { "fieldEquals": { "type": "object", "required": ["name","value"], "properties": { "name": {"type":"string"}, "value": {} }, "additionalProperties": false } }, "required": ["fieldEquals"], "additionalProperties": false }
                  ]
                }
              },
              "actions": {
                "type": "array",
                "minItems": 1,
                "items": {
                  "type": "object",
                  "oneOf": [
                    { "properties": { "setField": { "type": "object", "required": ["name","value"], "properties": { "name": {"type":"string"}, "value": {} }, "additionalProperties": false } }, "required": ["setField"], "additionalProperties": false },
                    { "properties": { "throw": { "type": "string" } }, "required": ["throw"], "additionalProperties": false },
                    { "properties": {
                        "if": {
                          "type": "object",
                          "required": ["when","then"],
                          "properties": {
                            "when": { "type": "string" },
                            "then": { "type": "array", "items": { "type": "object" } }
                          },
                          "additionalProperties": false
                        }
                      },
                      "required": ["if"], "additionalProperties": false
                    }
                  ]
                }
              }
            },
            "additionalProperties": false
          }
        }
      },
      "additionalProperties": false
    }
  },
  "additionalProperties": false
}
```

### 5.6 Implementation notes
- **Server:** `/validate` builds `ValidationResult`; `/generate` only accepts a valid DSL + `validationHash`.
- **Client:** React wizard (3–5 steps), inline errors, Quick-Fix buttons.
- **Storage:** Persist raw input, validation results, normalized spec, and a short audit trail.
- **Telemetry:** Track rejection causes and questions asked to improve the wizard.