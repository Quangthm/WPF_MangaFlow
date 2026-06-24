# WPF Manga Management Mini Project — Implementation Plan

**Target project:** WPF desktop mini-project based on MangaFlow  
**Database:** `WPFMangaManagementDB`  
**Core required flows:**

1. **Series CRUD → Editor approve/reject → Editorial Board vote**
2. **Chapter CRUD → Editor approve/reject**

This plan assumes the mini-project should stay close to the existing MangaFlow design while cutting page-level, task, AI, notification, ranking, and full audit complexity.

---

## 1. Direction Summary

Your chosen direction is strong because it keeps the important MangaFlow concepts:

- `Series` remains the main workflow entity.
- `SeriesProposal` is cut for the WPF mini-project.
- `Series.proposal_file_id` stores the current submitted proposal document.
- `FileResource` remains the file metadata abstraction.
- Genres/tags stay normalized through `Genre`, `Tag`, `SeriesGenre`, and `SeriesTag`.
- `SeriesEditorialReview` stores editor actions for the series workflow.
- `SeriesBoardPoll` + `SeriesBoardVote` preserve actual board voting.
- `Chapter` + `ChapterEditorialReview` support chapter CRUD and editor review.
- Audit logs are intentionally excluded to keep the WPF project smaller.

This is a good balance between reuse and scope control.

---

## 2. Current Schema Scope

The selected mini-project schema includes:

| Area | Tables / Objects |
|---|---|
| Auth | `auth.Roles`, `auth.Users` |
| Files | `manga.FileResource` |
| Series | `manga.Series`, `manga.SeriesContributor`, `manga.SeriesEditorialReview` |
| Genre/Tag | `manga.Genre`, `manga.SeriesGenre`, `manga.Tag`, `manga.SeriesTag` |
| Chapter | `manga.Chapter`, `manga.ChapterEditorialReview` |
| Board | `manga.SeriesBoardPoll`, `manga.SeriesBoardVote`, `manga.vw_SeriesBoardPollVoteSummary` |

The schema already includes `proposal_file_id` on `manga.Series`, which is important because `SeriesProposal` is removed but proposal documents still need to be linked through `FileResource`.

---

## 3. Key Simplification Decisions

### 3.1 `SeriesProposal` is removed

Full MangaFlow uses `SeriesProposal` to preserve proposal version history. The WPF mini-project does not need that history.

For WPF:

```text
Series = current editable series profile
       + current proposal document
       + current workflow status
```

When the editor requests changes:

```text
UNDER_EDITORIAL_REVIEW → PROPOSAL_DRAFT
```

The Mangaka edits the same `Series` row and resubmits.

### 3.2 No `REVISION_REQUESTED` status for Series

The project intentionally does not add `REVISION_REQUESTED` to `Series.status_code`.

Instead:

| Editor action | New status |
|---|---|
| Requires changes | `PROPOSAL_DRAFT` |
| Passes to board | `UNDER_BOARD_REVIEW` |
| Cancels/rejects hard | `CANCELLED` |

Editor comments are stored in `manga.SeriesEditorialReview`.

### 3.3 Board voting remains real

The mini-project should not use one simple “Board Approve” button only. It should show real board voting:

```text
Board Chief opens poll
Board Members vote APPROVE / REJECT / ABSTAIN
Board Chief closes poll
System computes result from votes
```

The existing `vw_SeriesBoardPollVoteSummary` supports this well.

### 3.4 Chapter page workflow is cut

The WPF mini-project uses `Chapter.chapter_file_id` as the simplified replacement for full page/page-version workflow.

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
→ API client / Application service adapter
→ Existing API endpoints or new WPF mini API endpoints
→ MediatR commands/queries
→ Application handlers
→ Infrastructure repositories / stored procedure wrappers
→ SQL Server
```

### 4.2 Why this is preferred

This lets the WPF project reuse the current backend shape instead of rewriting business logic in the WPF app.

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

This is faster but less aligned with current MangaFlow. Use it only if the subject does not require API or the existing API is too tied to full web workflows.

---

## 5. Initial Inspection Phase

Before coding WPF screens, inspect the current MangaFlow solution.

### 5.1 Inspect existing backend workflows

Check whether the current API/Application already has usable commands/queries for:

| Workflow | Existing reuse target |
|---|---|
| Create series draft | `CreateSeriesDraftCommand` / handler |
| Update series draft | `UpdateSeriesDraftCommand` / handler |
| Submit series proposal/review | Current proposal submit flow may need adaptation because WPF cuts `SeriesProposal` |
| Editor proposal review | Current editor review may need adaptation to operate on `Series` directly |
| Board poll open/close/vote | Likely reusable conceptually |
| Genre/tag lookup | Likely reusable |
| File upload / FileResource creation | Reusable concept, but WPF file selection may need adapter |
| Chapter CRUD | May need new simplified commands |
| Chapter editorial review | May be reusable or partially reusable |

### 5.2 Inspect existing DTOs

Look for DTOs that can be reused or adapted:

| Needed DTO | Reuse possibility |
|---|---|
| `SeriesDto` | High |
| `SeriesDetailDto` | High |
| `GenreDto` | High |
| `TagDto` | High |
| `FileResourceDto` | Medium |
| `BoardPollDto` | Medium/High |
| `BoardVoteDto` | Medium/High |
| `ChapterDto` | Medium |
| `ChapterReviewDto` | Medium |

### 5.3 Inspect current assumptions that conflict with WPF mini schema

The existing MangaFlow system may still assume:

- `SeriesProposal` exists.
- Proposal review works through `series_proposal_id`.
- Proposal status is separate from series status.
- Cloudinary upload is done through Web/API form upload.
- Blazor-specific `returnUrl` navigation exists.
- Page/version/chapter workspace links exist.

For WPF, these need to be ignored, adapted, or replaced.

---

## 6. Backend/API Reuse Strategy

### 6.1 Keep reusable backend parts

Try to keep:

- Auth role/user lookup logic.
- Password hash verification if login is implemented.
- Genre/tag reference data query.
- FileResource helper/service shape.
- Series create/update command validation ideas.
- Board poll/vote workflow logic.
- Chapter review decision logic.
- Stored procedure transaction style.

### 6.2 Add WPF-mini-specific workflows

Because `SeriesProposal` is removed, create mini-specific commands/endpoints instead of forcing the full MangaFlow proposal workflow.

Recommended command names:

```text
CreateWpfSeriesDraftCommand
UpdateWpfSeriesDraftCommand
SubmitWpfSeriesForEditorialReviewCommand
ReturnWpfSeriesToDraftCommand
PassWpfSeriesToBoardCommand
CancelWpfSeriesCommand
OpenWpfSeriesBoardPollCommand
CastWpfSeriesBoardVoteCommand
CloseWpfSeriesBoardPollCommand
CreateWpfChapterCommand
UpdateWpfChapterCommand
DeleteWpfChapterCommand
SubmitWpfChapterForReviewCommand
ApproveWpfChapterCommand
ReturnWpfChapterForRevisionCommand
CancelWpfChapterCommand
```

The `Wpf` prefix is optional. If this is a separate mini API/project, use simpler names.

### 6.3 Avoid changing full MangaFlow if possible

If the WPF mini-project lives beside the full MangaFlow solution, do not break the full web app just to support WPF.

Better options:

1. Create a separate mini API area, for example `/api/wpf/series`.
2. Create separate handlers under `Application/Features/WpfMini`.
3. Use the separate `WPFMangaManagementDB`.
4. Keep full MangaFlow commands untouched.

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
│   ├── ChapterModels.cs
│   └── BoardModels.cs
│
├── Services/
│   ├── ApiClientBase.cs
│   ├── AuthApiClient.cs
│   ├── ReferenceDataApiClient.cs
│   ├── SeriesApiClient.cs
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
│   ├── EditorSeriesReviewViewModel.cs
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
│   ├── EditorSeriesReviewView.xaml
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
- Edit series if `PROPOSAL_DRAFT`.
- Delete/cancel draft if `PROPOSAL_DRAFT`.
- Submit for editorial review if valid.

Fields shown:

- Title
- Slug
- Status
- Genres
- Tags
- Content language
- Publication frequency
- Cover file name
- Proposal file name
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
| Proposal file | Required before submit | `FileResource`, purpose `SERIES_PROPOSAL` |

Recommended validation:

- Title not blank.
- Synopsis not blank.
- At least one genre selected.
- Slug unique.
- Proposal file required when submitting.
- Cover file must be image type if selected.
- Proposal file must be `.pdf`, `.doc`, or `.docx`.

### 8.3 Submit Series

Action:

```text
PROPOSAL_DRAFT → UNDER_EDITORIAL_REVIEW
```

Backend should:

1. Validate actor is Mangaka.
2. Validate series belongs to Mangaka through `SeriesContributor`.
3. Validate required profile fields.
4. Validate at least one genre.
5. Validate `proposal_file_id` exists and is active.
6. Update `Series.status_code`.

### 8.4 Editor Series Review Queue

Query:

```text
Series.status_code = UNDER_EDITORIAL_REVIEW
```

Editor actions:

| Action | DB effect |
|---|---|
| Return for changes | Insert `SeriesEditorialReview` with `REVISION_REQUESTED`; set `Series.status_code = PROPOSAL_DRAFT` |
| Pass to board | Insert `SeriesEditorialReview` with `PASSED_TO_BOARD`; set `Series.status_code = UNDER_BOARD_REVIEW` |
| Cancel | Insert `SeriesEditorialReview` with `CANCELLED`; set `Series.status_code = CANCELLED` |

Important UI note:

Even though the review decision code is `REVISION_REQUESTED`, the `Series.status_code` returns to `PROPOSAL_DRAFT`. This keeps the status model simple while still storing review intent.

### 8.5 Board Poll List

Query:

```text
Series.status_code = UNDER_BOARD_REVIEW
OR open polls from SeriesBoardPoll
```

Board Chief can:

- Open `START_SERIALIZATION` poll.
- Enter `poll_reason`.
- Select official publication frequency.
- Close poll.
- Cancel poll.

Board Member/Chief can:

- Vote `APPROVE`.
- Vote `REJECT` with required reason.
- Vote `ABSTAIN`.

### 8.6 Close Board Poll

When closing poll:

1. Set `poll_status_code = CLOSED`.
2. Read `vw_SeriesBoardPollVoteSummary`.
3. Apply result:
   - `APPROVED` → `Series.status_code = SERIALIZED`
   - `REJECTED` → `Series.status_code = CANCELLED`
   - `NO_DECISION` → keep `UNDER_BOARD_REVIEW`

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
4. Update `Chapter.status_code`.

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

For WPF mini-project, use local test values instead of actual Cloudinary upload.

When the user selects a local file:

```text
original_file_name = selected file name
cloudinary_public_id = "local/wpf/{guid}/{fileName}"
cloudinary_secure_url = selected local path or file:// URI
content_type = guessed from extension
file_size_bytes = FileInfo.Length
sha256_hash = calculated SHA-256
```

This keeps the schema compatible without requiring Cloudinary setup.

---

## 11. Stored Procedure / Command Plan

### 11.1 Series commands

| Command | Purpose |
|---|---|
| `CreateSeriesDraft` | Insert `Series`, `SeriesGenre`, `SeriesTag`, creator `SeriesContributor` |
| `UpdateSeriesDraft` | Update profile and replace genre/tag links while `PROPOSAL_DRAFT` |
| `DeleteSeriesDraft` / `CancelSeriesDraft` | Remove or cancel draft |
| `SubmitSeriesForEditorialReview` | `PROPOSAL_DRAFT → UNDER_EDITORIAL_REVIEW` |
| `ReturnSeriesToDraft` | Editor requires changes: insert review, `UNDER_EDITORIAL_REVIEW → PROPOSAL_DRAFT` |
| `PassSeriesToBoard` | Insert review, `UNDER_EDITORIAL_REVIEW → UNDER_BOARD_REVIEW` |
| `CancelSeriesFromEditorialReview` | Insert review, `UNDER_EDITORIAL_REVIEW → CANCELLED` |

### 11.2 Board commands

| Command | Purpose |
|---|---|
| `OpenSeriesBoardPoll` | Create `SeriesBoardPoll` for `UNDER_BOARD_REVIEW` series |
| `CastSeriesBoardVote` | Insert/update one vote per user per poll |
| `CancelSeriesBoardPoll` | `OPEN → CANCELLED` |
| `CloseSeriesBoardPoll` | `OPEN → CLOSED`, compute result, update Series |

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
| `GetSeriesListForMangaka` | My series |
| `GetSeriesEditorialQueue` | Editor queue |
| `GetBoardPolls` | Board poll list |
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
POST   /api/wpf/series/{seriesId}/submit
```

### Editor series review

```text
GET  /api/wpf/editor/series-review-queue
POST /api/wpf/editor/series/{seriesId}/return-to-draft
POST /api/wpf/editor/series/{seriesId}/pass-to-board
POST /api/wpf/editor/series/{seriesId}/cancel
```

### Board

```text
GET  /api/wpf/board/polls
POST /api/wpf/board/series/{seriesId}/polls
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
| `SeriesEditorViewModel` | Create/edit form, genres/tags, file selection, save/submit |
| `EditorSeriesReviewViewModel` | Editorial review queue, return/pass/cancel actions |
| `BoardPollListViewModel` | Poll list, vote counts, filters, open poll |
| `BoardPollDetailViewModel` | Cast vote, close/cancel poll, vote summary |
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
- Submit button.
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
- Proposal file picker.
- Save button.
- Submit button.

### 14.5 Editor Series Review View

- Review queue table.
- Detail panel.
- Genres/tags display.
- Cover/proposal file metadata.
- Comments textbox.
- Return to Draft button.
- Pass to Board button.
- Cancel button.

### 14.6 Board Poll View

- Poll list.
- Poll detail.
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
| Proposal file required before submit | WPF + backend |
| Only `PROPOSAL_DRAFT` series can be edited | WPF + backend |
| Only Mangaka contributor can edit/submit | Backend |
| Only Editor can review | Backend |
| Only Board Chief can open/close poll | Backend |
| Only Board roles can vote | Backend |

### 15.2 Board validation

| Rule | Where |
|---|---|
| Only one open poll per series/type | DB unique index |
| `START_SERIALIZATION` requires publication frequency | DB + WPF |
| Reject vote requires reason | DB + WPF |
| One vote per user per poll | DB unique constraint |
| Closed/cancelled poll cannot accept votes | Backend |

### 15.3 Chapter validation

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
2. Fix any schema syntax issues.
3. Seed roles.
4. Seed test users.
5. Seed genres and tags.
6. Create starter mock data.
7. Verify `vw_SeriesBoardPollVoteSummary`.

Deliverable:

```text
Database runs cleanly and contains test users + lookup data.
```

### Milestone 1 — Inspect existing MangaFlow backend

1. Open current MangaFlow solution.
2. List reusable commands/queries.
3. Identify proposal-dependent code that cannot be reused directly.
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
5. Attach cover and proposal file.
6. Submit to editorial review.

Deliverable:

```text
Series can move PROPOSAL_DRAFT → UNDER_EDITORIAL_REVIEW.
```

### Milestone 5 — Editor Series Review

1. Editor queue.
2. Detail panel.
3. Return to draft with comments.
4. Pass to board.
5. Cancel.

Deliverable:

```text
Editor can move series back to PROPOSAL_DRAFT or forward to UNDER_BOARD_REVIEW.
```

### Milestone 6 — Board voting

1. Board poll list.
2. Open poll.
3. Cast vote.
4. Show vote summary.
5. Close poll and apply result.

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
| Person B | Mangaka series CRUD, genre/tag selector, file picker, chapter CRUD |
| Person C | Editor series review, editor chapter review, comments/markup flow |
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
Attach proposal file.
Submit for editorial review.

Login as TestEditor1.
Open series review queue.
Pass series to board.

Login as TestBoardChief1.
Open START_SERIALIZATION poll with WEEKLY frequency.

Login as TestBoardMember1.
Vote APPROVE.

Login as TestBoardMember2.
Vote APPROVE.

Login as TestBoardChief1.
Close poll.
Series becomes SERIALIZED.
```

### Scenario 2 — Editor returns series to draft

```text
Mangaka submits series.
Editor opens review queue.
Editor enters comments and returns it.
Series.status_code becomes PROPOSAL_DRAFT.
Mangaka edits same Series.
Mangaka resubmits.
```

### Scenario 3 — Board rejects series

```text
Editor passes series to board.
Board Chief opens poll.
Board members vote REJECT with reasons.
Board Chief closes poll.
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
| Existing API is too tied to `SeriesProposal` | Create WPF mini endpoints instead of modifying full workflow |
| File upload is too complex | Store local file path in Cloudinary fields for demo |
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
6. Series submit.
7. Editor series review.
8. Board poll/vote/close.
9. Chapter CRUD.
10. Chapter review.
11. Polish and demo.

Do not start chapter UI before the series workflow reaches `SERIALIZED`.

---

## 21. Final Recommendation

Start by inspecting the current MangaFlow API/Application layer, but do not force reuse where the full workflow depends on `SeriesProposal`.

The best implementation strategy is:

```text
Reuse architecture ideas and simple lookup/read models.
Adapt or create mini-specific commands for workflows that changed.
Keep WPF as a thin UI layer with ViewModels calling API clients.
Use the selected WPFMangaManagementDB schema as the mini-project database.
```

This gives the team a project that is:

- close to MangaFlow,
- realistic for a WPF subject,
- easier than full web workflow,
- strong enough to demonstrate real approval/voting flows,
- not overloaded with page-level production features.
