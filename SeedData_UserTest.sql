USE MangaManagementDB;
GO

INSERT INTO auth.Roles (role_name)
VALUES (N'Mangaka'),
	(N'Assistant'),
	(N'Tantou Editor'),
	(N'Editorial Board Member'),
	(N'Editorial Board Chief'),
	(N'Admin');

INSERT INTO manga.Genre
(
    genre_name,
    description
)
VALUES
(N'Action', N'Fast-paced stories with combat, conflict, or physical intensity.'),
(N'Adventure', N'Stories focused on journeys, exploration, discovery, or quests.'),
(N'Comedy', N'Stories primarily designed around humor and amusing situations.'),
(N'Drama', N'Stories focused on emotional conflict, relationships, and character development.'),
(N'Fantasy', N'Stories involving magical, mythical, supernatural, or imaginary worlds.'),
(N'Horror', N'Stories intended to create fear, suspense, dread, or unease.'),
(N'Mystery', N'Stories centered on secrets, investigations, puzzles, or hidden truths.'),
(N'Romance', N'Stories focused on romantic relationships and emotional intimacy.'),
(N'Sci-Fi', N'Stories involving futuristic science, advanced technology, space, or speculative concepts.'),
(N'Slice of Life', N'Stories focused on everyday experiences, personal routines, and ordinary life moments.'),
(N'Sports', N'Stories centered on sports, athletic competition, teamwork, and personal growth.'),
(N'Historical', N'Stories set in or strongly inspired by past historical periods.'),
(N'Psychological', N'Stories focused on mental conflict, perception, trauma, strategy, or inner emotional tension.'),
(N'Mecha', N'Stories involving piloted robots, mechanical suits, or large-scale mechanical warfare.'),
(N'Music', N'Stories centered on musicians, bands, performances, or the music industry.'),
(N'Gourmet', N'Stories centered on cooking, food culture, restaurants, or culinary competition.');
GO

INSERT INTO manga.Tag
(
    tag_name,
    description
)
VALUES
(N'Based on a Novel', N'The series is adapted from or based on a novel.'),
(N'Based on a Web Novel', N'The series is adapted from or based on a web novel.'),
(N'Based on a Game', N'The series is adapted from or inspired by a game.'),
(N'Original Work', N'The series is an original story not directly adapted from another medium.'),

(N'Isekai', N'The story involves reincarnation, summoning, transportation, or existence in another world.'),
(N'Reincarnation', N'The story involves a character being reborn into a new life or body.'),
(N'Time Travel', N'The story involves movement between different points in time.'),
(N'Regression', N'The story involves a character returning to an earlier point in their life or timeline.'),
(N'Transported to Another World', N'The story includes transportation from one world to another.'),
(N'Game-Like World', N'The story world uses game-like systems, levels, quests, or status windows.'),

(N'School Life', N'The story is significantly set in a school environment.'),
(N'Workplace', N'The story is significantly set around jobs, offices, or professional life.'),
(N'Royalty', N'The story includes kings, queens, princes, princesses, nobles, or royal succession.'),
(N'Nobility', N'The story includes noble families, aristocratic society, or noble ranking systems.'),
(N'Military', N'The story involves soldiers, armies, military organizations, or warfare structures.'),
(N'Dungeon', N'The story includes dungeons, raids, monsters, or dungeon exploration.'),
(N'Post-Apocalyptic', N'The story is set after a major disaster or collapse of civilization.'),

(N'Magic', N'Magic is an important element of the story world or plot.'),
(N'Martial Arts', N'The story features martial arts training, combat techniques, or fighting schools.'),
(N'Swordsmanship', N'The story prominently features sword fighting or sword-based combat.'),
(N'Monsters', N'The story includes monsters as important enemies, creatures, or world elements.'),
(N'Demons', N'The story includes demons or demon-like beings as important elements.'),
(N'Vampires', N'The story includes vampires or vampire-like beings.'),
(N'Ghosts', N'The story includes ghosts, spirits, hauntings, or ghost-related plot elements.'),
(N'Mythology', N'The story draws heavily from myths, legends, gods, or folklore.'),

(N'Male Protagonist', N'The main protagonist is male.'),
(N'Female Protagonist', N'The main protagonist is female.'),
(N'Smart Protagonist', N'The protagonist is known for intelligence, planning, or strategy.'),
(N'Overpowered Protagonist', N'The protagonist is significantly stronger or more capable than most characters.'),
(N'Weak to Strong', N'The protagonist starts weak and grows stronger over time.'),
(N'Hard-Working Protagonist', N'The protagonist is characterized by effort, persistence, and growth.'),
(N'Determined Protagonist', N'The protagonist strongly pursues a goal despite obstacles.'),
(N'Kind Protagonist', N'The protagonist is notably kind, empathetic, or compassionate.'),
(N'Antihero Protagonist', N'The protagonist has morally gray traits or methods.'),
(N'Misunderstood Protagonist', N'The protagonist is frequently misunderstood or misjudged by others.'),
(N'Hidden Identity', N'The story includes a character hiding their true identity, status, or background.'),
(N'Masked Character/s', N'The series includes important characters who wear masks or hide their identity.'),

(N'Revenge', N'Revenge is a major motivation or recurring story theme.'),
(N'Survival', N'The story includes survival-focused conflict, danger, or harsh conditions.'),
(N'Tournament', N'The story includes tournaments, competitions, or ranked contests.'),
(N'Training', N'The story includes training arcs, skill development, or improvement through practice.'),
(N'Found Family', N'The story includes characters forming a family-like bond outside blood relations.'),
(N'Coming of Age', N'The story follows personal growth, maturity, or transition into adulthood.'),
(N'Redemption', N'The story includes a character seeking forgiveness, change, or moral recovery.'),
(N'Betrayal', N'The story includes betrayal as an important plot event or recurring conflict.'),

(N'Love Triangle', N'The story includes romantic tension involving three central characters.'),
(N'Slow Burn Romance', N'The romantic relationship develops gradually over time.'),
(N'Enemies to Lovers', N'The story includes characters moving from conflict or rivalry into romance.'),
(N'Childhood Friends', N'The story includes important relationships between characters who knew each other since childhood.'),

(N'Dark Tone', N'The story has a serious, grim, violent, or emotionally heavy tone.'),
(N'Lighthearted Tone', N'The story has a relaxed, gentle, or cheerful tone.'),
(N'Comedic Undertone', N'The story contains noticeable humorous elements without being primarily comedy.'),
(N'Tragic Past', N'The story includes a character with a painful or traumatic backstory.'),
(N'Moral Ambiguity', N'The story includes difficult choices, gray morality, or unclear right and wrong.');
GO

USE MangaManagementDB;
GO

DECLARE @PasswordHash NVARCHAR(255) =
N'$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq';

----------------------------------------------------------------------
-- Seed roles if they do not exist
----------------------------------------------------------------------

;WITH RequiredRoles AS
(
    SELECT *
    FROM (VALUES
        (N'Admin'),
        (N'Tantou Editor'),
        (N'Mangaka'),
        (N'Editorial Board Member'),
        (N'Editorial Board Chief'),
        (N'Assistant')
    ) AS r(role_name)
)
INSERT INTO [auth].[Roles]
(
    [role_name]
)
SELECT
    rr.role_name
FROM RequiredRoles rr
WHERE NOT EXISTS
(
    SELECT 1
    FROM [auth].[Roles] r
    WHERE r.role_name = rr.role_name
);

----------------------------------------------------------------------
-- Seed test users
----------------------------------------------------------------------

;WITH Numbers AS
(
    SELECT *
    FROM (VALUES
        (1),
        (2),
        (3),
        (4),
        (5)
    ) AS n(num)
),
GeneratedUsers AS
(
    -- 1 Admin only
    SELECT
        N'TestAdmin' AS username,
        N'Admin' AS role_name

    UNION ALL

    -- 5 Tantou Editors
    SELECT
        CONCAT(N'TestEditor', num),
        N'Tantou Editor'
    FROM Numbers

    UNION ALL

    -- 5 Mangaka
    SELECT
        CONCAT(N'TestMangaka', num),
        N'Mangaka'
    FROM Numbers

    UNION ALL

    -- 5 Editorial Board Members
    SELECT
        CONCAT(N'TestBoardMember', num),
        N'Editorial Board Member'
    FROM Numbers

    UNION ALL

    -- 5 Editorial Board Chiefs
    SELECT
        CONCAT(N'TestBoardChief', num),
        N'Editorial Board Chief'
    FROM Numbers

    UNION ALL

    -- 5 Assistants
    SELECT
        CONCAT(N'TestAssistant', num),
        N'Assistant'
    FROM Numbers
)
INSERT INTO [auth].[Users]
(
    [username],
    [password_hash],
    [role_id],
    [display_name],
    [email]
)
SELECT
    gu.username,
    @PasswordHash,
    r.role_id,
    gu.username,   -- display_name same as username for test users
    CONCAT(gu.username, N'@example.com')
FROM GeneratedUsers gu
INNER JOIN [auth].[Roles] r
    ON r.role_name = gu.role_name
WHERE NOT EXISTS
(
    SELECT 1
    FROM [auth].[Users] u
    WHERE u.username = gu.username
);

----------------------------------------------------------------------
-- Verification
----------------------------------------------------------------------

SELECT
    u.username,
    r.role_name
FROM [auth].[Users] u
INNER JOIN [auth].[Roles] r
    ON r.role_id = u.role_id
WHERE u.username = N'TestAdmin'
   OR u.username LIKE N'TestEditor%'
   OR u.username LIKE N'TestMangaka%'
   OR u.username LIKE N'TestBoardMember%'
   OR u.username LIKE N'TestBoardChief%'
   OR u.username LIKE N'TestAssistant%'
ORDER BY
    CASE r.role_name
        WHEN N'Admin' THEN 1
        WHEN N'Mangaka' THEN 2
        WHEN N'Assistant' THEN 3
        WHEN N'Tantou Editor' THEN 4
        WHEN N'Editorial Board Member' THEN 5
        WHEN N'Editorial Board Chief' THEN 6
        ELSE 99
    END,
    u.username;
