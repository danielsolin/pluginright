# PluginRight (generator-first)

This repo generates Dataverse plugins from templates.\
`PluginRight.Scratch` exists only to verify patterns locally. Final deliverables are the generated projects.

## Quick start
1. Build the CLI: `dotnet build src/PluginRight.Cli/PluginRight.Cli.csproj`
2. Generate a plugin project:
   ```
   dotnet run --project src/PluginRight.Cli -- \
     new plugin --name AccountCreate \
     --namespace Company.Dataverse.Plugins \
     --entity account --message Create --stage PostOperation \
     --out ./out/AccountCreate
   ```
3. Add CRM SDK package to the generated project (run on Windows):
   ```
   dotnet add ./out/AccountCreate/Plugin.csproj package Microsoft.CrmSdk.CoreAssemblies
   ```
4. Build the generated project: `dotnet build ./out/AccountCreate/Plugin.csproj`
```