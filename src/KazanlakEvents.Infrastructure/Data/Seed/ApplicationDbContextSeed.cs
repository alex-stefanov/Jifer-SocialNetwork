using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Infrastructure.Data.Seed;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        foreach (var roleName in UserRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName)
                {
                    Description = $"{roleName} role for KazanlakEvents"
                });
                logger.LogInformation("Created role: {Role}", roleName);
            }
        }

        const string adminEmail = "admin@gmail.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(adminUser, "Demo_123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.SuperAdmin);
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
        }

        var adminProfile = await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == adminUser.Id);
        if (adminProfile == null)
        {
            context.UserProfiles.Add(new UserProfile
            {
                UserId = adminUser.Id,
                FirstName = "Администратор",
                LastName = "Системен",
                City = "Kazanlak",
                Bio = "Platform administrator",
                PreferredLanguage = "bg"
            });
        }
        else if (adminProfile.FirstName == "Super")
        {
            adminProfile.FirstName = "Администратор";
            adminProfile.LastName = "Системен";
            adminProfile.City = "Kazanlak";
            adminProfile.Bio = "Platform administrator";
        }

        async Task<ApplicationUser> EnsureUser(string email, string userName, string role)
        {
            var u = await userManager.FindByEmailAsync(email);
            if (u != null) return u;
            u = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true, IsActive = true };
            var r = await userManager.CreateAsync(u, "Demo123!");
            if (r.Succeeded)
                await userManager.AddToRoleAsync(u, role);
            else
                logger.LogWarning("Failed to create {Email}: {Errors}", email,
                    string.Join(", ", r.Errors.Select(e => e.Description)));
            return u;
        }

        const string alexAvatar     = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&q=80";
        const string zdravkaAvatar  = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=200&q=80";
        const string bozhidarAvatar = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=200&q=80";
        const string monikaAvatar   = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=200&q=80";
        const string elicaAvatar    = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=200&q=80";
        const string silviqAvatar   = "https://images.unsplash.com/photo-1487412720507-e7ab37603c6f?w=200&q=80";

        var alexUser     = await EnsureUser("alex@gmail.com", "alex.stefanov", UserRoles.User);
        var zdravkaUser  = await EnsureUser("zdravka@gmail.com",     "zdravka.dim",   UserRoles.User);
        var bozhidarUser = await EnsureUser("bozhidar@gmail.com",    "bozhidar.t",    UserRoles.User);
        var monikaUser   = await EnsureUser("monika@gmail.com",      "monika.n",      UserRoles.User);
        var elicaUser    = await EnsureUser("elica@gmail.com",       "elica.k",       UserRoles.User);
        var silviqUser   = await EnsureUser("silviq@gmail.com",      "silviq.k",      UserRoles.User);
        var ngoUser      = await EnsureUser("ngo@gmail.com",   "ngo.kazanlak",  UserRoles.Organizer);
        var ngoBlogUser  = await EnsureUser("blog@gmail.com",  "ngo.blog",      UserRoles.User);
        if (!await userManager.IsInRoleAsync(ngoBlogUser, UserRoles.BlogAuthor))
            await userManager.AddToRoleAsync(ngoBlogUser, UserRoles.BlogAuthor);

        async Task EnsureTeamProfile(Guid userId, string firstName, string lastName, string bio, string? avatarUrl = null)
        {
            var profile = await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                context.UserProfiles.Add(new UserProfile
                {
                    UserId = userId, FirstName = firstName, LastName = lastName,
                    City = "Kazanlak", Bio = bio, PreferredLanguage = "bg", AvatarUrl = avatarUrl
                });
            else if (avatarUrl != null && profile.AvatarUrl == null)
                profile.AvatarUrl = avatarUrl;
        }

        await EnsureTeamProfile(alexUser.Id,     "Алекс",        "Стефанов",  "Основател и организатор на Rose Valley Hackathon и Rose Mission. Страстен към технологиите и общността.", alexAvatar);
        await EnsureTeamProfile(zdravkaUser.Id,  "Здравка",      "Димитрова", "Организатор и координатор на младежки проекти.", zdravkaAvatar);
        await EnsureTeamProfile(bozhidarUser.Id, "Божидар",      "Тошев",     "Технически поддръжка и координатор на IT инфраструктура.", bozhidarAvatar);
        await EnsureTeamProfile(monikaUser.Id,   "Моника",       "Николова",  "Координатор на събития и комуникации.", monikaAvatar);
        await EnsureTeamProfile(elicaUser.Id,    "Елица",        "Колева",    "Организатор и координатор на доброволчески инициативи.", elicaAvatar);
        await EnsureTeamProfile(silviqUser.Id,   "Силвия",       "Качакова",  "Координатор на Rose Mission и екологични инициативи.", silviqAvatar);
        await EnsureTeamProfile(ngoUser.Id,      "За Младежите", "НПО",       "Официален профил на НПО 'За младежите' — организатор на обществени събития в Казанлък.");
        await EnsureTeamProfile(ngoBlogUser.Id, "За Младежите", "Блог",      "Официалният блог на НПО 'За младежите'.", "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=200&q=80");

        var ngoBlogProfile = await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == ngoBlogUser.Id);
        if (ngoBlogProfile != null && !ngoBlogProfile.IsTrustedAuthor)
            ngoBlogProfile.IsTrustedAuthor = true;

        if (!await context.Categories.AnyAsync())
        {
            await context.Categories.AddRangeAsync(
                new Category { Name = "Концерти и фестивали",   Slug = "concerts-festivals",  IconCssClass = "bi-music-note-beamed", SortOrder = 1 },
                new Category { Name = "Работилници и обучения", Slug = "workshops-trainings",  IconCssClass = "bi-mortarboard",       SortOrder = 2 },
                new Category { Name = "Доброволчество",         Slug = "volunteering",         IconCssClass = "bi-heart",             SortOrder = 3 },
                new Category { Name = "Спорт и активности",     Slug = "sports-activities",    IconCssClass = "bi-trophy",            SortOrder = 4 },
                new Category { Name = "Изкуство и култура",     Slug = "art-culture",          IconCssClass = "bi-palette",           SortOrder = 5 },
                new Category { Name = "Друго",                  Slug = "other",                IconCssClass = "bi-grid",              SortOrder = 6 }
            );
            logger.LogInformation("Seeded event categories");
        }

        if (!await context.BlogCategories.AnyAsync())
        {
            await context.BlogCategories.AddRangeAsync(
                new BlogCategory { Name = "Community",    Slug = "community",    IsActive = true },
                new BlogCategory { Name = "Events",       Slug = "events",       IsActive = true },
                new BlogCategory { Name = "Guides",       Slug = "guides",       IsActive = true },
                new BlogCategory { Name = "Volunteering", Slug = "volunteering", IsActive = true }
            );
        }

        if (!await context.Tags.AnyAsync())
        {
            await context.Tags.AddRangeAsync(
                new Tag { Name = "music",     Slug = "music"     },
                new Tag { Name = "outdoor",   Slug = "outdoor"   },
                new Tag { Name = "free",      Slug = "free"      },
                new Tag { Name = "family",    Slug = "family"    },
                new Tag { Name = "sports",    Slug = "sports"    },
                new Tag { Name = "culture",   Slug = "culture"   },
                new Tag { Name = "education", Slug = "education" },
                new Tag { Name = "youth",     Slug = "youth"     }
            );
        }

        if (!await context.OrganizedEvents.AnyAsync())
        {
            var seedNow = DateTime.UtcNow;

            var hackathon = new OrganizedEvent
            {
                Title          = "Rose Valley Hackathon",
                Slug           = "rose-valley-hackathon",
                Description    = "<p>„Rose Valley Hackathon“ е 48-часово интензивно събитие за иновации, организирано от сдружение „За младежите“, което събира разработчици, дизайнери и предприемачи от цяла България, за да решават глобален индустриален проект чрез технологии.</p> <p>Отбори от до четирима участници работиха денонощно, подпомагани от опитни професионалисти от технологичната индустрия и подкрепяни от нашия отдаден доброволчески екип.</p> <p>До края на хакатона 15 отбора представиха работещи прототипи — вариращи от решения за „умен град“ за Казанлък до агротехнологични инструменти за региона на Розовата долина. Победителите получиха начално финансиране и награди от нашите спонсори.</p>",
                CoverImageUrl  = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=800&q=80",
                EventDate      = seedNow.AddMonths(-3),
                AttendeesCount = 85,
                IsActive       = true
            };

            var mission = new OrganizedEvent
            {
                Title          = "Rose Mission",
                Slug           = "rose-mission",
                Description    = "<p>Мисия Роза е мащабна ученическа инициатива, организирана от сдружение „За младежите“ в партньорство с Община Казанлък. В нея участваха над 90 участници, подпомагани от нашия отдаден екип от доброволци.</p> <p>Мисия Роза беше събитие, проведено в рамките на три дни, в което участници от цялата страна се предизвикаха, като се състезаваха в отбори от по 4 души за дял от нашия награден фонд в размер на 2800 евро. Те трябваше да демонстрират своето логическо и критично мислене, както и общата си култура.</p>",
                CoverImageUrl  = "https://images.unsplash.com/photo-1416169607655-0c2b3ce2e1cc?w=800&q=80",
                EventDate      = seedNow.AddMonths(-6),
                AttendeesCount = 120,
                IsActive       = true
            };

            hackathon.TeamMembers.Add(new TeamMember { FullName = "Алекс Стефанов",   Role = "Organizer",   Tags = "organizer,lead,founder",       Description = "Основател и главен организатор на хакатона",              Quote = "Технологиите могат да променят нашата общност", LinkedUserId = alexUser.Id,     PhotoUrl = alexAvatar,     SortOrder = 1 });
            hackathon.TeamMembers.Add(new TeamMember { FullName = "Здравка Димитрова", Role = "Organizer",   Tags = "organizer",                    Description = "Съорганизатор, отговорен за логистиката",                                                                          LinkedUserId = zdravkaUser.Id,  PhotoUrl = zdravkaAvatar,  SortOrder = 2 });
            hackathon.TeamMembers.Add(new TeamMember { FullName = "Божидар Тошев",    Role = "Coordinator", Tags = "coordinator,technical-support", Description = "Технически ментор и координатор на инфраструктурата",                                                              LinkedUserId = bozhidarUser.Id, PhotoUrl = bozhidarAvatar, SortOrder = 3 });
            hackathon.TeamMembers.Add(new TeamMember { FullName = "Моника Николова",  Role = "Coordinator", Tags = "coordinator",                  Description = "Координатор на участниците и регистрациите",                                                                       LinkedUserId = monikaUser.Id,   SortOrder = 4 });
            hackathon.TeamMembers.Add(new TeamMember { FullName = "Елица Колева",     Role = "Coordinator", Tags = "coordinator",                  Description = "Координатор на комуникациите и медиите",                                                                           LinkedUserId = elicaUser.Id,    SortOrder = 5 });
            hackathon.TeamMembers.Add(new TeamMember { FullName = "Петър Василев",    Role = "Volunteer",   Tags = "volunteer",                    Description = "Помощник по организацията и логистиката",                                                                          SortOrder = 6 });
            hackathon.TeamMembers.Add(new TeamMember { FullName = "Ирина Маринова",   Role = "Volunteer",   Tags = "volunteer",                    Description = "Доброволец по регистрацията и комуникациите",                                                                      SortOrder = 7 });
            hackathon.TeamMembers.Add(new TeamMember { FullName = "Красимир Димов",   Role = "Volunteer",   Tags = "volunteer",                    Description = "Технически доброволец и поддръжка на оборудването",                                                               SortOrder = 8 });

            hackathon.Sponsors.Add(new OrganizedEventSponsor { CompanyName = "TechPark Bulgaria",  LogoUrl = "https://images.unsplash.com/photo-1560179707-f14e90ef3623?w=200&q=80", Description = "Innovation hub providing workspace and mentorship" });
            hackathon.Sponsors.Add(new OrganizedEventSponsor { CompanyName = "Казанлък Софтуер",                                                                                    Description = "Local software company sponsoring prizes" });

            mission.TeamMembers.Add(new TeamMember { FullName = "Алекс Стефанов",   Role = "Organizer",   Tags = "organizer,lead",              Description = "Основател и главен организатор на мисията",                                                                          LinkedUserId = alexUser.Id,     PhotoUrl = alexAvatar,     SortOrder = 1 });
            mission.TeamMembers.Add(new TeamMember { FullName = "Елица Колева",     Role = "Organizer",   Tags = "organizer",                   Description = "Съорганизатор, отговорен за доброволческите инициативи",                                                             LinkedUserId = elicaUser.Id,    PhotoUrl = elicaAvatar,    SortOrder = 2 });
            mission.TeamMembers.Add(new TeamMember { FullName = "Божидар Тошев",    Role = "Coordinator", Tags = "coordinator,technical-support", Description = "Технически координатор и поддръжка на инфраструктурата",                                                           LinkedUserId = bozhidarUser.Id, PhotoUrl = bozhidarAvatar, SortOrder = 3 });
            mission.TeamMembers.Add(new TeamMember { FullName = "Моника Николова",  Role = "Coordinator", Tags = "coordinator",                 Description = "Координатор на участниците и комуникациите",                                                                          LinkedUserId = monikaUser.Id,   SortOrder = 4 });
            mission.TeamMembers.Add(new TeamMember { FullName = "Силвия Качакова",  Role = "Coordinator", Tags = "coordinator",                 Description = "Координатор на Rose Mission и екологичните инициативи",                                                               LinkedUserId = silviqUser.Id,   SortOrder = 5 });
            mission.TeamMembers.Add(new TeamMember { FullName = "Николай Димитров", Role = "Volunteer",   Tags = "volunteer",                   Description = "Доброволец по почистването и засаждането",                                                                           SortOrder = 6 });
            mission.TeamMembers.Add(new TeamMember { FullName = "Виктория Иванова", Role = "Volunteer",   Tags = "volunteer",                   Description = "Доброволец по екологичните кампании",                                                                                SortOrder = 7 });
            mission.TeamMembers.Add(new TeamMember { FullName = "Димитър Петков",   Role = "Volunteer",   Tags = "volunteer",                   Description = "Доброволец по логистиката и материалите",                                                                            SortOrder = 8 });

            mission.Sponsors.Add(new OrganizedEventSponsor { CompanyName = "Община Казанлък", Description = "Municipal support for environmental initiatives" });

            await context.OrganizedEvents.AddRangeAsync(hackathon, mission);
            logger.LogInformation("Seeded organized events");
        }

        await context.SaveChangesAsync();

        var roseFestivalEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Slug == "otkrivane-na-festivala-na-rozata");
        if (roseFestivalEvent != null && roseFestivalEvent.ShortDescription != null
            && !roseFestivalEvent.ShortDescription.Contains("Rose"))
        {
            roseFestivalEvent.ShortDescription =
                "Rose Festival Opening — " + roseFestivalEvent.ShortDescription;
            await context.SaveChangesAsync();
            logger.LogInformation("Patched rose festival event with English search term.");
        }

        var marathonEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Slug == "gradski-maration-kazanlak");
        if (marathonEvent != null && marathonEvent.ShortDescription != null
            && !marathonEvent.ShortDescription.Contains("Rose"))
        {
            marathonEvent.ShortDescription =
                "City Marathon through the Rose Valley — " + marathonEvent.ShortDescription;
            await context.SaveChangesAsync();
        }

        var coverPatches = new Dictionary<string, string>
        {
            ["otkrivane-na-festivala-na-rozata"] = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800&q=80",
            ["mladejki-liderski-lager-2026"]     = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&q=80",
            ["gradski-maration-kazanlak"]         = "https://images.unsplash.com/photo-1452626038306-9aae5e071dd3?w=800&q=80",
            ["lyatna-hudojestvena-izlojba"]       = "https://images.unsplash.com/photo-1531913764164-f85c3e01b2aa?w=800&q=80",
            ["den-na-dobrovoletsa"]               = "https://images.unsplash.com/photo-1559027615-cd4628902d4a?w=800&q=80",
            ["osnovi-na-fotografiyata"]            = "https://images.unsplash.com/photo-1542038784456-1ea8e935640e?w=800&q=80",
            ["vecher-na-proletnata-muzika"]        = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800&q=80",
        };
        var eventsToImage = await context.Events
            .Where(e => coverPatches.Keys.Contains(e.Slug) && e.CoverImageUrl == null)
            .ToListAsync();
        foreach (var ev in eventsToImage)
            ev.CoverImageUrl = coverPatches[ev.Slug];
        if (eventsToImage.Count > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Patched cover images for {Count} events.", eventsToImage.Count);
        }

        var mariaPatchUser = await userManager.FindByEmailAsync("maria@example.bg");
        if (mariaPatchUser != null)
        {
            var mariaOrgEvents = await context.Events
                .Where(e => e.OrganizerId == mariaPatchUser.Id)
                .ToListAsync();
            foreach (var ev in mariaOrgEvents)
                ev.OrganizerId = ngoUser.Id;
            if (mariaOrgEvents.Count > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Reassigned {Count} events from maria to NGO organizer.", mariaOrgEvents.Count);
            }
        }

        var hackathonPatch = await context.OrganizedEvents
            .Include(e => e.TeamMembers)
            .FirstOrDefaultAsync(e => e.Slug == "rose-valley-hackathon");
        if (hackathonPatch != null && hackathonPatch.TeamMembers.All(m => m.LinkedUserId == null))
        {
            var oldHackMembers = hackathonPatch.TeamMembers.ToList();
            context.TeamMembers.RemoveRange(oldHackMembers);
            hackathonPatch.TeamMembers.Clear();
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Алекс Стефанов",   Role = "Organizer",   Tags = "organizer,lead,founder",       Description = "Основател и главен организатор на хакатона",              Quote = "Технологиите могат да променят нашата общност", LinkedUserId = alexUser.Id,     PhotoUrl = alexAvatar,     SortOrder = 1 });
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Здравка Димитрова", Role = "Organizer",   Tags = "organizer",                    Description = "Съорганизатор, отговорен за логистиката",                                                                          LinkedUserId = zdravkaUser.Id,  PhotoUrl = zdravkaAvatar,  SortOrder = 2 });
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Божидар Тошев",    Role = "Coordinator", Tags = "coordinator,technical-support", Description = "Технически ментор и координатор на инфраструктурата",                                                              LinkedUserId = bozhidarUser.Id, PhotoUrl = bozhidarAvatar, SortOrder = 3 });
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Моника Николова",  Role = "Coordinator", Tags = "coordinator",                  Description = "Координатор на участниците и регистрациите",                                                                       LinkedUserId = monikaUser.Id,   SortOrder = 4 });
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Елица Колева",     Role = "Coordinator", Tags = "coordinator",                  Description = "Координатор на комуникациите и медиите",                                                                           LinkedUserId = elicaUser.Id,    SortOrder = 5 });
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Теодора Недкова",    Role = "Volunteer",   Tags = "volunteer",                    Description = "Помощник по организацията и логистиката",                                                                          SortOrder = 6 });
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Стилян Чанев",   Role = "Volunteer",   Tags = "volunteer",                    Description = "Доброволец по регистрацията и комуникациите",                                                                      SortOrder = 7 });
            hackathonPatch.TeamMembers.Add(new TeamMember { FullName = "Стефан Димитров",   Role = "Volunteer",   Tags = "volunteer",                    Description = "Технически доброволец и поддръжка на оборудването",                                                               SortOrder = 8 });
            await context.SaveChangesAsync();
            logger.LogInformation("Updated Rose Valley Hackathon team members.");
        }

        var missionPatch = await context.OrganizedEvents
            .Include(e => e.TeamMembers)
            .FirstOrDefaultAsync(e => e.Slug == "rose-mission");
        if (missionPatch != null && missionPatch.TeamMembers.All(m => m.LinkedUserId == null))
        {
            var oldMissionMembers = missionPatch.TeamMembers.ToList();
            context.TeamMembers.RemoveRange(oldMissionMembers);
            missionPatch.TeamMembers.Clear();
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Алекс Стефанов",   Role = "Organizer",   Tags = "organizer,lead",              Description = "Основател и главен организатор на мисията",                                                                          LinkedUserId = alexUser.Id,     PhotoUrl = alexAvatar,     SortOrder = 1 });
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Елица Колева",     Role = "Organizer",   Tags = "organizer",                   Description = "Съорганизатор, отговорен за доброволческите инициативи",                                                             LinkedUserId = elicaUser.Id,    PhotoUrl = elicaAvatar,    SortOrder = 2 });
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Божидар Тошев",    Role = "Coordinator", Tags = "coordinator,technical-support", Description = "Технически координатор и поддръжка на инфраструктурата",                                                           LinkedUserId = bozhidarUser.Id, PhotoUrl = bozhidarAvatar, SortOrder = 3 });
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Моника Николова",  Role = "Coordinator", Tags = "coordinator",                 Description = "Координатор на участниците и комуникациите",                                                                          LinkedUserId = monikaUser.Id,   SortOrder = 4 });
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Силвия Качакова",  Role = "Coordinator", Tags = "coordinator",                 Description = "Координатор на Rose Mission и екологичните инициативи",                                                               LinkedUserId = silviqUser.Id,   SortOrder = 5 });
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Ивон Урумова", Role = "Volunteer",   Tags = "volunteer",                   Description = "Доброволец по почистването и засаждането",                                                                           SortOrder = 6 });
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Кристиан Кирилов", Role = "Volunteer",   Tags = "volunteer",                   Description = "Доброволец по екологичните кампании",                                                                                SortOrder = 7 });
            missionPatch.TeamMembers.Add(new TeamMember { FullName = "Анастасия Пейчева",   Role = "Volunteer",   Tags = "volunteer",                   Description = "Доброволец по логистиката и материалите",                                                                            SortOrder = 8 });
            await context.SaveChangesAsync();
            logger.LogInformation("Updated Rose Mission team members.");
        }

        if (await context.Events.AnyAsync())
        {
            logger.LogInformation("Demo data already seeded - skipping.");
            return;
        }

        logger.LogInformation("Seeding comprehensive demo data…");

        var allCategories  = await context.Categories.ToListAsync();
        var catConcerts    = allCategories.First(c => c.Slug == "concerts-festivals");
        var catWorkshops   = allCategories.First(c => c.Slug == "workshops-trainings");
        var catVolunteer   = allCategories.First(c => c.Slug == "volunteering");
        var catSports      = allCategories.First(c => c.Slug == "sports-activities");
        var catArtCulture  = allCategories.First(c => c.Slug == "art-culture");

        var allTags        = await context.Tags.ToListAsync();
        int tagMusic       = allTags.First(t => t.Slug == "music").Id;
        int tagOutdoor     = allTags.First(t => t.Slug == "outdoor").Id;
        int tagFree        = allTags.First(t => t.Slug == "free").Id;
        int tagFamily      = allTags.First(t => t.Slug == "family").Id;
        int tagSports      = allTags.First(t => t.Slug == "sports").Id;
        int tagCulture     = allTags.First(t => t.Slug == "culture").Id;
        int tagEducation   = allTags.First(t => t.Slug == "education").Id;
        int tagYouth       = allTags.First(t => t.Slug == "youth").Id;

        var allBlogCats    = await context.BlogCategories.ToListAsync();
        var blogCatCom     = allBlogCats.FirstOrDefault(c => c.Slug == "community");
        var blogCatEv      = allBlogCats.FirstOrDefault(c => c.Slug == "events");
        var blogCatGuides  = allBlogCats.FirstOrDefault(c => c.Slug == "guides");

        async Task<ApplicationUser> CreateUser(
            string email, string userName, string password, string[] roles)
        {
            var u = await userManager.FindByEmailAsync(email);
            if (u != null) return u;
            u = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true, IsActive = true };
            var r = await userManager.CreateAsync(u, password);
            if (r.Succeeded)
                foreach (var role in roles)
                    await userManager.AddToRoleAsync(u, role);
            else
                logger.LogWarning("Failed to create {Email}: {Errors}", email,
                    string.Join(", ", r.Errors.Select(e => e.Description)));
            return u;
        }

        var mariaUser  = await CreateUser("maria@example.bg",  "maria.koleva",    "Demo123!", [UserRoles.Organizer]);
        var ivanUser   = await CreateUser("ivan@example.bg",   "ivan.petrov",     "Demo123!", [UserRoles.Organizer]);
        var elenaUser  = await CreateUser("elena@example.bg",  "elena.dim",       "Demo123!", [UserRoles.User]);
        var georgiUser = await CreateUser("georgi@example.bg", "georgi.stoyanov", "Demo123!", [UserRoles.User]);

        async Task EnsureProfile(Guid userId, string firstName, string lastName,
            string? city, string? bio, string lang = "bg")
        {
            if (!await context.UserProfiles.AnyAsync(p => p.UserId == userId))
                context.UserProfiles.Add(new UserProfile
                {
                    UserId = userId, FirstName = firstName, LastName = lastName,
                    City = city, Bio = bio, PreferredLanguage = lang
                });
        }

        await EnsureProfile(mariaUser.Id,  "Мария",  "Колева",    "Kazanlak",     "Community organizer at For the Youths NGO. Passionate about bringing people together.");
        await EnsureProfile(ivanUser.Id,   "Иван",   "Петров",    "Kazanlak",     "Sports event organizer and marathon runner.");
        await EnsureProfile(elenaUser.Id,  "Елена",  "Димитрова", "Kazanlak",     "Student and active volunteer.");
        await EnsureProfile(georgiUser.Id, "Георги", "Стоянов",   "Stara Zagora", null);

        await context.SaveChangesAsync();

        var venue1Id = Guid.NewGuid();
        var venue2Id = Guid.NewGuid();
        var venue3Id = Guid.NewGuid();

        await context.Venues.AddRangeAsync(
            new Venue { Id = venue1Id, Name = "Централен парк Казанлък", Address = "бул. Розова долина", City = "Kazanlak", Latitude = 42.619000m, Longitude = 25.399000m, Capacity = 2000, CreatedByUserId = adminUser.Id },
            new Venue { Id = venue2Id, Name = "Младежки център",         Address = "ул. Искра 21",        City = "Kazanlak", Latitude = 42.617000m, Longitude = 25.395000m, Capacity = 150,  CreatedByUserId = adminUser.Id },
            new Venue { Id = venue3Id, Name = "Градски стадион",         Address = "ул. Стадион 1",       City = "Kazanlak", Latitude = 42.621000m, Longitude = 25.402000m, Capacity = 5000, CreatedByUserId = adminUser.Id }
        );

        var now = DateTime.UtcNow;

        var event1Id = Guid.NewGuid();
        var event2Id = Guid.NewGuid();
        var event3Id = Guid.NewGuid();
        var event4Id = Guid.NewGuid();
        var event5Id = Guid.NewGuid();
        var event6Id = Guid.NewGuid();
        var event7Id = Guid.NewGuid();
        var event8Id = Guid.NewGuid();

        await context.Events.AddRangeAsync(
            new Event
            {
                Id = event1Id,
                Title = "Откриване на Фестивала на розата",
                Slug = "otkrivane-na-festivala-na-rozata",
                Description = "<p>Фестивалът на розата е един от най-красивите и обичани празници в България. Тази година събитието обещава незабравима вечер с музика, танци и аромат на рози. Очаквайте изпълнения на местни артисти и национални изпълнители в сърцето на Розовата долина.</p><p>Програмата включва концерти, фолклорни танци, изложби на рози и специални изненади за децата. Входът е свободен за всички желаещи. Не пропускайте уникалната възможност да се потопите в автентичната атмосфера на розобрача.</p><p>Събитието ще се проведе в Централния парк на Казанлък, където ще бъдат наредени щандове с местни продукти от роза, сувенири и традиционна храна. Очакваме ви с цялото семейство!</p>",
                ShortDescription = "Тържественото откриване на Фестивала на розата с концерти, танци и аромат на рози.",
                CoverImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800&q=80",
                OrganizerId = ngoUser.Id, VenueId = venue1Id, CategoryId = catConcerts.Id,
                StartDate = now.AddDays(35).Date.AddHours(18), EndDate = now.AddDays(35).Date.AddHours(22),
                Capacity = 500, IsFree = true, IsAccessible = true, Status = EventStatus.Published
            },
            new Event
            {
                Id = event2Id,
                Title = "Младежки лидерски лагер 2026",
                Slug = "mladejki-liderski-lager-2026",
                Description = "<p>Двудневен лагер за развитие на лидерски умения сред млади хора на възраст 16-25 години. Участниците ще преминат през интензивни обучителни сесии, групови дискусии и практически упражнения.</p><p>Програмата е разработена от специалисти в областта на младежкото развитие и лидерството. Всеки участник ще получи сертификат за завършено обучение и ценни контакти за бъдещо сътрудничество.</p>",
                ShortDescription = "Двудневен лагер за развитие на лидерски умения сред млади хора.",
                CoverImageUrl = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&q=80",
                OrganizerId = ngoUser.Id, VenueId = venue2Id, CategoryId = catWorkshops.Id,
                StartDate = now.AddDays(42).Date.AddHours(10), EndDate = now.AddDays(44).Date.AddHours(17),
                Capacity = 50, IsFree = false, IsAccessible = true, Status = EventStatus.Published
            },
            new Event
            {
                Id = event3Id,
                Title = "Градски маратон Казанлък",
                Slug = "gradski-maration-kazanlak",
                Description = "<p>Петото издание на Градския маратон на Казанлък! Трасето минава покрай най-красивите места в града и Розовата долина. Участниците могат да изберат между дистанции от 5 км, 10 км, полумаратон и пълен маратон.</p><p>Всички финиширали получават медал и участват в томбола с атрактивни награди. Регистрацията е безплатна, но задължителна поради ограничен брой места.</p>",
                ShortDescription = "Петото издание на Градския маратон с трасе покрай Розовата долина.",
                CoverImageUrl = "https://images.unsplash.com/photo-1452626038306-9aae5e071dd3?w=800&q=80",
                OrganizerId = ivanUser.Id, VenueId = venue3Id, CategoryId = catSports.Id,
                StartDate = now.AddDays(50).Date.AddHours(8), EndDate = now.AddDays(50).Date.AddHours(16),
                Capacity = 1000, IsFree = true, IsAccessible = false, Status = EventStatus.Published
            },
            new Event
            {
                Id = event4Id,
                Title = "Лятна художествена изложба",
                Slug = "lyatna-hudojestvena-izlojba",
                Description = "<p>Открийте талантливите местни художници в ежегодната Лятна художествена изложба. Тази година участват над 20 творци с над 150 творби в различни техники - живопис, графика, фотография и скулптура.</p><p>Изложбата е отворена ежедневно от 10:00 до 20:00 часа в Младежкия център. Входът е с билет, но децата до 12 години влизат безплатно.</p>",
                ShortDescription = "Ежегодна изложба на местни художници с над 150 творби.",
                CoverImageUrl = "https://images.unsplash.com/photo-1559027615-cd4628902d4a?w=800&q=80",
                OrganizerId = ngoUser.Id, VenueId = venue2Id, CategoryId = catArtCulture.Id,
                StartDate = now.AddDays(55).Date.AddHours(19), EndDate = now.AddDays(55).Date.AddHours(22),
                Capacity = 150, IsFree = false, IsAccessible = true, Status = EventStatus.Published
            },
            new Event
            {
                Id = event5Id,
                Title = "Ден на доброволеца",
                Slug = "den-na-dobrovoletsa",
                Description = "<p>Присъединете се към нас за почистване на Централния парк! Организацията 'За Младежта' организира голям ден на доброволчеството, в който всеки може да допринесе за по-чистата и красива среда в Казанлък.</p><p>Осигуряваме всички необходими материали - ръкавици, чували и инструменти. Участниците ще получат сертификат за доброволчески труд и лека закуска.</p>",
                ShortDescription = "Почистване на Централния парк - присъединете се към доброволците!",
                CoverImageUrl = "https://images.unsplash.com/photo-1559027615-cd4628902d4a?w=800&q=80",
                OrganizerId = ngoUser.Id, VenueId = venue1Id, CategoryId = catVolunteer.Id,
                StartDate = now.AddDays(60).Date.AddHours(9), EndDate = now.AddDays(60).Date.AddHours(14),
                Capacity = 100, IsFree = true, IsAccessible = true, Status = EventStatus.Published
            },
            new Event
            {
                Id = event6Id,
                Title = "Основи на фотографията",
                Slug = "osnovi-na-fotografiyata",
                Description = "<p>Практически курс по фотография за начинаещи. Ще се запознаете с основните настройки на фотоапарата, композицията, осветлението и обработката на снимки. Необходим е собствен фотоапарат или смартфон.</p>",
                ShortDescription = "Практически курс по фотография за начинаещи.",
                CoverImageUrl = "https://images.unsplash.com/photo-1542038784456-1ea8e935640e?w=800&q=80",
                OrganizerId = ivanUser.Id, VenueId = venue2Id, CategoryId = catWorkshops.Id,
                StartDate = now.AddDays(65).Date.AddHours(14), EndDate = now.AddDays(65).Date.AddHours(18),
                Capacity = 20, IsFree = false, IsAccessible = true, Status = EventStatus.PendingApproval
            },
            new Event
            {
                Id = event7Id,
                Title = "Вечер на пролетната музика",
                Slug = "vecher-na-proletnata-muzika",
                Description = "<p>Незабравима вечер с пролетна музика в Централния парк. Местни музиканти и специални гости изпълниха любими хитове под звездното небе. Събитието събра стотици любители на музиката.</p><p>Вечерта включваше изпълнения в различни жанрове - поп, джаз и фолк. Атмосферата беше вълшебна, а публиката - ентусиазирана и топла.</p>",
                ShortDescription = "Незабравима вечер с пролетна музика под звездното небе.",
                CoverImageUrl = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800&q=80",
                OrganizerId = ngoUser.Id, VenueId = venue1Id, CategoryId = catConcerts.Id,
                StartDate = now.AddDays(-20).Date.AddHours(19), EndDate = now.AddDays(-20).Date.AddHours(23),
                Capacity = 300, IsFree = false, IsAccessible = true, Status = EventStatus.Completed
            },
            new Event
            {
                Id = event8Id,
                Title = "Спам събитие",
                Slug = "spam-sabitie",
                Description = "<p>This is a test spam event.</p>",
                ShortDescription = "Spam event for testing rejection workflow.",
                OrganizerId = ivanUser.Id, VenueId = venue2Id, CategoryId = catWorkshops.Id,
                StartDate = now.AddDays(10).Date.AddHours(10), EndDate = now.AddDays(10).Date.AddHours(12),
                IsFree = true, IsAccessible = false, Status = EventStatus.Rejected,
                RejectionReason = "Spam content"
            }
        );

        await context.EventTags.AddRangeAsync(
            new EventTag { EventId = event1Id, TagId = tagMusic },
            new EventTag { EventId = event1Id, TagId = tagOutdoor },
            new EventTag { EventId = event1Id, TagId = tagFree },
            new EventTag { EventId = event1Id, TagId = tagFamily },
            new EventTag { EventId = event1Id, TagId = tagCulture },
            new EventTag { EventId = event2Id, TagId = tagEducation },
            new EventTag { EventId = event2Id, TagId = tagYouth },
            new EventTag { EventId = event3Id, TagId = tagSports },
            new EventTag { EventId = event3Id, TagId = tagOutdoor },
            new EventTag { EventId = event3Id, TagId = tagFree },
            new EventTag { EventId = event4Id, TagId = tagCulture },
            new EventTag { EventId = event5Id, TagId = tagOutdoor },
            new EventTag { EventId = event5Id, TagId = tagFree },
            new EventTag { EventId = event5Id, TagId = tagYouth },
            new EventTag { EventId = event6Id, TagId = tagEducation },
            new EventTag { EventId = event7Id, TagId = tagMusic },
            new EventTag { EventId = event7Id, TagId = tagOutdoor }
        );

        await context.EventImages.AddRangeAsync(
            new EventImage { Id = Guid.NewGuid(), EventId = event1Id, ImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800&q=80", Caption = "Festival opening",  SortOrder = 1, UploadedAt = now.AddDays(-7) },
            new EventImage { Id = Guid.NewGuid(), EventId = event1Id, ImageUrl = "https://images.unsplash.com/photo-1490750967868-88aa4f44baee?w=800&q=80", Caption = "Rose fields",       SortOrder = 2, UploadedAt = now.AddDays(-7) },
            new EventImage { Id = Guid.NewGuid(), EventId = event1Id, ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800&q=80", Caption = "Festival crowd",    SortOrder = 3, UploadedAt = now.AddDays(-7) },
            new EventImage { Id = Guid.NewGuid(), EventId = event1Id, ImageUrl = "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&q=80", Caption = "Park venue",         SortOrder = 4, UploadedAt = now.AddDays(-7) },
            new EventImage { Id = Guid.NewGuid(), EventId = event7Id, ImageUrl = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800&q=80", Caption = "Concert night",     SortOrder = 1, UploadedAt = now.AddDays(-19) },
            new EventImage { Id = Guid.NewGuid(), EventId = event7Id, ImageUrl = "https://images.unsplash.com/photo-1501386761578-eac5c94b800a?w=800&q=80", Caption = "Stage performance", SortOrder = 2, UploadedAt = now.AddDays(-19) },
            new EventImage { Id = Guid.NewGuid(), EventId = event7Id, ImageUrl = "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?w=800&q=80", Caption = "Live music",        SortOrder = 3, UploadedAt = now.AddDays(-19) }
        );

        var ttEvent2StandardId = Guid.NewGuid();
        var ttEvent2VipId      = Guid.NewGuid();
        var ttEvent4EntryId    = Guid.NewGuid();
        var ttEvent7GeneralId  = Guid.NewGuid();

        await context.TicketTypes.AddRangeAsync(
            new TicketType { Id = ttEvent2StandardId, EventId = event2Id, Name = "Standard", Price = 0m, Currency = "EUR", Quantity = 50,  QuantitySold = 2, SortOrder = 1 },
            new TicketType { Id = ttEvent2VipId,      EventId = event2Id, Name = "VIP",      Price = 0m, Currency = "EUR", Quantity = 20,  QuantitySold = 0, SortOrder = 2 },
            new TicketType { Id = ttEvent4EntryId,    EventId = event4Id, Name = "Entry",    Price = 0m, Currency = "EUR", Quantity = 100, QuantitySold = 0, SortOrder = 1 },
            new TicketType { Id = ttEvent7GeneralId,  EventId = event7Id, Name = "General",  Price = 0m, Currency = "EUR", Quantity = 200, QuantitySold = 3, SortOrder = 1 }
        );

        var orderElenaId  = Guid.NewGuid();
        var orderGeorgiId = Guid.NewGuid();

        await context.Orders.AddRangeAsync(
            new Order { Id = orderElenaId,  OrderNumber = "ORD-20260101-0001", UserId = elenaUser.Id,  EventId = event7Id, Status = OrderStatus.Confirmed, TotalAmount = 0m, DiscountAmount = 0m, Currency = "EUR", CreatedAt = now.AddDays(-25) },
            new Order { Id = orderGeorgiId, OrderNumber = "ORD-20260101-0002", UserId = georgiUser.Id, EventId = event7Id, Status = OrderStatus.Confirmed, TotalAmount = 0m, DiscountAmount = 0m, Currency = "EUR", CreatedAt = now.AddDays(-24) }
        );

        var orderItemElenaId  = Guid.NewGuid();
        var orderItemGeorgiId = Guid.NewGuid();

        await context.OrderItems.AddRangeAsync(
            new OrderItem { Id = orderItemElenaId,  OrderId = orderElenaId,  TicketTypeId = ttEvent7GeneralId, Quantity = 2, UnitPrice = 0m, Subtotal = 0m },
            new OrderItem { Id = orderItemGeorgiId, OrderId = orderGeorgiId, TicketTypeId = ttEvent7GeneralId, Quantity = 1, UnitPrice = 0m, Subtotal = 0m }
        );

        await context.Tickets.AddRangeAsync(
            new Ticket { Id = Guid.NewGuid(), TicketNumber = "TKT-E7-001", OrderItemId = orderItemElenaId,  TicketTypeId = ttEvent7GeneralId, HolderId = elenaUser.Id,  QrCode = Guid.NewGuid().ToString("N"), Status = TicketStatus.CheckedIn, CheckedInAt = now.AddDays(-20).AddHours(19.5), IssuedAt = now.AddDays(-25) },
            new Ticket { Id = Guid.NewGuid(), TicketNumber = "TKT-E7-002", OrderItemId = orderItemElenaId,  TicketTypeId = ttEvent7GeneralId, HolderId = elenaUser.Id,  QrCode = Guid.NewGuid().ToString("N"), Status = TicketStatus.CheckedIn, CheckedInAt = now.AddDays(-20).AddHours(19.5), IssuedAt = now.AddDays(-25) },
            new Ticket { Id = Guid.NewGuid(), TicketNumber = "TKT-E7-003", OrderItemId = orderItemGeorgiId, TicketTypeId = ttEvent7GeneralId, HolderId = georgiUser.Id, QrCode = Guid.NewGuid().ToString("N"), Status = TicketStatus.CheckedIn, CheckedInAt = now.AddDays(-20).AddHours(19.5), IssuedAt = now.AddDays(-24) }
        );

        await context.Ratings.AddRangeAsync(
            new Rating { EventId = event7Id, UserId = elenaUser.Id,  Score = 5, ReviewText = "Amazing show!",              CreatedAt = now.AddDays(-19) },
            new Rating { EventId = event7Id, UserId = georgiUser.Id, Score = 4, ReviewText = "Great music, a bit crowded", CreatedAt = now.AddDays(-19) },
            new Rating { EventId = event7Id, UserId = mariaUser.Id,  Score = 5, ReviewText = "Best event of the year",     CreatedAt = now.AddDays(-19) }
        );

        var comment1Id = Guid.NewGuid();
        var comment2Id = Guid.NewGuid();
        var comment3Id = Guid.NewGuid();
        var comment4Id = Guid.NewGuid();

        await context.Comments.AddRangeAsync(
            new Comment { Id = comment1Id, EventId = event1Id, UserId = mariaUser.Id,  Content = "Can't wait! Will there be parking nearby?",                        IsEdited = false, IsHidden = false, UpvoteCount = 3 },
            new Comment { Id = comment2Id, EventId = event1Id, UserId = ivanUser.Id,   Content = "Yes, free parking behind the Culture House, 5 min walk.", ParentCommentId = comment1Id, IsEdited = false, IsHidden = false, UpvoteCount = 5 },
            new Comment { Id = comment3Id, EventId = event1Id, UserId = elenaUser.Id,  Content = "Is this suitable for kids?",                                        IsEdited = false, IsHidden = false, UpvoteCount = 2 },
            new Comment { Id = comment4Id, EventId = event1Id, UserId = georgiUser.Id, Content = "Attending with the whole family!",                                  IsEdited = false, IsHidden = false, UpvoteCount = 4 }
        );

        await context.EventAttendances.AddRangeAsync(
            new EventAttendance { UserId = mariaUser.Id,  EventId = event1Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-10) },
            new EventAttendance { UserId = elenaUser.Id,  EventId = event1Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-9)  },
            new EventAttendance { UserId = georgiUser.Id, EventId = event1Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-8)  },
            new EventAttendance { UserId = ivanUser.Id,   EventId = event1Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-6)  },
            new EventAttendance { UserId = elenaUser.Id,  EventId = event3Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-5)  },
            new EventAttendance { UserId = georgiUser.Id, EventId = event3Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-4)  },
            new EventAttendance { UserId = elenaUser.Id,  EventId = event5Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-3)  },
            new EventAttendance { UserId = mariaUser.Id,  EventId = event5Id, Status = AttendanceStatus.Going, CreatedAt = now.AddDays(-3)  }
        );

        await context.Favorites.AddRangeAsync(
            new Favorite { UserId = elenaUser.Id,  EventId = event1Id, CreatedAt = now.AddDays(-10) },
            new Favorite { UserId = elenaUser.Id,  EventId = event2Id, CreatedAt = now.AddDays(-9)  },
            new Favorite { UserId = elenaUser.Id,  EventId = event3Id, CreatedAt = now.AddDays(-8)  },
            new Favorite { UserId = georgiUser.Id, EventId = event1Id, CreatedAt = now.AddDays(-7)  },
            new Favorite { UserId = georgiUser.Id, EventId = event4Id, CreatedAt = now.AddDays(-6)  }
        );

        await context.Follows.AddRangeAsync(
            new Follow { FollowerId = elenaUser.Id,  FolloweeId = mariaUser.Id, FollowedAt = now.AddDays(-15) },
            new Follow { FollowerId = elenaUser.Id,  FolloweeId = ivanUser.Id,  FollowedAt = now.AddDays(-14) },
            new Follow { FollowerId = georgiUser.Id, FolloweeId = mariaUser.Id, FollowedAt = now.AddDays(-12) }
        );

        await context.Notifications.AddRangeAsync(
            new Notification { Id = Guid.NewGuid(), UserId = mariaUser.Id, Type = NotificationType.EventApproved, Title = "Your event was approved!", Message = "Rose Festival Opening has been approved and published.", LinkUrl = "/Event/Details/otkrivane-na-festivala-na-rozata", IsRead = false, CreatedAt = now.AddHours(-2) },
            new Notification { Id = Guid.NewGuid(), UserId = mariaUser.Id, Type = NotificationType.TicketPurchased, Title = "New registration",        Message = "Elena registered for 2 spots at Youth Leadership Camp",                                                                IsRead = false, CreatedAt = now.AddHours(-5) },
            new Notification { Id = Guid.NewGuid(), UserId = mariaUser.Id, Type = NotificationType.NewFollower,     Title = "New follower",             Message = "Ivan Petrov started following you",                                                                                   IsRead = true,  CreatedAt = now.AddDays(-1)  },
            new Notification { Id = Guid.NewGuid(), UserId = mariaUser.Id, Type = NotificationType.EventReminder,   Title = "Event reminder",           Message = "Spring Music Night starts tomorrow at 19:00",                                                                         IsRead = true,  CreatedAt = now.AddDays(-22) }
        );

        var vtask1Id   = Guid.NewGuid();
        var vtask2Id   = Guid.NewGuid();
        var vtask3Id   = Guid.NewGuid();
        var vshift1A   = Guid.NewGuid();
        var vshift1B   = Guid.NewGuid();
        var vshift1C   = Guid.NewGuid();
        var vshift2D   = Guid.NewGuid();
        var vshift2E   = Guid.NewGuid();
        var vshift3    = Guid.NewGuid();
        var event1Day  = now.AddDays(35).Date;
        var event5Day  = now.AddDays(60).Date;

        await context.VolunteerTasks.AddRangeAsync(
            new VolunteerTask { Id = vtask1Id, EventId = event1Id, Name = "Посрещане на гости и регистрация",    Description = "Приветствайте посетителите, раздавайте програми, отговаряйте на въпроси", VolunteersNeeded = 12 },
            new VolunteerTask { Id = vtask2Id, EventId = event1Id, Name = "Подготовка и разглобяване на сцената", Description = "Помогнете за монтажа и демонтажа на главната сцена.",                          VolunteersNeeded = 6  },
            new VolunteerTask { Id = vtask3Id, EventId = event5Id, Name = "Екип за почистване на парка",          Description = "Помогнете за почистването на района на Централния парк.",                      VolunteersNeeded = 20 }
        );

        await context.VolunteerShifts.AddRangeAsync(
            new VolunteerShift { Id = vshift1A, TaskId = vtask1Id, StartTime = event1Day.AddHours(17),   EndTime = event1Day.AddHours(19),   MaxVolunteers = 4  },
            new VolunteerShift { Id = vshift1B, TaskId = vtask1Id, StartTime = event1Day.AddHours(19),   EndTime = event1Day.AddHours(21),   MaxVolunteers = 4  },
            new VolunteerShift { Id = vshift1C, TaskId = vtask1Id, StartTime = event1Day.AddHours(21),   EndTime = event1Day.AddHours(22),   MaxVolunteers = 4  },
            new VolunteerShift { Id = vshift2D, TaskId = vtask2Id, StartTime = event1Day.AddHours(15),   EndTime = event1Day.AddHours(17),   MaxVolunteers = 3  },
            new VolunteerShift { Id = vshift2E, TaskId = vtask2Id, StartTime = event1Day.AddHours(22),   EndTime = event1Day.AddHours(23).AddMinutes(30), MaxVolunteers = 3  },
            new VolunteerShift { Id = vshift3,  TaskId = vtask3Id, StartTime = event5Day.AddHours(9),    EndTime = event5Day.AddHours(13),   MaxVolunteers = 20 }
        );

        await context.VolunteerSignups.AddRangeAsync(
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift1A, UserId = elenaUser.Id,  Status = VolunteerSignupStatus.Confirmed,  HoursLogged = 2m,   SignedUpAt = now.AddDays(-10) },
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift1A, UserId = ivanUser.Id,   Status = VolunteerSignupStatus.Completed,  HoursLogged = 2m,   SignedUpAt = now.AddDays(-10) },
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift1B, UserId = elenaUser.Id,  Status = VolunteerSignupStatus.Registered, HoursLogged = null, SignedUpAt = now.AddDays(-10) },
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift1C, UserId = georgiUser.Id, Status = VolunteerSignupStatus.Registered, HoursLogged = null, SignedUpAt = now.AddDays(-9)  },
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift2D, UserId = mariaUser.Id,  Status = VolunteerSignupStatus.Confirmed,  HoursLogged = 2m,   SignedUpAt = now.AddDays(-8)  },
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift2E, UserId = elenaUser.Id,  Status = VolunteerSignupStatus.Registered, HoursLogged = 0m,   SignedUpAt = now.AddDays(-10) },
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift3,  UserId = elenaUser.Id,  Status = VolunteerSignupStatus.Registered, SignedUpAt = now.AddDays(-5) },
            new VolunteerSignup { Id = Guid.NewGuid(), ShiftId = vshift3,  UserId = mariaUser.Id,  Status = VolunteerSignupStatus.Registered, SignedUpAt = now.AddDays(-5) }
        );

        await context.BlogPosts.AddRangeAsync(
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Как доброволчеството промени живота ми",
                Slug = "how-volunteering-changed-my-life",
                Content = "<p>Преди три години реших да се включа като доброволец в организацията 'За Младежта'. Не знаех какво да очаквам - просто исках да помогна на общността и да опозная нови хора. Днес мога да кажа, че това беше едно от най-добрите решения в живота ми.</p><p>Чрез доброволчеството открих смисъл в малките неща - усмивката на дете, което е получило помощ, благодарността на възрастен човек, когото сме посетили. Научих се на търпение, работа в екип и лидерство. Познанствата, които направих, се превърнаха в истински приятелства.</p><p>Ако се колебаете дали да се включите, моят съвет е: направете го! Не е нужно много свободно време - дори и малкото, което можете да отделите, прави огромна разлика. Доброволчеството ни прави по-добри хора и по-силна общност.</p>",
                Excerpt = "Личен разказ за това как доброволчеството промени моя живот и защо препоръчвам на всеки да опита.",
                CoverImageUrl = "https://images.unsplash.com/photo-1559027615-cd4628902d4a?w=800&q=80",
                AuthorId = ngoBlogUser.Id, CategoryId = blogCatCom?.Id,
                Status = BlogPostStatus.Published, IsFeatured = true, PublishedAt = now.AddDays(-15)
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Фестивал на розата 2026: пълен гид",
                Slug = "rose-festival-2026-guide",
                Content = "<p>Фестивалът на розата е безспорно най-очакваното събитие в Казанлък всяка година. Тази година той ще се проведе в началото на юни и обещава да бъде по-грандиозен от всякога.</p><p>В този пълен гид ще намерите всичко, което трябва да знаете: програма на събитията, информация за паркиране, препоръки за настаняване и съвети как да се насладите максимално на розобрача.</p>",
                Excerpt = "Всичко, което трябва да знаете за Фестивала на розата 2026.",
                CoverImageUrl = "https://images.unsplash.com/photo-1559027615-cd4628902d4a?w=800&q=80",
                AuthorId = ngoBlogUser.Id, CategoryId = blogCatEv?.Id,
                Status = BlogPostStatus.Published, IsFeatured = false, PublishedAt = now.AddDays(-20)
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Топ 10 неща за правене в Казанлък това лято",
                Slug = "top-10-kazanlak-summer",
                Content = "<p>Казанлък предлага безброй начини да прекарате страхотно лято. От фестивали и спортни събития до разходки в природата и посещения на исторически обекти - има нещо за всеки вкус.</p><p>Ето нашата подборка на 10-те най-добри неща, които можете да направите в Казанлък и региона това лято. Спестете тази статия - ще ви трябва!</p>",
                Excerpt = "Нашата подборка на 10-те най-добри лятни занимания в Казанлък и региона.",
                CoverImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&q=80",
                AuthorId = ngoBlogUser.Id, CategoryId = blogCatGuides?.Id,
                Status = BlogPostStatus.Published, IsFeatured = false, PublishedAt = now.AddDays(-25)
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Запознайте се с нашите нови координатори",
                Slug = "meet-new-coordinators",
                Content = "<p>Радваме се да представим нашите нови доброволчески координатори, които се присъединиха към екипа на 'За Младежта' тази пролет. Те ще отговарят за организирането на доброволческите дейности в различните квартали на града.</p><p>Всеки от тях носи уникален опит и ентусиазъм. Вярваме, че с тяхната помощ ще достигнем до още повече хора и ще направим Казанлък по-добро място за живеене.</p>",
                Excerpt = "Запознайте се с новите доброволчески координатори на организацията.",
                CoverImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=800&q=80",
                AuthorId = ngoBlogUser.Id, CategoryId = blogCatCom?.Id,
                Status = BlogPostStatus.Published, IsFeatured = false, PublishedAt = now.AddDays(-30)
            }
        );

        await context.Sponsors.AddRangeAsync(
            new Sponsor { Id = Guid.NewGuid(), Name = "Община Казанлък",     LogoUrl = "https://images.unsplash.com/photo-1577415124269-fc1140a69e91?w=200&q=80", WebsiteUrl = "https://kazanlak.bg", Description = "Supporting community events and cultural initiatives", Tier = SponsorTier.Gold,   IsActive = true },
            new Sponsor { Id = Guid.NewGuid(), Name = "Банка Розова Долина", LogoUrl = "https://images.unsplash.com/photo-1501167786227-4cba60f6d58f?w=200&q=80",                                     Description = "Financial partner for youth development",              Tier = SponsorTier.Gold,   IsActive = true },
            new Sponsor { Id = Guid.NewGuid(), Name = "Хотел Казанлък",      LogoUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=200&q=80",                                     Description = "Hospitality and tourism partner",                      Tier = SponsorTier.Silver, IsActive = true },
            new Sponsor { Id = Guid.NewGuid(), Name = "ТехПарк БГ",          LogoUrl = "https://images.unsplash.com/photo-1497366754035-f200968a6e72?w=200&q=80",                                                                                                               Tier = SponsorTier.Silver, IsActive = true },
            new Sponsor { Id = Guid.NewGuid(), Name = "Роза Ойл",            LogoUrl = "https://images.unsplash.com/photo-1532635241-17e820acc59f?w=200&q=80",                                                                                                                   Tier = SponsorTier.Bronze, IsActive = true },
            new Sponsor { Id = Guid.NewGuid(), Name = "Марица Принт",        LogoUrl = "https://images.unsplash.com/photo-1588681664899-f142ff2dc9b1?w=200&q=80",                                                                                                                Tier = SponsorTier.Bronze, IsActive = true }
        );

        await context.AuditLogs.AddRangeAsync(
            new AuditLog { UserId = mariaUser.Id, Action = "Created event: Откриване на Фестивала на розата",  EntityType = "Event", EntityId = event1Id.ToString(),    Timestamp = now.AddDays(-7) },
            new AuditLog { UserId = adminUser.Id, Action = "Approved event: Откриване на Фестивала на розата", EntityType = "Event", EntityId = event1Id.ToString(),    Timestamp = now.AddDays(-6) },
            new AuditLog { UserId = elenaUser.Id, Action = "User registered: elena@example.bg",                EntityType = "User",  EntityId = elenaUser.Id.ToString(), Timestamp = now.AddDays(-5) },
            new AuditLog { UserId = elenaUser.Id, Action = "Registered for 2 spot(s) at: Вечер на пролетната музика", EntityType = "Ticket", EntityId = orderElenaId.ToString(), Timestamp = now.AddDays(-4) },
            new AuditLog { UserId = adminUser.Id, Action = "Rejected event: Спам събитие",                    EntityType = "Event", EntityId = event8Id.ToString(),    Timestamp = now.AddDays(-2) }
        );

        await context.Reports.AddRangeAsync(
            new Report { Id = Guid.NewGuid(), ReporterId = elenaUser.Id, TargetType = ReportTargetType.Comment, TargetId = comment4Id, Reason = ReportReason.Spam,         Status = ReportStatus.Pending, CreatedAt = now.AddDays(-3) },
            new Report { Id = Guid.NewGuid(), ReporterId = ivanUser.Id,  TargetType = ReportTargetType.Event,   TargetId = event8Id,   Reason = ReportReason.Inappropriate, Status = ReportStatus.Pending, CreatedAt = now.AddDays(-2) }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("Demo data seeding complete.");
    }
}
