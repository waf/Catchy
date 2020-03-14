using Catchy.CacheStrategies;
using Catchy.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catchy.HttpProxy
{
    public sealed class RequestResponseInterceptor
    {
        private readonly IReadOnlyCollection<ICacheStrategy> cacheStrategies;

        public RequestResponseInterceptor(IReadOnlyCollection<ICacheStrategy> cacheStrategies)
        {
            this.cacheStrategies = cacheStrategies;
        }

        public async Task InterceptRequest(IHttpExchange session)
        {
            var cacheStrategy = cacheStrategies.FirstOrDefault(handler => handler.CanHandle(session));
            if (cacheStrategy is null)
            {
                return;
            }

            if (await cacheStrategy.TrySetResponseFromCache(session))
            {
                ConsoleUI.CachedResponseMessage(session.RequestUrl.ToString());
            }
            else
            {
                // UserData is shared state between a request/response.
                // by setting this, InterceptResponse will use it to cache the response.
                session.UserData = cacheStrategy;
            }
        }

        public async Task InterceptResponse(IHttpExchange session)
        {
            if (session.UserData is ICacheStrategy handler)
            {
                ConsoleUI.CapturingResponseMessage(session.RequestUrl.ToString());
                await handler.StoreResponseInCacheAsync(session);
            }
        }
    }
}