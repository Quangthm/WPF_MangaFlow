# WPF Manga Management Mini Project — Implementation Plan

**Target project:** WPF desktop mini-project based on MangaFlow  
**Database:** `WPFMangaManagementDB`  
**Latest direction:** keep `Series` as the user-facing CRUD entity, but restore `SeriesProposal` as the internal submitted proposal/review version table.  
**Core required flows:**

1. **Series CRUD → Editor approve/reject → Editorial Board vote**
2. **Chapter CRUD → Editor approve/reject**

This plan assumes the mini-project should stay close to the existing MangaFlow design while cutting page-level production workflow, assistant task workflow, AI, notification, ranking, and full audit complexity.

---

## 1. Direction Summary

The updated direction is stronger for implementation speed because it matches the old MangaFlow workflow model more closely:

- `Series` remains the main user-facing entity.
- The WPF UI should still feel like **Series CRUD**, not Proposal CRUD.
- `SeriesProposal` is restored as the submitted proposal/version record.
- `SeriesProposal` stores the proposal document, proposal version, editor review result, comments, and markup file.
- `SeriesEditorialReview` is removed because `SeriesProposal` already contains editor review fields.
- `Series.proposal_file_id` is no longer needed because proposal files belong to `SeriesProposal.proposal_file_id`.
- `Series.cover_file_id` remains on `Series` because cover is current series profile metadata.
- Genres/tags stay normalized through `Genre`, `Tag`, `SeriesGenre`, and `SeriesTag`.
- `SeriesBoardPoll` and `SeriesBoardVote` remain tied to `series_id`, not `series_proposal_id`.
- Board voting still uses real votes: `APPROVE`, `REJECT`, and `ABSTAIN`.
- `Chapter` and `ChapterEditorialReview` support chapter CRUD and editor review.
- Audit logs are intentionally excluded to keep the WPF project smaller.

The key interpretation is:

```text
UI concept:
Series CRUD

Database workflow concept:
Series = current editable series profile
SeriesProposal = submitted proposal version used for editor/board review
```

---

## 2. Current Schema Scope

The selected mini-project schema should include:

| Area | Tables / Objects |
|---|---|
| Auth | `auth.Roles`, `auth.Users` |
| Files | `manga.FileResource` |
| Series | `manga.Series`, `manga.SeriesProposal`, `manga.SeriesContributor` |
| Genre/Tag | `manga.Genre`, `manga.SeriesGenre`, `manga.Tag`, `manga.SeriesTag` |
| Chapter | `manga.Chapter`, `manga.ChapterEditorialReview` |
| Board | `manga.SeriesBoardPoll`, `manga.SeriesBoardVote`, `manga.vw_SeriesBoardPollVoteSummary` |

The selected schema should **not** include:

| Removed / excluded object | Reason |
|---|---|
| `manga.SeriesEditorialReview` | Replaced by `SeriesProposal.reviewed_by_user_id`, `reviewed_at_utc`, `comments`, and `markup_file_id` |
| `Series.proposal_file_id` | Proposal file belongs to each submitted `SeriesProposal` version |
| `ChapterPage` | Page-level workflow cut from WPF scope |
| `ChapterPageVersion` | Replaced by `Chapter.chapter_file_id` for the mini-project |
| `PageRegion` | Page annotation/AI workflow cut |
| `ChapterPageAnnotation` | Out of scope |
| `ChapterPageTask` | Assistant task workflow out of scope |
| `audit.AuditEvent` | Optional; intentionally cut for time |
| Notification/ranking/AI tables | Out of scope |

---

## 3. Key Simplification Decisions

### 3.1 `SeriesProposal` is restored

Full MangaFlow uses `SeriesProposal` to preserve proposal submission history. This is useful even in the WPF mini-project because it avoids creating a new custom `SeriesEditorialReview` table.

For WPF:

```text
Series = current editable series profile
SeriesProposal = submitted snapshot/version for review
```

When the Mangaka submits a series, the system creates a new `SeriesProposal` row.

When the editor requests changes, the latest proposal becomes `REVISION_REQUESTED`, while the main series returns to `PROPOSAL_DRAFT` so the Mangaka can edit it.

### 3.2 `Series` does not need `REVISION_REQUESTED`

The project intentionally keeps `Series.status_code` simple.

When the editor requires changes:

```text
SeriesProposal.status_code = REVISION_REQUESTED
Series.status_code = PROPOSAL_DRAFT
```

This means `PROPOSAL_DRAFT` has two meanings in the WPF mini-project:

| Meaning | Context |
|---|---|
| New draft | Series has never been submitted |
| Returned draft | Latest proposal was reviewed and marked `REVISION_REQUESTED` |

That is acceptable because the UI can show the latest proposal review comments beside the editable series form.

### 3.3 Board poll remains tied to `series_id`

`SeriesBoardPoll` should keep:

```text
series_id
```

Do **not** add:

```text
series_proposal_id
```

For the WPF mini-project, this is simpler and consistent with the selected schema.

The backend should always resolve the active/latest proposal when needed:

```sql
SELECT TOP (1)
    sp.*
FROM manga.SeriesProposal sp
WHERE sp.series_id = @series_id
ORDER BY sp.proposal_version_no DESC;
```

When closing a board poll, the backend should update the latest proposal for that series whose status is `UNDER_BOARD_REVIEW`.

### 3.4 Board voting remains real

The mini-project should not use a single “Board Approve” button. It should demonstrate actual board voting:

```text
Board Chief opens poll
Board Members vote APPROVE / REJECT / ABSTAIN
Board Chief closes poll
System computes result from votes
```

The existing `vw_SeriesBoardPollVoteSummary` supports the vote summary and result computation.

### 3.5 Chapter page workflow is cut

The WPF mini-project uses `Chapter.chapter_file_id` as the simplified replacement for the full page/page-version workflow.

Cut from WPF scope:

- `ChapterPage`
- `ChapterPageVersion`
- `PageRegion`
- `ChapterPageAnnotation`
- `ChapterPageTask`
- AI segmentation/OCR
- Assistant page task workflow

---

## 4. Recommended Architecture

Because current MangaFlow already uses Clean Architecture and CQRS, the WPF project should begin by inspecting what can be reused.

### 4.1 Preferred architecture

```text
WPF Views
→ WPF ViewModels
→ Typed API clients
→ Existing or WPF-mini API endpoints
→ MediatR commands/queries
→ Application handlers
→ Infrastructure repositories / stored procedure wrappers
→ SQL Server
```

### 4.2 Why this is preferred

This lets the WPF project reuse the current backend shape instead of rewriting business logic inside the WPF app.

The WPF app should mainly replace the Blazor UI layer:

```text
Old:
Blazor Razor page → typed API client → API → MediatR → DB

New:
WPF ViewModel → typed API client → API → MediatR → DB
```

### 4.3 Fallback architecture if time is limited

If API reuse becomes too time-consuming:

```text
WPF ViewModels
→ WPF Services
→ EF Core / ADO.NET repositories
→ SQL Server
```

This is faster but less aligned with MangaFlow. Use it only if the subject does not require API or if adapting the existing API costs too much time.

---

## 5. Initial Inspection Phase

Before coding WPF screens, inspect the current MangaFlow solution.

### 5.1 Inspect existing backend workflows

Check whether the current API/Application already has usable commands/queries for:

| Workflow | Existing reuse target |
|---|---|
| Create series draft | `CreateSeriesDraftCommand` / handler |
| Update series draft | `UpdateSeriesDraftCommand` / handler |
| Submit series proposal | Existing `SubmitSeriesProposal` flow should now be highly reusable |
| Editor proposal review | Existing editor proposal review flow should be reusable/adaptable |
| Board poll open/close/vote | Reusable conceptually; poll stays tied to `series_id` |
| Genre/tag lookup | Likely reusable |
| FileResource creation | Reusable concept; WPF uses local file metadata instead of real Cloudinary upload if needed |
| Chapter CRUD | May need new simplified commands |
| Chapter editorial review | May be reusable or partially reusable |

### 5.2 Inspect existing DTOs

Look for DTOs that can be reused or adapted:

| Needed DTO | Reuse possibility |
|---|---|
| `SeriesDto` | High |
| `SeriesDetailDto` | High |
| `SeriesProposalDto` | High |
| `GenreDto` | High |
| `TagDto` | High |
| `FileResourceDto` | Medium |
| `BoardPollDto` | Medium/High |
| `BoardVoteDto` | Medium/High |
| `ChapterDto` | Medium |
| `ChapterReviewDto` | Medium |

### 5.3 Inspect assumptions that may conflict with WPF mini schema

The existing MangaFlow system may still assume:

- Full user table fields such as email, display name, account status, portfolio, and avatar.
- Full Cloudinary upload from Blazor/API multipart forms.
- Full proposal workflow with extra fields not present in the WPF mini schema.
- Proposal review screens are tied to web navigation and `returnUrl`.
- Page/version/chapter workspace links exist.
- Authorization uses web authentication/claims instead of WPF session state.

For WPF, these need to be ignored, adapted, or replaced with mini-specific endpoints/handlers.

---

## 6. Backend/API Reuse Strategy

### 6.1 Keep reusable backend parts

Try to keep:

- Role/user lookup logic.
- Password hash verification if login is implemented.
- Genre/tag reference data query.
- FileResource helper/service shape.
- Series create/update validation ideas.
- SeriesProposal submission and review workflow ideas.
- Board poll/vote workflow logic.
- Chapter review decision logic.
- Stored procedure transaction style.

### 6.2 Reuse or adapt `SeriesProposal` commands

Because `SeriesProposal` is restored, the WPF mini-project can reuse more of the old MangaFlow proposal pipeline.

Recommended workflow commands:

```text
CreateSeriesDraftCommand
UpdateSeriesDraftCommand
CancelSeriesDraftCommand
SubmitSeriesProposalCommand
RequestSeriesProposalRevisionCommand
PassSeriesProposalToBoardCommand
CancelSeriesProposalReviewCommand
OpenSeriesBoardPollCommand
CastSeriesBoardVoteCommand
CloseSeriesBoardPollCommand
CreateChapterDraftCommand
UpdateChapterDraftCommand
DeleteChapterDraftCommand
SubmitChapterForReviewCommand
ApproveChapterCommand
ReturnChapterForRevisionCommand
CancelChapterCommand
```

If existing commands are too tied to the full project, create WPF-mini versions under a separate folder/namespace, for example:

```text
Application/Features/WpfMini/Series
Application/Features/WpfMini/Proposals
Application/Features/WpfMini/Board
Application/Features/WpfMini/Chapters
```

### 6.3 Avoid breaking full MangaFlow

If the WPF mini-project lives beside the full MangaFlow solution, do not break the full web app just to support WPF.

Better options:

1. Create a separate mini API route area, for example `/api/wpf/...`.
2. Create WPF-mini handlers that target `WPFMangaManagementDB`.
3. Keep full MangaFlow commands untouched if their schema assumptions differ.
4. Reuse logic patterns, DTO shapes, and validation rules even when direct code reuse is not possible.

---

## 7. WPF Project Structure

Recommended project:

```text
MangaManagementSystem.WpfMini/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── Models/
│   ├── CurrentUserSession.cs
│   ├── LookupModels.cs
│   ├── SeriesModels.cs
│   ├── SeriesProposalModels.cs
│   ├── ChapterModels.cs
│   └── BoardModels.cs
│
├── Services/
│   ├── ApiClientBase.cs
│   ├── AuthApiClient.cs
│   ├── ReferenceDataApiClient.cs
│   ├── SeriesApiClient.cs
│   ├── SeriesProposalApiClient.cs
│   ├── BoardApiClient.cs
│   ├── ChapterApiClient.cs
│   ├── FilePickerService.cs
│   └── DialogService.cs
│
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── LoginViewModel.cs
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

Use `CommunityToolkit.Mvvm` for:

- `[ObservableProperty]`
- `[RelayCommand]`
- `ObservableObject`
- `AsyncRelayCommand`

---

## 8. Workflow 1 — Series CRUD → Editor Review → Board Vote

### 8.1 Mangaka Series List

Purpose:

```text
Show series created/contributed by the logged-in Mangaka.
```

Features:

- List series.
- Search by title.
- Filter by status.
- Create new draft.
- Edit series if `Series.status_code = PROPOSAL_DRAFT`.
- Delete/cancel draft if `Series.status_code = PROPOSAL_DRAFT`.
- Submit for editorial review if valid.
- Show latest proposal version/status if the series has been submitted before.
- Show latest editor feedback when the latest proposal was marked `REVISION_REQUESTED`.

Fields shown:

- Title
- Slug
- Series status
- Latest proposal version number
- Latest proposal status
- Genres
- Tags
- Content language
- Publication frequency
- Cover file name
- Last submitted proposal file name
- Last updated

### 8.2 Create/Edit Series screen

Fields:

| Field | Required | Source |
|---|---:|---|
| Title | Yes | `Series.title` |
| Slug | Auto/generated or editable | `Series.slug` |
| Synopsis | Yes | `Series.synopsis` |
| Genres | Yes | `Genre` lookup |
| Tags | No | `Tag` lookup |
| Content language | Yes | `ja`, `en`, `vi` |
| Publication frequency | Optional | `WEEKLY`, `MONTHLY`, `IRREGULAR` |
| Cover file | Optional | `FileResource`, purpose `SERIES_COVER` |
| Proposal file | Required only when submitting | `SeriesProposal.proposal_file_id`, purpose `SERIES_PROPOSAL` |

Important distinction:

```text
Saving a draft updates Series.
Submitting a draft creates SeriesProposal.
```

The proposal file should not be stored on `Series`.

### 8.3 Submit Series Proposal

Action:

```text
Series.status_code: PROPOSAL_DRAFT → UNDER_EDITORIAL_REVIEW
Create SeriesProposal: UNDER_EDITORIAL_REVIEW
```

Backend should:

1. Validate actor is Mangaka.
2. Validate series belongs to Mangaka through `SeriesContributor`.
3. Validate `Series.status_code = PROPOSAL_DRAFT`.
4. Validate required profile fields.
5. Validate at least one genre.
6. Validate proposal file exists and is active.
7. Calculate next `proposal_version_no` as max version + 1.
8. Insert `SeriesProposal` with:
   - `series_id`
   - `proposal_version_no`
   - `proposal_title = Series.title`
   - `synopsis_snapshot = Series.synopsis`
   - `proposal_file_id`
   - `status_code = UNDER_EDITORIAL_REVIEW`
   - `submitted_by_user_id`
9. Update `Series.status_code = UNDER_EDITORIAL_REVIEW`.

### 8.4 Editor Proposal Review Queue

Query:

```text
SeriesProposal.status_code = UNDER_EDITORIAL_REVIEW
```

Join to `Series` to show current series metadata:

- cover file
- genres/tags
- content language
- publication frequency
- current series status

Editor actions:

| Action | `SeriesProposal` effect | `Series` effect |
|---|---|---|
| Request changes | `status_code = REVISION_REQUESTED`, set reviewer/comments/markup | `status_code = PROPOSAL_DRAFT` |
| Pass to board | `status_code = UNDER_BOARD_REVIEW`, set reviewer/comments/markup | `status_code = UNDER_BOARD_REVIEW` |
| Cancel | `status_code = CANCELLED`, set reviewer/comments/markup | `status_code = CANCELLED` |

Important UI note:

Even though the proposal status is `REVISION_REQUESTED`, the series status returns to `PROPOSAL_DRAFT`. The Mangaka edits the same `Series` row and resubmits, creating a new `SeriesProposal` version.

### 8.5 Board Poll List

Query:

```text
Series.status_code = UNDER_BOARD_REVIEW
OR open polls from SeriesBoardPoll
```

Board Chief can:

- Open `START_SERIALIZATION` poll for a series.
- Enter `poll_reason`.
- Select official publication frequency.
- Close poll.
- Cancel poll.

Board Member/Chief can:

- Vote `APPROVE`.
- Vote `REJECT` with required reason.
- Vote `ABSTAIN`.

Because `SeriesBoardPoll` stores `series_id`, the UI should display the latest `SeriesProposal` for that series as read-only review context.

### 8.6 Close Board Poll

When closing poll:

1. Set `SeriesBoardPoll.poll_status_code = CLOSED`.
2. Read `vw_SeriesBoardPollVoteSummary`.
3. Find latest `SeriesProposal` for the poll's `series_id` with `status_code = UNDER_BOARD_REVIEW`.
4. Apply result:

| Computed result | `SeriesProposal` effect | `Series` effect |
|---|---|---|
| `APPROVED` | `status_code = APPROVED` | `status_code = SERIALIZED` |
| `REJECTED` | `status_code = CANCELLED` | `status_code = CANCELLED` |
| `NO_DECISION` | Keep `UNDER_BOARD_REVIEW` | Keep `UNDER_BOARD_REVIEW` |

For the mini-project demo, board rejection can mean the series is cancelled. Editor revision already covers the “return to draft and edit” path.

---

## 9. Workflow 2 — Chapter CRUD → Editor Review

### 9.1 Mangaka Chapter List

Accessible only for:

```text
Series.status_code = SERIALIZED
```

Features:

- Select serialized series.
- List chapters.
- Create chapter.
- Edit chapter if `DRAFT` or `REVISION_REQUESTED`.
- Delete/cancel chapter if `DRAFT`.
- Attach chapter file.
- Submit chapter for review.

Fields:

- Chapter number label
- Chapter title
- Chapter file
- Planned release date
- Status
- Last updated

### 9.2 Submit Chapter

Action:

```text
DRAFT or REVISION_REQUESTED → UNDER_REVIEW
```

Backend should:

1. Validate actor is Mangaka contributor.
2. Validate series is `SERIALIZED`.
3. Validate chapter file exists.
4. Update `Chapter.status_code = UNDER_REVIEW`.

### 9.3 Editor Chapter Review Queue

Query:

```text
Chapter.status_code = UNDER_REVIEW
```

Actions:

| Action | DB effect |
|---|---|
| Approve | Insert `ChapterEditorialReview` with `APPROVED`; set `Chapter.status_code = APPROVED` |
| Request revision | Insert `ChapterEditorialReview` with `REVISION_REQUESTED`; set `Chapter.status_code = REVISION_REQUESTED` |
| Cancel | Insert `ChapterEditorialReview` with `CANCELLED`; set `Chapter.status_code = CANCELLED` |

For rejected/revision chapters, comments or markup file should be required.

---

## 10. FileResource Handling

The selected schema keeps MangaFlow-style `FileResource` fields:

```text
cloudinary_public_id
cloudinary_secure_url
content_type
file_size_bytes
sha256_hash
```

For the WPF mini-project, use local test values instead of actual Cloudinary upload.

When the user selects a local file:

```text
original_file_name = selected file name
cloudinary_public_id = "local/wpf/{guid}/{fileName}"
cloudinary_secure_url = selected local path or file:// URI
content_type = guessed from extension
file_size_bytes = FileInfo.Length
sha256_hash = calculated SHA-256
```

Recommended file purposes:

| Purpose | Used by |
|---|---|
| `SERIES_COVER` | `Series.cover_file_id` |
| `SERIES_PROPOSAL` | `SeriesProposal.proposal_file_id` |
| `EDITORIAL_ATTACHMENT` | `SeriesProposal.markup_file_id`, `ChapterEditorialReview.markup_file_id` |
| `CHAPTER_PAGE_VERSION` | `Chapter.chapter_file_id` in the mini-project, even though the name comes from full MangaFlow |

If the purpose name feels confusing for chapter package files, either reuse `CHAPTER_PAGE_VERSION` for compatibility or add `CHAPTER_PACKAGE` to the check constraint.

---

## 11. Stored Procedure / Command Plan

### 11.1 Series + proposal commands

| Command | Purpose |
|---|---|
| `CreateSeriesDraft` | Insert `Series`, `SeriesGenre`, `SeriesTag`, creator `SeriesContributor` |
| `UpdateSeriesDraft` | Update profile and replace genre/tag links while `Series.status_code = PROPOSAL_DRAFT` |
| `DeleteSeriesDraft` / `CancelSeriesDraft` | Remove or cancel draft |
| `SubmitSeriesProposal` | Create new `SeriesProposal` version and move Series to `UNDER_EDITORIAL_REVIEW` |
| `RequestSeriesProposalRevision` | Editor requests changes: proposal `REVISION_REQUESTED`, series `PROPOSAL_DRAFT` |
| `PassSeriesProposalToBoard` | Proposal `UNDER_BOARD_REVIEW`, series `UNDER_BOARD_REVIEW` |
| `CancelSeriesProposalReview` | Proposal `CANCELLED`, series `CANCELLED` |

### 11.2 Board commands

| Command | Purpose |
|---|---|
| `OpenSeriesBoardPoll` | Create `SeriesBoardPoll` for `UNDER_BOARD_REVIEW` series |
| `CastSeriesBoardVote` | Insert/update one vote per user per poll |
| `CancelSeriesBoardPoll` | `OPEN → CANCELLED` |
| `CloseSeriesBoardPoll` | `OPEN → CLOSED`, compute result, update latest proposal and series |

### 11.3 Chapter commands

| Command | Purpose |
|---|---|
| `CreateChapterDraft` | Insert chapter under serialized series |
| `UpdateChapterDraft` | Update draft/revision chapter |
| `DeleteChapterDraft` | Delete or cancel draft chapter |
| `SubmitChapterForReview` | `DRAFT/REVISION_REQUESTED → UNDER_REVIEW` |
| `ApproveChapter` | Insert review, `UNDER_REVIEW → APPROVED` |
| `ReturnChapterForRevision` | Insert review, `UNDER_REVIEW → REVISION_REQUESTED` |
| `CancelChapterReview` | Insert review, `UNDER_REVIEW → CANCELLED` |

### 11.4 Reference data queries

| Query | Purpose |
|---|---|
| `GetGenres` | Populate genre multi-select |
| `GetTags` | Populate tag multi-select |
| `GetCurrentUser` | Load role/session |
| `GetSeriesListForMangaka` | My series + latest proposal summary |
| `GetSeriesDetail` | Series profile + genres/tags + latest proposal |
| `GetProposalReviewQueue` | Editor queue |
| `GetBoardPolls` | Board poll list + vote summary |
| `GetChaptersForSeries` | Chapter CRUD |
| `GetChapterReviewQueue` | Editor chapter queue |

---

## 12. Suggested API Endpoints

If reusing API/CQRS, expose WPF-friendly endpoints.

### Auth

```text
POST /api/wpf/auth/login
GET  /api/wpf/auth/test-users
```

### Reference data

```text
GET /api/wpf/reference/genres
GET /api/wpf/reference/tags
```

### Series

```text
GET    /api/wpf/series/my
GET    /api/wpf/series/{seriesId}
POST   /api/wpf/series
PUT    /api/wpf/series/{seriesId}
DELETE /api/wpf/series/{seriesId}
POST   /api/wpf/series/{seriesId}/submit-proposal
```

### Editor proposal review

```text
GET  /api/wpf/editor/proposal-review-queue
GET  /api/wpf/editor/proposals/{proposalId}
POST /api/wpf/editor/proposals/{proposalId}/request-revision
POST /api/wpf/editor/proposals/{proposalId}/pass-to-board
POST /api/wpf/editor/proposals/{proposalId}/cancel
```

### Board

```text
GET  /api/wpf/board/polls
POST /api/wpf/board/series/{seriesId}/polls
GET  /api/wpf/board/polls/{pollId}
POST /api/wpf/board/polls/{pollId}/votes
POST /api/wpf/board/polls/{pollId}/close
POST /api/wpf/board/polls/{pollId}/cancel
```

### Chapters

```text
GET    /api/wpf/series/{seriesId}/chapters
POST   /api/wpf/series/{seriesId}/chapters
PUT    /api/wpf/chapters/{chapterId}
DELETE /api/wpf/chapters/{chapterId}
POST   /api/wpf/chapters/{chapterId}/submit
```

### Editor chapter review

```text
GET  /api/wpf/editor/chapter-review-queue
POST /api/wpf/editor/chapters/{chapterId}/approve
POST /api/wpf/editor/chapters/{chapterId}/return
POST /api/wpf/editor/chapters/{chapterId}/cancel
```

---

## 13. WPF ViewModels

| ViewModel | Responsibility |
|---|---|
| `LoginViewModel` | Login or quick test-user selection |
| `MainWindowViewModel` | Role-aware navigation, current page, logout |
| `MangakaSeriesListViewModel` | Load current user's series, search/filter, create/edit/delete/submit |
| `SeriesEditorViewModel` | Create/edit form, genres/tags, cover file selection, proposal file selection for submit |
| `EditorProposalReviewViewModel` | Proposal review queue, request revision/pass/cancel actions |
| `BoardPollListViewModel` | Poll list, vote counts, filters, open poll |
| `BoardPollDetailViewModel` | Cast vote, close/cancel poll, vote summary, latest proposal display |
| `ChapterListViewModel` | Chapter CRUD, file selection, submit chapter |
| `EditorChapterReviewViewModel` | Chapter review queue, approve/return/cancel |

---

## 14. UI Screen Plan

### 14.1 Login View

- Username textbox.
- Password box.
- Login button.
- Quick login combo box for demo.
- Error text.

### 14.2 Shell View

- Left sidebar navigation.
- Top user/role display.
- Main content area.
- Status bar.

### 14.3 Mangaka Series View

- Search box.
- Status filter.
- Series table/cards.
- Create button.
- Edit button.
- Submit Proposal button.
- Latest proposal status/version badge.
- Latest editor feedback display when returned.
- Manage Chapters button for serialized series.
- Status badges.

### 14.4 Series Editor View

- Title textbox.
- Slug textbox or auto slug preview.
- Synopsis textbox.
- Genre multi-select.
- Tag multi-select.
- Language combo box.
- Publication frequency combo box.
- Cover file picker.
- Proposal file picker for submit action.
- Save Draft button.
- Submit Proposal button.

### 14.5 Editor Proposal Review View

- Proposal queue table.
- Detail panel.
- Proposal version number.
- Proposal title and synopsis snapshot.
- Current series genres/tags display.
- Cover/proposal file metadata.
- Comments textbox.
- Optional markup file picker.
- Request Changes button.
- Pass to Board button.
- Cancel button.

### 14.6 Board Poll View

- Poll list.
- Poll detail.
- Series information.
- Latest proposal information.
- Vote counts.
- Vote choice radio buttons.
- Reject reason textbox.
- Cast vote button.
- Open poll button for Board Chief.
- Close poll button for Board Chief.

### 14.7 Chapter Management View

- Serialized series selector.
- Chapter table.
- Add/Edit chapter form.
- Chapter file picker.
- Submit chapter button.
- Status badge.

### 14.8 Editor Chapter Review View

- Chapter review queue.
- Chapter detail panel.
- Chapter file metadata.
- Comments textbox.
- Markup file picker.
- Approve button.
- Return for Revision button.
- Cancel button.

---

## 15. Validation Rules

### 15.1 Series validation

| Rule | Where |
|---|---|
| Title required | WPF + backend |
| Synopsis required | WPF + backend |
| At least one genre required | WPF + backend |
| Tags optional | WPF + backend |
| Slug required/unique | Backend |
| Cover file optional | WPF + backend |
| Only `PROPOSAL_DRAFT` series can be edited | WPF + backend |
| Only Mangaka contributor can edit/submit | Backend |

### 15.2 Proposal validation

| Rule | Where |
|---|---|
| Proposal file required before submit | WPF + backend |
| Proposal file must exist in `FileResource` | Backend |
| Proposal file purpose should be `SERIES_PROPOSAL` | Backend |
| New proposal version number must be previous max + 1 | Backend |
| Only latest proposal should be reviewed | Backend |
| Request revision requires comments or markup | DB + WPF |
| Pass to board can allow optional comments | DB + WPF |

### 15.3 Board validation

| Rule | Where |
|---|---|
| Only Board Chief can open/close poll | Backend |
| Only Board roles can vote | Backend |
| Only one open poll per series/type | DB unique index |
| `START_SERIALIZATION` requires publication frequency | DB + WPF |
| Reject vote requires reason | DB + WPF |
| One vote per user per poll | DB unique constraint |
| Closed/cancelled poll cannot accept votes | Backend |
| Poll remains tied to `series_id` | DB + backend |

### 15.4 Chapter validation

| Rule | Where |
|---|---|
| Chapter number unique per series | DB + WPF |
| Chapter belongs to serialized series | Backend |
| Chapter file required before submit | WPF + backend |
| Only Mangaka contributor can create/edit/submit | Backend |
| Only Editor can review | Backend |
| Revision/cancel review requires comments or markup | DB + WPF |

---

## 16. Implementation Milestones

### Milestone 0 — Database and seed setup

1. Run `WPFMangaManagementDB` schema script.
2. Ensure `SeriesProposal` is included.
3. Ensure `SeriesEditorialReview` is removed.
4. Ensure `Series` does not contain `proposal_file_id`.
5. Verify `SeriesBoardPoll` keeps `series_id` only.
6. Seed roles.
7. Seed test users.
8. Seed genres and tags.
9. Create starter mock data.
10. Verify `vw_SeriesBoardPollVoteSummary`.

Deliverable:

```text
Database runs cleanly and contains test users + lookup data + final proposal workflow schema.
```

### Milestone 1 — Inspect existing MangaFlow backend

1. Open current MangaFlow solution.
2. List reusable commands/queries around `Series` and `SeriesProposal`.
3. Identify full-web assumptions that do not fit the WPF mini schema.
4. Decide API reuse vs direct DB fallback.
5. Create endpoint/command mapping table.

Deliverable:

```text
Reuse decision document: keep / adapt / rewrite.
```

### Milestone 2 — WPF shell + login

1. Create WPF project.
2. Add `CommunityToolkit.Mvvm`.
3. Add base services and DI.
4. Implement login or quick test-user selection.
5. Implement shell navigation based on role.

Deliverable:

```text
User can login and see role-specific navigation.
```

### Milestone 3 — Reference data + file handling

1. Load genres.
2. Load tags.
3. Implement genre/tag multi-select model.
4. Implement local file picker.
5. Calculate SHA-256.
6. Save `FileResource` metadata through API/service.

Deliverable:

```text
WPF can select files and load lookup data.
```

### Milestone 4 — Mangaka Series CRUD

1. Series list.
2. Create draft.
3. Edit draft.
4. Save genres/tags.
5. Attach cover file.
6. Select proposal file for submission.
7. Submit proposal, creating `SeriesProposal` version 1.

Deliverable:

```text
Series can move PROPOSAL_DRAFT → UNDER_EDITORIAL_REVIEW and create SeriesProposal.
```

### Milestone 5 — Editor Proposal Review

1. Proposal review queue.
2. Proposal detail panel.
3. Request revision with comments/markup.
4. Pass to board.
5. Cancel.

Deliverable:

```text
Editor can review SeriesProposal and move Series back to PROPOSAL_DRAFT or forward to UNDER_BOARD_REVIEW.
```

### Milestone 6 — Board voting

1. Board poll list.
2. Open poll using `series_id`.
3. Display latest proposal context for the selected series.
4. Cast vote.
5. Show vote summary.
6. Close poll and apply result to latest proposal + series.

Deliverable:

```text
Board can move series UNDER_BOARD_REVIEW → SERIALIZED or CANCELLED.
```

### Milestone 7 — Chapter CRUD

1. Serialized series selector.
2. Chapter list.
3. Create chapter.
4. Edit chapter.
5. Attach chapter file.
6. Submit chapter for review.

Deliverable:

```text
Chapter can move DRAFT → UNDER_REVIEW.
```

### Milestone 8 — Editor Chapter Review

1. Chapter review queue.
2. Approve chapter.
3. Return chapter for revision.
4. Cancel chapter.
5. Store `ChapterEditorialReview`.

Deliverable:

```text
Editor can approve or reject/request revision for chapters.
```

### Milestone 9 — Demo polish

1. Status badges.
2. Search/filter.
3. Error messages.
4. Confirmation dialogs.
5. Demo seed data.
6. Clean build.
7. Prepare demo script.

Deliverable:

```text
Project is demo-ready.
```

---

## 17. Team Assignment Suggestion

| Person | Ownership |
|---|---|
| Person A | Database setup, seed scripts, API/Application reuse inspection, backend command mapping |
| Person B | Mangaka series CRUD, genre/tag selector, file picker, proposal submission, chapter CRUD |
| Person C | Editor proposal review, editor chapter review, comments/markup flow |
| Person D | Board voting, poll summary, close poll logic, UI styling and demo polish |

---

## 18. Demo Scenarios

### Scenario 1 — Series approved by board

```text
Login as TestMangaka1.
Create a series draft.
Select Action/Fantasy.
Select tags.
Attach cover file.
Select proposal file.
Submit proposal.
System creates SeriesProposal v1 UNDER_EDITORIAL_REVIEW.
Series becomes UNDER_EDITORIAL_REVIEW.

Login as TestEditor1.
Open proposal review queue.
Pass proposal to board.
Proposal becomes UNDER_BOARD_REVIEW.
Series becomes UNDER_BOARD_REVIEW.

Login as TestBoardChief1.
Open START_SERIALIZATION poll for the series with WEEKLY frequency.

Login as TestBoardMember1.
Vote APPROVE.

Login as TestBoardMember2.
Vote APPROVE.

Login as TestBoardChief1.
Close poll.
Latest proposal becomes APPROVED.
Series becomes SERIALIZED.
```

### Scenario 2 — Editor returns proposal for revision

```text
Mangaka submits proposal v1.
Editor opens proposal review queue.
Editor enters comments and requests changes.
Proposal v1 becomes REVISION_REQUESTED.
Series.status_code becomes PROPOSAL_DRAFT.
Mangaka edits the same Series.
Mangaka submits again.
System creates SeriesProposal v2 UNDER_EDITORIAL_REVIEW.
```

### Scenario 3 — Board rejects series

```text
Editor passes latest proposal to board.
Board Chief opens poll for the series.
Board members vote REJECT with reasons.
Board Chief closes poll.
Latest proposal becomes CANCELLED.
Series becomes CANCELLED.
```

### Scenario 4 — Chapter approved

```text
Mangaka selects serialized series.
Creates chapter.
Attaches chapter file.
Submits for review.
Editor approves.
Chapter.status_code becomes APPROVED.
```

### Scenario 5 — Chapter revision

```text
Mangaka submits chapter.
Editor enters comments and requests revision.
Chapter.status_code becomes REVISION_REQUESTED.
Mangaka edits chapter and resubmits.
Editor approves.
```

---

## 19. Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Existing API uses full user/account schema | Create WPF-mini auth/session endpoints or use quick login |
| Existing proposal workflow expects extra full-MangaFlow columns | Adapt handlers or create WPF-mini proposal commands |
| File upload is too complex | Store local file path in Cloudinary fields for demo |
| Board poll lacks `series_proposal_id` | Always resolve latest `UNDER_BOARD_REVIEW` proposal by `series_id` |
| Board voting takes too long | Implement minimal poll open/vote/close only |
| Genre/tag multi-select is annoying in WPF | Use checklist `ListBox` with selected items tracked in ViewModel |
| Login/password is time-consuming | Add quick-login mode for test users |
| Too many statuses confuse demo | Show only statuses used in current workflow |
| Direct reuse breaks full MangaFlow | Use separate `WPFMangaManagementDB` and WPF-specific API route area |

---

## 20. Priority Build Order

Build in this order:

1. Database + seed data.
2. Login/quick user switch.
3. Reference data lookup.
4. FileResource local file handling.
5. Series CRUD.
6. Proposal submission through `SeriesProposal`.
7. Editor proposal review.
8. Board poll/vote/close using `series_id`.
9. Chapter CRUD.
10. Chapter review.
11. Polish and demo.

Do not start chapter UI before the series workflow reaches `SERIALIZED`.

---

## 21. Final Recommendation

Start by inspecting the current MangaFlow API/Application layer because the restored `SeriesProposal` direction gives you more reuse opportunities.

The best implementation strategy is:

```text
Keep WPF UI centered on Series CRUD.
Use SeriesProposal internally for submitted versions and editor review.
Keep BoardPoll tied to series_id only.
Resolve the latest active proposal when board actions need proposal context.
Reuse existing CQRS/API patterns where possible.
Create WPF-mini commands only where the full workflow is too heavy.
```

This gives the team a project that is:

- close to MangaFlow,
- faster to implement than the previous Series-only design,
- still understandable as Series CRUD,
- strong enough to demonstrate real editor review and board voting,
- not overloaded with page-level production features.
