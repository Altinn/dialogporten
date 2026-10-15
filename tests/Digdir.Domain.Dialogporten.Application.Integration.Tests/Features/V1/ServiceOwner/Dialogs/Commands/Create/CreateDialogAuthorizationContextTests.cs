using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using DialogDbContext = Digdir.Domain.Dialogporten.Infrastructure.Persistence.DialogDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DialogDtoSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogAuthorizationContextTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string OtherParty = "urn:altinn:organization:identifier-no:991825827";
    private const string DialogOrgParty = "urn:altinn:organization:identifier-no:310260800";
    private const string ViaParty = "urn:altinn:organization:identifier-no:310778737";

    private static List<DialogPartyCheckDto> RestrictedChecks() =>
    [
        new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy },
        new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.ClientDelegation, ViaParty = ViaParty },
        new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None, Action = "readall" }
    ];

    private static AuthorizationContextDto CreateContext(Action<AuthorizationContextDto>? modify = null)
    {
        var context = new AuthorizationContextDto
        {
            ServiceResource = "urn:altinn:resource:some-other-service",
            AdditionalResourceAttribute = "urn:altinn:task:Task_1",
            Parties = [OtherParty],
            DialogPartyChecks = [new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None }],
            Action = "read",
            UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Excluded
        };
        modify?.Invoke(context);
        return context;
    }

    private static AuthorizationContextDto CreateChildContext(Action<AuthorizationContextDto>? modify = null)
    {
        var context = new AuthorizationContextDto
        {
            AdditionalResourceAttribute = "urn:altinn:subresource:secret",
            Parties = [OtherParty],
            UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Disabled
        };
        modify?.Invoke(context);
        return context;
    }

    [Fact]
    public Task Create_With_AuthorizationContext_On_All_Carriers_Should_RoundTrip() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                });
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = null;
                    apiAction.AuthorizationContext = CreateContext(c => c.Action = "write");
                });
                x.AddAttachment(attachment => attachment.AuthorizationContext = CreateChildContext());
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.Action = null);
                    transmission.AddAttachment(attachment => attachment.AuthorizationContext = CreateChildContext());
                    transmission.AddNavigationalAction(navigationalAction => navigationalAction.AuthorizationContext = CreateChildContext());
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                var guiAction = x.GuiActions.Single();
                // Read back as the empty-string sentinel, not null: the column stays NOT NULL
                guiAction.Action.Should().BeEmpty();
                guiAction.AuthorizationAttribute.Should().BeNull();
                guiAction.AuthorizationContext.Should().NotBeNull();
                guiAction.AuthorizationContext!.ServiceResource.Should().Be("urn:altinn:resource:some-other-service");
                guiAction.AuthorizationContext.AdditionalResourceAttribute.Should().Be("urn:altinn:task:Task_1");
                guiAction.AuthorizationContext.Parties.Should().BeEquivalentTo([OtherParty]);
                guiAction.AuthorizationContext.DialogPartyChecks.Should().ContainSingle()
                    .Which.AccessRestriction.Should().Be(DialogPartyCheckAccessRestriction.Values.None);
                guiAction.AuthorizationContext.Action.Should().Be("read");
                guiAction.AuthorizationContext.UnauthorizedPresentation.Should().Be(AuthorizationContextUnauthorizedPresentation.Values.Excluded);

                var apiAction = x.ApiActions.Single();
                apiAction.Action.Should().BeEmpty();
                apiAction.AuthorizationContext.Should().NotBeNull();
                apiAction.AuthorizationContext!.Action.Should().Be("write");

                var attachment = x.Attachments.Single();
                attachment.AuthorizationContext.Should().NotBeNull();
                attachment.AuthorizationContext!.AdditionalResourceAttribute.Should().Be("urn:altinn:subresource:secret");
                attachment.AuthorizationContext.UnauthorizedPresentation.Should().Be(AuthorizationContextUnauthorizedPresentation.Values.Disabled);

                var transmission = x.Transmissions.Single();
                // The "dp-excluded" rollback sentinel is persisted but suppressed on read
                transmission.AuthorizationAttribute.Should().BeNull();
                transmission.AuthorizationContext.Should().NotBeNull();
                transmission.AuthorizationContext!.Action.Should().BeNull();
                transmission.Attachments.Should().ContainSingle(a => a.AuthorizationContext != null);
                transmission.NavigationalActions.Should().ContainSingle(a => a.AuthorizationContext != null);
            });

    [Fact]
    public async Task Create_Should_Persist_Sentinels_Rather_Than_Nulls_For_Context_Carrying_Entities()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                });
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.Action = null);
                });
            })
            .ExecuteAndAssert<CreateDialogSuccess>();

        // Empty rather than null, so the Action column stays NOT NULL and code predating authorization
        // contexts can still materialize the row - and denies the action, since nothing is named "".
        var guiActions = await Application.GetDbEntities<DialogGuiAction>();
        guiActions.Should().ContainSingle().Which.Action.Should().BeEmpty();

        // The rollback sentinel, so that same code keeps the transmission hidden instead of falling
        // through to the dialog's main resource. Inert while authorization contexts are understood.
        var transmissions = await Application.GetDbEntities<DialogTransmission>();
        transmissions.Should().ContainSingle()
            .Which.AuthorizationAttribute.Should().Be(Constants.ExcludedTransmissionAttribute);
    }

    [Fact]
    public async Task Create_Should_Normalize_A_Whitespace_Legacy_Action_To_The_Sentinel()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    // Passes the exclusivity rule, which is expressed with FluentValidation's Empty() and
                    // therefore counts a whitespace-only string as empty. Persisted verbatim it would read
                    // back through EffectiveLegacyAction as a legacy action that shadows the context's.
                    guiAction.Action = " ";
                    guiAction.AuthorizationContext = CreateContext();
                }))
            .ExecuteAndAssert<CreateDialogSuccess>();

        var guiAction = (await Application.GetDbEntities<DialogGuiAction>()).Should().ContainSingle().Subject;
        guiAction.Action.Should().BeEmpty();
        guiAction.EffectiveLegacyAction.Should().BeNull();
    }

    [Fact]
    public Task Create_Should_Fail_When_Transmission_Combines_AuthorizationAttribute_And_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = "element1";
                    transmission.AuthorizationContext = CreateContext(c => c.Action = null);
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be combined")));

    [Fact]
    public Task Create_Should_Fail_When_ApiAction_Combines_Action_And_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = "Test action";
                    apiAction.AuthorizationContext = CreateContext();
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be combined")));

    [Fact]
    public Task Create_Should_Fail_When_GuiAction_Combines_AuthorizationAttribute_And_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationAttribute = "element1";
                    guiAction.AuthorizationContext = CreateContext();
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be combined")));

    [Fact]
    public Task Create_Should_Succeed_When_ApiAction_Context_Is_Missing_Action() =>
        // The context action is optional everywhere and defaults to "read"
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = null;
                    apiAction.AuthorizationContext = CreateContext(c => c.Action = null);
                }))
            .ExecuteAndAssert<CreateDialogSuccess>(x => x.DialogId.Should().NotBeEmpty());

    [Fact]
    public Task Create_Should_Fail_When_GuiAction_Has_Neither_Action_Nor_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction => guiAction.Action = null))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.PropertyName.Contains("Action")));

    [Fact]
    public Task Create_Should_Fail_When_UnauthorizedPresentation_Is_Not_Supplied() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        // Simulates an omitted "unauthorizedPresentation" in the JSON payload
                        c.UnauthorizedPresentation = default;
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("required")));

    [Fact]
    public Task Create_Should_Fail_When_Context_Has_No_Parties_And_No_DialogPartyChecks() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = [];
                        c.DialogPartyChecks = [];
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least 1 and at most")));

    [Fact]
    public Task Create_Should_Succeed_When_Context_Has_No_Parties_But_Has_A_DialogPartyCheck() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = [];
                        c.DialogPartyChecks = [new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None }];
                    });
                }))
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Fact]
    public Task Create_Should_Fail_When_Context_Parties_Is_Explicit_Null_And_No_DialogPartyChecks() =>
        // An explicit JSON "parties": null replaces the DTO's [] initializer, reaching the validator and
        // mapper as an actual null; the DTO normalizes it to [] so this fails validation cleanly instead of
        // throwing (a NullReferenceException surfaced as a 500).
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = null!;
                        c.DialogPartyChecks = [];
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least 1 and at most")));

    [Fact]
    public Task Create_Should_Succeed_When_Context_Parties_Is_Explicit_Null_But_Has_A_DialogPartyCheck() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = null!;
                        c.DialogPartyChecks = [new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None }];
                    });
                }))
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Fact]
    public Task Create_Should_Fail_When_Context_Party_Is_Invalid() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = ["not-a-party"];
                    });
                }))
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Create_Should_Fail_When_Context_Has_Too_Many_Entries() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        // Together with the context's dialog party check, one more than allowed
                        c.Parties = Enumerable.Range(0, AuthorizationContext.MaxNumberOfEntries)
                            .Select(i => $"urn:altinn:organization:identifier-no:{910000000 + i}")
                            .ToList();
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least 1 and at most")));

    [Fact]
    public Task Create_Should_Fail_When_AdditionalResourceAttribute_Contains_Resource_Reference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.AdditionalResourceAttribute = "urn:altinn:resource:sneaky-resource";
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("ServiceResource")));

    [Fact]
    public Task Create_With_TokenRef_Should_RoundTrip_And_Survive_The_Patch_Mapping() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext(c => c.TokenRef = "my-own-reference");
                });
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.TokenRef = "transmission-reference");
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                x.GuiActions.Single().AuthorizationContext!.TokenRef.Should().Be("my-own-reference");
                x.Transmissions.Single().AuthorizationContext!.TokenRef.Should().Be("transmission-reference");

                // The PATCH endpoint maps the read model to the update model before applying the patch document;
                // a reference dropped here would silently be removed by every PATCH.
                x.ToUpdateDialogDto().GuiActions.Single().AuthorizationContext!.TokenRef.Should().Be("my-own-reference");
            });

    [Fact]
    public Task Update_Should_Replace_Context_On_GuiAction() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                }))
            .UpdateDialog(x =>
            {
                var guiAction = x.Dto.GuiActions.Single();
                guiAction.AuthorizationContext.Should().NotBeNull();
                guiAction.AuthorizationContext!.Parties = [OtherParty, "urn:altinn:organization:identifier-no:310778737"];
                guiAction.AuthorizationContext.UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Disabled;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.AuthorizationContext.Should().NotBeNull();
                guiAction.AuthorizationContext!.Parties.Should().HaveCount(2);
                guiAction.AuthorizationContext.UnauthorizedPresentation.Should().Be(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
            });

    [Fact]
    public Task Update_Should_Remove_Context_And_Allow_Legacy_Fields_Again() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                }))
            .UpdateDialog(x =>
            {
                var guiAction = x.Dto.GuiActions.Single();
                guiAction.AuthorizationContext = null;
                guiAction.Action = "read";
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.AuthorizationContext.Should().BeNull();
                guiAction.Action.Should().Be("read");
            });

    [Fact]
    public Task Create_With_DialogPartyChecks_Should_RoundTrip_And_Survive_The_Patch_Mapping() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = DialogOrgParty;
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Parties = [];
                        c.DialogPartyChecks = RestrictedChecks();
                    });
                });
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext(c =>
                    {
                        c.Parties = [];
                        c.DialogPartyChecks = RestrictedChecks();
                    });
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                var expected = RestrictedChecks();

                x.Transmissions.Single().AuthorizationContext!.DialogPartyChecks
                    .Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
                x.GuiActions.Single().AuthorizationContext!.DialogPartyChecks
                    .Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());

                // The PATCH endpoint maps the read model to the update model before applying the patch document;
                // checks dropped here would silently be removed by every PATCH.
                x.ToUpdateDialogDto().GuiActions.Single().AuthorizationContext!.DialogPartyChecks
                    .Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
            });

    [Fact]
    public Task Update_Should_Replace_DialogPartyChecks_On_GuiAction() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = DialogOrgParty;
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                });
            })
            .UpdateDialog(x =>
            {
                var context = x.Dto.GuiActions.Single().AuthorizationContext!;
                context.Parties = [];
                context.DialogPartyChecks = RestrictedChecks();
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
                x.GuiActions.Single().AuthorizationContext!.DialogPartyChecks
                    .Should().BeEquivalentTo(RestrictedChecks(), o => o.WithStrictOrdering()));

    [Fact]
    public Task Create_Should_Fail_When_ViaParty_Is_The_Dialog_Party() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = DialogOrgParty;
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.DialogPartyChecks =
                    [
                        new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.ClientDelegation, ViaParty = DialogOrgParty }
                    ]);
                });
            })
            .ExecuteAndAssert<DomainError>(x => x.ShouldHaveErrorWithText("dialog's own party"));

    [Fact]
    public Task Create_Should_Fail_When_Parties_Contains_The_Dialog_Party() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = DialogOrgParty;
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.Parties = [OtherParty, DialogOrgParty]);
                });
            })
            .ExecuteAndAssert<DomainError>(x => x.ShouldHaveErrorWithText("cannot contain the dialog's own party"));

    [Fact]
    public Task CreateTransmission_Should_Fail_When_Parties_Contains_The_Dialog_Party() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Party = DialogOrgParty)
            .AssertResult<CreateDialogSuccess>()
            .CreateTransmission((transmission, _) =>
            {
                transmission.AuthorizationAttribute = null;
                transmission.AuthorizationContext = CreateContext(c => c.Parties = [DialogOrgParty]);
            })
            .ExecuteAndAssert<DomainError>(x => x.ShouldHaveErrorWithText("cannot contain the dialog's own party"));

    [Fact]
    public Task CreateTransmission_Should_Fail_When_ViaParty_Is_The_Dialog_Party() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Party = DialogOrgParty)
            .AssertResult<CreateDialogSuccess>()
            .CreateTransmission((transmission, _) =>
            {
                transmission.AuthorizationAttribute = null;
                transmission.AuthorizationContext = CreateContext(c => c.DialogPartyChecks =
                [
                    new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.ClientDelegation, ViaParty = DialogOrgParty }
                ]);
            })
            .ExecuteAndAssert<DomainError>(x => x.ShouldHaveErrorWithText("dialog's own party"));

    [Fact]
    public Task Create_Should_Fail_When_ClientDelegation_Check_Has_No_ViaParty() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.DialogPartyChecks =
                    [
                        new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.ClientDelegation }
                    ]);
                }))
            .ExecuteAndAssert<ValidationError>(x => x.ShouldHaveErrorWithText("is required when"));

    [Fact]
    public async Task DialogPartyChecks_Should_Be_Stored_In_The_Shape_The_Migration_Backfills()
    {
        // The AddDialogPartyChecks migration backfills the JSON column with hand-written SQL; this pins
        // that EF reads that shape, and writes the property names and enum encoding the SQL assumes.
        Guid contextId = default;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = DialogOrgParty;
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.DialogPartyChecks =
                    [
                        new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.ClientDelegation, ViaParty = ViaParty }
                    ]);
                });
            })
            .Do(async ctx =>
            {
                using var scope = Application.GetServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
                var dialogId = ctx.GetDialogId();
                contextId = await db.Set<DialogTransmissionAuthorizationContext>()
                    .Where(x => x.Transmission.DialogId == dialogId)
                    .Select(x => x.Id)
                    .SingleAsync(TestContext.Current.CancellationToken);

                var written = await db.Database
                    .SqlQuery<string>($"""SELECT "DialogPartyChecks"::text AS "Value" FROM "AuthorizationContext" WHERE "Id" = {contextId}""")
                    .SingleAsync(TestContext.Current.CancellationToken);
                written.Should().Contain("\"AccessRestriction\": 3").And.Contain($"\"ViaParty\": \"{ViaParty}\"");

                var updated = await db.Database.ExecuteSqlAsync($$"""
                    UPDATE "AuthorizationContext"
                    SET "DialogPartyChecks" = '[{"AccessRestriction": 1, "Action": null, "ServiceResource": null, "ViaParty": null}]'
                    WHERE "Id" = {{contextId}}
                    """, TestContext.Current.CancellationToken);
                updated.Should().Be(1);
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
                x.Transmissions.Single().AuthorizationContext!.DialogPartyChecks.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None, ViaParty = (string?)null }));
    }
}
