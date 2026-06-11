using System.Globalization;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class TokenGeneratorEnvironmentExtensions
{
    extension(TokenGeneratorEnvironment env)
    {
        public string ToTokenGeneratorUrlParameter()
        {
            return env.ToString().ToLower(CultureInfo.InvariantCulture);
        }
    }
}
