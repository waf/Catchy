using System;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;

namespace Catchy.HttpProxy
{
    /// <summary>
    /// Our model of a single request and corresponding response.
    /// This class exposes Titanium Proxy's <see cref="SessionEventArgs"/> properties and
    /// methods on an interface for easier testing / mocking.
    /// </summary>
    public class HttpExchange : IHttpExchange
    {
        private readonly SessionEventArgs session;

        public HttpExchange(SessionEventArgs session)
        {
            this.session = session;
        }

        public Request Request => session.HttpClient.Request;
        public Response Response => session.HttpClient.Response;

        public object? UserData
        {
            get => session.UserData;
            set => session.UserData = value;
        }

        public void Respond(Response response) =>
            session.Respond(response);

        public async Task KeepResponseBody(CancellationToken cancellationToken = default)
        {
            Response.KeepBody = true; // keep the response body around after the request/response is finished
            if(Response.ContentLength > 0) // GetResponseBody will throw if there's no content
            {
                _ = await session.GetResponseBody(cancellationToken); // force the body to be read, so we can access it later
            }
        }
    }

    /// <summary>
    /// Our model of a single request and corresponding response.
    /// </summary>
    public interface IHttpExchange
    {
        Request Request { get; }
        Response Response { get; }
        object? UserData { get; set; }

        void Respond(Response response);
        Task KeepResponseBody(CancellationToken cancellationToken = default);
    }
}
