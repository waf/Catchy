using Catchy.HttpProxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Xml;
using Titanium.Web.Proxy.Http;
using RequestHash = System.String;

namespace Catchy.CacheStrategies
{
    /// <summary>
    /// Cache by SOAP request body content. The SOAP header is ignored.
    /// </summary>
    public class CacheBySoapRequest : ICacheStrategy
    {
        private readonly MemoryCache cache = MemoryCache.Default;

        public IReadOnlyCollection<string> HandledHosts { get; }

        public CacheBySoapRequest(IReadOnlyCollection<string> handledHosts)
        {
            HandledHosts = handledHosts;
        }

        public bool CanHandle(Request request) =>
            HandledHosts.Contains(request.Host)
            && request.Headers.HeaderExists("SOAPAction");

        public async Task StoreResponseInCacheAsync(IHttpExchange httpExchange)
        {
            RequestHash hash = GetHashForSoapBody(httpExchange);
            await httpExchange.KeepResponseBody();
            cache[hash] = httpExchange.Response;
        }

        public bool TrySetResponseFromCache(IHttpExchange httpExchange)
        {
            RequestHash hash = GetHashForSoapBody(httpExchange);
            if (cache.Get(hash) is Response response)
            {
                httpExchange.Respond(response);
                return true;
            }
            return false;
        }

        private RequestHash GetHashForSoapBody(IHttpExchange httpExchange) =>
            GetSoapBodyText(httpExchange.Request.BodyString).GetHash();

        private string GetSoapBodyText(string xmlText)
        {
            using (var xml = XmlReader.Create(new StringReader(xmlText)))
            {
                var bodyElement = xml.ReadToDescendant("Body", "http://schemas.xmlsoap.org/soap/envelope/");
                return xml.ReadInnerXml();
            }
        }
    }
}
