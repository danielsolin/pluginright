using System;
using Microsoft.Xrm.Sdk;

namespace PluginRight.Plugins
{
    public class Gemini1 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(
                typeof(ITracingService)
            );
            var context = (IPluginExecutionContext)serviceProvider.GetService(
                typeof(IPluginExecutionContext)
            );
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(
                typeof(IOrganizationServiceFactory)
            );

            if (context.Depth > 1)
                return;

            var service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target")
                && context.InputParameters["Target"] is Entity)
            {
                var entity = (Entity)context.InputParameters["Target"];
                try
                {
                    tracingService.Trace("Plugin execution started.");

                    // Check if the entity is a Contact and if the ParentCustomerId is
                    // modified. We rely on the platform to only trigger this plugin on
                    // updates.
                    if (entity.LogicalName != Contact.EntityLogicalName
                        || !entity.Contains("parentcustomerid"))
                    {
                        return;
                    }

                    // Get the new ParentCustomerId.
                    EntityReference newParentCustomerId =
                        entity.GetAttributeValue<EntityReference>("parentcustomerid");

                    // Handle null ParentCustomerId
                    if (newParentCustomerId == null)
                        return;

                    try
                    {
                        // Retrieve all contacts with the old ParentCustomerId.
                        // This assumes the old value is available in the pre-image.
                        // If not, a different approach (e.g., using the changed attributes)
                        // is needed. This is a significant performance consideration.
                        Entity preImage = ((IOrganizationServiceContext)service).PreImage;
                        if (preImage == null || !preImage.Contains("parentcustomerid"))
                            return;

                        EntityReference oldParentCustomerId =
                            preImage.GetAttributeValue<EntityReference>("parentcustomerid");

                        if (oldParentCustomerId == null
                            || oldParentCustomerId.Id == newParentCustomerId.Id)
                            return; // No change

                        // Fetch contacts with the old parent customer ID.
                        // Use paging for large datasets.
                        var query = new QueryExpression(Contact.EntityLogicalName);
                        query.Criteria.AddCondition(
                            "parentcustomerid",
                            ConditionOperator.Equal,
                            oldParentCustomerId.Id
                        );
                        query.ColumnSet = new ColumnSet("contactid");
                        // Only retrieve the necessary field.

                        var contactsToUpdate = service.RetrieveMultiple(query);

                        foreach (var contact in contactsToUpdate.Entities)
                        {
                            // Update the ParentCustomerId for each contact.
                            contact["parentcustomerid"] = newParentCustomerId;
                            service.Update(contact);
                            tracingService.Trace(
                                "Updated contact {0} with new parent customer {1}",
                                contact.Id,
                                newParentCustomerId.Id
                            );
                        }
                    }
                    catch (FaultException<OrganizationServiceFault> ex)
                    {
                        tracingService.Trace("Error updating contacts: {0}", ex.Message);
                        // Consider more sophisticated error handling, such as logging to a custom
                        // entity or using a dedicated error handling service.
                        throw new InvalidPluginExecutionException(
                            "Error updating contacts. See Plugin Trace Log for details.",
                            ex
                        );
                    }
                    catch (Exception ex)
                    {
                        tracingService.Trace("Unexpected error: {0}", ex.Message);
                        throw new InvalidPluginExecutionException(
                            "An unexpected error occurred.",
                            ex
                        );
                    }

                    tracingService.Trace("Plugin execution finished successfully.");
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    tracingService.Trace($"FaultException: {ex.Message}");
                    throw new InvalidPluginExecutionException("An error occurred.", ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace($"Exception: {ex}");
                    throw new InvalidPluginExecutionException(ex.Message, ex);
                }
            }
        }
    }
}