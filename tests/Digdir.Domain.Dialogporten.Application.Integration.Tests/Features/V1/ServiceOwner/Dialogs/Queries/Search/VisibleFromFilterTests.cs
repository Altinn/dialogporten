// using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
// using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
// using FluentAssertions;
// using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
//
// namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;
//
// [Collection(nameof(DialogCqrsCollectionFixture))]
// public class VisibleFromFilterTests : ApplicationCollectionFixture
// {
//     public VisibleFromFilterTests(DialogApplication application) : base(application) { }
//
//     [Fact]
//     public async Task Cannot_Filter_On_VisibleFrom_After_With_Value_Greater_Than_VisibleFrom_Before()
//     {
//         // Act
//         var response = await Application.Send(new SearchDialogQuery
//         {
//             VisibleAfter = CreateDateFromYear(2022),
//             VisibleBefore = CreateDateFromYear(2021)
//         });
//
//         // Assert
//         response.TryPickT1(out var result, out _).Should().BeTrue();
//         result.Should().NotBeNull();
//     }
//
//     [Theory, ClassData(typeof(DynamicDateFilterTestData))]
//     public async Task Should_Filter_On_VisibleFrom_Date(int? afterYear, int? beforeYear, int expectedCount, int[] expectedYears)
//     {
//         // Arrange
//         var currentYear = DateTimeOffset.UtcNow.Year;
//
//         var oneYearInTheFuture = currentYear + 1;
//         var twoYearsInTheFuture = currentYear + 2;
//         var threeYearsInTheFuture = currentYear + 3;
//         var fourYearsInTheFuture = currentYear + 4;
//
//         var dialogOneYearInTheFuture = await Application.CreateDialogWithDateInYear(oneYearInTheFuture, VisibleFrom);
//         var dialogTwoYearsInTheFuture = await Application.CreateDialogWithDateInYear(twoYearsInTheFuture, VisibleFrom);
//         var dialogThreeYearsInTheFuture = await Application.CreateDialogWithDateInYear(threeYearsInTheFuture, VisibleFrom);
//         var dialogFourYearsInTheFuture = await Application.CreateDialogWithDateInYear(fourYearsInTheFuture, VisibleFrom);
//
//         // Act
//         var response = await Application.Send(new SearchDialogQuery
//         {
//             VisibleAfter = afterYear.HasValue ? CreateDateFromYear(afterYear.Value) : null,
//             VisibleBefore = beforeYear.HasValue ? CreateDateFromYear(beforeYear.Value) : null
//         });
//
//         // Assert
//         response.TryPickT0(out var result, out _).Should().BeTrue();
//         result.Should().NotBeNull();
//
//         result.Items.Should().HaveCount(expectedCount);
//         foreach (var year in expectedYears)
//         {
//             var dialogId = year switch
//             {
//                 _ when year == oneYearInTheFuture => dialogOneYearInTheFuture,
//                 _ when year == twoYearsInTheFuture => dialogTwoYearsInTheFuture,
//                 _ when year == threeYearsInTheFuture => dialogThreeYearsInTheFuture,
//                 _ when year == fourYearsInTheFuture => dialogFourYearsInTheFuture,
//                 _ => throw new ArgumentOutOfRangeException()
//             };
//
//             result.Items.Should().ContainSingle(x => x.Id == dialogId);
//         }
//     }
// }
