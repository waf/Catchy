using Catchy.HttpProxy;
using System.Collections.Generic;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Http;

namespace Catchy.CacheStrategies
{
    /// <summary>
    /// An abstraction for classes that implement a request/response caching pattern
    /// </summary>
    /// <remarks>
    /// Implementing classes have a singleton lifetime in the application and
    /// should be threadsafe.
    /// </remarks>
    public interface ICacheStrategy
    {
        /// <summary>
        /// A list of hostnames that this request handler can intercept.
        /// SSL will be decrypted for these hostnames.
        /// </summary>
        IReadOnlyCollection<string> HandledHosts { get; }

        /// <summary>
        /// Whether or not this request handler knows how to handle the given HTTP request.
        /// If this method returns false, the other methods on this interface won't be called.
        /// </summary>
        bool CanHandle(Request request);

        /// <summary>
        /// Stores the response for the given request/response pair.
        /// </summary>
        Task StoreResponseInCacheAsync(IHttpExchange httpExchange);

        /// <summary>
        /// Sets the cached response if request handler has a cached response for the given request.
        /// </summary>
        /// <remarks>
        /// The implementation should use the API provided on the <paramref name="httpExchange"/>
        /// to set the response.
        /// </remarks>
        bool TrySetResponseFromCache(IHttpExchange httpExchange);
    }
}
