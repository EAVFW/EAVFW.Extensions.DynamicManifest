# EAVFW.Extensions.DynamicManifest

###
Using a project entity that implements the following properties
```
    [EntityInterface(EntityKey = "*")]
    public interface IDynamicManifestEntity<T> where T : IDocumentEntity
    {
        public Guid Id { get; set; }
        public string Schema { get; set; }
        public string Version { get; set; }
        public Guid? ManifestId { get; set; }
        public T Manifest { get; set; }

        public DateTime? CreatedOn { get; set; }
    }
    
```
a dynamic manifest can be set up with publishing/authoring support within eavfw portal
```
   services.AddDynamicManifest<DataModelProject,Document>();
```

here is an example of configuration with ribbon for publishing:
```
{
  "variables": {
    "sitemaps": {
      "DynModel": {
        "app": "Kjeldager CRM",
        "area": "Dynamic Data Model",
        "group": "Dynamic Data Model"
      }
    }
  },
  "entities": {
    "Data Model Project": {
      "pluralName": "Data Model Projects",
      "TPT": "Project",
      "sitemap": {
        "[merge()]": "[variables('sitemaps').DynModel]",
        "title": "Projects"
      },
      "attributes": {
        "Schema": {
          "type": "Text"
        },
        "Version": {
          "type": "Text"
        },
        "Manifest": {
          "type": {
            "type": "lookup",
            "referenceType": "Document"
          }
        }
      },
      "forms": {
        "Main Information": {
          "type": "Main",
          "name": "Main Information",
          "ribbon": {
            "RUN_REMOTE_WORKFLOW": {
              "text": "Publish",
              "workflowName": "509d3bf3-18c5-6f1c-2c64-a2a5a33cb3f1"
            }
          },
          "layout": {
            "tabs": {
              "TAB_Editor": {
                "title": "Editor",
                "locale": { "1030": { "title": "Editor" } },
                "columns": "[variables('layouts').OneColumnTemplate]"
              },
              "TAB_General": "[variables('TAB_General')]",
              "TAB_Versions": {
                "title": "Versions",
                "locale": { "1030": { "title": "Versions" } },
                "columns": "[variables('layouts').OneColumnTemplate]"
              },
              "TAB_Administrative": "[variables('TAB_Administrative')]"
            }
          },
          "columns": {
            "[merge()]": "[variables('TAB_Administrative_Columns')]",
            "Name": "[variables('PrimaryInformation')]",
            "Description": "[variables('PrimaryInformation')]",
            "Schema": "[variables('PrimaryInformation')]",
            "Version": "[variables('PrimaryInformation')]",
            "Manifest": {
              "tab": "TAB_Editor",
              "column": "COLUMN_First",
              "section": "SECTION_General",
              "control": "MonacoEditorControl"
            }
          }
        }
      }
    }
  }
}
```