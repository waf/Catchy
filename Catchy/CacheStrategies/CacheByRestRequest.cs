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

        public bool CanHandle(Request request) =>
            HandledHosts.Contains(request.Host);

        public async Task StoreResponseInCacheAsync(IHttpExchange httpExchange)
        {
            await httpExchange.KeepResponseBody();
            string cacheKey = GetCacheKey(httpExchange.Request);
            cache[cacheKey] = httpExchange.Response;
        }

        public bool TrySetResponseFromCache(IHttpExchange httpExchange)
        {
            string cacheKey = GetCacheKey(httpExchange.Request);
            if (cache.Get(cacheKey) is Response response)
            {
                httpExchange.Respond(response);
                return true;
            }
            return false;
        }

        private string GetCacheKey(Request request)
        {
            string body = MethodsWithBody.Contains(request.Method)
                ? request.BodyString
                : "";
            return $"{request.Method} {request.Url} {body}".GetHash();
        }
    }
}
