using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MyAnimeList.Providers.MyAnimeList.APIs.JikanSystem
{
    public class JikanHeaderHandler : DelegatingHandler
    {
        // A date field holding only a year or a year and month, e.g. "from":"2027-10".
        private static readonly Regex PartialDate = new(
            "\"(from|to|aired|date)\"(\\s*:\\s*)\"(\\d{4})(?:-(\\d{2}))?\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public JikanHeaderHandler(HttpMessageHandler inner)
            : base(inner)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response is not null)
            {
                await FixPartialDatesAsync(response, cancellationToken).ConfigureAwait(false);

                var fullUrl = request.RequestUri.ToString();

                DateTime? expiry = null;

                var expiresHeader = response.Content?.Headers.Expires;

                if (expiresHeader.HasValue)
                {
                    expiry = expiresHeader.Value.UtcDateTime;
                }

                if (expiry.HasValue)
                {
                    JikanHttpMetadataStore.SetExpiry(fullUrl, expiry.Value);
                }
            }

            return response;
        }

        // Tenrai gives anime that haven't aired yet dates like "2027-10" or "2027". JikanDotNet
        // can only read full timestamps, so one such anime made a whole search or lookup fail
        // (and fall back to the backup API). Pad them to the first day of the month or year.
        internal static async Task FixPartialDatesAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = response.Content;

            if (!response.IsSuccessStatusCode || content is null ||
                content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
            {
                return;
            }

            var body = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var fixedBody = PartialDate.Replace(body, m =>
                $"\"{m.Groups[1].Value}\"{m.Groups[2].Value}\"{m.Groups[3].Value}-{(m.Groups[4].Success ? m.Groups[4].Value : "01")}-01T00:00:00+00:00\"");

            if (fixedBody == body)
            {
                return;
            }

            var replacement = new StringContent(fixedBody, Encoding.UTF8);

            // Keep the original headers (Expires is used for cache times).
            replacement.Headers.Clear();
            foreach (var header in content.Headers)
            {
                if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            response.Content = replacement;
            content.Dispose();
        }
    }
}
