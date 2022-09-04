using DotNetDevOps.Extensions.EAVFramework;
using DotNetDevOps.Extensions.EAVFramework.Shared;
using EAVFW.Extensions.Documents;
using EAVFW.Extensions.SecurityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using JsonPropertyName = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using System.Data.SqlClient;
using System.Security.Claims;
using ExpressionEngine;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    [Serializable()]
    [Entity(LogicalName = "document", SchemaName = "Document", CollectionSchemaName = "Documents", IsBaseClass = false)]
    [EntityDTO(LogicalName = "document", Schema = "KFST")]
    public partial class Document : BaseOwnerEntity<Identity>, IDocumentEntity, IAuditFields
    {
        public Document()
        {
        }

        [DataMember(Name = "name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }

        [DataMember(Name = "size")]
        [JsonProperty("size")]
        [JsonPropertyName("size")]
        public Int32? Size { get; set; }

        [DataMember(Name = "container")]
        [JsonProperty("container")]
        [JsonPropertyName("container")]
        public String Container { get; set; }

        [DataMember(Name = "path")]
        [JsonProperty("path")]
        [JsonPropertyName("path")]
        public String Path { get; set; }

        [DataMember(Name = "contenttype")]
        [JsonProperty("contenttype")]
        [JsonPropertyName("contenttype")]
        public String ContentType { get; set; }

        [DataMember(Name = "compressed")]
        [JsonProperty("compressed")]
        [JsonPropertyName("compressed")]
        public Boolean? Compressed { get; set; }

        [DataMember(Name = "data")]
        [JsonProperty("data")]
        [JsonPropertyName("data")]
        public Byte[] Data { get; set; }

        [InverseProperty("Manifest")]
        [JsonProperty("forms")]
        [JsonPropertyName("forms")]
        public ICollection<Form> Forms { get; set; }



    }

    [Serializable()]
    [Entity(LogicalName = "identity", SchemaName = "Identity", CollectionSchemaName = "Identities", IsBaseClass = true)]
    [EntityDTO(LogicalName = "identity", Schema = "KFST")]
    public partial class Identity : BaseOwnerEntity<Identity>, IAuditFields, IIdentity
    {
        public Identity()
        {
        }

        [DataMember(Name = "name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }

        [InverseProperty("Owner")]
        [JsonProperty("owneridentities")]
        [JsonPropertyName("owneridentities")]
        public ICollection<Identity> OwnerIdentities { get; set; }

        [InverseProperty("ModifiedBy")]
        [JsonProperty("modifiedbyidentities")]
        [JsonPropertyName("modifiedbyidentities")]
        public ICollection<Identity> ModifiedByIdentities { get; set; }

        [InverseProperty("CreatedBy")]
        [JsonProperty("createdbyidentities")]
        [JsonPropertyName("createdbyidentities")]
        public ICollection<Identity> CreatedByIdentities { get; set; }

        [InverseProperty("Owner")]
        [JsonProperty("ownerdocuments")]
        [JsonPropertyName("ownerdocuments")]
        public ICollection<Document> OwnerDocuments { get; set; }

        [InverseProperty("ModifiedBy")]
        [JsonProperty("modifiedbydocuments")]
        [JsonPropertyName("modifiedbydocuments")]
        public ICollection<Document> ModifiedByDocuments { get; set; }

        [InverseProperty("CreatedBy")]
        [JsonProperty("createdbydocuments")]
        [JsonPropertyName("createdbydocuments")]
        public ICollection<Document> CreatedByDocuments { get; set; }



        [InverseProperty("Owner")]
        [JsonProperty("ownerforms")]
        [JsonPropertyName("ownerforms")]
        public ICollection<Form> OwnerForms { get; set; }

        [InverseProperty("ModifiedBy")]
        [JsonProperty("modifiedbyforms")]
        [JsonPropertyName("modifiedbyforms")]
        public ICollection<Form> ModifiedByForms { get; set; }

        [InverseProperty("CreatedBy")]
        [JsonProperty("createdbyforms")]
        [JsonPropertyName("createdbyforms")]
        public ICollection<Form> CreatedByForms { get; set; }



    }

    [Serializable()]
    [Entity(LogicalName = "form", SchemaName = "Form", CollectionSchemaName = "Forms", IsBaseClass = false)]
    [EntityDTO(LogicalName = "form", Schema = "KFST")]
    public partial class Form : BaseOwnerEntity<Identity>, IDynamicManifestEntity<Document>, IAuditFields
    {
        public Form()
        {
        }

        [DataMember(Name = "name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }
 

      

        [DataMember(Name = "schema")]
        [JsonProperty("schema")]
        [JsonPropertyName("schema")]
        public String Schema { get; set; }

        [DataMember(Name = "version")]
        [JsonProperty("version")]
        [JsonPropertyName("version")]
        public String Version { get; set; }


 



        [DataMember(Name = "manifestid")]
        [JsonProperty("manifestid")]
        [JsonPropertyName("manifestid")]
        public Guid? ManifestId { get; set; }

        [ForeignKey("ManifestId")]
        [JsonProperty("manifest")]
        [JsonPropertyName("manifest")]
        [DataMember(Name = "manifest")]
        public Document Manifest { get; set; }

       
      
       



    }


    public class MyDynamicContext : DynamicManifestContext<Form, Document>
    {
        protected MyDynamicContext(DynamicManifestContextFeature<MyDynamicContext, Form, Document> feature,
            DbContextOptions<MyDynamicContext> options,
            IOptions<DynamicContextOptions> modelOptions,
            IMigrationManager migrationManager,
            ILogger logger) : base(options, feature, logger)
        {

        }
    }
    [TestClass]
    public class UnitTest1
    {

        public async Task<(IServiceProvider, Guid, ClaimsPrincipal)> Setup()
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


            services.AddSingleton<IMigrationManager, MigrationManager>();
            services.AddEAVFramework<DynamicContext>()
                .AddDocumentPlugins<DynamicContext, Document>()
                .WithAuditFieldsPlugins<DynamicContext, Identity>();

            services.AddExpressionEngine();

            services.AddOptions<DynamicContextOptions>().Configure((o) =>
            {
                o.Manifests = new[]
                {
                   JToken.Parse(System.IO.File.ReadAllText("specs/dynamicmodelmanifest.json"))
                };
                o.PublisherPrefix = "KFST";

                o.EnableDynamicMigrations = true;
                o.Namespace = "DummyNamespace";
                //o.DTOBaseClasses = new[] { typeof(BaseOwnerEntity<Model.Identity>), typeof(BaseIdEntity<Model.Identity>) };
                o.DTOAssembly = typeof(UnitTest1).Assembly;

            });
            //services.AddEntityFrameworkSqlServer();
            services.AddDbContext<DynamicContext>((sp, optionsBuilder) =>
            {

                optionsBuilder.UseSqlServer("Name=ApplicationDB", x => x.MigrationsHistoryTable("__MigrationsHistory", "KFST"));
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();

                optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();


            });
            services.AddDynamicManifest<Form, Document>();

            var rootServiceProvider = services.BuildServiceProvider();

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                try
                {

                    using (SqlConnection connection =
                        new SqlConnection(configuration.GetConnectionString("ApplicationDBMaster")))
                    {



                        var command = new SqlCommand("DROP DATABASE [DynManifest]", connection);

                        connection.Open();
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



                }
                catch (Exception ex)
                {

                }

            }

            var principalId = Guid.Parse("1b714972-8d0a-4feb-b166-08d93c6ae328");
            var prinpal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub", principalId.ToString())
                                }, DotNetDevOps.Extensions.EAVFramework.Constants.DefaultCookieAuthenticationScheme));

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<DotNetDevOps.Extensions.EAVFramework.Endpoints.EAVDBContext<DynamicContext>>();

                var migrator = ctx.Context.Database.GetInfrastructure().GetRequiredService<IMigrator>();
                var sql = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

                await ctx.MigrateAsync();

                ctx.Context.Add(new Identity
                {
                    Id = principalId,
                    Name = "Test User"


                });
                await ctx.SaveChangesAsync(prinpal);
            }


            return (rootServiceProvider, principalId, prinpal);
        }

        [TestMethod]
        [DeploymentItem(@"specs/dynamicmodelmanifest.sql")]
        [DeploymentItem(@"specs/dyn_form_manifest_1_0_0.json")]
        [DeploymentItem(@"specs/dyn_form_manifest_1_0_1.json")]
        public async Task TestNoSchemaName()
        {
            var (rootServiceProvider, principalId, prinpal) = await Setup();




            Guid? formid = await CreateFormAsync(rootServiceProvider, prinpal, new Form
            {
                //  Schema = "BK-001",
                Name = "Test Form",

                Manifest = new Document
                {
                    Name = "manifest.json",
                    Container = "manifests",
                    Compressed = false,
                    Data = File.ReadAllBytes("specs/dyn_form_manifest_1_0_0.json")
                }
            });

            await PublishAsync(rootServiceProvider, formid);
            await UpdateForm(rootServiceProvider, prinpal, formid, (form,ctx) => { form.Schema = "BK-001"; });

            await PublishAsync(rootServiceProvider, formid); //Nothing happens here as version has not changed.



            await UpdateForm(rootServiceProvider, prinpal, formid, (form,ctx) => {
                form.Manifest = ctx.Context.Find<Document>(form.ManifestId);
                form.Manifest.Data = File.ReadAllBytes("specs/dyn_form_manifest_1_0_1.json");

                });

            await PublishAsync(rootServiceProvider, formid); //Nothing happens here as version has not changed.

        }

        private static async Task UpdateForm(IServiceProvider rootServiceProvider, ClaimsPrincipal prinpal, Guid? formid, Action<Form, EAVDBContext<DynamicContext>> change)
        {
            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {

                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<DotNetDevOps.Extensions.EAVFramework.Endpoints.EAVDBContext<DynamicContext>>();

                var form = await ctx.Context.FindAsync<Form>(formid.Value);

                change(form,ctx);

                await ctx.SaveChangesAsync(prinpal);

            }
        }

        private static async Task PublishAsync(IServiceProvider rootServiceProvider, Guid? formid)
        {
            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {

                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<DotNetDevOps.Extensions.EAVFramework.Endpoints.EAVDBContext<DynamicContext>>();
                 
                var publisher = sp.GetRequiredService<PublishDynamicManifestAction<DynamicContext, DynamicManifestContext<Form, Document>, DynamicManifestContextFeature<DynamicManifestContext<Form, Document>, Form, Document>, Form, Document>>();

                await publisher.PublishAsync(formid.Value, null);

            }
        }

        private static async Task<Guid?> CreateFormAsync(IServiceProvider rootServiceProvider, ClaimsPrincipal prinpal, Form form)
        {
            Guid? formid = null;
            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<DotNetDevOps.Extensions.EAVFramework.Endpoints.EAVDBContext<DynamicContext>>();


                ctx.Context.Add(form);
                await ctx.SaveChangesAsync(prinpal);
                formid = form.Id;
            }

            return formid;
        }
    }
}