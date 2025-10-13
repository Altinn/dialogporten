using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.ServiceOwner.Dialogs.Validators;

public class ContentValueDtoValidatorTests
{
    [Theory]
    [InlineData(true, 512, true)]
    [InlineData(true, 513, false)]
    [InlineData(false, 256, false)]
    [InlineData(false, 255, true)]
    public void Validate_ContentValueDto_With_CorrespondenceScope(bool correspondenceScope, int length, bool expected)
    {
        var user = new ValidatorTestUser(correspondenceScope ? [new Claim("scope", AuthorizationScope.CorrespondenceScope)] : []);
        var dialogContentType = new DialogContentType(DialogContentType.Values.Title).MapValue(DialogContentType.Values.Title);

        var validator = new ContentValueDtoValidator(dialogContentType, user);

        var contentValueDto = new ContentValueDto
        {
            Value =
            [
                new LocalizationDto
                {
                    Value = new string('a', length),
                    LanguageCode = "nb"
                }
            ],
            MediaType = "text/plain"
        };

        var result = validator.Validate(contentValueDto);
        Assert.Equal(expected, result.IsValid);
    }
}

internal sealed class ValidatorTestUser : IUser
{

    private static string DefaultPid => "22834498646";
    private readonly ClaimsPrincipal _principal;

    public ValidatorTestUser(List<Claim> claims)
    {
        claims.AddRange(GetDefaultClaims());
        _principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    public ClaimsPrincipal GetPrincipal() => _principal;

    public static List<Claim> GetDefaultClaims()
    {
        return
        [
            new Claim(ClaimTypes.Name, "Integration Test User"),
            new Claim("acr", Constants.IdportenLoaHigh),
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim("pid", DefaultPid),
            new Claim("urn:altinn:org", "ttd"),
            new Claim("consumer",
                """
                {
                    "authority": "iso6523-actorid-upis",
                    "ID": "0192:991825827"
                }
                """)
        ];
    }
}
