﻿// <auto-generated />
using System;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(DialogueDbContext))]
    partial class DialogueDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Common.Localization", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CultureCode")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<long>("LocalizationSetId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("InternalId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("LocalizationSetId");

                    b.ToTable("Localization");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("InternalId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("LocalizationSet");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Actions.DialogueApiAction", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("DialogueId")
                        .HasColumnType("bigint");

                    b.Property<string>("DocumentationUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("HttpMethod")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<bool>("IsBackChannel")
                        .HasColumnType("bit");

                    b.Property<bool>("IsDeleteAction")
                        .HasColumnType("bit");

                    b.Property<string>("RequestSchema")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Resource")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("ResponseSchema")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("InternalId");

                    b.HasIndex("DialogueId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("DialogueApiAction");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Actions.DialogueGuiAction", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("DialogueId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<bool>("IsBackChannel")
                        .HasColumnType("bit");

                    b.Property<bool>("IsDeleteAction")
                        .HasColumnType("bit");

                    b.Property<string>("Resource")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<long>("TitleInternalId")
                        .HasColumnType("bigint");

                    b.Property<int>("TypeId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("InternalId");

                    b.HasIndex("DialogueId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("TitleInternalId");

                    b.HasIndex("TypeId");

                    b.ToTable("DialogueGuiAction");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Actions.DialogueGuiActionType", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.ToTable("DialogueGuiActionType");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Primary"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Secondary"
                        },
                        new
                        {
                            Id = 3,
                            Name = "Tertiary"
                        });
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Activities.DialogueActivity", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<string>("ActivityExtendedType")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("DescriptionInternalId")
                        .HasColumnType("bigint");

                    b.Property<string>("DetailsApiUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DetailsGuiUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("DialogueId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("PerformedBy")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int>("TypeId")
                        .HasColumnType("int");

                    b.HasKey("InternalId");

                    b.HasIndex("DescriptionInternalId");

                    b.HasIndex("DialogueId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("TypeId");

                    b.ToTable("DialogueActivity");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Activities.DialogueActivityType", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.ToTable("DialogueActivityType");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Submission"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Feedback"
                        },
                        new
                        {
                            Id = 3,
                            Name = "Information"
                        },
                        new
                        {
                            Id = 4,
                            Name = "Error"
                        },
                        new
                        {
                            Id = 5,
                            Name = "Closed"
                        },
                        new
                        {
                            Id = 6,
                            Name = "Seen"
                        },
                        new
                        {
                            Id = 7,
                            Name = "Forwarded"
                        });
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Attachments.DialogueAttachement", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("DialogueId")
                        .HasColumnType("bigint");

                    b.Property<long>("DisplayNameInternalId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("Resource")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<long>("SizeInBytes")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("InternalId");

                    b.HasIndex("DialogueId");

                    b.HasIndex("DisplayNameInternalId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("DialogueAttachement");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueConfiguration", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("DialogueId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("VisibleFrom")
                        .HasColumnType("datetime2");

                    b.HasKey("InternalId");

                    b.HasIndex("DialogueId")
                        .IsUnique();

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("DialogueConfiguration");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueContent", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<long>("BodyInternalId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("DialogueId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<long>("SenderNameInternalId")
                        .HasColumnType("bigint");

                    b.Property<long>("TitleInternalId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("InternalId");

                    b.HasIndex("BodyInternalId")
                        .IsUnique();

                    b.HasIndex("DialogueId")
                        .IsUnique();

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("SenderNameInternalId")
                        .IsUnique();

                    b.HasIndex("TitleInternalId")
                        .IsUnique();

                    b.ToTable("DialogueContent");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueDate", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("DialogueId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("DueDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiryDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime?>("ReadDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("InternalId");

                    b.HasIndex("DialogueId")
                        .IsUnique();

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("DialogueDate");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long?>("DialogueGroupId")
                        .HasColumnType("bigint");

                    b.Property<string>("ExtendedStatus")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("ExternalReference")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("Party")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("ServiceResourceIdentifier")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int>("StatusId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("InternalId");

                    b.HasIndex("DialogueGroupId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("StatusId");

                    b.ToTable("DialogueEntity");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueStatus", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.ToTable("DialogueStatus");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Unspecified"
                        },
                        new
                        {
                            Id = 2,
                            Name = "UnderProgress"
                        },
                        new
                        {
                            Id = 3,
                            Name = "Waiting"
                        },
                        new
                        {
                            Id = 4,
                            Name = "Signing"
                        },
                        new
                        {
                            Id = 5,
                            Name = "Cancelled"
                        },
                        new
                        {
                            Id = 6,
                            Name = "Completed"
                        });
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Groups.DialogueGroup", b =>
                {
                    b.Property<long>("InternalId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("InternalId"));

                    b.Property<DateTime>("CreatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<long>("NameInternalId")
                        .HasColumnType("bigint");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<Guid>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("InternalId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("NameInternalId");

                    b.ToTable("DialogueGroup");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Common.Localization", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "LocalizationSet")
                        .WithMany("Localizations")
                        .HasForeignKey("LocalizationSetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LocalizationSet");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Actions.DialogueApiAction", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", "Dialogue")
                        .WithMany("ApiActions")
                        .HasForeignKey("DialogueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dialogue");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Actions.DialogueGuiAction", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", "Dialogue")
                        .WithMany("GuiActions")
                        .HasForeignKey("DialogueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "Title")
                        .WithMany()
                        .HasForeignKey("TitleInternalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.Actions.DialogueGuiActionType", "Type")
                        .WithMany()
                        .HasForeignKey("TypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dialogue");

                    b.Navigation("Title");

                    b.Navigation("Type");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Activities.DialogueActivity", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "Description")
                        .WithMany()
                        .HasForeignKey("DescriptionInternalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", "Dialogue")
                        .WithMany("History")
                        .HasForeignKey("DialogueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.Activities.DialogueActivityType", "Type")
                        .WithMany()
                        .HasForeignKey("TypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Description");

                    b.Navigation("Dialogue");

                    b.Navigation("Type");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Attachments.DialogueAttachement", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", "Dialogue")
                        .WithMany("Attachments")
                        .HasForeignKey("DialogueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "DisplayName")
                        .WithMany()
                        .HasForeignKey("DisplayNameInternalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dialogue");

                    b.Navigation("DisplayName");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueConfiguration", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", "Dialogue")
                        .WithOne("Configuration")
                        .HasForeignKey("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueConfiguration", "DialogueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dialogue");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueContent", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "Body")
                        .WithOne()
                        .HasForeignKey("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueContent", "BodyInternalId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", "Dialogue")
                        .WithOne("Content")
                        .HasForeignKey("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueContent", "DialogueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "SenderName")
                        .WithOne()
                        .HasForeignKey("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueContent", "SenderNameInternalId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "Title")
                        .WithOne()
                        .HasForeignKey("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueContent", "TitleInternalId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Body");

                    b.Navigation("Dialogue");

                    b.Navigation("SenderName");

                    b.Navigation("Title");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueDate", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", "Dialogue")
                        .WithOne("Dates")
                        .HasForeignKey("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueDate", "DialogueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dialogue");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.Groups.DialogueGroup", "DialogueGroup")
                        .WithMany("Dialogues")
                        .HasForeignKey("DialogueGroupId");

                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueStatus", "Status")
                        .WithMany()
                        .HasForeignKey("StatusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DialogueGroup");

                    b.Navigation("Status");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Groups.DialogueGroup", b =>
                {
                    b.HasOne("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", "Name")
                        .WithMany()
                        .HasForeignKey("NameInternalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Name");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Common.LocalizationSet", b =>
                {
                    b.Navigation("Localizations");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.DialogueEntity", b =>
                {
                    b.Navigation("ApiActions");

                    b.Navigation("Attachments");

                    b.Navigation("Configuration")
                        .IsRequired();

                    b.Navigation("Content")
                        .IsRequired();

                    b.Navigation("Dates")
                        .IsRequired();

                    b.Navigation("GuiActions");

                    b.Navigation("History");
                });

            modelBuilder.Entity("Digdir.Domain.Dialogporten.Domain.Dialogues.Groups.DialogueGroup", b =>
                {
                    b.Navigation("Dialogues");
                });
#pragma warning restore 612, 618
        }
    }
}
