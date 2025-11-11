using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using CreateDialogContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ContentDto;
using TransmissionContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionContentDto;
using UpdateDialogContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ContentDto;
using EUGetDialogContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.ContentDto;
using EUSearchDialogContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.ContentDto;
using SOGetDialogContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.ContentDto;
using SOSearchDialogContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.ContentDto;


namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common;

public class ContentTypeTests
{
    [Fact]
    public void DialogContentType_Names_Should_Match_Props_On_ServiceOwner_DTOs()
    {
        // Arrange
        var dialogContentTypeNames = DialogContentType.GetValues()
            .Select(x => x.Name)
            .ToList();

        var dtoTypes = new[]
        {
            typeof(CreateDialogContentDto),
            typeof(UpdateDialogContentDto),
            typeof(SOGetDialogContentDto)
        };

        foreach (var dtoType in dtoTypes)
        {
            var dtoPropertyNames = dtoType.GetProperties()
                .Select(p => p.Name)
                .ToList();

            Assert.Equal(dialogContentTypeNames.Count, dtoPropertyNames.Count);
            foreach (var contentTypeName in dialogContentTypeNames)
            {
                Assert.Contains(contentTypeName, dtoPropertyNames, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void DialogContentType_Names_Should_Match_Props_On_EndUser_DTOs()
    {
        // Arrange
        var dialogContentTypeNames = DialogContentType.GetValues()
            .Where(x => x.Id is not DialogContentType.Values.NonSensitiveSummary
                and not DialogContentType.Values.NonSensitiveTitle)
            .Select(x => x.Name)
            .ToList();

        var dtoPropertyNames = typeof(EUGetDialogContentDto).GetProperties()
            .Select(p => p.Name)
            .ToList();

        // Assert
        Assert.Equal(dialogContentTypeNames.Count, dtoPropertyNames.Count);
        foreach (var contentTypeName in dialogContentTypeNames)
        {
            Assert.Contains(contentTypeName, dtoPropertyNames, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void OutPutInList_DialogContentType_Names_Should_Match_Props_On_ServiceOwner_Search_DTO()
    {
        // Arrange
        var dialogContentTypeNames = DialogContentType.GetValues()
            .Where(x => x.OutputInList)
            .Select(x => x.Name)
            .ToList();

        var dtoPropertyNames = typeof(SOSearchDialogContentDto).GetProperties()
            .Select(p => p.Name)
            .ToList();

        // Assert
        Assert.Equal(dialogContentTypeNames.Count, dtoPropertyNames.Count);
        foreach (var contentTypeName in dialogContentTypeNames)
        {
            Assert.Contains(contentTypeName, dtoPropertyNames, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void OutPutInList_DialogContentType_Names_Should_Match_Props_On_EndUser_Search_DTO()
    {
        // Arrange
        var dialogContentTypeNames = DialogContentType.GetValues()
            .Where(x => x.OutputInList)
            .Where(x => x.Id is not DialogContentType.Values.NonSensitiveSummary
                and not DialogContentType.Values.NonSensitiveTitle)
            .Select(x => x.Name)
            .ToList();

        var dtoPropertyNames = typeof(EUSearchDialogContentDto).GetProperties()
            .Select(p => p.Name)
            .ToList();

        // Assert
        Assert.Equal(dialogContentTypeNames.Count, dtoPropertyNames.Count);
        foreach (var contentTypeName in dialogContentTypeNames)
        {
            Assert.Contains(contentTypeName, dtoPropertyNames, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TransmissionContentType_Names_Should_Match_Props_On_All_DTOs()
    {
        // Arrange
        var transmissionContentTypeNames = DialogTransmissionContentType.GetValues()
            .Select(x => x.Name)
            .ToList();

        var dtoTypes = new[]
        {
            typeof(TransmissionContentDto),
            typeof(Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionContentDto),
            typeof(Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogTransmissionContentDto),
            typeof(Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogTransmissionContentDto)
        };

        foreach (var dtoType in dtoTypes)
        {
            var dtoPropertyNames = dtoType.GetProperties()
                .Select(p => p.Name)
                .ToList();

            Assert.Equal(transmissionContentTypeNames.Count, dtoPropertyNames.Count);
            foreach (var contentTypeName in transmissionContentTypeNames)
            {
                Assert.Contains(contentTypeName, dtoPropertyNames, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
