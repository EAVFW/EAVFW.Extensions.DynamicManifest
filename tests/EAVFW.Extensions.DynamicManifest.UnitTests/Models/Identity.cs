using EAVFramework.Shared;
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
    [Entity(LogicalName = "identity", SchemaName = "Identity", CollectionSchemaName = "Identities", IsBaseClass = true)]
    [EntityDTO(LogicalName = "identity", Schema = "dbo")]
    public partial class Identity : BaseOwnerEntity<Identity>, IAuditFields, IIdentity
    {
        public Identity()
        {
        }

        [DataMember(Name = "name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public string Name { get; set; }

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
}