using System;
using EAVFW.Extensions.DynamicManifest.UnitTests.Models;
using EAVFramework.Shared;
using EAVFW.Extensions.SecurityModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    [Serializable()]
    [EntityDTO(LogicalName = "migrationproject", Schema = "MigrationAsAService")]
    [Entity(LogicalName = "migrationproject", SchemaName = "MigrationProject", CollectionSchemaName = "MigrationProjects", IsBaseClass = false, EntityKey = "Migration Project")]
    public partial class MigrationProject : BaseOwnerEntity<Identity>, IDynamicManifestEntity<Document>, IAuditFields
    {
        public MigrationProject()
        {
        }

        [DataMember(Name = "name")]
        [EntityField(AttributeKey = "Name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }

        [DataMember(Name = "manifest")]
        [JsonProperty("manifest")]
        [JsonPropertyName("manifest")]
        [ForeignKey("ManifestId")]
        public Document Manifest { get; set; }

        [DataMember(Name = "manifestid")]
        [EntityField(AttributeKey = "Manifest")]
        [JsonProperty("manifestid")]
        [JsonPropertyName("manifestid")]
        public Guid? ManifestId { get; set; }

        [DataMember(Name = "schema")]
        [EntityField(AttributeKey = "Schema")]
        [JsonProperty("schema")]
        [JsonPropertyName("schema")]
        public String Schema { get; set; }

        [DataMember(Name = "targetcredentials")]
        [JsonProperty("targetcredentials")]
        [JsonPropertyName("targetcredentials")]
        [ForeignKey("TargetCredentialsId")]
        public Document TargetCredentials { get; set; }

        [DataMember(Name = "targetcredentialsid")]
        [EntityField(AttributeKey = "Target Credentials")]
        [JsonProperty("targetcredentialsid")]
        [JsonPropertyName("targetcredentialsid")]
        public Guid? TargetCredentialsId { get; set; }

        [DataMember(Name = "version")]
        [EntityField(AttributeKey = "Version")]
        [JsonProperty("version")]
        [JsonPropertyName("version")]
        public String Version { get; set; }

        [DataMember(Name = "migrationentities")]
        [JsonProperty("migrationentities")]
        [JsonPropertyName("migrationentities")]
        [InverseProperty("MigrationProject")]
        public ICollection<MigrationEntity> MigrationEntities { get; set; }

    }
}