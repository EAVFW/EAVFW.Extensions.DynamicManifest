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
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
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

        where TDynamicManifestContextFeature : DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument>
         where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
         where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
        {
            return services.AddDynamicManifest<DynamicManifestContext<TModel, TDocument>, TDynamicManifestContextFeature, TModel, TDocument>();

        }

        public static IServiceCollection AddDynamicManifest<TDynamicContext, TDynamicManifestContextFeature,TModel, TDocument>(this IServiceCollection services)
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<TDynamicContext, TModel, TDocument>
        where TDynamicContext : DynamicManifestContext<TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
        {
            return services.AddDynamicManifest<DynamicContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>();

        }

        public static IServiceCollection AddDynamicManifest<TStaticContext,TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceCollection services)
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<TDynamicContext, TModel, TDocument>
        where TDynamicContext : DynamicManifestContext<TModel, TDocument>
         where TStaticContext: DynamicContext
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
        {
            services.AddAction<PublishDynamicManifestAction<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>>("PublishDynamicManifestAction");
            services.AddWorkflow<PublishDynamicManifestWorkflow<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>>();

            services.AddScoped<TDynamicManifestContextFeature>();
            services.AddScoped<IExtendedFormContextFeature<TModel>>(sp => sp.GetService<TDynamicManifestContextFeature>());
            services.AddScoped<IFormContextFeature<TDynamicContext>>(sp => sp.GetService<TDynamicManifestContextFeature>());


            services.AddDbContext<TDynamicContext>((sp, optionsBuilder) =>
            {
                var config = sp.GetService<IConfiguration>();
                var connStr = config.GetValue<string>("ConnectionStrings:ApplicationDb");
                var feature = sp.GetRequiredService<TDynamicManifestContextFeature>();

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