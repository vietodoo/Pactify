using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pactify.Definitions;
using Pactify.Definitions.Http;

namespace Pactify.Verifiers
{
    internal sealed class HttpCouplingVerifier : ICouplingVerifier
    {
        private readonly HttpClient _httpClient;

        public HttpCouplingVerifier(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PactVerificationResult> VerifyAsync(ICouplingDefinition definition, PactDefinitionOptions options)
        {
            if(!(definition is HttpCouplingDefinition httpDefinition) )
            {
                throw new PactifyException("Definition type is invalid");
            }

            var getResult = GetHttpMethod(httpDefinition.Request.Method);
            var httpResponse = await getResult(httpDefinition.Request.Path);

            var errors = new List<string>();

            if(httpResponse.StatusCode != httpDefinition.Response.StatusCode)
            {
                errors.Add($"Expected status code: {httpDefinition.Response.StatusCode}, but was {httpResponse.StatusCode}");
            }

            var json = await httpResponse.Content.ReadAsStringAsync();
            IDictionary<string, object> providedBody = JsonConvert.DeserializeObject<ExpandoObject>(json);
            var expectedBody = httpDefinition.Response.Body;

            foreach (var pair in (IDictionary<string, object>)expectedBody)
            {
                var propertyName = options.IgnoreCasing? pair.Key.ToLowerInvariant() : pair.Key;
                var propertyValue = pair.Value;

                var providedProperty = providedBody.FirstOrDefault(p =>
                    (options.IgnoreCasing ? p.Key.ToLowerInvariant() : p.Key) == propertyName).Value;

                if (providedProperty is null)
                {
                    errors.Add($"Expected property {propertyName} was not present");
                }
                else
                {
                    if (options.IgnoreContractValues) continue;
                    var propertyHasExpectedValue = providedProperty.Equals(propertyValue);

                    if (!propertyHasExpectedValue)
                    {
                        errors.Add($"Expected property {propertyName} has invalid value");
                    }
                }
            }

            return new PactVerificationResult(errors);
        }

        private Func<string, Task<HttpResponseMessage>> GetHttpMethod(string method)
        {
            switch (method)
            {
                case "GET":
                    return path => _httpClient.GetAsync(path);
                case "POST":
                    return path => _httpClient.PostAsync(path, null);
                case "PUT":
                    return path => _httpClient.PutAsync(path, null);
                case "DELETE":
                    return path => _httpClient.DeleteAsync(path);
                default:
                    throw new PactifyException("Unknown HTTP method defined");
            }
        }
    }
}
