namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

public static class LegacyAuthorizationAttributeExtensions
{
    extension(string? authorizationAttribute)
    {
        /// <summary>
        /// The legacy authorization attribute as API consumers should see it, i.e. with the
        /// <see cref="Constants.ExcludedTransmissionAttribute"/> rollback sentinel hidden. The sentinel is
        /// an implementation detail of the rollback path, and echoing it would both leak it to clients and
        /// make a GET → PUT round trip fail the rule that a context and a legacy attribute are exclusive.
        /// </summary>
        public string? WithoutExclusionSentinel =>
            authorizationAttribute == Constants.ExcludedTransmissionAttribute ? null : authorizationAttribute;
    }
}
