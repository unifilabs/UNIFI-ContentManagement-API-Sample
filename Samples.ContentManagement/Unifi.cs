using Newtonsoft.Json;
using RestSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ContentManagement.Entities;
using Parameter = ContentManagement.Entities.Parameter;

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
            request.AddParameter("undefined", JsonConvert.SerializeObject(new { username, password }), ParameterType.RequestBody);
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
            request.AddParameter("undefined", JsonConvert.SerializeObject(new {
                terms = "*",
                libraries = new[] { libraryId },
                @return = "with-parameters",
                size = 20
            }), ParameterType.RequestBody);
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
            request.AddParameter("undefined", JsonConvert.SerializeObject(new { terms = name, @return = "with-parameters" }), ParameterType.RequestBody);
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
            foreach (Parameter p in content.Parameters) {
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
            request.AddParameter("undefined", JsonConvert.SerializeObject(new {
                Requests = new[] {
                    new {
                        ObjectId = content.RepositoryFileId,
                        Operation = "SetTypeValues",
                        ExistingName = parameterName,
                        Type = dataType,
                        RevitYear = revitYear,
                        Values = new[] { new { TypeName = ToLiteral(typeName), Value = parameterValue } }
                    }
                }
            }), ParameterType.RequestBody);

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
        public static async Task<BatchStatus> GetBatchStatus(string accessToken, string batchId) {
            var client = new RestClient("https://api.unifilabs.com/batch/" + batchId);
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddHeader("Content-Type", "application/json");
            var response = await client.ExecuteTaskAsync<BatchStatus>(request);

            return response.Data;
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
    }
}