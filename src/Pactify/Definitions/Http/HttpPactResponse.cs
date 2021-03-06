using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pactify.Definitions.Http
{
    internal sealed class HttpPactResponse
    {
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public HttpStatusCode Status { get; set; }

        [JsonConverter(typeof(ExpandoObjectConverter))]
        public object Body { get; set; }
    }
}
