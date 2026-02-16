using AwesomeAssertions;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ContentExtensions
{
    extension(string? content)
    {
        public Guid ToGuid()
        {
            if (content is null)
            {
                Assert.Fail("Content was null, expected GUID.");
            }

            var rawContent = content.Trim('"');

            if (!Guid.TryParse(rawContent, out var id))
            {
                Assert.Fail($"Could not parse guid from content, {rawContent}");
            }

            id.Should().NotBe(Guid.Empty);

            return id;
        }
    }
}
