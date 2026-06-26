# WPF Manga Management — Revised 7-Day Schedule for 4 People

> **Stack:** .NET 8 WPF, CommunityToolkit.Mvvm, typed API clients, existing ASP.NET Core API, MediatR/CQRS, EF Core, SQL Server, real file upload.  
> **Removed from teammate plan:** Dapper, direct DB access from WPF, stored procedure wrappers in WPF, dummy `FileResource` rows.  
> **Core direction:** WPF replaces the Blazor UI layer and calls the API. Backend WPF-mini write workflows should use EF Core handlers instead of stored procedures where possible.

---

## Team Assignments

| Person | Role | Ownership |
|---|---|---|
| **A** | Project Lead + Backend/API foundation | Inspect current API/CQRS, define endpoint reuse map, scaffold WPF shell, API client base, auth/quick-login, navigation, shared models/styles |
| **B** | Mangaka Series + File Upload | Reference data, file upload client, Series CRUD, Series editor, genre/tag selector, cover/proposal upload, submit proposal |
| **C** | Editor + Board | Editor proposal review endpoints/client/UI, board poll/vote/close endpoints/client/UI |
| **D** | Chapters + Polish | Chapter CRUD/review endpoints/client/UI, chapter/markup uploads, search/filter, dialogs, demo scripts |

---

## Dependency Map

```text
Day 1 ─── Backend/API inspection + endpoint mapping
  │
Day 2 ─── WPF shell + API client base + real upload foundation
  │
Day 3 ─── Series CRUD + reference data + file upload
  │
Day 4 ─── Proposal submit + editor review
  │
Day 5 ─── Board voting + chapter CRUD
  │
Day 6 ─── Chapter review + integration testing
  │
Day 7 ─── Bug fixes + demo prep
```

---

## Day 1 — Backend Inspection + Plan Alignment

> **Goal:** Stop guessing. Identify what existing MangaFlow API/CQRS/file services can be reused, adapted, or rewritten for WPF.

### A — API/CQRS inspection lead

- [ ] Open current MangaFlow solution.
- [ ] Inspect existing API controllers for:
  - Series draft create/update/cancel.
  - Series proposal submit.
  - Editor proposal review.
  - Board poll/vote.
  - Genre/tag reference data.
  - File upload/FileResource creation.
  - Chapter CRUD/review.
- [ ] Create a mapping table:

```text
Feature | Existing endpoint/handler | Reuse as-is | Adapt | New WPF endpoint
```

- [ ] Confirm WPF mini keeps `Series + SeriesProposal`.
- [ ] Confirm board poll remains tied to `series_id`, not `series_proposal_id`.
- [ ] Identify current stored-procedure-based handlers that should be replaced with EF Core handlers for WPF-mini.
- [ ] Decide route prefix: `/api/wpf/*`.

### B — Series + upload inspection

- [ ] Inspect current Series DTOs and typed API clients.
- [ ] Inspect current Genre/Tag query endpoints.
- [ ] Inspect Cloudinary/FileResource upload service.
- [ ] Identify required upload request shape for:
  - `SERIES_COVER`
  - `SERIES_PROPOSAL`
- [ ] Draft WPF file picker requirements.

### C — Editor/Board inspection

- [ ] Inspect proposal review handlers/endpoints.
- [ ] Inspect board poll/vote tables, queries, handlers.
- [ ] Confirm how latest proposal is resolved for a series in `UNDER_BOARD_REVIEW`.
- [ ] Draft needed board endpoints:
  - ready series
  - open poll
  - cast vote
  - close poll

### D — Chapter inspection

- [ ] Inspect Chapter entity/configuration and any existing chapter endpoints.
- [ ] Inspect `ChapterEditorialReview`.
- [ ] Decide chapter file purpose:
  - existing purpose if compatible, or
  - add `CHAPTER_PACKAGE` to WPF schema/check constraint.
- [ ] Draft needed chapter endpoints.

### Day 1 Deliverable

```text
Reuse/adapt/rewrite table + final endpoint list.
```

---

## Day 2 — WPF Shell + API Foundation + Real Upload Base

> **Goal:** WPF app can start, login/quick-login, call API, and upload a real local file.

### A — WPF scaffold + API client base

- [ ] Create WPF project targeting `net8.0-windows`.
- [ ] Install WPF packages:
  - `CommunityToolkit.Mvvm`
  - `Microsoft.Extensions.DependencyInjection`
  - `Microsoft.Extensions.Configuration.Json`
  - `System.Net.Http.Json`
- [ ] Create `appsettings.json` with `ApiBaseUrl`.
- [ ] Create `ApiClientBase` using `HttpClient`.
- [ ] Create shared API error handler.
- [ ] Create `LoginView`, `MainWindow`, role-aware shell navigation.
- [ ] Implement `AuthApiClient`.
- [ ] Implement quick-login if JWT/auth integration is too much.

### B — File upload client + reference data client

- [ ] Create `FilePickerService`.
- [ ] Create `FileUploadApiClient`.
- [ ] Implement multipart upload from WPF to API.
- [ ] Create `ReferenceDataApiClient`.
- [ ] Load genres/tags into test view/debug output.

### C — Backend WPF upload endpoint support

- [ ] Add or adapt `POST /api/wpf/files/upload`.
- [ ] Reuse existing Cloudinary/FileResource service.
- [ ] Validate file purpose and file type.
- [ ] Return `FileResourceDto`.
- [ ] Test from Swagger/Postman first.

### D — UI foundation/polish

- [ ] Create status converters.
- [ ] Create shared styles.
- [ ] Create dialog service.
- [ ] Create basic navigation pages/stubs for all roles.

### Day 2 Deliverable

```text
WPF can login/quick-login, call API, load genres/tags, and upload a real file.
```

---

## Day 3 — Mangaka Series CRUD

> **Goal:** Mangaka can create/edit Series drafts using API and EF Core backend handlers.

### A — Backend support

- [ ] Add/adapt endpoints:
  - `GET /api/wpf/series/my`
  - `GET /api/wpf/series/{seriesId}`
  - `POST /api/wpf/series`
  - `PUT /api/wpf/series/{seriesId}`
  - `DELETE /api/wpf/series/{seriesId}`
- [ ] Implement EF Core handlers if current handlers rely too heavily on SPs.
- [ ] Ensure create inserts:
  - `Series`
  - `SeriesContributor`
  - `SeriesGenre`
  - `SeriesTag`
- [ ] Ensure update replaces genre/tag links inside EF transaction.
- [ ] Keep validation in Application handler.

### B — Series list/editor UI

- [ ] Create `MangakaSeriesListViewModel`.
- [ ] Create `MangakaSeriesListView`.
- [ ] Create `SeriesEditorViewModel`.
- [ ] Create `SeriesEditorView`.
- [ ] Implement genre/tag checklist multi-select.
- [ ] Implement cover upload and store returned `coverFileId`.
- [ ] Save draft through API.

### C — Editor queue API prep

- [ ] Add/adapt `GET /api/wpf/editor/proposals/queue`.
- [ ] Create proposal queue DTO containing:
  - proposal id
  - series id
  - proposal version
  - proposal title
  - synopsis snapshot
  - submitted by
  - submitted date
  - current series genres/tags
  - file metadata
- [ ] Test API response.

### D — Chapter API prep

- [ ] Add/adapt:
  - `GET /api/wpf/series/{seriesId}/chapters`
  - `POST /api/wpf/series/{seriesId}/chapters`
- [ ] Ensure chapter creation requires serialized series.
- [ ] Test with seeded serialized series or wait until board flow is done.

### Day 3 Deliverable

```text
Mangaka can create and edit PROPOSAL_DRAFT series with real cover upload.
```

---

## Day 4 — Proposal Submit + Editor Review

> **Goal:** Full editorial flow works: draft → proposal version → editor request revision/pass/cancel.

### A — Proposal submit backend

- [ ] Add/adapt `POST /api/wpf/series/{seriesId}/submit`.
- [ ] Request body includes `proposalFileId`.
- [ ] EF handler should:
  - validate series is `PROPOSAL_DRAFT`
  - validate actor is Mangaka contributor
  - validate proposal file exists and purpose is `SERIES_PROPOSAL`
  - compute next `proposal_version_no`
  - create `SeriesProposal`
  - set `Series.status_code = UNDER_EDITORIAL_REVIEW`
- [ ] Do not call `usp_SeriesProposal_Submit` unless forced by time.

### B — WPF submit flow

- [ ] In `SeriesEditorViewModel`, implement proposal file picker/upload.
- [ ] Upload proposal file before submit.
- [ ] Call submit endpoint with `proposalFileId`.
- [ ] Refresh series list after submit.
- [ ] Show latest proposal status/version.

### C — Editor proposal review backend + UI

- [ ] Add/adapt endpoints:
  - `POST /api/wpf/editor/proposals/{proposalId}/request-revision`
  - `POST /api/wpf/editor/proposals/{proposalId}/pass-to-board`
  - `POST /api/wpf/editor/proposals/{proposalId}/cancel`
- [ ] EF handlers update `SeriesProposal` and `Series` in one transaction.
- [ ] Implement `EditorProposalReviewViewModel`.
- [ ] Implement `EditorProposalReviewView`.
- [ ] Allow optional markup file upload using `EDITORIAL_ATTACHMENT`.
- [ ] Comments required for revision/cancel.

### D — Chapter UI start

- [ ] Create `ChapterListViewModel`.
- [ ] Create `ChapterListView`.
- [ ] Add serialized series selector.
- [ ] Stub add/edit/submit buttons.

### Day 4 Deliverable

```text
Series can move PROPOSAL_DRAFT → UNDER_EDITORIAL_REVIEW → PROPOSAL_DRAFT / UNDER_BOARD_REVIEW / CANCELLED.
```

---

## Day 5 — Board Voting + Chapter CRUD

> **Goal:** Board can vote and serialize/cancel series. Mangaka can create chapters under serialized series.

### A — Board backend support

- [ ] Add/adapt endpoints:
  - `GET /api/wpf/board/series-ready`
  - `GET /api/wpf/board/polls`
  - `GET /api/wpf/board/polls/{pollId}`
  - `POST /api/wpf/board/series/{seriesId}/polls`
  - `POST /api/wpf/board/polls/{pollId}/votes`
  - `POST /api/wpf/board/polls/{pollId}/close`
- [ ] Keep poll table linked to `series_id`.
- [ ] Close poll EF transaction:
  - close poll
  - count votes
  - find latest `SeriesProposal` in `UNDER_BOARD_REVIEW`
  - approve: proposal `APPROVED`, series `SERIALIZED`
  - reject: proposal `CANCELLED`, series `CANCELLED`
  - tie: keep current statuses

### B — Board WPF UI

- [ ] Create `BoardPollListViewModel`.
- [ ] Create `BoardPollListView`.
- [ ] Create `BoardPollDetailViewModel`.
- [ ] Create `BoardPollDetailView`.
- [ ] Board Chief can open/close poll.
- [ ] Board roles can vote.
- [ ] Reject vote requires reason.
- [ ] Vote counts refresh after voting.

### C — Chapter CRUD backend

- [ ] Add/adapt endpoints:
  - `PUT /api/wpf/chapters/{chapterId}`
  - `DELETE /api/wpf/chapters/{chapterId}`
  - `POST /api/wpf/chapters/{chapterId}/submit`
- [ ] Add chapter file upload support if required.
- [ ] EF handler validates:
  - series is `SERIALIZED`
  - actor is Mangaka contributor
  - chapter status is editable
  - chapter number unique

### D — Chapter WPF UI completion

- [ ] Implement add/edit chapter form.
- [ ] Implement chapter file picker/upload.
- [ ] Implement submit chapter.
- [ ] Implement status badges.
- [ ] Test with serialized series from board flow.

### Day 5 Deliverable

```text
Board can make a series SERIALIZED, and Mangaka can create/submit chapters for it.
```

---

## Day 6 — Editor Chapter Review + End-to-End Testing

> **Goal:** Chapter review works, then all core demo scenarios pass.

### A — Integration support

- [ ] Fix backend/API integration issues.
- [ ] Ensure all role checks are enforced server-side.
- [ ] Ensure API returns user-friendly errors.

### B — Series polish

- [ ] Show proposal history.
- [ ] Show editor feedback when latest proposal is `REVISION_REQUESTED`.
- [ ] Add search/filter to series list.
- [ ] Add dirty-form warning.

### C — Editor chapter review

- [ ] Add/adapt endpoints:
  - `GET /api/wpf/editor/chapters/queue`
  - `POST /api/wpf/editor/chapters/{chapterId}/approve`
  - `POST /api/wpf/editor/chapters/{chapterId}/request-revision`
  - `POST /api/wpf/editor/chapters/{chapterId}/cancel`
- [ ] Implement EF handlers:
  - insert `ChapterEditorialReview`
  - update chapter status
  - support optional markup file upload
- [ ] Implement `EditorChapterReviewViewModel`.
- [ ] Implement `EditorChapterReviewView`.

### D — Integration testing

Run scenarios:

```text
1. Series approved by board.
2. Editor revision cycle.
3. Board rejection.
4. Chapter approved.
5. Chapter revision.
6. File upload for cover/proposal/chapter/markup.
7. Validation errors.
```

### Day 6 Deliverable

```text
All main scenarios pass once in a full team run.
```

---

## Day 7 — Bug Fixes + Demo Prep

> **Goal:** Clean build, stable demo, final script.

### A

- [ ] Fix shell/login/API client bugs.
- [ ] Prepare demo script.
- [ ] Confirm backend starts cleanly.
- [ ] Confirm WPF `ApiBaseUrl` config works on all team machines.

### B

- [ ] Fix series CRUD/submit/file upload bugs.
- [ ] Confirm genre/tag selection persists.
- [ ] Confirm proposal versions increment correctly.

### C

- [ ] Fix editor/board bugs.
- [ ] Confirm board poll result updates both proposal and series.
- [ ] Confirm role restrictions.

### D

- [ ] Fix chapter CRUD/review bugs.
- [ ] Polish status badges, dialogs, and search/filter.
- [ ] Prepare seed/demo data.

### All

- [ ] Run the demo script start-to-finish.
- [ ] Record known limitations.
- [ ] Clean build.
- [ ] Prepare presentation/demo explanation.

### Day 7 Deliverable

```text
Demo-ready WPF + API mini-project.
```

---

## Revised Demo Scenarios

### Scenario 1 — Series approved by board

```text
1. Mangaka logs in.
2. Creates Series draft.
3. Selects genres/tags.
4. Uploads cover image.
5. Uploads proposal document.
6. Submits Series.
7. Editor passes latest proposal to board.
8. Board Chief opens START_SERIALIZATION poll.
9. Board members vote APPROVE.
10. Board Chief closes poll.
11. Series becomes SERIALIZED.
```

### Scenario 2 — Revision cycle

```text
1. Mangaka submits proposal v1.
2. Editor requests revision with comments/markup.
3. Proposal v1 becomes REVISION_REQUESTED.
4. Series returns to PROPOSAL_DRAFT.
5. Mangaka edits same Series.
6. Mangaka uploads/submits proposal v2.
7. Editor passes to board.
8. Board approves.
```

### Scenario 3 — Board rejects

```text
1. Editor passes proposal to board.
2. Board Chief opens poll.
3. Board member votes REJECT with reason.
4. Board Chief closes poll.
5. Latest proposal becomes CANCELLED.
6. Series becomes CANCELLED.
```

### Scenario 4 — Chapter approved

```text
1. Mangaka selects serialized series.
2. Creates chapter.
3. Uploads chapter file.
4. Submits chapter.
5. Editor approves chapter.
6. Chapter becomes APPROVED.
```

### Scenario 5 — Chapter revision

```text
1. Mangaka submits chapter.
2. Editor requests revision with comments or markup file.
3. Chapter becomes REVISION_REQUESTED.
4. Mangaka edits and resubmits.
5. Editor approves.
```

---

## Final Rule for the Team

Do not build new Dapper/direct DB services unless the API approach fails near the deadline.

The intended implementation path is:

```text
WPF ViewModel
→ Typed API client
→ API Controller
→ MediatR Command/Query
→ EF Core Handler
→ SQL Server + real FileResource upload
```
