***2025-09-04: Project put on hold because of current state of  AI/LLMs.*** 

# PluginRight – AI Assisted Plugin Generator for Microsoft Dynamics 365 / Dataverse

## 1. Core Concept
PluginRight helps generate Microsoft Dynamics 365 / Dataverse plugins with AI assistance.
As of August 2025, the models tested (GPT-5, Gemini 2.5 Pro, Claude Sonnet 3.7) can’t
consistently produce production-ready plugins. The generated code should be treated as a
starting point or boilerplate rather than a final product. In some cases, you may get a
fully functional plugin, but results vary so human review and verification remain essential.

## 2. User Flow

### Step 1 – User Request
- CLI **UI/Wizard** helps define the plugin requirements:
  - Trigger conditions (Create/Update/Delete)
  - Entity/entities involved.
  - Desired logic
  - Any special constraints or exceptions

### Step 2 – Generation
- AI generates C# logic based on input from step 1.
- PluginRight generates the requested plugin (code, csproj, props, etc.).
- Zip is generated containing all generated files.
