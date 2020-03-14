using Catchy.CacheStrategies;
using Catchy.HttpProxy;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Http;
using Xunit;

namespace Catchy.Tests
{
    public class RequestResponseInterceptorTests
    {
        [Fact]
        public void InterceptRequest_NoCacheStrategies_DoesNotThrow()
        {
            var exchange = Substitute.For<IHttpExchange>();

            // system under test
            var interceptor = new RequestResponseInterceptor(Array.Empty<ICacheStrategy>());
            var result = interceptor.InterceptRequest(exchange);

            Assert.True(result.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task InterceptRequest_NoMatchingCacheStrategy_DoesNotCache()
        {
            var exchange = Substitute.For<IHttpExchange>();

            // cache strategy can't handle the request.
            var strategy = Substitute.For<ICacheStrategy>();
            strategy.CanHandle(exchange).Returns(false);

            // system under test
            var interceptor = new RequestResponseInterceptor(new[] { strategy });
            await interceptor.InterceptRequest(exchange);

            // no caching operations should have been called
            await strategy.DidNotReceive().TrySetResponseFromCache(exchange);
            await strategy.DidNotReceive().StoreResponseInCacheAsync(exchange);
        }

        [Fact]
        public async Task InterceptRequest_MatchingCacheStrategy_DoesCache()
        {
            var exchange = Substitute.For<IHttpExchange>();

            // this cache strategy can't handle the request.
            var nonMatchingStrategy = Substitute.For<ICacheStrategy>();
            nonMatchingStrategy.CanHandle(exchange).Returns(false);
            // but this one can
            var matchingStrategy = Substitute.For<ICacheStrategy>();
            matchingStrategy.CanHandle(exchange).Returns(true);

            // system under test
            var interceptor = new RequestResponseInterceptor(new[] { nonMatchingStrategy, matchingStrategy });
            await interceptor.InterceptRequest(exchange);

            // no caching operations should have been called on the one that does not match
            await nonMatchingStrategy.DidNotReceive().TrySetResponseFromCache(exchange);
            await nonMatchingStrategy.DidNotReceive().StoreResponseInCacheAsync(exchange);
            // caching operation should have been called on the one that does match
            await matchingStrategy.Received().TrySetResponseFromCache(exchange);
        }

        [Fact]
        public async Task InterceptRequestAndResponse_NoCachedResponseYet_SavesResponse()
        {
            var exchange = Substitute.For<IHttpExchange>();
            exchange.RequestUrl.Returns(new Uri("http://www.example.com"));
            exchange.GetResponse().Returns(new Response());

            // we have a caching strategy that matches
            var matchingStrategy = Substitute.For<ICacheStrategy>();
            matchingStrategy.CanHandle(exchange).Returns(true);
            // but it doesn't have a response yet
            matchingStrategy.TrySetResponseFromCache(exchange).Returns(false);

            // system under test - the request handler and then the response handler
            var interceptor = new RequestResponseInterceptor(new[] { matchingStrategy });
            await interceptor.InterceptRequest(exchange);
            await interceptor.InterceptResponse(exchange);

            // we should have stored the response in the cache
            await matchingStrategy.Received().StoreResponseInCacheAsync(exchange);
        }

        [Fact]
        public async Task InterceptRequestAndResponse_HasCachedResponse_DoesNotStoreResponse()
        {
            var exchange = Substitute.For<IHttpExchange>();
            exchange.RequestUrl.Returns(new Uri("http://www.example.com"));
            exchange.GetResponse().Returns(new Response());

            // we have a caching strategy that matches and has a response
            var matchingStrategy = Substitute.For<ICacheStrategy>();
            matchingStrategy.CanHandle(exchange).Returns(true);
            matchingStrategy.TrySetResponseFromCache(exchange).Returns(true);

            // system under test - the request handler and then the response handler
            var interceptor = new RequestResponseInterceptor(new[] { matchingStrategy });
            await interceptor.InterceptRequest(exchange);
            await interceptor.InterceptResponse(exchange);

            // we should not stored the response in the cache since we already have a response cached.
            await matchingStrategy.DidNotReceive().StoreResponseInCacheAsync(exchange);
        }
    }
}
