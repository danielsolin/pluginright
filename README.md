# PluginRight – AI Assisted Plugin Service

## 1. Core Concept
PluginRight generates Microsoft Dynamics 365 / Dataverse plugins with AI-assistance. It is a free tool used to attract customers for plugin development consulting.

## 2. User Flow

### Step 1 – User Request
- User visits **pluginright.com**.
- Guided **UI/Wizard** helps define the plugin requirements:
  - Trigger conditions (Create/Update/Delete)
  - Entity/entities involved:
  - Desired logic
  - Any special constraints or exceptions

### Step 2 – Generation
- AI generates C# logic based on input from step 1.
- Site logic generates the requested plugin (code, csproj, props, etc.).
- Code is viewable on the site, and a full plugin-project can be downloaded as a .zip.
- (Project can be compiled on pluginright.com)