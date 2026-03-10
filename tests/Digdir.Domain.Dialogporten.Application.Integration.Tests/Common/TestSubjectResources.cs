using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal readonly record struct SubjectResourceSeed(string Subject, string Resource);
internal readonly record struct SubjectResourceGroup(string Resource, IReadOnlyCollection<string> Subjects);

internal static class TestSubjectResourcesExtensions
{
    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        public TFlowStep SeedSubjectResources(string resource, IReadOnlyCollection<string> subjects)
        {
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(subjects);

            if (subjects.Count == 0)
            {
                return flowStep;
            }

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentException("Resource must be set.", nameof(resource));
            }

            var mappings = subjects
                .Select(subject => new SubjectResourceSeed(
                    string.IsNullOrWhiteSpace(subject)
                        ? throw new ArgumentException("Subject must be set.", nameof(subjects))
                        : subject,
                    resource))
                .ToArray();

            return flowStep.SeedSubjectResources(mappings);
        }

        public TFlowStep SeedSubjectResources(IReadOnlyCollection<(string Resource, IReadOnlyCollection<string> Subjects)> groups)
        {
            ArgumentNullException.ThrowIfNull(groups);

            if (groups.Count == 0)
            {
                return flowStep;
            }

            var normalizedGroups = groups
                .Select(group => new SubjectResourceGroup(group.Resource, group.Subjects))
                .ToArray();

            return flowStep.SeedSubjectResources(normalizedGroups);
        }

        public TFlowStep SeedSubjectResources(IReadOnlyCollection<SubjectResourceGroup> groups)
        {
            ArgumentNullException.ThrowIfNull(groups);

            if (groups.Count == 0)
            {
                return flowStep;
            }

            var mappings = groups
                .SelectMany(group => (group.Subjects
                                      ?? throw new ArgumentException(
                                          "Subjects must be set for each SubjectResourceGroup.",
                                          nameof(groups)))
                    .Select(subject => new SubjectResourceSeed(
                        string.IsNullOrWhiteSpace(subject)
                            ? throw new ArgumentException("Subject must be set.", nameof(groups))
                            : subject,
                        string.IsNullOrWhiteSpace(group.Resource)
                            ? throw new ArgumentException("Resource must be set.", nameof(groups))
                            : group.Resource)))
                .ToArray();

            return flowStep.SeedSubjectResources(mappings);
        }

        public TFlowStep SeedSubjectResources(IReadOnlyCollection<SubjectResourceSeed> mappings)
        {
            ArgumentNullException.ThrowIfNull(mappings);

            if (mappings.Count == 0)
            {
                return flowStep;
            }

            return flowStep.Do(async (_, ctx) =>
            {
                var normalizedMappings = mappings
                    .Select(x => new SubjectResourceSeed(
                        string.IsNullOrWhiteSpace(x.Subject)
                            ? throw new ArgumentException("Subject must be set.", nameof(mappings))
                            : x.Subject,
                        string.IsNullOrWhiteSpace(x.Resource)
                            ? throw new ArgumentException("Resource must be set.", nameof(mappings))
                            : x.Resource))
                    .ToArray();

                var distinctMappings = normalizedMappings
                    .Distinct()
                    .ToArray();

                var resources = distinctMappings
                    .Select(x => x.Resource)
                    .Distinct()
                    .ToArray();

                var subjects = distinctMappings
                    .Select(x => x.Subject)
                    .Distinct()
                    .ToArray();

                var lookupKeys = distinctMappings
                    .Select(x => CreateLookupKey(x.Resource, x.Subject))
                    .ToHashSet(StringComparer.Ordinal);

                using var scope = ctx.Application.GetServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

                var existingMappings = await db.SubjectResources
                    .Where(x => resources.Contains(x.Resource) && subjects.Contains(x.Subject))
                    .ToListAsync();

                db.SubjectResources.RemoveRange(
                    existingMappings
                        .Where(x => lookupKeys.Contains(CreateLookupKey(x.Resource, x.Subject))));

                var now = DialogApplication.Clock.UtcNowOffset;

                db.SubjectResources.AddRange(
                    distinctMappings.Select(x => new SubjectResource
                    {
                        Id = Guid.NewGuid(),
                        Subject = x.Subject,
                        Resource = x.Resource,
                        CreatedAt = now,
                        UpdatedAt = now
                    }));

                await db.SaveChangesAsync();
            });
        }
    }

    private static string CreateLookupKey(string resource, string subject) => $"{resource}\u001F{subject}";
}
