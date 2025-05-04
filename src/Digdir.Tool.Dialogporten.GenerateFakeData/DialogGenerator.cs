using Bogus;
using System.Globalization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

namespace Digdir.Tool.Dialogporten.GenerateFakeData;

public static class DialogGenerator
{
    private static readonly DateTime RefTime = new(2026, 1, 1);
    private static readonly Randomizer MyRandomizer = new();
    private static readonly Faker MyFaker = new();

    private static readonly Faker<SearchTagDto> SearchTagFaker = new Faker<SearchTagDto>()
        .RuleFor(o => o.Value, f => f.Random.AlphaNumeric(10));

    private static readonly Faker<AttachmentUrlDto> AttachmentUrlFaker = new Faker<AttachmentUrlDto>()
        .RuleFor(o => o.Url, f => new Uri(f.Internet.UrlWithPath(Uri.UriSchemeHttps))) // Potential Optimization: Use string or reuse Uri instances
        .RuleFor(o => o.ConsumerType, f => f.PickRandom<AttachmentUrlConsumerType.Values>());

    private static readonly Faker<AttachmentDto> AttachmentFaker = new Faker<AttachmentDto>()
        .RuleFor(o => o.Id, _ => IdentifiableExtensions.CreateVersion7())
        .RuleFor(o => o.DisplayName, f => GenerateFakeLocalizations(f.Random.Number(2, 5)))
        .RuleFor(o => o.Urls, _ => AttachmentUrlFaker.Generate(MyRandomizer.Number(1, 3))); // Reuse static faker

    private static readonly Faker<ApiActionEndpointDto> ApiActionEndpointFaker = new Faker<ApiActionEndpointDto>()
        .RuleFor(o => o.Url, f => new Uri(f.Internet.UrlWithPath(Uri.UriSchemeHttps))) // Potential Optimization
        .RuleFor(o => o.HttpMethod, f => f.PickRandom<HttpVerb.Values>())
        .RuleFor(o => o.Version, f => "v" + f.Random.Number(100, 999))
        .RuleFor(o => o.Deprecated, f => f.Random.Bool())
        .RuleFor(o => o.RequestSchema, f => new Uri(f.Internet.UrlWithPath(Uri.UriSchemeHttps))) // Potential Optimization
        .RuleFor(o => o.ResponseSchema, f => new Uri(f.Internet.UrlWithPath(Uri.UriSchemeHttps))) // Potential Optimization
        .RuleFor(o => o.DocumentationUrl, f => new Uri(f.Internet.UrlWithPath(Uri.UriSchemeHttps))); // Potential Optimization

    private static readonly Faker<ApiActionDto> ApiActionFaker = new Faker<ApiActionDto>()
        .RuleFor(o => o.Id, () => IdentifiableExtensions.CreateVersion7())
        .RuleFor(o => o.Action, f => f.Random.AlphaNumeric(8))
        .RuleFor(o => o.Name, f => f.Random.AlphaNumeric(8))
        .RuleFor(o => o.Endpoints, _ => ApiActionEndpointFaker.Generate(MyRandomizer.Number(min: 1, 4)));

    private static readonly List<DialogActivityType.Values> AllowedActivityTypes = Enum.GetValues<DialogActivityType.Values>()
            .Where(x => x is not DialogActivityType.Values.TransmissionOpened
                and not DialogActivityType.Values.CorrespondenceConfirmed
                and not DialogActivityType.Values.CorrespondenceOpened)
            .ToList();

    private static readonly Faker<ActivityDto> ActivityFaker = new Faker<ActivityDto>()
        .RuleFor(o => o.Id, () => IdentifiableExtensions.CreateVersion7())
        .RuleFor(o => o.CreatedAt, f => f.Date.Past())
        .RuleFor(o => o.ExtendedType, f => new Uri(f.Internet.UrlWithPath(Uri.UriSchemeHttps))) // Potential Optimization
        .RuleFor(o => o.Type, f => AllowedActivityTypes[f.Random.Number(0, AllowedActivityTypes.Count - 1)])
        .RuleFor(o => o.PerformedBy, f => new ActorDto { ActorType = ActorType.Values.PartyRepresentative, ActorName = f.Name.FullName() }) // Allocates ActorDto
        .RuleFor(o => o.Description, (f, o) => o.Type == DialogActivityType.Values.Information
            ? GenerateFakeLocalizations(f.Random.Number(4, 8)) // Potential Optimization
            : null);

    private static readonly Faker<TransmissionDto> TransmissionFaker = new Faker<TransmissionDto>()
        .RuleFor(o => o.Id, _ => IdentifiableExtensions.CreateVersion7())
        .RuleFor(o => o.CreatedAt, f => f.Date.Past())
        .RuleFor(o => o.Type, f => f.PickRandom<DialogTransmissionType.Values>())
        .RuleFor(o => o.Sender, _ => new ActorDto { ActorType = ActorType.Values.ServiceOwner })
        .RuleFor(o => o.Content, _ => GenerateFakeTransmissionContent());

    private static readonly Faker<CreateDialogDto> CreateDialogFaker = new Faker<CreateDialogDto>()
        // We need to handle id, serviceResource, party, and others passed as arguments
        // RuleFor cannot directly use external parameters easily. We handle these post-generation.
        // Placeholder rules are set, real values might be overridden later.
        .RuleFor(o => o.Id, _ => IdentifiableExtensions.CreateVersion7())
        .RuleFor(o => o.ServiceResource, _ => GenerateFakeResource())
        .RuleFor(o => o.Party, _ => GenerateRandomParty())
        .RuleFor(o => o.Progress, f => f.Random.Number(0, 100))
        .RuleFor(o => o.ExtendedStatus, f => f.Random.AlphaNumeric(10))
        .RuleFor(o => o.ExternalReference, f => f.Random.AlphaNumeric(10))
        .RuleFor(o => o.CreatedAt, _ => null)
        .RuleFor(o => o.UpdatedAt, _ => null)
        .RuleFor(o => o.DueAt, f => f.Date.Future(10, RefTime))
        .RuleFor(o => o.ExpiresAt, f => f.Date.Future(20, RefTime.AddYears(11)))
        .RuleFor(o => o.VisibleFrom, _ => null)
        .RuleFor(o => o.Status, f => f.PickRandom<DialogStatus.Values>())
        .RuleFor(o => o.Content, _ => GenerateFakeContent())
        .RuleFor(o => o.SearchTags, _ => GenerateFakeSearchTags())
        .RuleFor(o => o.Attachments, _ => GenerateFakeDialogAttachments())
        .RuleFor(o => o.GuiActions, _ => GenerateUniquePriorityGuiActionsList())
        .RuleFor(o => o.ApiActions, _ => GenerateFakeDialogApiActions())
        .RuleFor(o => o.Activities, _ => GenerateFakeDialogActivities())
        .RuleFor(o => o.Process, _ => GenerateFakeProcessUri())
        .RuleFor(o => o.Transmissions, _ => GenerateFakeDialogTransmissions());


    public static CreateDialogCommand GenerateSimpleFakeCreateDialogCommand(Guid? id = null) => new()
    {
        Dto = GenerateSimpleFakeDialog(id)
    };

    public static CreateDialogCommand GenerateFakeCreateDialogCommand(
        Guid? id = null,
        string? serviceResource = null,
        string? party = null,
        int? progress = null,
        string? extendedStatus = null,
        string? externalReference = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? visibleFrom = null,
        string? process = null,
        DialogStatus.Values? status = null,
        ContentDto? content = null,
        List<SearchTagDto>? searchTags = null,
        List<AttachmentDto>? attachments = null,
        List<GuiActionDto>? guiActions = null,
        List<ApiActionDto>? apiActions = null,
        List<ActivityDto>? activities = null,
        List<TransmissionDto>? transmissions = null,
        bool isSilentUpdate = false) => new()
        {
            IsSilentUpdate = isSilentUpdate,
            Dto = GenerateFakeDialogs(
                count: 1,
                id: id,
                serviceResource: serviceResource,
                party: party,
                progress: progress,
                extendedStatus: extendedStatus,
                externalReference: externalReference,
                createdAt: createdAt,
                updatedAt: updatedAt,
                dueAt: dueAt,
                expiresAt: expiresAt,
                visibleFrom: visibleFrom,
                process: process,
                status: status,
                content: content,
                searchTags: searchTags,
                attachments: attachments,
                guiActions: guiActions,
                apiActions: apiActions,
                activities: activities,
                transmissions: transmissions
            )[0]
        };

    public static CreateDialogDto GenerateFakeDialog(
        Guid? id = null,
        string? serviceResource = null,
        string? party = null,
        int? progress = null,
        string? extendedStatus = null,
        string? externalReference = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? visibleFrom = null,
        string? process = null,
        DialogStatus.Values? status = null,
        ContentDto? content = null,
        List<SearchTagDto>? searchTags = null,
        List<AttachmentDto>? attachments = null,
        List<GuiActionDto>? guiActions = null,
        List<ApiActionDto>? apiActions = null,
        List<ActivityDto>? activities = null,
        List<TransmissionDto>? transmissions = null)
    {
        return GenerateFakeDialogs(
            count: 1,
            id: id,
            serviceResource: serviceResource,
            party: party,
            progress: progress,
            extendedStatus: extendedStatus,
            externalReference: externalReference,
            createdAt: createdAt,
            updatedAt: updatedAt,
            dueAt: dueAt,
            expiresAt: expiresAt,
            visibleFrom: visibleFrom,
            process: process,
            status: status,
            content: content,
            searchTags: searchTags,
            attachments: attachments,
            guiActions: guiActions,
            apiActions: apiActions,
            activities: activities,
            transmissions: transmissions
        )[0];
    }

    public static List<CreateDialogDto> GenerateFakeDialogs(
        int count = 1,
        Guid? id = null,
        string? serviceResource = null,
        string? party = null,
        Func<string?>? serviceResourceGenerator = null,
        Func<string?>? partyGenerator = null,
        int? progress = null,
        string? extendedStatus = null,
        string? externalReference = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? visibleFrom = null,
        string? process = null,
        DialogStatus.Values? status = null,
        ContentDto? content = null,
        List<SearchTagDto>? searchTags = null,
        List<AttachmentDto>? attachments = null,
        List<GuiActionDto>? guiActions = null,
        List<ApiActionDto>? apiActions = null,
        List<ActivityDto>? activities = null,
        List<TransmissionDto>? transmissions = null)
    {
        var dialogs = CreateDialogFaker.Generate(count);

        for (var i = 0; i < count; i++)
        {
            var dialog = dialogs[i];

            if (id.HasValue) dialog.Id = id.Value;
            dialog.ServiceResource = serviceResourceGenerator?.Invoke() ?? serviceResource ?? dialog.ServiceResource;
            dialog.Party = partyGenerator?.Invoke() ?? party ?? dialog.Party;
            if (progress.HasValue) dialog.Progress = progress.Value;
            if (extendedStatus != null) dialog.ExtendedStatus = extendedStatus;
            if (externalReference != null) dialog.ExternalReference = externalReference;
            if (createdAt.HasValue) dialog.CreatedAt = createdAt.Value;
            if (updatedAt.HasValue) dialog.UpdatedAt = updatedAt.Value;
            if (dueAt.HasValue) dialog.DueAt = dueAt.Value;
            if (expiresAt.HasValue) dialog.ExpiresAt = expiresAt.Value;
            if (visibleFrom.HasValue) dialog.VisibleFrom = visibleFrom.Value;
            if (process != null) dialog.Process = process;
            if (status.HasValue) dialog.Status = status.Value;


            if (content != null) dialog.Content = content;
            if (searchTags != null) dialog.SearchTags = searchTags;
            if (attachments != null) dialog.Attachments = attachments;
            if (guiActions != null) dialog.GuiActions = guiActions;
            if (apiActions != null) dialog.ApiActions = apiActions;
            if (activities != null) dialog.Activities = activities;
            if (transmissions != null) dialog.Transmissions = transmissions;
        }

        return dialogs;
    }

    public static CreateDialogDto GenerateSimpleFakeDialog(Guid? id = null)
    {
        return GenerateFakeDialog(
            id: id,
            activities: [],
            attachments: [],
            guiActions: [],
            apiActions: [],
            searchTags: [],
            transmissions: []);
    }

    public static string GenerateFakeResource(Func<string?>? generator = null)
    {
        var generatedValue = generator?.Invoke();
        if (generatedValue != null) return generatedValue;

        const string resourcePrefix = "urn:altinn:resource:";
        // Apply a power function to skew the distribution towards higher numbers
        const int numberOfDistinctResources = 1000;
        const int exponent = 15;
        var biasedRandom = Math.Pow(MyRandomizer.Double(), 1.0 / exponent);
        var result = 1 + (int)(biasedRandom * (numberOfDistinctResources - 1));

        return resourcePrefix + result.ToString("D4", CultureInfo.InvariantCulture);
    }

    public static string GenerateRandomParty(Func<string?>? generator = null, bool forcePerson = false)
    {
        var generatedValue = generator?.Invoke();
        if (generatedValue != null) return generatedValue;

        return MyRandomizer.Bool() && !forcePerson
            ? $"urn:altinn:organization:identifier-no:{GenerateFakeOrgNo()}"
            : $"urn:altinn:person:identifier-no:{GenerateFakePid()}";
    }

    private static readonly int[] SocialSecurityNumberWeights1 = [3, 7, 6, 1, 8, 9, 4, 5, 2];
    private static readonly int[] SocialSecurityNumberWeights2 = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
    private static readonly int[] OrgNumberWeights = [3, 2, 7, 6, 5, 4, 3, 2];
    private static readonly DateTime BirthDateRangeBegin = new(1965, 1, 1);
    private static readonly DateTime BirthDateRangeEnd = new(1970, 1, 1);
    private static readonly int BirthDateRangeDays = (BirthDateRangeEnd - BirthDateRangeBegin).Days;

    private static string GenerateFakePid()
    {
        return GenerateNewFakePid();
    }

    private static string GenerateNewFakePid() // Renamed original logic
    {
        int c1, c2;
        string pidWithoutControlDigits;
        do
        {
            var dateOfBirth = BirthDateRangeBegin.AddDays(MyRandomizer.Number(BirthDateRangeDays));
            var individualNumber = GetRandomIndividualNumber(dateOfBirth.Year);
            pidWithoutControlDigits = dateOfBirth.ToString("ddMMyy", CultureInfo.InvariantCulture) + individualNumber;
            c1 = CalculateControlDigit(pidWithoutControlDigits, SocialSecurityNumberWeights1);
            c2 = CalculateControlDigit(pidWithoutControlDigits + c1, SocialSecurityNumberWeights2);
        } while (c1 == -1 || c2 == -1);
        return pidWithoutControlDigits + c1 + c2;
    }


    private static string GenerateFakeOrgNo()
    {
        string orgNumberWithoutControlDigit;
        int c;
        do
        {
            orgNumberWithoutControlDigit = MyRandomizer.Number(99000000, 99999999).ToString(CultureInfo.InvariantCulture);
            c = CalculateControlDigit(orgNumberWithoutControlDigit, OrgNumberWeights);
        } while (c == -1);
        return orgNumberWithoutControlDigit + c;
    }

    private static string GetRandomIndividualNumber(int year)
    {
        int rangeStart, rangeEnd;
        switch (year)
        {
            case < 1900:
                throw new ArgumentException($"Invalid birth year: {year}", nameof(year));
            case < 1940:
                rangeStart = 1; rangeEnd = 499;
                break;
            case < 2000:
                rangeStart = 900; rangeEnd = 999;
                break;
            case < 2040:
                rangeStart = 500; rangeEnd = 999;
                break;
            default:
                throw new ArgumentException($"Invalid birth year: {year}", nameof(year));
        }

        return MyRandomizer.Number(rangeStart, rangeEnd).ToString("D3", CultureInfo.InvariantCulture);
    }

    private static int CalculateControlDigit(string input, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (input[i] - '0') * weights[i];
        }
        var mod = sum % 11;
        return mod == 1 ? -1 : (11 - mod) % 11;
    }

    public static ActivityDto GenerateFakeDialogActivity(DialogActivityType.Values? type = null)
    {
        var activity = ActivityFaker.Generate();
        if (type != null)
        {
            activity.Type = type.Value;
        }

        return activity;
    }

    public static List<TransmissionDto> GenerateFakeDialogTransmissions(int? count = null, DialogTransmissionType.Values? type = null)
    {
        var transmissions = TransmissionFaker.Generate(count ?? MyRandomizer.Number(1, 4));
        if (type == null) return transmissions;
        foreach (var transmission in transmissions)
        {
            transmission.Type = type.Value;
        }

        return transmissions;
    }

    public static List<ActivityDto> GenerateFakeDialogActivities(int? count = null, DialogActivityType.Values? type = null)
    {
        var activities = ActivityFaker.Generate(count ?? MyRandomizer.Number(1, 4));
        if (type == null) return activities;
        foreach (var activity in activities)
        {
            activity.Type = type.Value;
        }

        return activities;
    }

    public static List<ApiActionDto> GenerateFakeDialogApiActions(int? count = null)
        => ApiActionFaker.Generate(count ?? MyRandomizer.Number(1, 4));

    public static List<ApiActionEndpointDto> GenerateFakeDialogApiActionEndpoints(int? count = null)
         => ApiActionEndpointFaker.Generate(count ?? MyRandomizer.Number(1, 4));

    public static string GenerateFakeProcessUri() => MyFaker.Internet.UrlWithPath(Uri.UriSchemeHttps);

    public static List<GuiActionDto> GenerateFakeDialogGuiActions(int? count = null)
        => GenerateUniquePriorityGuiActionsList(count);

    public static AttachmentDto GenerateFakeDialogAttachment()
        => AttachmentFaker.Generate();

    public static List<AttachmentDto> GenerateFakeDialogAttachments(int? count = null)
        => AttachmentFaker.Generate(count ?? MyRandomizer.Number(1, 6));

    public static List<AttachmentUrlDto> GenerateFakeDialogAttachmentUrls(int? count = null)
        => AttachmentUrlFaker.Generate(count ?? MyRandomizer.Number(1, 3));

    public static List<SearchTagDto> GenerateFakeSearchTags(int? count = null)
        => SearchTagFaker.Generate(count ?? MyRandomizer.Number(1, 6));

    public static ContentDto GenerateFakeContent()
    {
        var content = new ContentDto
        {
            Title = new ContentValueDto { Value = GenerateFakeLocalizations(MyRandomizer.Number(1, 4)) },
            Summary = new ContentValueDto { Value = GenerateFakeLocalizations(MyRandomizer.Number(7, 10)) }
        };

        if (MyRandomizer.Bool())
        {
            content.SenderName = new ContentValueDto { Value = GenerateFakeLocalizations(MyRandomizer.Number(1, 3)) };
        }
        if (MyRandomizer.Bool())
        {
            content.AdditionalInfo = new ContentValueDto { MediaType = Domain.Dialogporten.Domain.MediaTypes.PlainText, Value = GenerateFakeLocalizations(MyRandomizer.Number(10, 20)) };
        }
        return content;
    }

    public static TransmissionContentDto GenerateFakeTransmissionContent()
    {
        var content = new TransmissionContentDto
        {
            Title = new ContentValueDto { Value = GenerateFakeLocalizations(MyRandomizer.Number(1, 4)) },
            Summary = new ContentValueDto { Value = GenerateFakeLocalizations(MyRandomizer.Number(7, 10)) }
        };

        return content;
    }

    public static List<LocalizationDto> GenerateFakeLocalizations(int wordCount)
    {
        return
        [
            new() { LanguageCode = "nb", Value = MyRandomizer.Words(wordCount) },
            new() { LanguageCode = "nn", Value = MyRandomizer.Words(wordCount) },
            new() { LanguageCode = "en", Value = MyRandomizer.Words(wordCount) }
        ];
    }

    private static List<GuiActionDto> GenerateUniquePriorityGuiActionsList(int? count = null)
    {
        var actualCount = count ?? MyRandomizer.Number(min: 1, 4);
        var actions = new List<GuiActionDto>(actualCount);

        for (var i = 0; i < actualCount; i++)
        {
            var priority = i switch
            {
                0 => DialogGuiActionPriority.Values.Primary,
                1 => DialogGuiActionPriority.Values.Secondary,
                _ => DialogGuiActionPriority.Values.Tertiary
            };

            var action = new GuiActionDto
            {
                Id = IdentifiableExtensions.CreateVersion7(),
                Action = MyRandomizer.AlphaNumeric(8),
                Priority = priority,
                Url = new Uri(MyFaker.Internet.UrlWithPath(Uri.UriSchemeHttps)),
                Title = GenerateFakeLocalizations(MyRandomizer.Number(1, 3))
            };
            actions.Add(action);
        }
        return actions;
    }
}
