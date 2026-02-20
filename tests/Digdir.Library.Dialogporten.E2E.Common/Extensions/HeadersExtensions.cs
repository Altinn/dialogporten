using System.Net.Http.Headers;
using AwesomeAssertions;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class HeadersExtensions
{
    extension(HttpResponseHeaders headers)
    {
        public Guid ETagToGuid()
        {
            headers.TryGetValues("ETag", out var etagValues)
                .Should().BeTrue("ETag header was missing.");

            return etagValues!.First().ToGuid();
        }
    }
}
