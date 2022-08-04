using DotNetDevOps.Extensions.EAVFramework;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Extensions;
using EAVFW.Extensions.Documents;
using EAVFW.Extensions.DynamicManifest;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkflowEngine;
using WorkflowEngine.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DynamicManifestExtensions
    {

        public static IEndpointRouteBuilder MapWorkFlowEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/workflows", async x =>
            {
                var workflows = x.RequestServices.GetService<IEnumerable<IWorkflow>>()
                .Select(x => new { name = x.GetType().Name, id = x.Id })
                .ToArray();
                await x.Response.WriteJsonAsync(new { value = workflows });
            });
            endpoints.MapPost("/api/entities/{entityName}/records/{recordId}/workflows/{workflowId}/runs", async (httpcontext) =>
            {
                //Run custom workflow
                var backgroundJobClient = httpcontext.RequestServices.GetRequiredService<IBackgroundJobClient>();
                var record = await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(httpcontext.Request.BodyReader.AsStream())));
                var workflowname = httpcontext.GetRouteValue("workflowId") as string;
                var inputs = new Dictionary<string, object>
                {

                    ["entityName"] = httpcontext.GetRouteValue("entityName") as string,
                    ["recordId"] = httpcontext.GetRouteValue("recordId") as string,
                    ["data"] = record

                };

                var workflows = httpcontext.RequestServices.GetRequiredService<IEnumerable<IWorkflow>>();



                var workflow = workflows.FirstOrDefault(n => n.Id.ToString() == workflowname || string.Equals(n.GetType().Name, workflowname, StringComparison.OrdinalIgnoreCase));


                if (workflow == null)
                {
                    httpcontext.Response.StatusCode = 404;
                    return;

                }


                var job = backgroundJobClient.Enqueue<IHangfireWorkflowExecutor>((executor) => executor.TriggerAsync(
                   new TriggerContext
                   {
                       Workflow = workflow,
                       Trigger = new Trigger
                       {
                           Inputs = inputs,
                           ScheduledTime = DateTimeOffset.UtcNow,
                           Type = workflow.Manifest.Triggers.FirstOrDefault().Value.Type,
                           Key = workflow.Manifest.Triggers.FirstOrDefault().Key
                       },
                   }));

                await httpcontext.Response.WriteJsonAsync(new { id = job });

            });

            return endpoints;
        }
        public static async Task<(DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>, EAVDBContext<DynamicManifestContext<TModel, TDocument>>)> GetDynamicManifestContext<TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)
       where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
         where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            var feat = serviceProvider.GetService<DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>>();
            await feat.LoadAsync(serviceProvider.GetService<DynamicContext>(), id, loadAllversions);
            var test = serviceProvider.GetService<EAVDBContext<DynamicManifestContext<TModel, TDocument>>>();

            return (feat, test);

        }

        public static Task<(DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>, EAVDBContext<DynamicManifestContext<TModel, TDocument>>)> GetDynamicManifestContext<TModel, TDocument>(this HttpContext context, Guid id)
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
          where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<TModel, TDocument>(id);

        }


        public static IServiceCollection AddDynamicManifest<TModel, TDocument>(this IServiceCollection services)

          where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
          where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            services.AddAction<PublishDynamicManifestAction<DynamicContext>>(nameof(PublishDynamicManifestAction<DynamicContext>));
            services.AddWorkflow<PublishDynamicManifestWorkflow<DynamicContext, TModel, TDocument>>();
            services.AddDynamicManifest<DynamicManifestContext<TModel, TDocument>, DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>, TModel, TDocument>();


            return services;
        }

        public static IServiceCollection AddDynamicManifest<TContext, TModel, TDocument>(this IServiceCollection services)

           where TContext : DynamicContext
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return services.AddDynamicManifest<TContext, DynamicManifestContextFeature<TContext, TModel, TDocument>, TModel, TDocument>();

        }

        public static IServiceCollection AddDynamicManifest<TContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceCollection services)
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<TContext, TModel, TDocument>
        where TContext : DynamicContext
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {

            services.AddScoped<DynamicManifestContextFeature<TContext, TModel, TDocument>>();
            services.AddScoped<IFormContextFeature<TContext>>(sp => sp.GetService<DynamicManifestContextFeature<TContext, TModel, TDocument>>());

            services.AddDbContext<TContext>((sp, optionsBuilder) =>
            {
                var config = sp.GetService<IConfiguration>();
                var connStr = config.GetValue<string>("ConnectionStrings:ApplicationDb");
                var feature = sp.GetService<DynamicManifestContextFeature<TContext, TModel, TDocument>>();

                optionsBuilder.UseSqlServer(feature.ConnectionString ?? connStr,
                    x => x.MigrationsHistoryTable("__MigrationsHistory", feature.SchemaName).EnableRetryOnFailure()
                        .CommandTimeout(180));


                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
                optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
                optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory<TDynamicManifestContextFeature, TContext, TModel, TDocument>>();



            }, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped);

            return services;
        }


    }
}