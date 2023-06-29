using System;
using EAVFW.Extensions.DynamicManifest.UnitTests.Models;
using EAVFramework.Shared;
using EAVFW.Extensions.SecurityModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    [Serializable()]
    [EntityDTO(LogicalName = "migrationentity", Schema = "MigrationAsAService")]
    [Entity(LogicalName = "migrationentity", SchemaName = "MigrationEntity", CollectionSchemaName = "MigrationEntities", IsBaseClass = false, EntityKey = "Migration Entity")]
    public partial class MigrationEntity : BaseOwnerEntity<Identity>, IAuditFields
    {
        public MigrationEntity()
        {
        }

        [DataMember(Name = "sourcename")]
        [EntityField(AttributeKey = "Source Name")]
        [JsonProperty("sourcename")]
        [JsonPropertyName("sourcename")]
        [PrimaryField()]
        [Description("Name of source table")]
        public String SourceName { get; set; }

        [DataMember(Name = "mapping")]
        [JsonProperty("mapping")]
        [JsonPropertyName("mapping")]
        [ForeignKey("MappingId")]
        public Document Mapping { get; set; }

        [DataMember(Name = "mappingid")]
        [EntityField(AttributeKey = "Mapping")]
        [JsonProperty("mappingid")]
        [JsonPropertyName("mappingid")]
        public Guid? MappingId { get; set; }

        [DataMember(Name = "migrationproject")]
        [JsonProperty("migrationproject")]
        [JsonPropertyName("migrationproject")]
        [ForeignKey("MigrationProjectId")]
        public MigrationProject MigrationProject { get; set; }

        [DataMember(Name = "migrationprojectid")]
        [EntityField(AttributeKey = "Migration Project")]
        [JsonProperty("migrationprojectid")]
        [JsonPropertyName("migrationprojectid")]
        [Description("The project in which the Entity belongs")]
        public Guid? MigrationProjectId { get; set; }

        [DataMember(Name = "sourcedata")]
        [JsonProperty("sourcedata")]
        [JsonPropertyName("sourcedata")]
        [ForeignKey("SourceDataId")]
        public Document SourceData { get; set; }

        [DataMember(Name = "sourcedataid")]
        [EntityField(AttributeKey = "Source Data")]
        [JsonProperty("sourcedataid")]
        [JsonPropertyName("sourcedataid")]
        public Guid? SourceDataId { get; set; }

        [DataMember(Name = "sourcemetadata")]
        [JsonProperty("sourcemetadata")]
        [JsonPropertyName("sourcemetadata")]
        [ForeignKey("SourceMetadataId")]
        public Document SourceMetadata { get; set; }

        [DataMember(Name = "sourcemetadataid")]
        [EntityField(AttributeKey = "Source Metadata")]
        [JsonProperty("sourcemetadataid")]
        [JsonPropertyName("sourcemetadataid")]
        [Description("Metadata for source data")]
        public Guid? SourceMetadataId { get; set; }

        [DataMember(Name = "targetmetadata")]
        [JsonProperty("targetmetadata")]
        [JsonPropertyName("targetmetadata")]
        [ForeignKey("TargetMetadataId")]
        public Document TargetMetadata { get; set; }

        [DataMember(Name = "targetmetadataid")]
        [EntityField(AttributeKey = "Target Metadata")]
        [JsonProperty("targetmetadataid")]
        [JsonPropertyName("targetmetadataid")]
        [Description("Metadata of the target system")]
        public Guid? TargetMetadataId { get; set; }

        [DataMember(Name = "targetname")]
        [EntityField(AttributeKey = "Target Name")]
        [JsonProperty("targetname")]
        [JsonPropertyName("targetname")]
        [Description("Name of target table")]
        public String TargetName { get; set; }

    }
}