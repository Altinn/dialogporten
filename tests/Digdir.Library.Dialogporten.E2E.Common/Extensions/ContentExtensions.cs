using AwesomeAssertions;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ContentExtensions
{
    extension(string? content)
    {
        public Guid ToGuid()
        {
            content.Should().NotBeNull();
            var rawContent = content.Trim('"');

            Guid.TryParse(rawContent, out var id)
                .Should().BeTrue();

            id.Should().NotBe(Guid.Empty);
            return id;
        }
    }
}
