using System;
using Microsoft.Xrm.Sdk;

namespace __NAMESPACE__.Plugins
{
    /// <summary>__DESCRIPTION__</summary>
    public sealed class __CLASS_NAME__ : SafePluginBase
    {
        protected override void OnExecute(IServiceProvider services)
        {
            var ctx = GetContext(services);
            var trace = GetTracing(services);

            // Message: __MESSAGE__  Stage: __STAGE__  Entity: __ENTITY__
            trace.Trace($"[PluginRight] __CLASS_NAME__ fired. Message={ctx.MessageName}; Stage={ctx.Stage}; Correlation={ctx.CorrelationId}");

            // TODO: generated logic here
        }
    }
}