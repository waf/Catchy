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

        public bool CanHandle(IHttpExchange httpExchange) =>
            HandledHosts.Contains(httpExchange.RequestUrl.Host)
            && httpExchange.RequestHeaders.HeaderExists("SOAPAction");

        public async Task StoreResponseInCacheAsync(IHttpExchange httpExchange)
        {
            RequestHash hash = await GetRequestBodyHash(httpExchange);
            cache[hash] = await httpExchange.GetResponse();
        }

        public async Task<bool> TrySetResponseFromCache(IHttpExchange httpExchange)
        {
            string hash = await GetRequestBodyHash(httpExchange);
            if (cache.Get(hash) is Response response)
            {
                httpExchange.Respond(response);
                return true;
            }
            return false;
        }

        private async Task<RequestHash> GetRequestBodyHash(IHttpExchange httpExchange)
        {
            var body = await httpExchange.GetRequestBody();
            return GetSoapBodyText(body).GetHash();
        }

        private string GetSoapBodyText(string xmlText)
        {
            using (var xml = XmlReader.Create(new StringReader(xmlText)))
            {
                _ = xml.ReadToDescendant("Body", "http://schemas.xmlsoap.org/soap/envelope/");
                return xml.ReadInnerXml();
            }
        }
    }
}
