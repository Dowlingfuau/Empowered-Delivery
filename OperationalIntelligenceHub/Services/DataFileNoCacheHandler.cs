using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace OperationalIntelligenceHub.Services
{
    // Makes the browser revalidate JSON under data/ on every request so edits are picked up without a version bump.
    public class DataFileNoCacheHandler : DelegatingHandler
    {
        public DataFileNoCacheHandler() : base(new HttpClientHandler()) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.Contains("/data/", StringComparison.OrdinalIgnoreCase))
            {
                request.SetBrowserRequestCache(BrowserRequestCache.NoCache);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
