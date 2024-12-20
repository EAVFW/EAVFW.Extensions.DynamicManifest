using EAVFramework;
using EAVFramework.Shared;
using EAVFW.Extensions.Documents;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DynamicManifest
{
    public class PublishDynamicManifestWorkflow<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument> : Workflow
        where TStaticContext : DynamicContext
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<TStaticContext, TDynamicContext, TModel, TDocument>
        where TDynamicContext : DynamicManifestContext<TStaticContext, TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields, new()
    {
        public static Guid CalculateId()
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"PublishDynamicManifestWorkflow-{typeof(TModel).GetCustomAttribute<EntityAttribute>().CollectionSchemaName}-{typeof(TDocument).GetCustomAttribute<EntityAttribute>().CollectionSchemaName}"));
                return new Guid(hash);
            }
        }
        public PublishDynamicManifestWorkflow()
        {
            Id = CalculateId();
            Version = "1.0";
            Manifest = new WorkflowManifest
            {
                Triggers =
                {
                    ["Trigger"] = new TriggerMetadata
                    {
                        Type = "Manual",
                        Inputs =
                        {
                            ["schema"] = new
                            {
                                type = "object",
                                properties = new Dictionary<string,object>
                                {
                                    ["recordId"] = new
                                    {
                                        title="Entity ID",
                                        type="string",
                                        description= "Please pick your record id",
                                    },
                                    ["entityName"] = new
                                    {
                                        title="Entity Type",
                                        type="string",
                                        description= "Please pick your entity type",
                                    },
                                    ["payload"] =new{
                                        type = "object",
                                        properties = new Dictionary<string,object>
                                        {
                                            ["enrichManifest"] = new
                                            {
                                                title="Should run manifest enrichment",
                                                type="boolean",
                                                description= "Should run manifest enrichment",
                                                @default = false
                                            },
                                            ["runSecurityModelInitializationScript"] = new
                                            {
                                                title="Run securitymodel intialization script",
                                                type="boolean",
                                                description= "Run securitymodel intialization script",
                                                @default = false
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                Actions =
                {
                   //[nameof(RetrieveRecordAction<TContext>)] = new ActionMetadata
                   // {
                   //     Type = nameof(RetrieveRecordAction<TContext>),
                   //     Inputs =
                   //     {
                   //         ["entityName"] = "@triggerBody()?['entityName']",
                   //         ["recordId"] = "@triggerBody()?['recordId']"
                   //     }
                   // },
                   ["PublishDynamicManifestAction"] = new ActionMetadata
                    {
                        Type = nameof(PublishDynamicManifestAction<TStaticContext, TDynamicContext, TDynamicManifestContextFeature,TModel, TDocument>),
                        Inputs =
                        {
                            ["entityName"] = "@triggerBody()?['entityName']",
                            ["recordId"] = "@triggerBody()?['recordId']",
                            ["dynamicManifestEntityCollectionSchemaName"] = typeof(TModel).GetCustomAttribute<EntityAttribute>().CollectionSchemaName,
                            ["documentEntityCollectionSchemaName"] = typeof(TDocument).GetCustomAttribute<EntityAttribute>().CollectionSchemaName,
                            ["enrichManifest"] = "@triggerPayload()?['enrichManifest']",
                            ["runSecurityModelInitializationScript"]= "@triggerPayload()?['runSecurityModelInitializationScript']",
                        }
                    },



                }
            };
        }
    }
}