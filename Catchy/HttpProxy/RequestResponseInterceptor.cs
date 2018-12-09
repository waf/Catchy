using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catchy.CacheStrategies;
using Catchy.UI;

namespace Catchy.HttpProxy
{
    public sealed class RequestResponseInterceptor
    {
        private readonly IReadOnlyCollection<ICacheStrategy> cacheStrategies;

        public RequestResponseInterceptor(IReadOnlyCollection<ICacheStrategy> cacheStrategies)
        {
            this.cacheStrategies = cacheStrategies;
        }

        public Task InterceptRequest(IHttpExchange session)
        {
            var request = session.Request;
            var cacheStrategy = cacheStrategies.FirstOrDefault(handler => handler.CanHandle(request));
            if(cacheStrategy is null)
            {
                return Task.CompletedTask;
            }

            if(cacheStrategy.TrySetResponseFromCache(session))
            {
                ConsoleUI.CachedResponseMessage(request.Url);
            }
            else
            {
                // UserData is shared state between a request/response.
                // by setting this, InterceptResponse will use it to cache the response.
                session.UserData = cacheStrategy;
            }
            return Task.CompletedTask;
        }

        public async Task InterceptResponse(IHttpExchange session)
        {
            if(session.UserData is ICacheStrategy handler)
            {
                ConsoleUI.CapturingResponseMessage(session.Request.Url);
                await handler.StoreResponseInCacheAsync(session);
            }
        }
    }
}