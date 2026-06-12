using System.Net;
using Altinn.Authorization.ProblemDetails;

namespace Digdir.Domain.Dialogporten.Domain.Errors;

internal static class MyAppErrors
{
    private static readonly ProblemDescriptorFactory Factory
        = ProblemDescriptorFactory.New("APP");

    public static ProblemDescriptor BadRequest { get; }
        = Factory.Create(0, HttpStatusCode.BadRequest, "Bad request");

    public static ProblemDescriptor NotFound { get; }
        = Factory.Create(1, HttpStatusCode.NotFound, "Not found");

    public static ProblemDescriptor InternalServerError { get; }
        = Factory.Create(2, HttpStatusCode.InternalServerError, "Internal server error");

    public static ProblemDescriptor NotImplemented { get; }
        = Factory.Create(3, HttpStatusCode.NotImplemented, "Not implemented");
}

internal static class MyAppValidationDescriptors
{
    private static readonly ValidationErrorDescriptorFactory Factory
        = ValidationErrorDescriptorFactory.New("APP");

    public static ValidationErrorDescriptor FieldRequired { get; }
        = Factory.Create(0, "Field is required.");

    public static ValidationErrorDescriptor FieldOutOfRange { get; }
        = Factory.Create(1, "Field is out of range.");

    public static ValidationErrorDescriptor PasswordsMustMatch { get; }
        = Factory.Create(2, "Passwords must match.");
}
