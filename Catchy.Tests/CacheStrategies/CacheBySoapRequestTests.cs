using Catchy.CacheStrategies;
using Catchy.HttpProxy;
using NSubstitute;
using System;
using System.IO;
using System.Reflection;
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
            var request = new Request { Host = "example.com" };
            request.Headers.AddHeader("SOAPAction", "hokey-pokey");
            var canHandle = cache.CanHandle(request);
            Assert.True(canHandle);
        }

        [Fact]
        public void CanHandle_RequestMatchesConfigurationButNotSoapRequest_ReturnsFalse()
        {
            var cache = new CacheBySoapRequest(new[] { "example.com" });
            var request = new Request { Host = "example.com" }; // no soap header, should not match
            var canHandle = cache.CanHandle(request);
            Assert.False(canHandle);
        }

        [Fact]
        public void CanHandle_RequestDoesNotMatchConfiguration_ReturnsFalse()
        {
            var cache = new CacheBySoapRequest(new[] { "example.com" });
            var request = new Request { Host = "ejemplo.com" };
            request.Headers.AddHeader("SOAPAction", "hokey-pokey");
            var canHandle = cache.CanHandle(request);
            Assert.False(canHandle);
        }

        [Fact]
        public async Task StoreResponseInCacheAsync_ThenRetrieve_ReturnsStoredRequest()
        {
            // first request goes through, and the response should be cached
            var firstExchange = Substitute.For<IHttpExchange>();
            var firstRequest = new Request
            {
                Method = "POST",
                RequestUri = new Uri("http://example.com"),
            };
            SetRequestBody(firstRequest, File.ReadAllText(@"CacheStrategies\SoapRequest1.xml"));
            firstExchange.Request.Returns(firstRequest);
            var response = new Response(Encoding.UTF8.GetBytes("Hello World!"));
            firstExchange.Response.Returns(response);

            // system under test, part 1
            var cache = new CacheBySoapRequest(new[] { "example.com" });
            await cache.StoreResponseInCacheAsync(firstExchange);

            // second request comes, and the response should be returned from the cache
            // the second request will have a different requestId in the header, but it shouldn't
            // stop the caching
            var secondExchange = Substitute.For<IHttpExchange>();
            Request secondRequest = new Request
            {
                Method = "POST",
                RequestUri = new Uri("http://example.com")
            };
            SetRequestBody(secondRequest, File.ReadAllText(@"CacheStrategies\SoapRequest2.xml"));
            secondExchange.Request.Returns(secondRequest);

            // system under test, part 2
            bool success = cache.TrySetResponseFromCache(secondExchange);

            Assert.True(success);
            secondExchange.Received().Respond(response);
        }

        private void SetRequestBody(Request request, string body)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            PropertyInfo nameProperty = typeof(Request).GetProperty(nameof(request.Body));
            nameProperty.SetValue(request, bytes);
        }
    }
}
