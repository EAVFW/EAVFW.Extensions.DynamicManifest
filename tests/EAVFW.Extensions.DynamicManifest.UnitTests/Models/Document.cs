using EAVFramework.Shared;
using EAVFW.Extensions.Documents;
using EAVFW.Extensions.SecurityModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using JsonPropertyName = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace EAVFW.Extensions.DynamicManifest.UnitTests.Models
{
    [Serializable()]
    [Entity(LogicalName = "document", SchemaName = "Document", CollectionSchemaName = "Documents", IsBaseClass = false)]
    [EntityDTO(LogicalName = "document", Schema = "dbo")]
    public partial class Document : BaseOwnerEntity<Identity>, IDocumentEntity, IAuditFields
    {
        public Document()
        {
        }

        [DataMember(Name = "name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public string Name { get; set; }

        [DataMember(Name = "size")]
        [JsonProperty("size")]
        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [DataMember(Name = "container")]
        [JsonProperty("container")]
        [JsonPropertyName("container")]
        public string Container { get; set; }

        [DataMember(Name = "path")]
        [JsonProperty("path")]
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [DataMember(Name = "contenttype")]
        [JsonProperty("contenttype")]
        [JsonPropertyName("contenttype")]
        public string ContentType { get; set; }

        [DataMember(Name = "compressed")]
        [JsonProperty("compressed")]
        [JsonPropertyName("compressed")]
        public bool? Compressed { get; set; }

        [DataMember(Name = "data")]
        [JsonProperty("data")]
        [JsonPropertyName("data")]
        public byte[] Data { get; set; }

        [InverseProperty("Manifest")]
        [JsonProperty("forms")]
        [JsonPropertyName("forms")]
        public ICollection<Form> Forms { get; set; }

        [DataMember(Name = "hash")]
        [EntityField(AttributeKey = "Hash")]
        [JsonProperty("hash")]
        [JsonPropertyName("hash")]       
        public String Hash { get; set; }


    }
}