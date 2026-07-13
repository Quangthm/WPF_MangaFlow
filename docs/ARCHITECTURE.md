# WPF Manga Management System — Architecture Reference

> **Mục đích:** Tài liệu này ghi lại toàn bộ cấu trúc project, nhiệm vụ từng phần, luồng dữ liệu và quy tắc để các AI session sau có thể triển khai code chính xác, tránh technical debt.
>
> **Last updated:** 2026-07-05

---

## 1. Tổng quan hệ thống

### 1.1 Stack công nghệ

| Layer | Công nghệ | Version |
|---|---|---|
| Desktop Client | WPF (.NET 8, Windows) | `net8.0-windows` |
| MVVM Framework | CommunityToolkit.Mvvm | 8.4.2 |
| DI Container | Microsoft.Extensions.DependencyInjection | 10.0.9 |
| Backend API | ASP.NET Core 8 Web API | `net8.0` |
| CQRS | MediatR | 12.x (Registered via Assembly Scanning) |
| ORM | Entity Framework Core 8 + SQL Server | Via Infrastructure |
| Auth | JWT Bearer + BCrypt password hashing | Via Infrastructure |
| File Storage | Cloudinary (CloudinaryDotNet) | Via Infrastructure |
| Database | SQL Server 2022 | `WPFMangaManagementDB` |

### 1.2 Kiến trúc tổng thể

```
┌──────────────────────────────────────────────────────────────────┐
│                     WPF Desktop Client                           │
│           MangaManagementSystem.WpfMini                          │
│                                                                  │
│  Views (XAML) → ViewModels → ApiClientBase → HTTP → Backend API │
└──────────────────────────────────────────────────────────────────┘
                              │ HTTP REST (JSON / Multipart)
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                     ASP.NET Core API                             │
│           MangaManagementSystem.API                              │
│                                                                  │
│  Controllers → MediatR Commands/Queries → Handlers              │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│           MangaManagementSystem.Application                      │
│                                                                  │
│  CQRS (Commands + Queries + Handlers)                            │
│  Application Services                                            │
│  DTOs                                                            │
│  Interfaces (Ports)                                              │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                     Domain Layer                                 │
│           MangaManagementSystem.Domain                           │
│                                                                  │
│  Entities (pure C#)                                              │
│  Repository Interfaces                                           │
│  Enums / ReadModels / Common                                     │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                         │
│           MangaManagementSystem.Infrastructure                   │
│                                                                  │
│  EF Core DbContext + Configurations                              │
│  Repository Implementations                                      │
│  Unit of Work                                                    │
│  External Services (Cloudinary, MailKit, BCrypt, AI)             │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                     SQL Server                                   │
│           WPFMangaManagementDB                                   │
│                                                                  │
│  Schemas: auth, manga, audit                                     │
│  Tables: Users, Roles, Series, SeriesProposal,                   │
│          Chapter, FileResource, Genre, Tag,                       │
│          SeriesBoardPoll, SeriesBoardVote, ...                    │
│  Views: vw_SeriesBoardPollVoteSummary,                            │
│         vw_ActiveSeriesContributor                                │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Cấu trúc Solution

### 2.1 Sơ đồ project dependencies

```
MangaManagementSystem.API
  ├── MangaManagementSystem.Application
  │     └── MangaManagementSystem.Domain
  └── MangaManagementSystem.Infrastructure
        └── MangaManagementSystem.Domain
        └── MangaManagementSystem.Application (via Interface implementations)

MangaManagementSystem.WpfMini  (standalone — NO references to other projects)
  └── HTTP → MangaManagementSystem.API
```

**Quan trọng:** WpfMini là standalone application, không reference trực tiếp các project backend. Nó giao tiếp qua HTTP REST với API.

### 2.2 Chi tiết từng project

#### 2.2.1 `MangaManagementSystem.Domain` — Domain Layer

**Vai trò:** Pure domain entities, repository interfaces, common base types. **Zero dependencies.**

```
Domain/
├── Common/
│   └── BaseEntity.cs
├── Entities/
│   ├── Series.cs              — Series entity (title, slug, synopsis, cover, status_code...)
│   ├── SeriesProposal.cs      — Submitted proposal version (snapshot + editor review fields)
│   ├── SeriesContributor.cs   — Junction: User ↔ Series contributor link
│   ├── Chapter.cs             — Chapter entity (number label, title, status_code...)
│   ├── ChapterEditorialReview.cs — Editor decision record for chapter
│   ├── ChapterPage.cs         — Page-level entity (CUT from WPF scope)
│   ├── ChapterPageVersion.cs  — Page version (CUT from WPF scope)
│   ├── ChapterPageAnnotation.cs — Page annotation (CUT from WPF scope)
│   ├── ChapterPageTask.cs     — Assistant task (CUT from WPF scope)
│   ├── PageRegion.cs          — Page region (CUT from WPF scope)
│   ├── SeriesBoardPoll.cs     — Board poll (tied to series_id only)
│   ├── SeriesBoardVote.cs     — Board vote (per user per poll)
│   ├── SeriesBoardPollVoteSummary.cs — View model for vote summary
│   ├── ActiveSeriesContributor.cs — View model for active contributors
│   ├── FileResource.cs        — File metadata (Cloudinary fields)
│   ├── Genre.cs / Tag.cs      — Lookup entities
│   ├── User.cs / Role.cs      — Auth entities
│   ├── Notification.cs        — Notification (CUT from WPF scope)
│   └── AuditEvent.cs          — Audit log (CUT from WPF scope)
├── Enums/
│   └── enums.cs              — (Placeholder, chưa dùng)
├── ReadModels/
│   └── SeriesContributorReadModel.cs
└── Interfaces/
    ├── ISeriesRepository.cs
    ├── IChapterRepository.cs
    ├── IUserRepository.cs
    ├── ISeriesProposalRepository.cs
    ├── ISeriesContributorRepository.cs
    ├── IReferenceDataRepository.cs
    ├── IUnitOfWork.cs
    ├── IGenericRepository.cs
    ├── IEditorDashboardRepository.cs
    ├── IEditorChapterReviewRepository.cs
    ├── IEditorAnnotationRepository.cs
    ├── IEditorSeriesRepository.cs
    ├── IEditorialBoardRepository.cs
    ├── IQuickSelectRepository.cs
    ├── ILandingPageRepository.cs
    ├── IAssistantCompletedWorkRepository.cs
    ├── IChapterPageTaskRepository.cs
    ├── IChapterPageAnnotationRepository.cs
    └── IMangakaChapterRepository.cs
```

#### 2.2.2 `MangaManagementSystem.Application` — Application Layer

**Vai trò:** Business logic orchestration. Chứa CQRS (MediatR commands/queries/handlers), application services, DTOs, interfaces (ports).

```
Application/
├── DependencyInjection.cs      — Đăng ký MediatR + Application Services
│
├── DTOs/
│   ├── Auth/
│   │   ├── AuthDtos.cs         — LoginDto, LoginResultDto, ...
│   │   ├── UserDtos.cs         — UserDto
│   │   ├── RoleDtos.cs         — RoleDto
│   │   └── GoogleSignupDtos.cs
│   ├── Editor/
│   │   ├── EditorDashboardDtos.cs       — Dashboard KPIs + queue preview
│   │   ├── EditorChapterReviewDtos.cs   — Chapter review queue + detail
│   │   ├── EditorSeriesDtos.cs          — Series list for editor
│   │   └── EditorAnnotationDtos.cs      — Annotation workspace (CUT)
│   └── Manga/
│       ├── SeriesDtos.cs               — SeriesDto, SeriesDetailDto
│       ├── SeriesProposalDtos.cs        — ProposalQueueItem, EditorProposalDetailDto (KEY)
│       ├── ChapterDtos.cs
│       ├── MangakaChapterDtos.cs
│       ├── GenreDto.cs / TagDto.cs
│       ├── FileResourceDtos.cs          — FileResourceDto
│       ├── SeriesBoardPollDtos.cs       — Board poll DTOs
│       ├── SeriesBoardVoteDtos.cs       — Board vote DTOs
│       └── ... (20+ DTO files)
│
├── Features/                         ← CQRS Workflows
│   ├── Mangaka/
│   │   ├── Series/Commands/          — Create/Update/Cancel Series draft
│   │   ├── Series/Queries/           — GetMyMangakaSeries, GetMyMangakaSeriesCardById
│   │   ├── Chapters/Commands/        — Create/Update/Submit Chapter draft
│   │   ├── Chapters/Queries/         — GetMangakaSeriesChapters
│   │   ├── SeriesProposals/Commands/ + Queries/  — Proposal submit/cancel
│   │   └── Contributors/Commands/ + Queries/
│   ├── Editor/
│   │   ├── SeriesProposals/Commands/ — Claim, RequestRevision, PassToBoard, Cancel
│   │   ├── SeriesProposals/Queries/  — GetEditorialProposalQueue, GetEditorProposalDetail
│   │   ├── ChapterReviews/Queries/   — GetEditorChapterReviewQueue, GetEditorChapterReviewDetail
│   │   ├── Dashboard/Queries/        — GetEditorDashboard
│   │   ├── Series/Queries/           — GetEditorSeries
│   │   └── Annotations/Queries/      — GetEditorAnnotations (CUT)
│   ├── EditorialBoard/
│   │   ├── Commands/                 — OpenPoll, CastVote, FinalizeApproval, CancelPoll
│   │   ├── Queries/                  — GetDashboard, GetOpenPolls, GetPollHistory
│   │   ├── Dtos/                     — EditorialBoardPollDto, OpenPoll request/result, ...
│   │   └── Repositories/            — IEditorialBoardRepository
│   ├── Series/Queries/               — GetSeriesBySlug, GetSeriesWorkspaceEntry
│   ├── ReferenceData/Queries/        — GetGenres, GetTags
│   ├── Assistant/ (CUT from WPF)
│   └── Auth/ (handled via Services)
│
├── Interfaces/                   ← 31 interfaces (ports)
│   ├── IAuthService.cs
│   ├── ISeriesService.cs
│   ├── IChapterService.cs
│   ├── IFileStorageService.cs
│   ├── IFileResourceService.cs
│   ├── ISeriesProposalService.cs
│   ├── ISeriesBoardPollService.cs
│   ├── ISeriesBoardVoteService.cs
│   ├── IChapterEditorialReviewService.cs
│   ├── IEmailService.cs
│   ├── IPasswordHasher.cs
│   ├── INotificationService.cs
│   └── ... (20+ interfaces)
│
├── Services/                      ← 21 service implementations
│   ├── AuthService.cs             — Login, password verification, JWT token generation
│   ├── SeriesService.cs
│   ├── ChapterService.cs
│   ├── FileResourceService.cs
│   ├── SeriesProposalService.cs
│   ├── SeriesBoardPollService.cs
│   ├── SeriesBoardVoteService.cs
│   ├── ChapterEditorialReviewService.cs
│   └── ... (12+ services)
│
├── Mappers/
│   └── UserMapper.cs
└── Common/
    ├── Constants/
    ├── Policies/SeriesNavigationPolicy.cs
    ├── Security/LogSanitizer.cs
    └── SlugGenerator.cs
```

#### 2.2.3 `MangaManagementSystem.Infrastructure` — Infrastructure Layer

**Vai trò:** Triển khai các interface Domain + Application. EF Core, repository implementations, external services.

```
Infrastructure/
├── DependencyInjection.cs        — Đăng ký DbContext, repositories, services
├── Options/
│   ├── CloudinarySettings.cs
│   └── SmtpSettings.cs
├── Persistence/
│   ├── ApplicationDbContext.cs   — DbContext (22 DbSet, 2 views)
│   └── Configurations/           — 22 Fluent API config files
│       ├── SeriesConfiguration.cs
│       ├── SeriesProposalConfiguration.cs
│       ├── ChapterConfiguration.cs
│       ├── UserConfiguration.cs
│       ├── FileResourceConfiguration.cs
│       ├── GenreConfiguration.cs
│       ├── TagConfiguration.cs
│       ├── SeriesBoardPollConfiguration.cs
│       ├── SeriesBoardVoteConfiguration.cs
│       └── ... (22 files total)
├── Repositories/                 — 20 repository implementations
│   ├── GenericRepository.cs
│   ├── UnitOfWork.cs
│   ├── SeriesRepository.cs
│   ├── SeriesProposalRepository.cs
│   ├── ChapterRepository.cs
│   ├── UserRepository.cs
│   ├── EditorialBoardRepository.cs
│   ├── EditorDashboardRepository.cs
│   ├── EditorChapterReviewRepository.cs
│   ├── EditorSeriesRepository.cs
│   └── ... (20 files total)
└── Services/
    ├── CloudinaryFileStorageService.cs  — File upload to Cloudinary
    ├── BcryptPasswordHasher.cs          — Password hashing
    ├── EmailService.cs                  — MailKit SMTP
    ├── AiService.cs                     — AI HTTP client (CUT)
    ├── OtpCacheService.cs              — OTP cache
    └── AssistantTaskSubmissionService.cs
```

#### 2.2.4 `MangaManagementSystem.API` — API Layer

**Vai trò:** HTTP boundary. Thin controllers → delegate to MediatR/Application services.

```
API/
├── Program.cs                        — Host builder, DI, JWT, Swagger
├── appsettings.json                  — JWT, SMTP config (connection string in User Secrets)
├── Properties/launchSettings.json
├── Controllers/
│   ├── AuthController.cs             — POST /api/auth/login (JWT)
│   ├── ReferenceDataController.cs    — Genres, Tags
│   ├── SeriesController.cs           — GET /api/series/{slug}
│   ├── BoardPollsController.cs       — (STUB — TODO only, use EditorialBoardController instead)
│   ├── EditorialBoardController.cs   — Board dashboard, polls, votes (FULL, JWT auth)
│   ├── RegistrationController.cs
│   ├── ProfileController.cs
│   ├── ProfilePasswordController.cs
│   ├── Mangaka/
│   │   ├── MangakaSeriesController.cs
│   │   ├── MangakaChaptersController.cs
│   │   ├── MangakaSeriesContributorController.cs
│   │   └── QuickSelectController.cs
│   ├── Editor/
│   │   ├── EditorProposalsController.cs    — Proposal queue, detail, claim, revision, pass, cancel
│   │   ├── EditorChapterReviewsController.cs — Chapter review queue + detail
│   │   ├── EditorDashboardController.cs    — Dashboard KPIs
│   │   ├── EditorSeriesController.cs       — Series library
│   │   └── EditorAnnotationsController.cs  — (CUT)
│   └── Assistant/
│       ├── AssistantCompletedWorkController.cs
│       └── AssistantTaskController.cs
│
└── Contracts/                       — Request/Response DTOs cho API boundary
    ├── LoginRequest.cs / LoginResponse.cs
    ├── CreateSeriesDraftForm.cs
    ├── UpdateSeriesDraftForm.cs
    ├── SubmitSeriesProposalForm.cs
    ├── EditorProposalRequests.cs    — ClaimProposalRequest, RequestRevisionForm, PassToBoardForm, CancelProposalForm
    ├── MangakaChapterRequests.cs
    ├── ApiResponses.cs
    └── BoardPolls/
```

#### 2.2.5 `MangaManagementSystem.WpfMini` — WPF Desktop Client

**Vai trò:** Standalone WPF desktop app gọi API backend.

```
WpfMini/
├── App.xaml / App.xaml.cs           — Entry point, DI setup
├── MainWindow.xaml / .xaml.cs      — Shell window (ContentControl binds CurrentViewModel)
├── appsettings.json                 — { "ApiBaseUrl": "http://localhost:5234" }
├── Models/
│   ├── AuthModels.cs                — LoginRequest, LoginResponse, TestUserDto
│   └── CurrentUserSession.cs        — Session model (role checks: IsMangaka, IsEditor...)
├── Services/
│   ├── ApiClientBase.cs             — Generic HTTP client (SetAuthToken, GET, POST, PostForm)
│   └── AuthApiClient.cs             — LoginAsync, GetTestUsersAsync
├── ViewModels/
│   ├── LoginViewModel.cs            — Login, QuickLogin, LoadTestUsers
│   ├── MainWindowViewModel.cs       — Session management, navigation (SetSession, Logout)
│   └── ShellViewModel.cs            — Header + logout relay
├── Views/
│   ├── LoginView.xaml / .xaml.cs    — Login form + quick test-user buttons
│   └── ShellView.xaml / .xaml.cs    — Header (display name + role + logout) + placeholder content
├── Converters/
│   └── InverseBoolConverter.cs
├── Styles/
│   └── AppStyles.xaml
└── AssemblyInfo.cs
```

---

## 3. Database Schema

### 3.1 Database: `WPFMangaManagementDB`

Script: `WPFMangaManagementDB.sql` (root project folder)

### 3.2 Schemas

| Schema | Mục đích |
|---|---|
| `auth` | Authentication — Roles, Users |
| `manga` | Core business — Series, Chapter, FileResource, Genre/Tag, Board polls/votes |
| `audit` | Audit logs (CUT from WPF scope) |

### 3.3 Tables & Objects

| Table / View | Schema | Ghi chú |
|---|---|---|
| `Roles` | `auth` | role_id, role_name |
| `Users` | `auth` | user_id, role_id, username, password_hash, display_name, email |
| `FileResource` | `manga` | file storage metadata (Cloudinary fields) |
| `Series` | `manga` | title, slug, synopsis, cover_file_id, status_code, content_language_code |
| `Genre` | `manga` | genre_name, description |
| `SeriesGenre` | `manga` | Junction: Series ↔ Genre |
| `Tag` | `manga` | tag_name, description |
| `SeriesTag` | `manga` | Junction: Series ↔ Tag |
| `SeriesContributor` | `manga` | user_id ↔ series_id + role_code |
| `SeriesProposal` | `manga` | proposal_version_no, proposal_title, synopsis_snapshot, proposal_file_id, status_code (UNDER_EDITORIAL_REVIEW / UNDER_BOARD_REVIEW / REVISION_REQUESTED / APPROVED / CANCELLED / WITHDRAWN) |
| `Chapter` | `manga` | chapter_number_label, chapter_title, status_code (DRAFT / UNDER_REVIEW / APPROVED / REVISION_REQUESTED / CANCELLED) |
| `ChapterEditorialReview` | `manga` | chapter_id, reviewer_id, decision_code, comments, markup_file_id |
| `SeriesBoardPoll` | `manga` | series_id, poll_type_code, poll_status_code, started_at_utc, ends_at_utc |
| `SeriesBoardVote` | `manga` | poll_id, user_id, choice_code (APPROVE/REJECT/ABSTAIN), vote_reason |
| `vw_SeriesBoardPollVoteSummary` | `manga` | View — vote counts per poll |
| `vw_ActiveSeriesContributor` | `manga` | View — active contributors |

### 3.4 Series Status Flow

```
PROPOSAL_DRAFT ──Submit──▶ UNDER_EDITORIAL_REVIEW ──PassToBoard──▶ UNDER_BOARD_REVIEW ──ClosePoll──▶ SERIALIZED
       ▲                        │                                                     │
       └──RequestRevision───────┘                                                     └──CANCELLED
```

### 3.5 Chapter Status Flow

```
DRAFT ──Submit──▶ UNDER_REVIEW ──Approve──▶ APPROVED
                    │
                    └──RequestRevision──▶ REVISION_REQUESTED ──Submit──▶ UNDER_REVIEW
                    │
                    └──Cancel──▶ CANCELLED
```

---

## 4. Authentication

### 4.1 Cơ chế

| Component | Cách hoạt động |
|---|---|
| **Backend** | JWT Bearer tokens. `AuthService.LoginAsync()` → verify BCrypt hash → generate JWT. |
| **Editor controllers** | Dùng transitional `X-Actor-User-Id` header (`TryResolveActorUserId()` từ header) |
| **Board controller** | Dùng `[Authorize]` + JWT claims (`GetCurrentUserId()` từ `ClaimTypes.NameIdentifier`) |
| **WpfMini** | Gọi `POST /api/wpf/auth/login` (chưa có backend), lưu token trong `CurrentUserSession`, set vào `ApiClientBase.SetAuthToken()` |

### 4.2 WIP: Cần đồng bộ auth pattern

Hiện tại backend Editor dùng `X-Actor-User-Id` header (cũ), Board dùng JWT (mới). Khi tạo WpfMini endpoints, **nên dùng JWT pattern** (giống Board) để thống nhất.

### 4.3 Test Users (Seed)

| Username | Role | Password |
|---|---|---|
| TestAdmin | Admin | password |
| TestEditor1..5 | Tantou Editor | password |
| TestMangaka1..5 | Mangaka | password |
| TestBoardMember1..5 | Editorial Board Member | password |
| TestBoardChief1..5 | Editorial Board Chief | password |
| TestAssistant1..5 | Assistant | password |

---

## 5. Quan trọng: WPF Scope — Cái được giữ và cái bị cắt

### 5.1 ✅ Trong scope WPF mini-project

| Workflow | Tables liên quan | Ghi chú |
|---|---|---|
| Auth + Login | `auth.Roles`, `auth.Users` | Quick login + JWT |
| Series CRUD | `manga.Series`, `manga.SeriesGenre`, `manga.SeriesTag`, `manga.SeriesContributor` | Mangaka tạo/sửa draft |
| Series Proposal | `manga.SeriesProposal`, `manga.FileResource` | Submit version → editor review |
| Editor Proposal Review | `manga.SeriesProposal`, `manga.Series` | Request revision / Pass to board / Cancel |
| Board Poll & Vote | `manga.SeriesBoardPoll`, `manga.SeriesBoardVote` | Chief opens poll → members vote → Chief closes |
| Chapter CRUD | `manga.Chapter` | Chỉ khi Series = SERIALIZED |
| Chapter Review | `manga.ChapterEditorialReview` | Editor approve / revision / cancel |
| File Upload | `manga.FileResource` | Cover, proposal, markup, chapter files |

### 5.2 ❌ Cắt khỏi WPF scope

| Entity / Workflow | Lý do |
|---|---|
| `ChapterPage`, `ChapterPageVersion`, `PageRegion` | Page-level workflow — dùng `Chapter.chapter_file_id` thay thế |
| `ChapterPageAnnotation` | AI/annotation workflow |
| `ChapterPageTask` | Assistant task workflow |
| `Notification` | Real-time notification phức tạp |
| `AuditEvent` | Audit log không cần cho mini-project |
| `SeriesRankingSnapshot` | Ranking không cần |
| `SeriesEditorialReview` | Đã replace bởi `SeriesProposal.reviewed_by_user_id`, `comments`, `markup_file_id` |
| AI/OCR/segmentation | Out of scope |

---

## 6. API Endpoints Plan (cho WpfMini)

### 6.1 Trạng thái endpoints

| Endpoint | Backend tồn tại? | WpfMini gọi đến? | Ghi chú |
|---|---|---|---|
| `POST /api/wpf/auth/login` | ❌ Chưa | ✅ WpfMini gọi | **Cần tạo** — wrapper từ `AuthController` |
| `GET /api/wpf/auth/test-users` | ❌ Chưa | ✅ WpfMini gọi | **Cần tạo** — query test users |
| `GET /api/wpf/series/my` | ❌ Chưa | ❌ Chưa | Cần tạo cho Person B |
| `POST /api/wpf/series` | ❌ Chưa | ❌ Chưa | Cần tạo cho Person B |
| `GET /api/wpf/editor/proposals/queue` | ❌ Chưa | ❌ Chưa | **Cần tạo** — wrapper từ `EditorProposalsController` |
| `GET /api/wpf/editor/proposals/{id}` | ❌ Chưa | ❌ Chưa | **Cần tạo** |
| `POST /api/wpf/editor/proposals/{id}/request-revision` | ❌ Chưa | ❌ Chưa | **Cần tạo** |
| `POST /api/wpf/editor/proposals/{id}/pass-to-board` | ❌ Chưa | ❌ Chưa | **Cần tạo** |
| `POST /api/wpf/editor/proposals/{id}/cancel` | ❌ Chưa | ❌ Chưa | **Cần tạo** |
| `GET /api/wpf/board/polls` | ❌ Chưa | ❌ Chưa | Cần tạo — wrapper từ `EditorialBoardController` |
| `POST /api/wpf/board/series/{id}/polls` | ❌ Chưa | ❌ Chưa | Cần tạo |
| `POST /api/wpf/board/polls/{id}/votes` | ❌ Chưa | ❌ Chưa | Cần tạo |
| `POST /api/wpf/board/polls/{id}/close` | ❌ Chưa | ❌ Chưa | Cần tạo |
| `POST /api/wpf/files/upload` | ❌ Chưa | ❌ Chưa | Cần tạo file upload endpoint |

### 6.2 Chiến lược tạo WpfMini endpoints

**Không viết lại business logic.** Tạo `Controllers/WpfMini/*` controllers mới, gọi lại:
- `IMediator.Send()` → tái sử dụng Command/Query Handlers có sẵn
- Hoặc gọi Application Services có sẵn

Ví dụ pattern:
```csharp
[ApiController]
[Route("api/wpf/editor/proposals")]
public class WpfEditorProposalsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] string? status)
    {
        var query = new GetEditorialProposalQueueQuery(status);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

---

## 7. WpfMini — Quy tắc phát triển

### 7.1 File naming conventions

| Loại | Convention | Ví dụ |
|---|---|---|
| Models | `*Models.cs` | `EditorModels.cs`, `BoardModels.cs`, `SeriesModels.cs` |
| Services | `*ApiClient.cs` | `EditorApiClient.cs`, `BoardApiClient.cs` |
| ViewModels | `*ViewModel.cs` | `ProposalReviewViewModel.cs` |
| Views | `*View.xaml` + `.xaml.cs` | `ProposalReviewView.xaml` |

### 7.2 MVVM Rules

- Dùng **CommunityToolkit.Mvvm**:
  - `[ObservableProperty]` cho properties
  - `[RelayCommand]` cho commands
  - `ObservableObject` base class
  - `AsyncRelayCommand` cho async (generated từ `async Task` + `[RelayCommand]`)
- `MainWindowViewModel` quản lý navigation bằng cách swap `CurrentViewModel`
- `ApiClientBase` là singleton, dùng chung cho mọi API call
- `SetAuthToken()` gọi sau login thành công
- `ClearAuthToken()` gọi khi logout

### 7.3 WPF Layout Rules

- **Ưu tiên Canvas layout** thay vì DataGrid để linh hoạt UI
- Dùng `ListBox` + `ItemTemplate` cho danh sách (queue, list)
- Dùng `Border` + CornerRadius cho card-style components
- `StackPanel`, `Grid`, `WrapPanel` cho layout cơ bản
- `ScrollViewer` bọc nội dung dài
- Converters:
  - `StatusToBrushConverter` — màu theo status code
  - `RoleToVisibilityConverter` — ẩn/hiện theo role
  - `InverseBoolConverter` — đã có

### 7.4 Editor UI Pattern (Master-Detail)

Màn hình Editor Proposal Review là **1 View duy nhất** (master-detail), không tách rời:
```
┌──────────────────────────────────────────────┐
│              HEADER: Title                    │
├─────────────────────┬────────────────────────┤
│ Queue (ListBox)     │ Detail + Actions Panel  │
│ ← chọn proposal     │ (read-only thông tin)   │
│                     │ Comments textbox        │
│                     │ Markup file picker      │
│                     │ Action buttons          │
└─────────────────────┴────────────────────────┘
```

### 7.5 Data Flow

```
User Action → View (XAML event binding)
  → ViewModel ([RelayCommand] method)
    → ApiClient (HTTP call to backend)
      → Backend API (Controller → MediatR Handler → DB)
    ← Response DTO
  ← Update ObservableProperty
← UI tự động cập nhật (data binding)
```

---

## 8. Team Assignment & Trạng thái

| Person | Ownership | Trạng thái |
|---|---|---|
| **A** | Backend inspection, WPF scaffold, API client base, auth/login, shell/navigation | ✅ WPF shell + login done. Cần tạo `/api/wpf/auth/*` backend |
| **B** | Series CRUD, reference data, file upload, proposal submit | ❌ Chưa bắt đầu |
| **C (bạn)** | **Editor proposal review + Board poll/vote** | ❌ Chưa bắt đầu |
| **D** | Chapter CRUD/review, demo polish | ❌ Chưa bắt đầu |

### 8.1 Person C — Chi tiết nhiệm vụ

**Backend (tạo mới):**
- `Controllers/WpfMini/WpfEditorProposalsController.cs` — 5 endpoints
- `Controllers/WpfMini/WpfBoardController.cs` — 4 endpoints

**WPF Client (tạo mới):**
- `Models/EditorModels.cs`
- `Services/EditorApiClient.cs`
- `Services/BoardApiClient.cs`
- `ViewModels/EditorProposalReviewViewModel.cs` (master-detail)
- `ViewModels/BoardPollListViewModel.cs`
- `ViewModels/BoardPollDetailViewModel.cs`
- `Views/EditorProposalReviewView.xaml`
- `Views/BoardPollListView.xaml`
- `Views/BoardPollDetailView.xaml`
- Mở rộng `ShellView.xaml` (sidebar navigation)
- `Converters/StatusToBrushConverter.cs`
- `Converters/RoleToVisibilityConverter.cs`

---

## 9. Các lưu ý kỹ thuật quan trọng

### 9.1 Database connection

**Hiện tại:** API đã cấu hình kết nối DB qua EF Core. Connection string lưu trong **User Secrets** (không trong `appsettings.json`), key là `ConnectionStrings:DefaultConnection`.

```
Server=localhost;Database=WPFMangaManagementDB;User Id=sa;Password=12345;...
```

Tuy nhiên User Secrets hiện tại vẫn đang dùng `Database=MangaManagementDB` (cần cập nhật). Xem `setup-secrets.ps1` để sửa.

### 9.2 Migration / Tạo DB

Chưa có EF Core Migration — DB được tạo bằng script SQL thuần (`WPFMangaManagementDB.sql`). Khi thêm field mới, cần cập nhật cả SQL script + Domain entity + EF Configuration.

### 9.3 File Upload

Backend dùng Cloudinary cho file storage thật. WpfMini cần:
1. Upload file → `POST /api/wpf/files/upload` (multipart)
2. Nhận về `fileResourceId`
3. Dùng `fileResourceId` trong các submit command

### 9.4 Auth pattern cho WpfMini endpoints

**Khuyến nghị:** Dùng JWT pattern (giống `EditorialBoardController`) thay vì `X-Actor-User-Id` header (giống `EditorProposalsController`) cho các WpfMini endpoints mới.

### 9.5 UI Polish

- Dùng `StatusToBrushConverter` để màu sắc theo status (đỏ = CANCELLED, xanh = APPROVED, vàng = UNDER_REVIEW, v.v.)
- Dùng role checks (`CurrentUserSession.IsEditor`, `IsBoardChief`, `IsBoardMember`) để ẩn/hiện controls
- Error messages hiển thị qua `ErrorMessage` property + `TextBlock` trong XAML

---

## 10. Seed Data

File: `SeedData_UserTest.sql`

Đã seed:
- 6 roles
- 16 genres
- 54 tags
- 26 test users (1 Admin + 5 mỗi role)

Password hash: `$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq` (BCrypt của "password")

---

## 11. File .gitignore

Đã ignore:
- `setup-secrets.ps1` (chứa API keys, SMTP passwords, Cloudinary secrets, Google OAuth, Recaptcha keys)
- Build output (`bin/`, `obj/`)
- VS user files (`.suo`, `.user`)
- `.vs/` directory

---

## 12. Các quyết định kiến trúc (ADR)

| Quyết định | Lý do |
|---|---|
| **SeriesProposal giữ lại** | UI là Series CRUD, nhưng submit tạo SeriesProposal version cho editor review |
| **BoardPoll gắn với series_id** | Không thêm series_proposal_id. Backend resolve latest proposal khi cần |
| **EF Core thay stored procedures** | WPF-mini handlers dùng EF Core transactions, không gọi stored procedures |
| **`/api/wpf/*` route prefix riêng** | Tránh ảnh hưởng full MangaFlow web app |
| **DataGrid → Canvas** | Canvas layout linh hoạt hơn cho WPF custom UI |
