# PluginRight – Human-Reviewed AI Plugin Service

## 1. Core Concept
PluginRight generates Microsoft Dynamics 365 / Dataverse plugins using AI, but every plugin is **reviewed and verified by a human expert** before delivery.

- Speed from AI + reliability from expert oversight.
- Monetization comes from delivering working plugins, with optional paid consulting for custom work.

## 2. Business Flow

### Step 1 – User Request
- User visits **pluginright.com**.
- Guided **UI/Wizard** helps define the plugin requirements:
  - Trigger conditions (Create/Update/Delete)
  - Entity/entities involved
  - Desired logic
  - Any special constraints or exceptions
- Clear *“In Scope” / “Out of Scope”* examples are shown to set expectations.

### Step 2 – AI Generation
- AI generates the requested plugin (code, csproj, props, etc.).
- Code is viewable on the site, but no download yet.

### Step 3 – Human Review Task
- An **internal task** is created for manual review.
- Reviewer checks:
  - Code compiles
  - Meets PluginRight standards
  - Matches request
- **Decision point**:
  - ✅ *Acceptable*: Trigger “Send ZIP to User” action.
  - ⚠ *Minor fixes*: Adjust manually, then approve.
  - ❌ *Beyond Scope*: Notify user with two options:
    - Cancel (no charge)
    - Approve extra consulting (normal fee + custom rate)

### Step 4 – Delivery
- User receives:
  - Verified ZIP file of the plugin.
  - Quick install guide.
- Delivery promised **within 24 hours** from request submission.

## 3. Monetization
- **Standard Plugin Generation Fee** – Flat price for in-scope requests.
- **Custom Work / Out-of-Scope Consulting** – Additional hourly or per-project fee.
- Optional **discounted retainer** for companies submitting multiple requests monthly.

## 4. Technical Scope Guidelines
**In Scope**:
- Single entity logic
- Common business rules
- Standard Dynamics operations (field updates, validation, notifications)

**Out of Scope**:
- Multi-entity complex workflows beyond standard relations
- Custom integrations with external APIs
- Large data migrations or transformations

## 5. Customer Communication
- Instant confirmation email after request submission.
- 24-hour delivery promise displayed prominently.
- Out-of-scope requests:
  - Immediate email + web notification with decision and cost options.
- Final delivery email includes:
  - ZIP file link
  - Short install instructions
  - Support contact link (paid issues only)

## 6. Internal Workflow
- All requests enter a **Task Queue** for review.
- Each task:
  1. Review generated code locally
  2. Fix small issues if needed
  3. Classify: In-Scope / Out-of-Scope
  4. Trigger delivery or escalate for consulting

## 7. Success Metrics
- Average time-to-delivery
- Acceptance rate (In-Scope vs. Out-of-Scope)
- Refund/cancellation rate
- Repeat customer rate

## 8. Future Expansion
- If AI quality improves:
  - Move toward **semi-automatic delivery** for repeat/trusted customers
  - Offer instant downloads for low-risk plugin types
- Potential **self-hosted enterprise version** for large customers