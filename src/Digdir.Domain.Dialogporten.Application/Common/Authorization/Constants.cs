﻿namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

public static class Constants
{
    public const string MainResource = "main";
    public const string ReadAction = "read";
    public const string TransmissionReadAction = "transmissionread";
    public static readonly Uri UnauthorizedUri = new("urn:dialogporten:unauthorized");
    public const string CorrespondenceScope = "digdir:dialogporten.correspondence";
    public const string ServiceOwnerAdminScope = "digdir:dialogporten.serviceprovider.admin";
    public const string LegacyHtmlScope = "digdir:dialogporten.serviceprovider.legacyhtml";
}
