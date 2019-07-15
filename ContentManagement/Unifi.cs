using Newtonsoft.Json;
using RestSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ContentManagement {
    class Unifi {
        /// <summary>
        /// Retrieve an access token using basic authentication by passing a UNIFI username and password.
        /// </summary>
        /// <param name="username">UNIFI username</param>
        /// <param name="password">UNIFI password</param>
        /// <returns></returns>
        public static string GetAccessToken(string username, string password) {
            var client = new RestClient("https://api.unifilabs.com/login");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("undefined", "{\n\t\"username\": \"" + username + "\",\n\t\"password\": \"" + password + "\"\n}\n", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            // Deserialize JSON response to a dynamic object to retrieve the access token
            var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);

            // Return the access_token value as a string
            return obj.access_token;
        }

        /// <summary>
        /// Get a list of libraries within the UNIFI Content Management System
        /// </summary>
        /// <param name="accessToken">UNIFI Access token</param>
        /// <returns></returns>
        public static List<Library> GetLibraries(string accessToken) {
            // Instantiate a list of Library objects to deserialize the JSON response
            List<Library> _libraries = new List<Library>();

            var client = new RestClient("https://api.unifilabs.com/libraries");
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Authorization", "Bearer " + accessToken);
            IRestResponse response = client.Execute(request);

            // Deserialize JSON response to a List of Library objects
            _libraries = JsonConvert.DeserializeObject<List<Library>>(response.Content);

            return _libraries;
        }

        /// <summary>
        /// Retrieves a list of content from a specific library.
        /// </summary>
        /// <param name="accessToken">UNIFI access token.</param>
        /// <param name="libraryId">The library ID to search.</param>
        /// <returns></returns>
        public static List<Content> GetContentFromLibrary(string accessToken, Guid libraryId) {
            List<Content> _content = new List<Content>();

            var client = new RestClient("https://api.unifilabs.com/search");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("undefined", "{\n  \"terms\": \"*\",\n  \"libraries\": [\"" + libraryId + "\"],\n  \"return\": \"with-parameters\"," +
                                              "\"size\": " + "20" + "\n" +
                                              "}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            // Deserialize JSON response to a List of Content objects
            try { _content = JsonConvert.DeserializeObject<List<Content>>(response.Content); }
            catch (Exception ex) { MessageBox.Show(response.Content, "Exception"); }

            return _content;
        }

        /// <summary>
        /// Get a specific Content by its name.
        /// </summary>
        /// <param name="accessToken">UNIFI access token.</param>
        /// <param name="name">The name of the Content to search for.</param>
        /// <returns></returns>
        public static Content GetContentByName(string accessToken, string name) {
            List<Content> content = new List<Content>();

            var client = new RestClient("https://api.unifilabs.com/search");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("undefined", "{\n  \"terms\": \"" + name + "\",\n  \"return\": \"with-parameters\"\n}\n", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            // Deserialize JSON response to a Content object
            try { content = JsonConvert.DeserializeObject<List<Content>>(response.Content); }
            catch (Exception ex) {
                // Display debug info if exception is caught
                MessageBox.Show(ex.ToString(), "Exception");
                MessageBox.Show(response.Content, "Exception");
            }

            // TODO: Ensure that only one content is returned rather than hardcoding the first item in list
            if (content.Count > 1) { MessageBox.Show("Warning, more than one piece of content matches this query.", "Warning"); }

            return content[0];
        }

        /// <summary>
        /// Get Revit Family Types from a Content object.
        /// </summary>
        /// <param name="content">The UNIFI Content object to get parameters from.</param>
        /// <returns></returns>
        public static List<string> GetFamilyTypes(Content content) {
            List<string> familyTypes = new List<string>();

            // Loop through all parameters
            foreach (Unifi.Parameter p in content.Parameters) {
                // Pass parameter values to Content object
                if (p.TypeName != "") { familyTypes.Add(p.TypeName); }
            }

            // Pass the list of unique list items as Family Type names array
            var uniqueItems = new HashSet<string>(familyTypes);
            familyTypes = uniqueItems.ToList();

            return familyTypes;
        }

        /// <summary>
        /// Set the Revit Family Type Parameter value.
        /// </summary>
        /// <param name="accessToken">UNIFI access token</param>
        /// <param name="content">The content to modify</param>
        /// <param name="typeName">The Revit Family Type name</param>
        /// <param name="parameterName">The name of the Type Parameter to modify</param>
        /// <param name="parameterValue">The new value of the Type Parameter</param>
        /// <param name="dataType">The data type for the Type Parameter</param>
        /// <param name="revitYear">The Revit year of the Family to modify</param>
        /// <returns></returns>
        public static Batch SetTypeParameterValue(
            string accessToken,
            Content content,
            string typeName,
            string parameterName,
            string parameterValue,
            string dataType,
            int revitYear
        ) {
            var client = new RestClient("https://api.unifilabs.com/batch");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("undefined", "{\n    \"Requests\": [\n        {\n            " +
                                              "\"ObjectId\": \"" + content.RepositoryFileId + "\",\n            " +
                                              "\"Operation\": \"SetTypeValues\",\n            " +
                                              "\"ExistingName\": \"" + parameterName + "\",\n            " +
                                              "\"Type\": \"" + dataType + "\",\n            " +
                                              "\"RevitYear\": " + revitYear + ",\n            " +
                                              "\"Values\": " +
                                              "[\n            \t\t{\n            \t\t\t" +
                                              "\"TypeName\": " + ToLiteral(typeName) + ",\n            \t\t\t" +
                                              "\"Value\": \"" + parameterValue + "\"\n            \t\t}\n            \t]\n        }\n    ]\n}",
                ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            // Deserialize reponse as Batch object
            Batch batch = JsonConvert.DeserializeObject<Batch>(response.Content);

            return batch;
        }

        /// <summary>
        /// Get the status of a Batch from its ID
        /// </summary>
        /// <param name="accessToken">UNIFI access token</param>
        /// <param name="batchId">The ID of the Batch</param>
        /// <returns></returns>
        public static BatchStatus GetBatchStatus(string accessToken, string batchId) {
            var client = new RestClient("https://api.unifilabs.com/batch/" + batchId);
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddHeader("Content-Type", "application/json");
            IRestResponse response = client.Execute(request);

            // Deserialize reponse as BatchStatus object
            BatchStatus batchStatus = JsonConvert.DeserializeObject<BatchStatus>(response.Content);

            return batchStatus;
        }

        /// <summary>
        /// Convert a string to a string literal. Useful for passing Type Names and other values that have quotation marks.
        /// </summary>
        /// <param name="input">The string to convert</param>
        /// <returns></returns>
        private static string ToLiteral(string input) {
            using (var writer = new StringWriter()) {
                using (var provider = CodeDomProvider.CreateProvider("CSharp")) {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }

        public partial class Content {
            // Pass values from Manufacturer and Model parameters to object
            public string Manufacturer { get; set; }
            public string Model { get; set; }

            [JsonProperty("ActiveRevisionId")]
            public Guid ActiveRevisionId { get; set; }

            [JsonProperty("AggregateRating")]
            public object AggregateRating { get; set; }

            [JsonProperty("ApprovalStatus")]
            public long ApprovalStatus { get; set; }

            [JsonProperty("ApprovedRevisionId")]
            public Guid ApprovedRevisionId { get; set; }

            [JsonProperty("Brands")]
            public object[] Brands { get; set; }

            [JsonProperty("Category")]
            public string Category { get; set; }

            [JsonProperty("Channels")]
            public object[] Channels { get; set; }

            [JsonProperty("CreatedDate")]
            public string CreatedDate { get; set; }

            [JsonProperty("CreatorUsername")]
            public string CreatorUsername { get; set; }

            [JsonProperty("CurrentRevisionId")]
            public Guid CurrentRevisionId { get; set; }

            [JsonProperty("Downloads")]
            public long Downloads { get; set; }

            public List<string> FamilyTypes { get; set; }

            [JsonProperty("Favorites")]
            public object[] Favorites { get; set; }

            [JsonProperty("FileImageBytes")]
            public object FileImageBytes { get; set; }

            [JsonProperty("FileType")]
            public long FileType { get; set; }

            [JsonProperty("Filename")]
            public string Filename { get; set; }

            [JsonProperty("FillPatternType")]
            public object FillPatternType { get; set; }

            [JsonProperty("HasCustomPreviewImage")]
            public bool HasCustomPreviewImage { get; set; }

            [JsonProperty("HasTypeCatalog")]
            public bool HasTypeCatalog { get; set; }

            [JsonProperty("LastModifiedDate")]
            public string LastModifiedDate { get; set; }

            [JsonProperty("Libraries")]
            public Library[] Libraries { get; set; }

            [JsonProperty("LocalPath")]
            public object LocalPath { get; set; }

            [JsonProperty("MaterialClass")]
            public object MaterialClass { get; set; }

            [JsonProperty("MeasurementSystem")]
            public long MeasurementSystem { get; set; }

            [JsonProperty("Measurements")]
            public object[] Measurements { get; set; }

            [JsonProperty("NextRevisionNumber")]
            public object NextRevisionNumber { get; set; }

            [JsonProperty("OriginalDateAdded")]
            public string OriginalDateAdded { get; set; }

            [JsonProperty("ParseStatus")]
            public long ParseStatus { get; set; }

            [JsonProperty("PreviewImageUrl")]
            public string PreviewImageUrl { get; set; }

            [JsonProperty("Ratings")]
            public object[] Ratings { get; set; }

            [JsonProperty("RepositoryFileId")]
            public Guid RepositoryFileId { get; set; }

            [JsonProperty("RepositoryNumber")]
            public object RepositoryNumber { get; set; }

            [JsonProperty("RevisionNumber")]
            public object RevisionNumber { get; set; }

            [JsonProperty("Revisions")]
            public Revision[] Revisions { get; set; }

            [JsonProperty("RevitYear")]
            public long RevitYear { get; set; }

            [JsonProperty("Size")]
            public long Size { get; set; }

            [JsonProperty("Tags")]
            public Tag[] Tags { get; set; }

            [JsonProperty("Title")]
            public string Title { get; set; }

            [JsonProperty("TypeCatalogBytes")]
            public object TypeCatalogBytes { get; set; }

            [JsonProperty("UserRating")]
            public long UserRating { get; set; }

            [JsonProperty("Parameters")]
            public Parameter[] Parameters { get; set; }
        }

        public partial class Library {
            [JsonProperty("AccessibleUserGroups")]
            public object[] AccessibleUserGroups { get; set; }

            [JsonProperty("AccessibleUsers")]
            public object[] AccessibleUsers { get; set; }

            [JsonProperty("AdminUserGroups")]
            public object[] AdminUserGroups { get; set; }

            [JsonProperty("AdminUsers")]
            public object[] AdminUsers { get; set; }

            [JsonProperty("CompanyId")]
            public Guid CompanyId { get; set; }

            [JsonProperty("CompanyName")]
            public string CompanyName { get; set; }

            [JsonProperty("DateCreated")]
            public string DateCreated { get; set; }

            [JsonProperty("IsProtected")]
            public bool IsProtected { get; set; }

            [JsonProperty("id")]
            public Guid Id { get; set; }

            [JsonProperty("LibraryId")]
            private Guid LibraryId {
                set { Id = value; }
            }

            [JsonProperty("LibraryType")]
            public long LibraryType { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("Name")]
            private string Name2 {
                set { Name = value; }
            }

            [JsonProperty("RepositoryId")]
            public Guid RepositoryId { get; set; }
        }

        public partial class Parameter {
            [JsonProperty("TypeName")]
            public string TypeName { get; set; }

            [JsonProperty("ParameterType")]
            public long ParameterType { get; set; }

            [JsonProperty("StorageType")]
            public long StorageType { get; set; }

            [JsonProperty("DisplayUnitType")]
            public long? DisplayUnitType { get; set; }

            [JsonProperty("IsDeterminedByFormula")]
            public bool IsDeterminedByFormula { get; set; }

            [JsonProperty("GUID")]
            public string Guid { get; set; }

            [JsonProperty("CanAssignFormula")]
            public bool CanAssignFormula { get; set; }

            [JsonProperty("IsBaseVersion")]
            public bool IsBaseVersion { get; set; }

            [JsonProperty("FamilyCategory")]
            public string FamilyCategory { get; set; }

            [JsonProperty("FileVersionId")]
            public Guid FileVersionId { get; set; }

            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("BuiltInParameter")]
            public long BuiltInParameter { get; set; }

            [JsonProperty("RevitYear")]
            public long RevitYear { get; set; }

            [JsonProperty("IsReporting")]
            public bool IsReporting { get; set; }

            [JsonProperty("FileRevisionId")]
            public Guid FileRevisionId { get; set; }

            [JsonProperty("ParameterGroup")]
            public long ParameterGroup { get; set; }

            [JsonProperty("Visible")]
            public bool Visible { get; set; }

            [JsonProperty("Value")]
            public string Value { get; set; }

            [JsonProperty("FileId")]
            public Guid FileId { get; set; }

            [JsonProperty("NumericValue")]
            public string NumericValue { get; set; }

            [JsonProperty("SetByTypeCatalog")]
            public bool SetByTypeCatalog { get; set; }

            [JsonProperty("IsReadOnly")]
            public bool IsReadOnly { get; set; }

            [JsonProperty("ElementId")]
            public long ElementId { get; set; }

            [JsonProperty("IsInstance")]
            public bool IsInstance { get; set; }
        }

        public partial class Revision {
            [JsonProperty("BaseFileVersions")]
            public BaseFileVersion[] BaseFileVersions { get; set; }

            [JsonProperty("Created")]
            public string Created { get; set; }

            [JsonProperty("FileRevisionId")]
            public Guid FileRevisionId { get; set; }

            [JsonProperty("FillPatternType")]
            public object FillPatternType { get; set; }

            [JsonProperty("MaterialClass")]
            public object MaterialClass { get; set; }

            [JsonProperty("Notes")]
            public string Notes { get; set; }

            [JsonProperty("RevisionNumber")]
            public long RevisionNumber { get; set; }

            [JsonProperty("Status")]
            public long Status { get; set; }

            [JsonProperty("Username")]
            public string Username { get; set; }
        }

        public partial class BaseFileVersion {
            [JsonProperty("FileVersionId")]
            public Guid FileVersionId { get; set; }

            [JsonProperty("FillPatternType")]
            public object FillPatternType { get; set; }

            [JsonProperty("IsBaseVersion")]
            public bool IsBaseVersion { get; set; }

            [JsonProperty("MaterialClass")]
            public object MaterialClass { get; set; }

            [JsonProperty("OriginalFilename")]
            public string OriginalFilename { get; set; }

            [JsonProperty("RepositoryNumber")]
            public long RepositoryNumber { get; set; }

            [JsonProperty("RevitYear")]
            public long RevitYear { get; set; }
        }

        public partial class Tag {
            [JsonProperty("RepositoryNumber")]
            public long RepositoryNumber { get; set; }

            [JsonProperty("TagId")]
            public Guid TagId { get; set; }

            [JsonProperty("TagString")]
            public string TagString { get; set; }
        }

        public partial class Batch {
            //[JsonProperty("Summary")]
            //public Summary Summary { get; set; }

            [JsonProperty("Details")]
            public object[] Details { get; set; }

            //[JsonProperty("FileStatus")]
            //public FileStatus[] FileStatus { get; set; }

            [JsonProperty("BatchId")]
            public Guid BatchId { get; set; }
        }

        public partial class BatchStatus {
            [JsonProperty("resultDetails")]
            public string ResultDetails { get; set; }

            [JsonProperty("TotalFiles")]
            public long TotalFiles { get; set; }

            [JsonProperty("OkFiles")]
            public long OkFiles { get; set; }

            [JsonProperty("PendingFiles")]
            public long PendingFiles { get; set; }

            [JsonProperty("FailedFiles")]
            public long FailedFiles { get; set; }

            [JsonProperty("TotalOperations")]
            public long TotalOperations { get; set; }

            [JsonProperty("CompletedOperations")]
            public long CompletedOperations { get; set; }

            [JsonProperty("Error")]
            public string Error { get; set; }
        }
    }
}