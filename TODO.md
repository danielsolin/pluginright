# PluginRight CLI — Local E2E Test

## TODO Next
1. Use SharedVariables instead of context.Deptch
    if (context.SharedVariables.ContainsKey("PluginExecuted"))
    {
        return;
    }
    context.SharedVariables["PluginExecuted"] = true;

2. Generate working plugin code file

3. Generate project files

4. Zip files for download

## Directory Layout
- [ ] `orders/` incoming JSON
- [ ] `work/<orderId>/` temp build
- [ ] `out/` delivered ZIPs
- [ ] `review/` notes/decisions
- [ ] `logs/` events + timings

## Order JSON (minimum)
- [ ] `orderId` (string)
- [ ] `customer.name`, `customer.email`
- [ ] `plugin.name`, `entity`, `triggers[]`, `stage`
- [ ] `fieldsOfInterest[]`, `logic`, `constraints[]`
- [ ] `submittedAt` (ISO)