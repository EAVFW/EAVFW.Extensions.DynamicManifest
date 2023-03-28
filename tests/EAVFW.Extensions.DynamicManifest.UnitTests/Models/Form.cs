using EAVFramework.Shared;
using EAVFW.Extensions.SecurityModel;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using JsonPropertyName = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace EAVFW.Extensions.DynamicManifest.UnitTests.Models
{
    [Serializable()]
    [Entity(LogicalName = "form", SchemaName = "Form", CollectionSchemaName = "Forms", IsBaseClass = false)]
    [EntityDTO(LogicalName = "form", Schema = "dbo")]
    public partial class Form : BaseOwnerEntity<Identity>,
        IDynamicManifestEntity<Document>, IAuditFields, IHasAdminEmail
    {
        public Form()
        {
        }

        [DataMember(Name = "name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public string Name { get; set; }

        [DataMember(Name = "adminemail")]
        [JsonProperty("adminemail")]
        [JsonPropertyName("adminemail")]

        public string AdminEmail { get; set; }


        [DataMember(Name = "schema")]
        [JsonProperty("schema")]
        [JsonPropertyName("schema")]
        public string Schema { get; set; }

        [DataMember(Name = "version")]
        [JsonProperty("version")]
        [JsonPropertyName("version")]
        public string Version { get; set; }






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
}