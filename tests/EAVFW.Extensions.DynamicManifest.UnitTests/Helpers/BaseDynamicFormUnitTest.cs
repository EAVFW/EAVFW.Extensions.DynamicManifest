using EAVFramework;
using EAVFW.Extensions.Documents;
using EAVFW.Extensions.SecurityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Security.Claims;
using ExpressionEngine;
using EAVFW.Extensions.Manifest.SDK;
using EAVFramework.Endpoints;
using EAVFW.Extensions.DynamicManifest.UnitTests.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EAVFW.Extensions.DynamicManifest.UnitTests.Helpers
{
    public class BaseDynamicFormUnitTest
    {
        protected async Task UpdateForm(IServiceProvider rootServiceProvider, ClaimsPrincipal prinpal, Guid? formid, Action<Form, EAVDBContext<DynamicContext>> change)
        {
            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {

                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();

                var form = await ctx.Context.FindAsync<Form>(formid.Value);

                change(form, ctx);

                await ctx.SaveChangesAsync(prinpal);

            }
        }

        protected async Task PublishAsync<TModel,TDocument>(IServiceProvider rootServiceProvider, Guid? formid, bool enrich = false, bool initScript = false)
          where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields, new()
        {
            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {

                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();

                var publisher = sp.GetRequiredService<PublishDynamicManifestAction<DynamicContext, DynamicManifestContext<DynamicContext, TModel, TDocument>, DynamicManifestContextFeature<DynamicContext, DynamicManifestContext<DynamicContext, TModel, TDocument>, TModel, TDocument>, TModel, TDocument>>();

                await publisher.PublishAsync(formid.Value, null, enrich, initScript);

            }
        }

        protected async Task<Guid?> CreateFormAsync<TModel,TDocument>(IServiceProvider rootServiceProvider, ClaimsPrincipal prinpal, TModel form)
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields, new()
        {
            Guid? formid = null;
            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();


                ctx.Context.Add(form);
                await ctx.SaveChangesAsync(prinpal);
                formid = form.Id;
            }

            return formid;
        }

        private static async Task ExecuteCommand(SqlConnection connection, string cmd)
        {
            try
            {


                var command = new SqlCommand(cmd, connection);


                SqlDataReader reader = await command.ExecuteReaderAsync();
                try
                {
                    while (reader.Read())
                    {


                    }
                }
                finally
                {
                    // Always call Close when done reading.
                    reader.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task<(IServiceProvider, Guid, ClaimsPrincipal)> Setup<TModel,TDocument>(string rootManifestPath,string schema="dbo")
           where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
           where TDocument : DynamicEntity, IDocumentEntity, IAuditFields, new()
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddInMemoryCollection(new Dictionary<string, string>
                {

                    ["ConnectionStrings:ApplicationDB"] = "Server=127.0.0.1; Initial Catalog=DynManifest; User ID=sa; Password=Bigs3cRet; TrustServerCertificate=True",
                    ["ConnectionStrings:ApplicationDBMaster"] = "Server=127.0.0.1;  User ID=sa; Password=Bigs3cRet; TrustServerCertificate=True",


                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddManifestSDK<DataClientParameterGenerator>();

            services.AddCodeServices();

            services.AddEAVFramework<DynamicContext>()
                .AddDocumentPlugins<DynamicContext, Document>()
                .WithAuditFieldsPlugins<DynamicContext, Identity>();

            services.AddExpressionEngine();

            services.AddOptions<DynamicContextOptions>().Configure((o) =>
            {
                o.Manifests = new[]
                {
                   JToken.Parse(System.IO.File.ReadAllText(rootManifestPath))
                };
                o.Schema = schema;
                o.EnableDynamicMigrations = true;
                
                o.Namespace = "DummyNamespace";
                //o.DTOBaseClasses = new[] { typeof(BaseOwnerEntity<Model.Identity>), typeof(BaseIdEntity<Model.Identity>) };
                o.DTOAssembly = typeof(CDMTests).Assembly;
                o.DTOBaseInterfaces = new[] { typeof(IAuditFields), typeof(IHasAdminEmail) };
            });
            //services.AddEntityFrameworkSqlServer();
            services.AddDbContext<DynamicContext>((sp, optionsBuilder) =>
            {

                optionsBuilder.UseSqlServer("Name=ApplicationDB", x => x.MigrationsHistoryTable("__MigrationsHistory", schema));
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();

                optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();

                optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicContextModelCacheKeyFactory>();
            });

            services
              .AddSingleton<IDynamicManifestContextOptionFactory<DynamicContext, DynamicManifestContext<DynamicContext, MigrationProject, Document>
                      , MigrationProject, Document>,
                  MyDefaultDynamicManifestContextOptionFactory<DynamicManifestContext<DynamicContext, MigrationProject, Document>,
                      MigrationProject, Document>>();


            services.AddDynamicManifest<TModel, TDocument>();

           

            var rootServiceProvider = services.BuildServiceProvider();

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {


                using (SqlConnection connection =
                    new SqlConnection(configuration.GetConnectionString("ApplicationDBMaster")))
                {

                    connection.Open();

                    await ExecuteCommand(connection, "DROP DATABASE [DynManifest]");

                    await ExecuteCommand(connection, "CREATE DATABASE [DynManifest];ALTER DATABASE [DynManifest] SET RECOVERY SIMPLE;");

                }



            }

            var principalId = Guid.Parse("1b714972-8d0a-4feb-b166-08d93c6ae328");
            var prinpal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub", principalId.ToString())
                                }, Constants.DefaultCookieAuthenticationScheme));

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();

                await ctx.MigrateAsync();


            }

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();


                ctx.Context.Add(new Identity
                {
                    Id = principalId,
                    Name = "Test User"


                });
                await ctx.SaveChangesAsync(prinpal);

            }





            return (rootServiceProvider, principalId, prinpal);
        }
    }
}