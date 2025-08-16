# PluginRight – AI Assisted Plugin Service

## 1. Core Concept
PluginRight helps generate Microsoft Dynamics 365 / Dataverse plugins with AI assistance. As of August 2025, the models we’ve tested (GPT-5, Gemini 2.5 Pro, Claude Sonnet 3.7) can’t consistently produce production-ready plugins. The generated code should be treated as a starting point or boilerplate rather than a final product. In some cases, you may get a fully functional plugin, but results vary—human review and verification remain essential.

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
