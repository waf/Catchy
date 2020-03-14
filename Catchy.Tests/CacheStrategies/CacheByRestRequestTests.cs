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

            var exchange = Substitute.For<IHttpExchange>();
            exchange.RequestUrl.Returns(new Uri("http://example.com/cats/"));
            exchange.RequestHeaders.Returns(new HeaderCollection());

            var canHandle = cache.CanHandle(exchange);
            Assert.True(canHandle);
        }

        [Fact]
        public void CanHandle_RequestDoesNotMatchConfiguration_ReturnsFalse()
        {
            var cache = new CacheByRestRequest(new[] { "example.com" });

            var exchange = Substitute.For<IHttpExchange>();
            exchange.RequestUrl.Returns(new Uri("http://ejemplo.com/cats/"));
            exchange.RequestHeaders.Returns(new HeaderCollection());

            var canHandle = cache.CanHandle(exchange);
            Assert.False(canHandle);
        }

        [Fact]
        public async Task StoreResponseInCacheAsync_ThenRetrieve_ReturnsStoredRequest()
        {
            // first request goes through, and the response should be cached
            var firstExchange = Substitute.For<IHttpExchange>(); firstExchange.RequestUrl.Returns(new Uri("http://example.com"));
            firstExchange.RequestMethod.Returns("GET");
            var response = new Response(Encoding.UTF8.GetBytes("Hello World!"));
            firstExchange.GetResponse().Returns(response);

            // system under test, part 1
            var cache = new CacheByRestRequest(new[] { "example.com" });
            await cache.StoreResponseInCacheAsync(firstExchange);

            // second request comes, and the response should be returned from the cache
            var secondExchange = Substitute.For<IHttpExchange>();
            secondExchange.RequestMethod.Returns("GET");
            secondExchange.RequestUrl.Returns(new Uri("http://example.com"));

            // system under test, part 2
            bool success = await cache.TrySetResponseFromCache(secondExchange);

            Assert.True(success);
            secondExchange.Received().Respond(response);
        }
    }
}
