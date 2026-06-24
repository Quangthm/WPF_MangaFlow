
CREATE DATABASE WPFMangaManagementDB;
GO

USE WPFMangaManagementDB;
GO

SET NOCOUNT ON;
GO

IF SCHEMA_ID(N'manga') IS NULL
	EXEC (N'CREATE SCHEMA manga');
GO

IF SCHEMA_ID(N'auth') IS NULL
	EXEC (N'CREATE SCHEMA auth');
GO

IF SCHEMA_ID(N'audit') IS NULL
	EXEC (N'CREATE SCHEMA audit');
GO
CREATE TABLE auth.Roles (
    role_id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT df_roles_role_id DEFAULT NEWID()
        CONSTRAINT pk_roles PRIMARY KEY,

    role_name NVARCHAR(30) NOT NULL,

    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);



CREATE TABLE auth.Users (
	user_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_users_user_id DEFAULT NEWID() PRIMARY KEY,
	role_id UNIQUEIDENTIFIER NOT NULL,
	username NVARCHAR(50) NOT NULL,
	password_hash NVARCHAR(255) NOT NULL,
	CONSTRAINT uq_users_username UNIQUE (username),
	CONSTRAINT fk_userrole_role FOREIGN KEY (role_id) REFERENCES auth.Roles(role_id)
	);
	
CREATE TABLE manga.FileResource (
	file_resource_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_file_resource_id DEFAULT NEWID() CONSTRAINT pk_file_resource PRIMARY KEY,
	file_purpose_code NVARCHAR(50) NOT NULL,
	original_file_name NVARCHAR(260) NOT NULL,
	cloudinary_public_id NVARCHAR(255) NOT NULL,
	cloudinary_secure_url NVARCHAR(1000) NOT NULL,
	content_type NVARCHAR(100) NOT NULL,
	file_size_bytes BIGINT NOT NULL,
	sha256_hash CHAR(64) NOT NULL,
	uploaded_by_user_id UNIQUEIDENTIFIER NULL,
	uploaded_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_file_resource_uploaded_at_utc DEFAULT(SYSUTCDATETIME()),
	deleted_at_utc DATETIME2(0) NULL,
	deleted_by_user_id UNIQUEIDENTIFIER NULL,
	CONSTRAINT ck_file_resource_file_purpose_code CHECK (
		file_purpose_code IN (
			N'SERIES_PROPOSAL',
			N'SERIES_COVER',
			N'CHAPTER_PAGE_VERSION',
			N'EDITORIAL_ATTACHMENT',
			N'REGISTRATION_PORTFOLIO',
			N'USER_AVATAR'
			)
		),
	CONSTRAINT ck_file_resource_file_size_positive CHECK (file_size_bytes > 0),
	CONSTRAINT ck_file_resource_deleted_pair CHECK (
		(
			deleted_at_utc IS NULL
			AND deleted_by_user_id IS NULL
			)
		OR (
			deleted_at_utc IS NOT NULL
			AND deleted_by_user_id IS NOT NULL
			)
		),
	CONSTRAINT uq_file_resource_cloudinary_public_id UNIQUE (cloudinary_public_id),
	CONSTRAINT fk_file_resource_deleted_by_user FOREIGN KEY (deleted_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_file_resource_uploaded_by_user FOREIGN KEY (uploaded_by_user_id) REFERENCES auth.Users(user_id)
	);

CREATE TABLE manga.Series (
	series_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_series_id DEFAULT NEWID() CONSTRAINT pk_series PRIMARY KEY,
	title NVARCHAR(200) NOT NULL,
	slug NVARCHAR(220) NOT NULL,
	synopsis NVARCHAR(MAX) NOT NULL,
	cover_file_id UNIQUEIDENTIFIER NULL,
	status_code NVARCHAR(50) NOT NULL CONSTRAINT df_series_current_status_code DEFAULT(N'PROPOSAL_DRAFT'),
	content_language_code NVARCHAR(10) NOT NULL CONSTRAINT df_series_content_language_code DEFAULT(N'ja'),
	source_series_id UNIQUEIDENTIFIER NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_created_at_utc DEFAULT(SYSUTCDATETIME()),
	updated_at_utc DATETIME2(0) NULL,
	updated_by_user_id UNIQUEIDENTIFIER NULL,
	publication_frequency_code NVARCHAR(50) NULL,
	CONSTRAINT ck_series_current_status_code CHECK (
		status_code IN (
			N'PROPOSAL_DRAFT',
			N'UNDER_EDITORIAL_REVIEW',
			N'UNDER_BOARD_REVIEW',
			N'SERIALIZED',
			N'HIATUS',
			N'CANCELLED',
			N'COMPLETED'
			)
		),
	CONSTRAINT ck_series_content_language_code CHECK (
		content_language_code IN (
			N'ja',
			N'en',
			N'vi'
			)
		),
	CONSTRAINT ck_series_source_not_self CHECK (
		source_series_id IS NULL
		OR source_series_id <> series_id
		),
	CONSTRAINT ck_series_updated_pair CHECK (
		(
			updated_at_utc IS NULL
			AND updated_by_user_id IS NULL
			)
		OR (
			updated_at_utc IS NOT NULL
			AND updated_by_user_id IS NOT NULL
			)
		),
	CONSTRAINT ck_series_publication_frequency_code CHECK (
		publication_frequency_code IS NULL
		OR publication_frequency_code IN (
			N'WEEKLY',
			N'MONTHLY',
			N'IRREGULAR'
			)
		),
	CONSTRAINT uq_series_slug UNIQUE (slug),
	CONSTRAINT fk_series_cover_file FOREIGN KEY (cover_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT fk_series_source_series FOREIGN KEY (source_series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id)
	);

CREATE TABLE manga.Genre
(
    genre_id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT df_genre_id DEFAULT NEWID(),

    genre_name NVARCHAR(100) NOT NULL,

    description NVARCHAR(500) NULL,

    CONSTRAINT pk_genre
        PRIMARY KEY (genre_id),

    CONSTRAINT uq_genre_name
        UNIQUE (genre_name),

    CONSTRAINT ck_genre_name_not_blank
        CHECK (LEN(LTRIM(RTRIM(genre_name))) > 0)
);
CREATE TABLE manga.SeriesGenre
(
    series_id UNIQUEIDENTIFIER NOT NULL,

    genre_id UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT pk_series_genre
        PRIMARY KEY (series_id, genre_id),

    CONSTRAINT fk_series_genre_series
        FOREIGN KEY (series_id)
        REFERENCES manga.Series(series_id),

    CONSTRAINT fk_series_genre_genre
        FOREIGN KEY (genre_id)
        REFERENCES manga.Genre(genre_id)
);

CREATE TABLE manga.Tag
(
    tag_id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT df_tag_id DEFAULT NEWID(),

    tag_name NVARCHAR(100) NOT NULL,

    description NVARCHAR(500) NULL,

    CONSTRAINT pk_tag
        PRIMARY KEY (tag_id),

    CONSTRAINT uq_tag_name
        UNIQUE (tag_name),

    CONSTRAINT ck_tag_name_not_blank
        CHECK (LEN(LTRIM(RTRIM(tag_name))) > 0)
);

CREATE TABLE manga.SeriesTag
(
    series_id UNIQUEIDENTIFIER NOT NULL,

    tag_id UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT pk_series_tag
        PRIMARY KEY (series_id, tag_id),

    CONSTRAINT fk_series_tag_series
        FOREIGN KEY (series_id)
        REFERENCES manga.Series(series_id),

    CONSTRAINT fk_series_tag_tag
        FOREIGN KEY (tag_id)
        REFERENCES manga.Tag(tag_id)
);

CREATE TABLE manga.SeriesContributor (
	series_contributor_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_series_contributor_id DEFAULT NEWID() CONSTRAINT pk_series_contributor PRIMARY KEY,
	series_id UNIQUEIDENTIFIER NOT NULL,
	user_id UNIQUEIDENTIFIER NOT NULL,
	start_date DATE NOT NULL CONSTRAINT df_series_contributor_start_date DEFAULT(CONVERT(DATE, SYSUTCDATETIME())),
	end_date DATE NULL,
	notes NVARCHAR(500) NULL,
	CONSTRAINT ck_series_contributor_date_range CHECK (
		end_date IS NULL
		OR end_date >= start_date
		),
	CONSTRAINT fk_series_contributor_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_contributor_user FOREIGN KEY (user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT uq_series_contributor_series_user_start UNIQUE (
		series_id,
		user_id,
		start_date
		)
	);

CREATE TABLE manga.SeriesProposal (
	series_proposal_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_series_proposal_id DEFAULT NEWID() CONSTRAINT pk_series_proposal PRIMARY KEY,
	series_id UNIQUEIDENTIFIER NOT NULL,
	proposal_version_no SMALLINT NOT NULL,
	proposal_title NVARCHAR(200) NOT NULL,
	synopsis_snapshot NVARCHAR(MAX) NOT NULL,
	proposal_file_id UNIQUEIDENTIFIER NOT NULL,
	status_code NVARCHAR(50) NOT NULL CONSTRAINT df_series_proposal_status_code DEFAULT(N'UNDER_EDITORIAL_REVIEW'),
	submitted_by_user_id UNIQUEIDENTIFIER NOT NULL,
	submitted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_proposal_submitted_at_utc DEFAULT SYSUTCDATETIME(),
	withdrawn_at_utc DATETIME2(0) NULL,
	reviewed_by_user_id UNIQUEIDENTIFIER NULL,
	reviewed_at_utc DATETIME2(0) NULL,
	comments NVARCHAR(MAX) NULL,
	markup_file_id UNIQUEIDENTIFIER NULL,
	CONSTRAINT ck_series_proposal_status_code CHECK (
		status_code IN (
			N'UNDER_EDITORIAL_REVIEW',
			N'UNDER_BOARD_REVIEW',
			N'REVISION_REQUESTED',
			N'APPROVED',
			N'CANCELLED',
			N'WITHDRAWN'
			)
		),
	CONSTRAINT ck_series_proposal_version_positive CHECK (proposal_version_no > 0),
	CONSTRAINT ck_series_proposal_withdrawn_at_matches_status CHECK (
		(
			status_code = N'WITHDRAWN'
			AND withdrawn_at_utc IS NOT NULL
			)
		OR (
			status_code <> N'WITHDRAWN'
			AND withdrawn_at_utc IS NULL
			)
		),
	CONSTRAINT ck_series_proposal_review_pair CHECK (
		(
			reviewed_by_user_id IS NULL
			AND reviewed_at_utc IS NULL
			)
		OR (
			reviewed_by_user_id IS NOT NULL
			AND reviewed_at_utc IS NOT NULL
			)
		),
	CONSTRAINT fk_series_proposal_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_proposal_file FOREIGN KEY (proposal_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT fk_series_proposal_submitted_by FOREIGN KEY (submitted_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_series_proposal_reviewed_by FOREIGN KEY (reviewed_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_series_proposal_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT uq_series_proposal_series_version UNIQUE (
		series_id,
		proposal_version_no
		)
	);

CREATE TABLE manga.Chapter (
	chapter_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_chapter_id DEFAULT NEWID() CONSTRAINT pk_chapter PRIMARY KEY,
	series_id UNIQUEIDENTIFIER NOT NULL,
	chapter_number_label NVARCHAR(20) NOT NULL,
	chapter_title NVARCHAR(200) NULL,
	status_code NVARCHAR(50) NOT NULL CONSTRAINT df_chapter_status_code DEFAULT(N'DRAFT'),
	chapter_file_id UNIQUEIDENTIFIER NULL,
	planned_release_date DATE NULL,
	released_at_utc DATETIME2(0) NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_created_at_utc DEFAULT(SYSUTCDATETIME()),
	created_by_user_id UNIQUEIDENTIFIER NULL,
	updated_at_utc DATETIME2(0) NULL,
	CONSTRAINT ck_chapter_status_code CHECK (
		status_code IN (
			N'DRAFT',
			N'UNDER_REVIEW',
			N'REVISION_REQUESTED',
			N'APPROVED',
			N'SCHEDULED',
			N'RELEASED',
			N'ON_HOLD',
			N'CANCELLED'
			)
		),
	CONSTRAINT ck_chapter_released_at_required CHECK (
		status_code <> N'RELEASED'
		OR released_at_utc IS NOT NULL
		),
	CONSTRAINT ck_chapter_scheduled_planned_release_required CHECK (
		status_code <> N'SCHEDULED'
		OR planned_release_date IS NOT NULL
		),
	CONSTRAINT fk_chapter_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_chapter_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_chapter_file
    FOREIGN KEY (chapter_file_id)
    REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT uq_chapter_series_chapter_number UNIQUE (
		series_id,
		chapter_number_label
		)
	);

CREATE TABLE manga.ChapterEditorialReview (
	chapter_editorial_review_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_chapter_editorial_review_id DEFAULT NEWID() CONSTRAINT pk_chapter_editorial_review PRIMARY KEY,
	chapter_id UNIQUEIDENTIFIER NOT NULL,
	reviewer_user_id UNIQUEIDENTIFIER NOT NULL,
	decision_code NVARCHAR(50) NOT NULL,
	comments NVARCHAR(MAX) NULL,
	markup_file_id UNIQUEIDENTIFIER NULL,
	reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_editorial_review_reviewed_at_utc DEFAULT(SYSUTCDATETIME()),
	CONSTRAINT ck_chapter_editorial_review_decision_code CHECK (
		decision_code IN (
			N'APPROVED',
			N'REVISION_REQUESTED',
			N'CANCELLED'
			)
		),
	CONSTRAINT ck_chapter_editorial_review_feedback_required CHECK (
		decision_code = N'APPROVED'
		OR comments IS NOT NULL
		OR markup_file_id IS NOT NULL
		),
	CONSTRAINT fk_chapter_editorial_review_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
	CONSTRAINT fk_chapter_editorial_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_chapter_editorial_review_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id)
	);

CREATE TABLE manga.SeriesBoardPoll (
	series_board_poll_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_series_board_poll_id DEFAULT NEWID() CONSTRAINT pk_series_board_poll PRIMARY KEY,
	series_id UNIQUEIDENTIFIER NOT NULL,
	poll_type_code NVARCHAR(50) NOT NULL,
	poll_reason NVARCHAR(MAX) NOT NULL,
	poll_status_code NVARCHAR(50) NOT NULL CONSTRAINT df_series_board_poll_status_code DEFAULT(N'OPEN'),
	board_publication_frequency_code NVARCHAR(50) NULL,
	created_by_user_id UNIQUEIDENTIFIER NOT NULL,
	started_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_poll_started_at_utc DEFAULT(SYSUTCDATETIME()),
	ends_at_utc DATETIME2(0) NULL,
	CONSTRAINT ck_series_board_poll_type_code CHECK (
		poll_type_code IN (
			N'START_SERIALIZATION',
			N'CANCEL_SERIALIZATION'
			)
		),
	CONSTRAINT ck_series_board_poll_status_code CHECK (
		poll_status_code IN (
			N'OPEN',
			N'CLOSED',
			N'CANCELLED'
			)
		),
	CONSTRAINT ck_series_board_poll_frequency_code CHECK (
		board_publication_frequency_code IS NULL
		OR board_publication_frequency_code IN (
			N'WEEKLY',
			N'MONTHLY',
			N'IRREGULAR'
			)
		),
	CONSTRAINT ck_series_board_poll_frequency_required CHECK (
		(
			poll_type_code = N'START_SERIALIZATION'
			AND board_publication_frequency_code IS NOT NULL
			)
		OR (
			poll_type_code = N'CANCEL_SERIALIZATION'
			AND board_publication_frequency_code IS NULL
			)
		),
	CONSTRAINT ck_series_board_poll_time_range CHECK (
		ends_at_utc IS NULL
		OR ends_at_utc > started_at_utc
		),
	CONSTRAINT fk_series_board_poll_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_board_poll_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id)
	);

	CREATE UNIQUE INDEX ux_series_board_poll_open_type ON manga.SeriesBoardPoll (
	series_id,
	poll_type_code
	)
WHERE poll_status_code = N'OPEN';
CREATE TABLE manga.SeriesBoardVote (
	series_board_vote_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_series_board_vote_id DEFAULT NEWID() CONSTRAINT pk_series_board_vote PRIMARY KEY,
	series_board_poll_id UNIQUEIDENTIFIER NOT NULL,
	user_id UNIQUEIDENTIFIER NOT NULL,
	choice_code NVARCHAR(50) NOT NULL,
	vote_reason NVARCHAR(500) NULL,
	voted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_vote_voted_at_utc DEFAULT(SYSUTCDATETIME()),
	CONSTRAINT ck_series_board_vote_choice_code CHECK (
		choice_code IN (
			N'APPROVE',
			N'REJECT',
			N'ABSTAIN'
			)
		),
	CONSTRAINT ck_series_board_vote_reject_reason CHECK (
		choice_code <> N'REJECT'
		OR NULLIF(LTRIM(RTRIM(vote_reason)), N'') IS NOT NULL
		),
	CONSTRAINT fk_series_board_vote_poll FOREIGN KEY (series_board_poll_id) REFERENCES manga.SeriesBoardPoll(series_board_poll_id),
	CONSTRAINT fk_series_board_vote_board_member FOREIGN KEY (user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT uq_series_board_vote_poll_board_member UNIQUE (
		series_board_poll_id,
		user_id
		)
	);
Go
CREATE VIEW manga.vw_SeriesBoardPollVoteSummary
AS
SELECT p.series_board_poll_id,
	p.series_id,
	s.title AS series_title,
	p.poll_type_code,
	p.poll_status_code,
	p.poll_reason,
	p.created_by_user_id,
	p.started_at_utc,
	p.ends_at_utc,
	SUM(CASE 
			WHEN v.choice_code = N'APPROVE'
				THEN 1
			ELSE 0
			END) AS approve_count,
	SUM(CASE 
			WHEN v.choice_code = N'REJECT'
				THEN 1
			ELSE 0
			END) AS reject_count,
	SUM(CASE 
			WHEN v.choice_code = N'ABSTAIN'
				THEN 1
			ELSE 0
			END) AS abstain_count,
	COUNT(v.series_board_vote_id) AS total_vote_count,
	CASE 
		WHEN p.poll_status_code = N'CANCELLED'
			THEN N'INVALIDATED'
		WHEN p.poll_status_code <> N'CLOSED'
			THEN N'PENDING'
		WHEN SUM(CASE 
					WHEN v.choice_code = N'APPROVE'
						THEN 1
					ELSE 0
					END) > SUM(CASE 
					WHEN v.choice_code = N'REJECT'
						THEN 1
					ELSE 0
					END)
			THEN N'APPROVED'
		WHEN SUM(CASE 
					WHEN v.choice_code = N'REJECT'
						THEN 1
					ELSE 0
					END) > SUM(CASE 
					WHEN v.choice_code = N'APPROVE'
						THEN 1
					ELSE 0
					END)
			THEN N'REJECTED'
		ELSE N'NO_DECISION'
		END AS computed_result_code,
	CASE 
		WHEN p.poll_status_code = N'CLOSED'
			THEN CAST(1 AS BIT)
		ELSE CAST(0 AS BIT)
		END AS is_applicable
FROM manga.SeriesBoardPoll p
JOIN manga.Series s ON p.series_id = s.series_id
LEFT JOIN manga.SeriesBoardVote v ON p.series_board_poll_id = v.series_board_poll_id
GROUP BY p.series_board_poll_id,
	p.series_id,
	s.title,
	p.poll_type_code,
	p.poll_status_code,
	p.poll_reason,
	p.created_by_user_id,
	p.started_at_utc,
	p.ends_at_utc;
 