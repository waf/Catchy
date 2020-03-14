using Catchy.HttpProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Http;

namespace Catchy.CacheStrategies
{
    /// <summary>
    /// Caches by URL / HTTP Method / Request body for a list of hosts.
    /// </summary>
    public class CacheByRestRequest : ICacheStrategy
    {
        private readonly MemoryCache cache = MemoryCache.Default;
        private readonly IReadOnlyCollection<string> MethodsWithBody = new[] { "POST", "PUT", "PATCH" };

        public IReadOnlyCollection<string> HandledHosts { get; }

        public CacheByRestRequest(IReadOnlyCollection<string> handledHosts)
        {
            HandledHosts = handledHosts;
        }

        public bool CanHandle(IHttpExchange httpExchange) =>
            HandledHosts.Contains(httpExchange.RequestUrl.Host);

        public async Task StoreResponseInCacheAsync(IHttpExchange httpExchange)
        {
            string cacheKey = await GetCacheKey(httpExchange);
            cache[cacheKey] = await httpExchange.GetResponse();
        }

        public async Task<bool> TrySetResponseFromCache(IHttpExchange httpExchange)
        {
            string cacheKey = await GetCacheKey(httpExchange);
            if (cache.Get(cacheKey) is Response response)
            {
                httpExchange.Respond(response);
                return true;
            }
            return false;
        }

        private async Task<string> GetCacheKey(IHttpExchange exchange)
        {
            string body = MethodsWithBody.Contains(exchange.RequestMethod)
                ? await exchange.GetRequestBody()
                : "";
            return $"{exchange.RequestMethod} {exchange.RequestUrl} {body}".GetHash();
        }
    }
}
