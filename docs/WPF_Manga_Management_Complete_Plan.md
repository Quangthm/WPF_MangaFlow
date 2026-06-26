# WPF Manga Management — Revised API + EF Core Implementation Plan

> **Target:** WPF desktop app for manga series submission, editorial review, board voting, chapter CRUD, and chapter review.  
> **Database:** `WPFMangaManagementDB` or a WPF mini database branch compatible with the selected MangaFlow schema.  
> **Direction:** Reuse the current MangaFlow Clean Architecture/CQRS/API shape where possible.  
> **Major changes from teammate plan:** Use API calls instead of direct DB/Dapper, do real file upload instead of dummy `FileResource`, and implement WPF-mini write workflows with EF Core rather than stored procedures.

---

## 1. Revised Architecture Direction

The WPF app should behave like another frontend client of the existing MangaFlow backend.

```text
WPF Views
→ WPF ViewModels
→ Typed API Clients
→ ASP.NET Core API
→ MediatR Commands / Queries
→ Application Handlers
→ Infrastructure EF Core Repositories / Services
→ SQL Server
→ Cloudinary / FileResource service for uploads
```

### Why this is better

| Area | Teammate plan | Revised direction |
|---|---|---|
| Data access | WPF → Dapper → SQL Server | WPF → API → MediatR → EF Core |
| Workflow writes | Stored procedures | EF Core handlers for WPF-mini scope |
| File handling | Dummy `FileResource` rows | Real upload using existing backend file services |
| Validation | DB/SP validation + WPF validation | WPF validation + Application handler validation + DB constraints |
| Reuse | Reuses some SPs only | Reuses API, DTOs, upload services, CQRS structure, auth/session concepts |
| Risk | Duplicates backend logic inside WPF | Keeps WPF thin and closer to MangaFlow architecture |

The WPF app should **not** directly insert workflow rows using Dapper unless the API approach becomes impossible near the deadline.

---

## 2. Important Design Decisions

### 2.1 Keep `Series + SeriesProposal`

The UI should still feel like **Series CRUD**, but internally:

```text
Series = current editable series profile
SeriesProposal = submitted proposal snapshot/version for review
```

The WPF screen should not be named “Proposal CRUD.” The user journey is:

```text
Mangaka creates/edits Series
→ Mangaka submits Series
→ API creates SeriesProposal version
→ Editor reviews latest SeriesProposal
→ Board votes on Series via SeriesBoardPoll
```

### 2.2 Board poll stays tied to `series_id`

The selected direction keeps:

```text
SeriesBoardPoll.series_id
```

No `series_proposal_id` is added to the board poll. The API should resolve the latest proposal by:

```text
latest SeriesProposal for the series where status is UNDER_BOARD_REVIEW
```

This is acceptable for the WPF mini-project and avoids more schema changes.

### 2.3 Use actual file uploads

Do not use dummy FileResource rows.

WPF should select files locally, then send them to the API as multipart upload:

| File | Purpose code |
|---|---|
| Series cover | `SERIES_COVER` |
| Proposal document | `SERIES_PROPOSAL` |
| Chapter package / chapter file | Use existing compatible purpose or add `CHAPTER_PACKAGE` if schema allows |
| Editor markup file | `EDITORIAL_ATTACHMENT` |

If the current `FileResource` purpose check does not include `CHAPTER_PACKAGE`, either:

1. Use `EDITORIAL_ATTACHMENT` for chapter review markup and keep `chapter_file_id` limited to an existing allowed purpose; or
2. Add `CHAPTER_PACKAGE` to the WPF mini schema and API validation.

### 2.4 Use EF Core instead of stored procedures for WPF-mini writes

Reason: the current stored procedures include audit, validation, locking, legacy workflow assumptions, and extra parameters. For a WPF mini-project, duplicating that on top of Application validation adds overhead.

The API handlers should use EF Core transactions where needed:

```text
Create/update Series
Replace SeriesGenre / SeriesTag rows
Create FileResource after successful upload
Create SeriesProposal version
Update Series/Proposal status together
Create poll/vote rows
Create/update Chapter and review rows
```

Keep database constraints as the final safety layer, but business validation should live in Application handlers.

---

## 3. Backend Reuse Strategy

### 3.1 Inspect existing MangaFlow first

Before writing new WPF code, inspect current backend features:

| Area | Inspect |
|---|---|
| Series draft | Create/update/cancel draft commands and DTOs |
| Proposal submit | Current `SeriesProposal` submit command/API |
| Editor review | Current proposal review commands |
| Board voting | Existing poll/vote handlers, DTOs, queries |
| Chapter | Existing Chapter CRUD/review support |
| Reference data | Genre/tag endpoints |
| File upload | Cloudinary/FileResource service and API upload endpoints |
| Auth | Existing login/session/JWT or test login strategy |

### 3.2 Reuse as-is

Likely reusable:

```text
Genre/tag lookup queries
DTO mapping patterns
Typed API client pattern
File upload service
Cloudinary/FileResource creation logic
Auth/password verification ideas
MediatR command/query organization
EF Core configurations/entities
```

### 3.3 Adapt or add WPF-mini endpoints

If existing endpoints are too tied to the full web UI, add a small WPF route area:

```text
/api/wpf/auth
/api/wpf/reference
/api/wpf/series
/api/wpf/editor/proposals
/api/wpf/board
/api/wpf/chapters
/api/wpf/editor/chapters
/api/wpf/files
```

These endpoints can still use Application/MediatR and EF Core. They should not call stored procedures unless there is a strong reason.

---

## 4. Revised Project Structure

### 4.1 WPF Client

```text
MangaManagementSystem.WpfMini/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml
├── appsettings.json
│
├── Models/
│   ├── CurrentUserSession.cs
│   ├── AuthModels.cs
│   ├── SeriesModels.cs
│   ├── SeriesProposalModels.cs
│   ├── ChapterModels.cs
│   ├── BoardModels.cs
│   ├── FileModels.cs
│   └── LookupModels.cs
│
├── Services/
│   ├── ApiClientBase.cs
│   ├── AuthApiClient.cs
│   ├── ReferenceDataApiClient.cs
│   ├── SeriesApiClient.cs
│   ├── EditorProposalApiClient.cs
│   ├── BoardApiClient.cs
│   ├── ChapterApiClient.cs
│   ├── EditorChapterApiClient.cs
│   ├── FileUploadApiClient.cs
│   ├── FilePickerService.cs
│   └── DialogService.cs
│
├── ViewModels/
│   ├── LoginViewModel.cs
│   ├── MainWindowViewModel.cs
│   ├── MangakaSeriesListViewModel.cs
│   ├── SeriesEditorViewModel.cs
│   ├── EditorProposalReviewViewModel.cs
│   ├── BoardPollListViewModel.cs
│   ├── BoardPollDetailViewModel.cs
│   ├── ChapterListViewModel.cs
│   └── EditorChapterReviewViewModel.cs
│
├── Views/
│   ├── LoginView.xaml
│   ├── ShellView.xaml
│   ├── MangakaSeriesListView.xaml
│   ├── SeriesEditorView.xaml
│   ├── EditorProposalReviewView.xaml
│   ├── BoardPollListView.xaml
│   ├── BoardPollDetailView.xaml
│   ├── ChapterListView.xaml
│   └── EditorChapterReviewView.xaml
│
├── Converters/
│   ├── StatusToBrushConverter.cs
│   ├── RoleToVisibilityConverter.cs
│   └── NullToVisibilityConverter.cs
│
└── Styles/
    └── AppStyles.xaml
```

### 4.2 Backend additions

Prefer adding WPF-mini feature folders instead of rewriting full existing code:

```text
Application/
└── Features/
    └── WpfMini/
        ├── Auth/
        ├── Series/
        ├── Proposals/
        ├── Board/
        ├── Chapters/
        ├── Files/
        └── ReferenceData/

API/
└── Controllers/
    └── WpfMini/
        ├── WpfAuthController.cs
        ├── WpfReferenceController.cs
        ├── WpfSeriesController.cs
        ├── WpfEditorProposalController.cs
        ├── WpfBoardController.cs
        ├── WpfChapterController.cs
        ├── WpfEditorChapterController.cs
        └── WpfFilesController.cs
```

---

## 5. API Endpoints

### 5.1 Auth

```text
POST /api/wpf/auth/login
GET  /api/wpf/auth/test-users
```

For a demo, quick login is acceptable. For real login, the API verifies username/password and returns a lightweight session DTO or token.

### 5.2 Reference Data

```text
GET /api/wpf/reference/genres
GET /api/wpf/reference/tags
```

### 5.3 File Upload

```text
POST /api/wpf/files/upload
```

Request:

```text
multipart/form-data
- file
- purposeCode
```

Response:

```json
{
  "fileResourceId": "guid",
  "originalFileName": "proposal.pdf",
  "cloudinarySecureUrl": "https://...",
  "contentType": "application/pdf",
  "fileSizeBytes": 12345
}
```

### 5.4 Series

```text
GET    /api/wpf/series/my
GET    /api/wpf/series/{seriesId}
POST   /api/wpf/series
PUT    /api/wpf/series/{seriesId}
DELETE /api/wpf/series/{seriesId}
POST   /api/wpf/series/{seriesId}/submit
```

Submit request should include:

```json
{
  "proposalFileId": "guid"
}
```

The proposal file is uploaded first through `/api/wpf/files/upload`.

### 5.5 Editor Proposal Review

```text
GET  /api/wpf/editor/proposals/queue
GET  /api/wpf/editor/proposals/{proposalId}
POST /api/wpf/editor/proposals/{proposalId}/claim
POST /api/wpf/editor/proposals/{proposalId}/request-revision
POST /api/wpf/editor/proposals/{proposalId}/pass-to-board
POST /api/wpf/editor/proposals/{proposalId}/cancel
```

Revision/cancel request:

```json
{
  "comments": "Please revise the synopsis and improve character pitch.",
  "markupFileId": "optional-guid"
}
```

### 5.6 Board

```text
GET  /api/wpf/board/series-ready
GET  /api/wpf/board/polls
GET  /api/wpf/board/polls/{pollId}
POST /api/wpf/board/series/{seriesId}/polls
POST /api/wpf/board/polls/{pollId}/votes
POST /api/wpf/board/polls/{pollId}/close
POST /api/wpf/board/polls/{pollId}/cancel
```

Board poll remains tied to `series_id`.

### 5.7 Chapters

```text
GET    /api/wpf/series/{seriesId}/chapters
POST   /api/wpf/series/{seriesId}/chapters
PUT    /api/wpf/chapters/{chapterId}
DELETE /api/wpf/chapters/{chapterId}
POST   /api/wpf/chapters/{chapterId}/submit
```

Chapter creation/update may include:

```json
{
  "chapterNumberLabel": "1",
  "chapterTitle": "Beginning",
  "chapterFileId": "optional-guid",
  "plannedReleaseDate": "2026-07-01"
}
```

### 5.8 Editor Chapter Review

```text
GET  /api/wpf/editor/chapters/queue
POST /api/wpf/editor/chapters/{chapterId}/approve
POST /api/wpf/editor/chapters/{chapterId}/request-revision
POST /api/wpf/editor/chapters/{chapterId}/cancel
```

---

## 6. EF Core Command/Handler Plan

### 6.1 Series commands

| Command | EF Core behavior |
|---|---|
| `CreateWpfSeriesDraftCommand` | Insert `Series`, insert `SeriesGenre`, `SeriesTag`, insert creator `SeriesContributor` |
| `UpdateWpfSeriesDraftCommand` | Update profile if `PROPOSAL_DRAFT`, replace genre/tag links |
| `CancelWpfSeriesDraftCommand` | Set `Series.status_code = CANCELLED` or delete only if safe |
| `SubmitWpfSeriesProposalCommand` | Create new `SeriesProposal` version from current `Series`, link uploaded proposal file, set `Series.status_code = UNDER_EDITORIAL_REVIEW` |

### 6.2 Proposal review commands

| Command | EF Core behavior |
|---|---|
| `ClaimWpfProposalReviewCommand` | Optionally set reviewer fields or use claim marker if schema supports it |
| `RequestWpfProposalRevisionCommand` | Set proposal `REVISION_REQUESTED`, save comments/markup, set Series `PROPOSAL_DRAFT` |
| `PassWpfProposalToBoardCommand` | Set proposal `UNDER_BOARD_REVIEW`, save comments/markup, set Series `UNDER_BOARD_REVIEW` |
| `CancelWpfProposalReviewCommand` | Set proposal `CANCELLED`, save comments/markup, set Series `CANCELLED` |

### 6.3 Board commands

| Command | EF Core behavior |
|---|---|
| `OpenWpfSeriesBoardPollCommand` | Insert `SeriesBoardPoll` for `series_id`; validate latest proposal is `UNDER_BOARD_REVIEW` |
| `CastWpfSeriesBoardVoteCommand` | Insert or update one `SeriesBoardVote` per user per poll |
| `CloseWpfSeriesBoardPollCommand` | Close poll, count votes, update latest proposal + series status |
| `CancelWpfSeriesBoardPollCommand` | Set poll `CANCELLED` |

### 6.4 Chapter commands

| Command | EF Core behavior |
|---|---|
| `CreateWpfChapterDraftCommand` | Insert `Chapter` under `SERIALIZED` series |
| `UpdateWpfChapterDraftCommand` | Update if `DRAFT` or `REVISION_REQUESTED` |
| `DeleteWpfChapterDraftCommand` | Delete/cancel if `DRAFT` |
| `SubmitWpfChapterForReviewCommand` | Set chapter `UNDER_REVIEW`; require chapter file if project scope requires |
| `ApproveWpfChapterCommand` | Insert `ChapterEditorialReview(APPROVED)`, set chapter `APPROVED` |
| `RequestWpfChapterRevisionCommand` | Insert review with comments/markup, set chapter `REVISION_REQUESTED` |
| `CancelWpfChapterReviewCommand` | Insert review with comments/markup, set chapter `CANCELLED` |

---

## 7. Transaction Rules

### Submit proposal

```text
Begin transaction
Load Series with genres/tags
Validate actor is Mangaka contributor
Validate Series.status_code = PROPOSAL_DRAFT
Validate proposalFileId exists and purpose = SERIES_PROPOSAL
Compute next proposal version number
Insert SeriesProposal
Update Series.status_code = UNDER_EDITORIAL_REVIEW
SaveChanges
Commit
```

### Pass proposal to board

```text
Begin transaction
Load proposal + series
Validate proposal.status_code = UNDER_EDITORIAL_REVIEW
Validate actor is Tantou Editor
Update proposal fields
Update series.status_code = UNDER_BOARD_REVIEW
SaveChanges
Commit
```

### Close board poll

```text
Begin transaction
Load poll + votes + series
Validate poll is OPEN
Set poll CLOSED
Count votes
Find latest SeriesProposal for poll.series_id with status UNDER_BOARD_REVIEW
If approve > reject:
    proposal.status_code = APPROVED
    series.status_code = SERIALIZED
If reject > approve:
    proposal.status_code = CANCELLED
    series.status_code = CANCELLED
If tie:
    keep proposal and series under board review
SaveChanges
Commit
```

### Chapter review

```text
Begin transaction
Load chapter
Validate chapter.status_code = UNDER_REVIEW
Insert ChapterEditorialReview
Update chapter.status_code
SaveChanges
Commit
```

---

## 8. Real File Upload Flow

### 8.1 WPF file picker

WPF should use `OpenFileDialog`.

For proposal file:

```text
Allowed: .pdf, .doc, .docx
```

For cover file:

```text
Allowed: .jpg, .jpeg, .png, .webp
```

For chapter file:

```text
Allowed: .zip, .pdf, .jpg, .jpeg, .png, .webp
```

For editor markup:

```text
Allowed: .pdf, .doc, .docx, .png, .jpg
```

### 8.2 Upload process

```text
User selects local file
→ WPF sends multipart/form-data to API
→ API validates purpose + file type + size
→ Existing upload service uploads to Cloudinary or configured storage
→ API creates FileResource
→ API returns fileResourceId
→ WPF stores that ID in the relevant form ViewModel
→ Submit/update command sends fileResourceId
```

### 8.3 WPF upload client pseudo-code

```csharp
public async Task<FileResourceDto> UploadAsync(string filePath, string purposeCode)
{
    using var form = new MultipartFormDataContent();
    await using var stream = File.OpenRead(filePath);

    var fileContent = new StreamContent(stream);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(filePath));

    form.Add(fileContent, "file", Path.GetFileName(filePath));
    form.Add(new StringContent(purposeCode), "purposeCode");

    var response = await _httpClient.PostAsync("/api/wpf/files/upload", form);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadFromJsonAsync<FileResourceDto>();
}
```

---

## 9. WPF ViewModels

| ViewModel | Responsibility |
|---|---|
| `LoginViewModel` | Login or quick test-user selection through API |
| `MainWindowViewModel` | Role-aware navigation and current session |
| `MangakaSeriesListViewModel` | Load my series, search/filter, create/edit/delete/submit |
| `SeriesEditorViewModel` | Series form, genres/tags, cover upload, proposal file upload |
| `EditorProposalReviewViewModel` | Proposal queue, detail, claim, revision/pass/cancel |
| `BoardPollListViewModel` | Series ready for board, poll list, open poll |
| `BoardPollDetailViewModel` | Vote, vote summary, close/cancel poll |
| `ChapterListViewModel` | Chapters for serialized series, upload chapter file, submit |
| `EditorChapterReviewViewModel` | Review queue, approve/revision/cancel, markup upload |

---

## 10. Revised NuGet Packages

### WPF client

| Package | Purpose |
|---|---|
| `CommunityToolkit.Mvvm` | MVVM source generators |
| `Microsoft.Extensions.DependencyInjection` | DI in WPF |
| `Microsoft.Extensions.Configuration.Json` | `appsettings.json` support |
| `System.Net.Http.Json` | JSON API calls |

### Backend/API

Likely already present:

| Package | Purpose |
|---|---|
| `MediatR` | CQRS commands/queries |
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core SQL Server |
| `CloudinaryDotNet` | File upload |
| `BCrypt.Net-Next` | Password verification if needed |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Existing auth if JWT is reused |

Remove from teammate WPF plan:

```text
Dapper
Microsoft.Data.SqlClient
Direct DB connection factory
Stored-procedure call wrappers in WPF
Dummy FileResource logic
```

---

## 11. Validation Rules

### Series

| Rule | WPF | API/Application | DB |
|---|---:|---:|---:|
| Title required | Yes | Yes | Yes |
| Synopsis required | Yes | Yes | Yes |
| At least one genre | Yes | Yes | Junction data |
| Tags optional | Yes | Yes | Yes |
| Only `PROPOSAL_DRAFT` editable | Yes | Yes | No |
| Slug unique | Pre-check optional | Yes | Unique constraint |
| Proposal file required on submit | Yes | Yes | Proposal FK |
| Actor must be Mangaka contributor | Hide actions | Yes | No |

### Proposal review

| Rule | WPF | API/Application | DB |
|---|---:|---:|---:|
| Only editor can review | Hide actions | Yes | No |
| Revision/cancel requires comments | Yes | Yes | Check constraint if applicable |
| Pass-to-board changes proposal + series | No | Yes | Transaction |

### Board

| Rule | WPF | API/Application | DB |
|---|---:|---:|---:|
| Only Chief opens/closes poll | Hide actions | Yes | No |
| Only board roles vote | Hide actions | Yes | No |
| One open poll per series/type | Disable button | Yes | Filtered unique index |
| Reject vote requires reason | Yes | Yes | Check constraint |
| Close poll applies result atomically | No | Yes | Transaction |

### Chapter

| Rule | WPF | API/Application | DB |
|---|---:|---:|---:|
| Only serialized series allows chapter CRUD | Yes | Yes | No |
| Chapter number unique | Pre-check optional | Yes | Unique constraint |
| Chapter file required before submit | Yes | Yes | FK if required |
| Revision/cancel requires comments | Yes | Yes | Check constraint |

---

## 12. Team Assignment

| Person | Ownership |
|---|---|
| **A** | Backend inspection, WPF project scaffold, API client base, auth/login, shell/navigation, shared models/styles |
| **B** | Series API client, reference data client, series list/editor, genre/tag selector, cover/proposal upload |
| **C** | Editor proposal review API/client/UI, board poll/vote API/client/UI |
| **D** | Chapter API/client/UI, editor chapter review, upload markup/chapter files, integration polish |

---

## 13. Implementation Milestones

### Milestone 0 — Backend inspection and schema alignment

1. Confirm selected schema contains `Series`, `SeriesProposal`, `FileResource`, genres/tags, board tables, chapter tables.
2. Remove `SeriesEditorialReview` from current WPF mini plan.
3. Confirm board poll uses `series_id` only.
4. Identify current endpoints and handlers that can be reused.
5. Decide which WPF-mini endpoints must be added.

Deliverable:

```text
Endpoint reuse/adapt/rewrite table.
```

### Milestone 1 — WPF shell and API client foundation

1. Create WPF project.
2. Add MVVM and DI packages.
3. Create `ApiClientBase`.
4. Implement auth or quick-login API.
5. Implement shell navigation based on role.
6. Implement shared API error handling.

Deliverable:

```text
WPF app can login and call API.
```

### Milestone 2 — file upload + reference data

1. Implement `/api/wpf/files/upload` if existing upload endpoint is not WPF-friendly.
2. Implement `FileUploadApiClient`.
3. Implement file picker service.
4. Load genres/tags through API.
5. Validate real upload to Cloudinary/FileResource.

Deliverable:

```text
WPF can select a local file, upload it, and receive fileResourceId.
```

### Milestone 3 — Series CRUD

1. Implement/get series endpoints.
2. Create/edit form.
3. Save genre/tag selections.
4. Upload cover and attach cover file ID.
5. Show series list with latest proposal status.

Deliverable:

```text
Mangaka can create/edit PROPOSAL_DRAFT series.
```

### Milestone 4 — Proposal submit and editor review

1. Upload proposal file.
2. Submit series to create `SeriesProposal`.
3. Editor proposal queue.
4. Request revision/pass to board/cancel.
5. Show editor feedback to Mangaka.

Deliverable:

```text
Series can move draft → editorial review → draft/board/cancel.
```

### Milestone 5 — Board voting

1. Board Chief opens poll for series.
2. Board members vote.
3. Vote summary display.
4. Board Chief closes poll.
5. API updates latest proposal and series result.

Deliverable:

```text
Board can move series to SERIALIZED or CANCELLED.
```

### Milestone 6 — Chapters

1. Mangaka creates chapters under serialized series.
2. Upload chapter file.
3. Submit chapter for review.
4. Editor approves or requests revision/cancels.
5. Optional markup upload.

Deliverable:

```text
Chapter CRUD and editor review are complete.
```

### Milestone 7 — integration and demo polish

1. Run all demo scenarios.
2. Fix API/client integration errors.
3. Improve status badges and validation messages.
4. Prepare quick-login and seed data.
5. Clean build.

---

## 14. Demo Scenarios

### Scenario 1 — Series approved by board

```text
Mangaka logs in.
Creates Series draft.
Selects genres/tags.
Uploads cover.
Uploads proposal document.
Submits Series.
Editor passes latest proposal to board.
Board Chief opens START_SERIALIZATION poll.
Board members vote APPROVE.
Board Chief closes poll.
Series becomes SERIALIZED.
```

### Scenario 2 — Editor requests revision

```text
Mangaka submits proposal v1.
Editor requests revision with comments.
Proposal v1 becomes REVISION_REQUESTED.
Series returns to PROPOSAL_DRAFT.
Mangaka edits same Series and uploads/resubmits proposal v2.
```

### Scenario 3 — Board rejects

```text
Editor passes proposal to board.
Board Chief opens poll.
Board member votes REJECT with reason.
Board Chief closes poll.
Latest proposal becomes CANCELLED.
Series becomes CANCELLED.
```

### Scenario 4 — Chapter approved

```text
Mangaka selects serialized series.
Creates chapter.
Uploads chapter file.
Submits chapter.
Editor approves chapter.
Chapter becomes APPROVED.
```

### Scenario 5 — Chapter revision

```text
Mangaka submits chapter.
Editor requests revision with comments or markup file.
Chapter becomes REVISION_REQUESTED.
Mangaka edits and resubmits.
Editor approves.
```

---

## 15. Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Existing API endpoints are too web-specific | Add `/api/wpf/*` endpoints but reuse Application/Infrastructure services |
| Existing SP handlers are hard to adapt | Create EF Core-based WPF-mini handlers instead |
| Real upload takes time | Reuse current Cloudinary/FileResource service; keep WPF client upload simple |
| Auth/JWT integration is too much | Use quick-login endpoint for demo but keep role checks in API |
| Board close result logic has edge cases | Keep result rule simple: approve > reject = approved, reject > approve = cancelled, tie = no decision |
| Chapter file purpose does not exist | Add a WPF-compatible file purpose or reuse an existing allowed purpose |
| Deadline pressure | Prioritize series full lifecycle first, then chapters |

---

## 16. Priority Build Order

1. Inspect existing API/CQRS/file services.
2. Add/adjust WPF-mini API endpoints.
3. Build WPF shell + API client base.
4. Implement real file upload.
5. Implement reference data.
6. Implement Series CRUD.
7. Implement proposal submit/review.
8. Implement board vote/close.
9. Implement chapter CRUD/review.
10. Polish demo.

Do **not** start direct DB/Dapper services unless the API approach fails and the deadline forces a fallback.
