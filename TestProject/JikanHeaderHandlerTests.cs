using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Jellyfin.Plugin.MyAnimeList.Providers.MyAnimeList.APIs.JikanSystem;
using NUnit.Framework;

namespace Jellyfin.Plugin.MyAnimeList.Tests
{
    public class JikanHeaderHandlerTests
    {
        private static async Task<HttpResponseMessage> Fix(string json, string mediaType = "application/json")
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, mediaType)
            };
            response.Content.Headers.Expires = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

            await JikanHeaderHandler.FixPartialDatesAsync(response, CancellationToken.None);
            return response;
        }

        [Test]
        public async Task PadsYearMonthAndYearOnlyDates()
        {
            var response = await Fix("{\"aired\":{\"from\":\"2027-10\",\"to\":null},\"x\":{\"from\": \"2028\"}}");
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(body, Is.EqualTo("{\"aired\":{\"from\":\"2027-10-01T00:00:00+00:00\",\"to\":null},\"x\":{\"from\": \"2028-01-01T00:00:00+00:00\"}}"));
        }

        [Test]
        public async Task LeavesFullDatesAndOtherFieldsAlone()
        {
            const string json = "{\"title\":\"2027\",\"aired\":{\"from\":\"2023-09-29T00:00:00+00:00\",\"to\":\"2024-03-22T00:00:00+00:00\"}}";
            var response = await Fix(json);

            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(json));
        }

        [Test]
        public async Task KeepsContentHeaders()
        {
            var response = await Fix("{\"from\":\"2027-10\"}");

            Assert.That(response.Content.Headers.Expires, Is.EqualTo(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        }

        [Test]
        public async Task IgnoresNonJson()
        {
            var response = await Fix("from:\"2027-10\"", "text/plain");

            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("from:\"2027-10\""));
        }
    }
}
