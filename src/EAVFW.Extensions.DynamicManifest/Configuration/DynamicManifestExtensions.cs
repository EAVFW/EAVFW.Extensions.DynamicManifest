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

        public static async Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)> 
            GetDynamicManifestContext<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)
            where TStaticContext : DynamicContext
            where TDynamicContext : DynamicManifestContext<TModel, TDocument>
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TModel>, IFormContextFeature<TDynamicContext>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            var feat = serviceProvider.GetService<TDynamicManifestContextFeature>();
            await feat.LoadAsync(serviceProvider.GetService<TStaticContext>(), id, loadAllversions);
            var test = serviceProvider.GetService<EAVDBContext<TDynamicContext>>();

            return (feat, test);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)>
            GetDynamicManifestContext<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this HttpContext context, Guid id)
           where TStaticContext : DynamicContext
            where TDynamicContext : DynamicManifestContext<TModel, TDocument>
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TModel>, IFormContextFeature<TDynamicContext>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(id);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)>
           GetDynamicManifestContext<TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)
           
           where TDynamicContext : DynamicManifestContext<TModel, TDocument>
           where TDynamicManifestContextFeature : class, IExtendedFormContextFeature< TModel>, IFormContextFeature<TDynamicContext>
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return serviceProvider.GetDynamicManifestContext<DynamicContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(id,loadAllversions);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)>
            GetDynamicManifestContext<TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this HttpContext context, Guid id)
           
            where TDynamicContext : DynamicManifestContext<TModel, TDocument>
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature< TModel>, IFormContextFeature<TDynamicContext>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<DynamicContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(id);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<DynamicManifestContext<TModel, TDocument>>)>
           GetDynamicManifestContext<TDynamicManifestContextFeature, TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)

         
           where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TModel>, IFormContextFeature<DynamicManifestContext<TModel, TDocument>>
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return serviceProvider.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<TModel, TDocument>, TDynamicManifestContextFeature, TModel, TDocument>(id, loadAllversions);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<DynamicManifestContext<TModel, TDocument>>)>
            GetDynamicManifestContext< TDynamicManifestContextFeature, TModel, TDocument>(this HttpContext context, Guid id)

           
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TModel>, IFormContextFeature<DynamicManifestContext<TModel, TDocument>>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<TModel, TDocument>, TDynamicManifestContextFeature, TModel, TDocument>(id);

        }

        public static Task<(DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>, EAVDBContext<DynamicManifestContext<TModel, TDocument>>)>
          GetDynamicManifestContext< TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)


        
          where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
          where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return serviceProvider.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<TModel, TDocument>, DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>, TModel, TDocument>(id, loadAllversions);

        }

        public static Task<(DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>, EAVDBContext<DynamicManifestContext<TModel, TDocument>>)>
            GetDynamicManifestContext< TModel, TDocument>(this HttpContext context, Guid id)


           
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<TModel, TDocument>, DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>, TModel, TDocument>(id);

        }






 

        public static IServiceCollection AddDynamicManifest< TModel, TDocument>(this IServiceCollection services)

           
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return services.AddDynamicManifest<DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>,  TModel, TDocument>();

        }
        //public static IServiceCollection AddDynamicManifest<TDynamicContext, TModel, TDocument>(this IServiceCollection services)

        //  where TDynamicContext : DynamicManifestContext<TModel, TDocument>
        //  where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        //  where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        //{
        //    return services.AddDynamicManifest<DynamicContext, TDynamicContext, DynamicManifestContextFeature<TDynamicContext, TModel, TDocument>, TModel, TDocument>();

        //}
        public static IServiceCollection AddDynamicManifest<TDynamicManifestContextFeature, TModel, TDocument>(this IServiceCollection services)

        where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TModel>, IFormContextFeature<DynamicManifestContext<TModel, TDocument>>
         where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
         where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return services.AddDynamicManifest<DynamicManifestContext<TModel, TDocument>, TDynamicManifestContextFeature, TModel, TDocument>();

        }

        public static IServiceCollection AddDynamicManifest<TDynamicContext, TDynamicManifestContextFeature,TModel, TDocument>(this IServiceCollection services)
        where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TModel>, IFormContextFeature<TDynamicContext>
        where TDynamicContext : DynamicManifestContext<TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return services.AddDynamicManifest<DynamicContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>();

        }

        public static IServiceCollection AddDynamicManifest<TStaticContext,TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceCollection services)
        where TDynamicManifestContextFeature : class,IExtendedFormContextFeature<TModel>, IFormContextFeature<TDynamicContext>
        where TDynamicContext : DynamicManifestContext<TModel, TDocument>
         where TStaticContext: DynamicContext
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            services.AddAction<PublishDynamicManifestAction<DynamicContext>>(nameof(PublishDynamicManifestAction<DynamicContext>));
            services.AddWorkflow<PublishDynamicManifestWorkflow<DynamicContext, TModel, TDocument>>();

            services.AddScoped<TDynamicManifestContextFeature>();
            services.AddScoped<IExtendedFormContextFeature<TModel>>(sp => sp.GetService<TDynamicManifestContextFeature>());
            services.AddScoped<IFormContextFeature<TDynamicContext>>(sp => sp.GetService<TDynamicManifestContextFeature>());


            services.AddDbContext<TDynamicContext>((sp, optionsBuilder) =>
            {
                var config = sp.GetService<IConfiguration>();
                var connStr = config.GetValue<string>("ConnectionStrings:ApplicationDb");
                var feature = sp.GetService<DynamicManifestContextFeature<TDynamicContext, TModel, TDocument>>();

                optionsBuilder.UseSqlServer(feature.ConnectionString ?? connStr,
                    x => x.MigrationsHistoryTable("__MigrationsHistory", feature.SchemaName).EnableRetryOnFailure()
                        .CommandTimeout(180));


                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
                optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
                optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory< TDynamicContext,TDynamicManifestContextFeature, TModel, TDocument>>();



            }, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped);

            return services;
        }


    }
}