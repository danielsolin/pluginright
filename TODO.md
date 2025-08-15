# PluginRight TODO

## Next
1. Use SharedVariables instead of context.Depth
```csharp
if (context.SharedVariables.ContainsKey("PluginExecuted"))
{
    return;
}
context.SharedVariables["PluginExecuted"] = true;
```

2. Generate working plugin code file

3. Generate project files

4. Zip files for download