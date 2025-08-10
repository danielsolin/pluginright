using System;
using Microsoft.Xrm.Sdk;

namespace __NAMESPACE__.Plugins
{
    /// <summary>
    /// Small convenience base: wraps Execute with try/catch and quick accessors.
    /// Keep this tiny; it is source-included into each generated project.
    /// </summary>
    public abstract class SafePluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var trace = GetTracing(serviceProvider);
            try
            {
                OnExecute(serviceProvider);
            }
            catch (InvalidPluginExecutionException)
            {
                // Let CRM-friendly exceptions bubble unchanged
                throw;
            }
            catch (Exception ex)
            {
                trace.Trace($"[PluginRight] Unhandled: {ex}");
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred in the plugin.", ex
                );
            }
        }

        protected abstract void OnExecute(IServiceProvider services);

        protected IPluginExecutionContext GetContext(IServiceProvider services)
            => (IPluginExecutionContext)services
                    .GetService(typeof(IPluginExecutionContext));

        protected ITracingService GetTracing(IServiceProvider services)
            => (ITracingService)services
                    .GetService(typeof(ITracingService));

        protected IOrganizationServiceFactory GetFactory(IServiceProvider services)
            => (IOrganizationServiceFactory)services
                    .GetService(typeof(IOrganizationServiceFactory));

        protected IOrganizationService GetService(IServiceProvider services, Guid? userId = null)
            => GetFactory(services).CreateOrganizationService(userId);
    }
}