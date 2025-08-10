using System;
using Microsoft.Xrm.Sdk;

namespace PluginRight.Scratch.Plugins.Sample
{
    /// <summary>
    /// Minimal example that just traces the invocation. No hard-coded filters.
    /// </summary>
    public sealed class HelloWorldPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var ctx = (IPluginExecutionContext)serviceProvider.GetService(
                typeof(IPluginExecutionContext)
            );

            var trace = (ITracingService)serviceProvider.GetService(
                typeof(ITracingService)
            );

            trace.Trace($"[Scratch] HelloWorld fired. Message={ctx.MessageName}; Stage={ctx.Stage}; Correlation={ctx.CorrelationId}");
        }
    }
}