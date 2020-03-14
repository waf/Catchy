using Catchy.CacheStrategies;
using Catchy.HttpProxy;
using NSubstitute;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Http;
using Xunit;

namespace Catchy.Tests.CacheStrategies
{
    public class CacheBySoapRequestTests
    {
        [Fact]
        public void CanHandle_RequestMatchesConfigurationAndIsSoapRequest_ReturnsTrue()
        {
            var cache = new CacheBySoapRequest(new[] { "example.com" });

            var exchange = Substitute.For<IHttpExchange>();
            exchange.RequestUrl.Returns(new Uri("http://example.com/websvc"));
            exchange.RequestHeaders.Returns(new HeaderCollection());
            exchange.RequestHeaders.AddHeader("SOAPAction", "hokey-pokey");

            var canHandle = cache.CanHandle(exchange);
            Assert.True(canHandle);
        }

        [Fact]
        public void CanHandle_RequestMatchesConfigurationButNotSoapRequest_ReturnsFalse()
        {
            var cache = new CacheBySoapRequest(new[] { "example.com" });

            var exchange = Substitute.For<IHttpExchange>();
            exchange.RequestUrl.Returns(new Uri("http://example.com/websvc"));
            exchange.RequestHeaders.Returns(new HeaderCollection());
            // no soap header, should not handle

            var canHandle = cache.CanHandle(exchange);
            Assert.False(canHandle);
        }

        [Fact]
        public void CanHandle_RequestDoesNotMatchConfiguration_ReturnsFalse()
        {
            var cache = new CacheBySoapRequest(new[] { "example.com" });

            // url does not match
            var exchange = Substitute.For<IHttpExchange>();
            exchange.RequestUrl.Returns(new Uri("http://ejemplo.com/websvc"));
            exchange.RequestHeaders.Returns(new HeaderCollection());
            exchange.RequestHeaders.AddHeader("SOAPAction", "hokey-pokey");

            var canHandle = cache.CanHandle(exchange);
            Assert.False(canHandle);
        }

        [Fact]
        public async Task StoreResponseInCacheAsync_ThenRetrieve_ReturnsStoredRequest()
        {
            // first request goes through, and the response should be cached
            var firstExchange = Substitute.For<IHttpExchange>();
            firstExchange.RequestMethod.Returns("POST");
            firstExchange.RequestUrl.Returns(new Uri("http://example.com"));
            firstExchange.GetRequestBody().Returns(
                Task.FromResult(
                    File.ReadAllText(@"CacheStrategies\SoapRequest1.xml")
                )
            );

            var response = new Response(Encoding.UTF8.GetBytes("Hello World!"));
            firstExchange.GetResponse().Returns(response);

            // system under test, part 1
            var cache = new CacheBySoapRequest(new[] { "example.com" });
            await cache.StoreResponseInCacheAsync(firstExchange);

            // second request comes, and the response should be returned from the cache
            // the second request will have a different requestId in the header, but it shouldn't
            // stop the caching
            var secondExchange = Substitute.For<IHttpExchange>();
            secondExchange.RequestMethod.Returns("POST");
            secondExchange.RequestUrl.Returns(new Uri("http://example.com"));
            secondExchange.GetRequestBody().Returns(
                Task.FromResult(
                    File.ReadAllText(@"CacheStrategies\SoapRequest2.xml")
                )
            );

            // system under test, part 2
            bool success = await cache.TrySetResponseFromCache(secondExchange);

            Assert.True(success);
            secondExchange.Received().Respond(response);
        }
    }
}
