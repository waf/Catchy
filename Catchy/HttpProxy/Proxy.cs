using System;
using System.Net;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace Catchy.HttpProxy
{
    /// <summary>
    /// Configures and disposes the <see cref="Titanium.Web.Proxy.ProxyServer"/>.
    /// </summary>
    public class Proxy : IDisposable
    {
        private readonly ProxyServer proxyServer;
        private readonly ExplicitProxyEndPoint explicitEndPoint;
        private readonly Predicate<Uri> shouldDecrypt;

        public event AsyncEventHandler<IHttpExchange>? OnRequest;
        public event AsyncEventHandler<IHttpExchange>? OnResponse;
        public event EventHandler<Exception>? OnError;

        public Proxy(IPAddress ipAddress, int port, Predicate<Uri> shouldDecrypt)
        {
            this.proxyServer = new ProxyServer();
            this.explicitEndPoint = new ExplicitProxyEndPoint(ipAddress, port, true);
            this.shouldDecrypt = shouldDecrypt;

            proxyServer.EnableHttp2 = true;
            proxyServer.CertificateManager.CreateRootCertificate(persistToFile: true);
            proxyServer.CertificateManager.TrustRootCertificate(machineTrusted: true);
            proxyServer.AddEndPoint(explicitEndPoint);

            explicitEndPoint.BeforeTunnelConnectRequest += BeforeTunnelConnectRequest;
            proxyServer.BeforeRequest += OnRequestHandler;
            proxyServer.BeforeResponse += OnResponseHandler;
            proxyServer.ExceptionFunc = exception => OnError?.Invoke(this, exception);

            proxyServer.Start();
            proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
        }

        private Task BeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            var uri = e.HttpClient.Request.RequestUri;
            e.DecryptSsl = this.shouldDecrypt(uri);
            return Task.CompletedTask;
        }

        private async Task OnRequestHandler(object sender, SessionEventArgs sessionEventArgs)
        {
            if (OnRequest is null) return;
            await OnRequest.Invoke(sender, new HttpExchange(sessionEventArgs));
        }

        private async Task OnResponseHandler(object sender, SessionEventArgs sessionEventArgs)
        {
            if (OnResponse is null) return;
            await OnResponse.Invoke(sender, new HttpExchange(sessionEventArgs));
        }

        public void Dispose()
        {
            explicitEndPoint.BeforeTunnelConnectRequest -= BeforeTunnelConnectRequest;
            proxyServer.BeforeRequest -= OnRequestHandler;
            proxyServer.BeforeResponse -= OnResponseHandler;
            proxyServer.Stop();
        }
    }
}
