using Catchy.CacheStrategies;
using Catchy.HttpProxy;
using NSubstitute;
using System;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Http;
using Xunit;

namespace Catchy.Tests.CacheStrategies
{
    public class CacheByRestRequestTests
    {
        [Fact]
        public void CanHandle_RequestMatchesConfiguration_ReturnsTrue()
        {
            var cache = new CacheByRestRequest(new[] { "example.com" });
            var request = new Request { Host = "example.com" };
            var canHandle = cache.CanHandle(request);
            Assert.True(canHandle);
        }

        [Fact]
        public void CanHandle_RequestDoesNotMatchConfiguration_ReturnsFalse()
        {
            var cache = new CacheByRestRequest(new[] { "example.com" });
            var request = new Request { Host = "ejemplo.com" };
            var canHandle = cache.CanHandle(request);
            Assert.False(canHandle);
        }

        [Fact]
        public async Task StoreResponseInCacheAsync_ThenRetrieve_ReturnsStoredRequest()
        {
            // first request goes through, and the response should be cached
            var firstExchange = Substitute.For<IHttpExchange>();
            firstExchange.Request.Returns(new Request {
                Method = "GET",
                RequestUri = new Uri("http://example.com")
            });
            var response = new Response(Encoding.UTF8.GetBytes("Hello World!"));
            firstExchange.Response.Returns(response);

            // system under test, part 1
            var cache = new CacheByRestRequest(new[] { "example.com" });
            await cache.StoreResponseInCacheAsync(firstExchange);

            // second request comes, and the response should be returned from the cache
            var secondExchange = Substitute.For<IHttpExchange>();
            secondExchange.Request.Returns(new Request
            {
                Method = "GET",
                RequestUri = new Uri("http://example.com")
            });

            // system under test, part 2
            bool success = cache.TrySetResponseFromCache(secondExchange);

            Assert.True(success);
            secondExchange.Received().Respond(response);
        }
    }
}
