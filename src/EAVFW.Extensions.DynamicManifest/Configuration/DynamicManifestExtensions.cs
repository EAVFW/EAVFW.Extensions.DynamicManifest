using EAVFramework;
using EAVFramework.Endpoints;
using EAVFramework.Extensions;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using WorkflowEngine;
using WorkflowEngine.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DynamicManifestExtensions
    {

       

        public static async Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)> 
            GetDynamicManifestContext<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)
            where TStaticContext : DynamicContext
            where TDynamicContext : DynamicManifestContext<TStaticContext,TModel, TDocument>
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TStaticContext,TModel>, IFormContextFeature<TDynamicContext>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            var feat = serviceProvider.GetService<TDynamicManifestContextFeature>();
            try
            {
                await feat.LoadAsync(serviceProvider.GetService<EAVDBContext<TStaticContext>>(), id, loadAllversions);
                 
            }catch(Exception ex)
            {
                throw new Exception("Failed to load", ex);
            }
          
            var test = serviceProvider.GetService<EAVDBContext<TDynamicContext>>();

            return (feat, test);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)>
            GetDynamicManifestContext<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this HttpContext context, Guid id)
           where TStaticContext : DynamicContext
            where TDynamicContext : DynamicManifestContext<TStaticContext,TModel, TDocument>
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<TStaticContext, TModel>, IFormContextFeature<TDynamicContext>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(id);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)>
           GetDynamicManifestContext<TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)
           
           where TDynamicContext : DynamicManifestContext<DynamicContext,TModel, TDocument>
           where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<DynamicContext, TModel>, IFormContextFeature<TDynamicContext>
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return serviceProvider.GetDynamicManifestContext<DynamicContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(id,loadAllversions);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<TDynamicContext>)>
            GetDynamicManifestContext<TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this HttpContext context, Guid id)
           
            where TDynamicContext : DynamicManifestContext<DynamicContext, TModel, TDocument>
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<DynamicContext,TModel>, IFormContextFeature<TDynamicContext>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<DynamicContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(id);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<DynamicManifestContext<DynamicContext,TModel, TDocument>>)>
           GetDynamicManifestContext<TDynamicManifestContextFeature, TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)

         
           where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<DynamicContext, TModel>, IFormContextFeature<DynamicManifestContext<DynamicContext,TModel, TDocument>>
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return serviceProvider.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<DynamicContext,TModel, TDocument>, TDynamicManifestContextFeature, TModel, TDocument>(id, loadAllversions);

        }

        public static Task<(TDynamicManifestContextFeature, EAVDBContext<DynamicManifestContext<DynamicContext,TModel, TDocument>>)>
            GetDynamicManifestContext< TDynamicManifestContextFeature, TModel, TDocument>(this HttpContext context, Guid id)

           
            where TDynamicManifestContextFeature : class, IExtendedFormContextFeature<DynamicContext,TModel>, IFormContextFeature<DynamicManifestContext<DynamicContext,TModel, TDocument>>
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<DynamicContext,TModel, TDocument>, TDynamicManifestContextFeature, TModel, TDocument>(id);

        }

        public static Task<(DynamicManifestContextFeature<DynamicContext,DynamicManifestContext<DynamicContext,TModel, TDocument>, TModel, TDocument>, EAVDBContext<DynamicManifestContext<DynamicContext,TModel, TDocument>>)>
          GetDynamicManifestContext< TModel, TDocument>(this IServiceProvider serviceProvider, Guid id, bool loadAllversions = false)


        
          where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
          where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return serviceProvider.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<DynamicContext,TModel, TDocument>, DynamicManifestContextFeature<DynamicContext,DynamicManifestContext<DynamicContext,TModel, TDocument>, TModel, TDocument>, TModel, TDocument>(id, loadAllversions);

        }

        public static Task<(DynamicManifestContextFeature<DynamicContext,DynamicManifestContext<DynamicContext,TModel, TDocument>, TModel, TDocument>, EAVDBContext<DynamicManifestContext<DynamicContext,TModel, TDocument>>)>
            GetDynamicManifestContext< TModel, TDocument>(this HttpContext context, Guid id)


           
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
        {
            return context.RequestServices.GetDynamicManifestContext<DynamicContext, DynamicManifestContext<DynamicContext,TModel, TDocument>, DynamicManifestContextFeature<DynamicContext,DynamicManifestContext<DynamicContext,TModel, TDocument>, TModel, TDocument>, TModel, TDocument>(id);

        }






 

        public static IServiceCollection AddDynamicManifest< TModel, TDocument>(this IServiceCollection services)

           
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
        {
            return services.AddDynamicManifest<DynamicManifestContextFeature<DynamicContext,DynamicManifestContext<DynamicContext,TModel, TDocument>, TModel, TDocument>,  TModel, TDocument>();

        }
         
        public static IServiceCollection AddDynamicManifest<TDynamicManifestContextFeature, TModel, TDocument>(this IServiceCollection services)

        where TDynamicManifestContextFeature : DynamicManifestContextFeature<DynamicContext,DynamicManifestContext<DynamicContext,TModel, TDocument>, TModel, TDocument>
         where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
         where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
        {
            return services.AddDynamicManifest<DynamicContext,DynamicManifestContext<DynamicContext,TModel, TDocument>, TDynamicManifestContextFeature, TModel, TDocument>();

        }

        public static IServiceCollection AddDynamicManifest<TDynamicContext, TDynamicManifestContextFeature,TModel, TDocument>(this IServiceCollection services)
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<DynamicContext,TDynamicContext, TModel, TDocument>
        where TDynamicContext : DynamicManifestContext<DynamicContext,TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
        {
            return services.AddDynamicManifest<DynamicContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>();

        }

        public static IServiceCollection AddDynamicManifest<TStaticContext,TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(this IServiceCollection services)
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<TStaticContext, TDynamicContext, TModel, TDocument>
        where TDynamicContext : DynamicManifestContext<TStaticContext,TModel, TDocument>
         where TStaticContext: DynamicContext
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
        {
            services.AddAction<PublishDynamicManifestAction<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>>("PublishDynamicManifestAction");
            services.AddWorkflow<PublishDynamicManifestWorkflow<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>>();

            services.AddScoped<TDynamicManifestContextFeature>();
            services.AddScoped<IExtendedFormContextFeature<TStaticContext, TModel>>(sp => sp.GetService<TDynamicManifestContextFeature>());
            services.AddScoped<IFormContextFeature<TDynamicContext>>(sp => sp.GetService<TDynamicManifestContextFeature>());

            services
                .TryAddSingleton<IDynamicManifestContextOptionFactory<TStaticContext, TDynamicContext, TModel, TDocument>,
                    DefaultDynamicManifestContextOptionFactory<TStaticContext, TDynamicContext, TModel, TDocument>>();

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
                optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory< TStaticContext, TDynamicContext,TDynamicManifestContextFeature, TModel, TDocument>>();



            }, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped);

            return services;
        }


    }
}