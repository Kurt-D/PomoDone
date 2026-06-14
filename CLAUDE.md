# POMODONE — Project Brief & Technical Specification

> Paste this entire document as the first message of a new AI coding session. It contains all context, decisions, and constraints. Treat every decision here as FINAL unless I explicitly change it.

---

## 1. Project Overview

**Pomodone** is a gamified Pomodoro mobile app for Android, built as a BSIT capstone project. It will be demoed at an informal defense (live demo + Q&A + manuscript cross-reference; manuscript is written AFTER the app is finished so it matches reality).

**Core value proposition / differentiation (use this framing everywhere):**
Existing apps (Focus To-Do, Forest, Pomofocus) are online-first, account-gated, and subscription-gated, and split timing, task tracking, and study review across separate apps. Pomodone integrates focus timing, break-time flashcard review, and consistency analytics into ONE fully offline app: no account, no internet, no paywall, all data stays on-device (privacy by architecture). Target users: students with limited connectivity and budget.

**Hard constraints:**
- 100% offline. No network calls, no accounts, no cloud, no analytics SDKs.
- Android 10+ only (API 29+).
- ~1 week build timeline, single developer working with AI assistance.
- Must survive a live defense demo on a real physical Android phone.

---

## 2. Tech Stack (pinned — do not substitute or upgrade)

| Concern | Choice |
|---|---|
| Language / Framework | C# / .NET MAUI on **.NET 9** |
| Target framework | `net9.0-android` ONLY (all other TFMs removed from csproj) |
| Min Android version | `SupportedOSPlatformVersion = 29.0` |
| IDE | Visual Studio 2022 Community (build/deploy/debug); VS Code optional for editing |
| Database | SQLite via **sqlite-net-pcl** |
| MVVM | **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`) |
| Charts | **LiveCharts2** (`LiveChartsCore.SkiaSharpView.Maui`), v2 API |
| Chart PNG export | LiveCharts2 headless `SKCartesianChart` → `.GetImage()` → PNG bytes (NO screenshots of on-screen views) |
| Image saving | Android `MediaStore` API (scoped storage, zero permissions on API 29+) |
| Notifications | Android `AlarmManager.SetExactAndAllowWhileIdle()` + local notification, via platform code in `Platforms/Android` |
| Avatar picking | MAUI `MediaPicker.PickPhotoAsync()` |
| Navigation | **Shell only** — routes registered in one place, `Shell.Current.GoToAsync` with query params. Never `Navigation.PushAsync`. |

**csproj essentials (already working — keep as-is):**
```xml
<TargetFrameworks>net9.0-android</TargetFrameworks>
<SupportedOSPlatformVersion>29.0</SupportedOSPlatformVersion>
<!-- Microsoft.Maui.Controls pinned at 9.0.120 -->
```

**Standing instruction for the AI assistant (repeat in prompts when needed):**
This is .NET MAUI on .NET 9 — NOT Xamarin.Forms. Reject and never generate: `Xamarin.Forms` namespaces, `DependencyService`, custom renderers, `Device.BeginInvokeOnMainThread` (use `MainThread.BeginInvokeOnMainThread`). Use LiveCharts2 **v2** API only. Never add or upgrade NuGet packages without asking. Never reintroduce iOS/Windows/MacCatalyst targets.

---

## 3. Architecture Decisions (FINAL)

### 3.1 Timer = wall clock, not ticks
- The timer is **timestamp-based**. On session start, persist `SessionStartUtc`, `DurationMinutes`, `Type` immediately (SQLite row for the in-progress session).
- Remaining time is always computed: `remaining = (StartUtc + Duration) - DateTime.UtcNow`. A 1-second UI tick exists ONLY to refresh the display.
- App killed / phone rebooted mid-session → on relaunch, read the in-progress session row, recompute remaining, resume display seamlessly. The session never "stops" because wall-clock time is the source of truth.
- Defense soundbite: "We don't trust the process to stay alive; we trust the clock."

### 3.2 NO foreground service (explicitly cut)
- End-of-session alert is delivered by **`AlarmManager.SetExactAndAllowWhileIdle()`**, scheduled at session start to fire a local notification at the exact end time. Survives Doze, app swipe-away, and process death. (~40 lines of platform-specific code in `Platforms/Android` + notification channel setup.)
- Cancel the scheduled alarm if the user cancels/abandons the session.
- Permissions: `SCHEDULE_EXACT_ALARM` (or `USE_EXACT_ALARM`) in the manifest; request `POST_NOTIFICATIONS` at runtime on Android 13+.
- Terminology for docs/defense: "scheduled exact alarms for session-completion notifications" — never "foreground service."

### 3.3 Backgrounding / anti-cheat stance: honor system
- Leaving the app does NOT pause or penalize the session (rationale: single-device students legitimately read materials on the same phone; a Pomodoro app is a self-regulation tool, not surveillance).
- BUT: track time spent away per session via app lifecycle events (`Window.Activated/Deactivated`) into a `SecondsAway` column. Display it in stats as a "focus purity" metric. Recorded, shown, never penalized. (This is the prepared answer to "can't users cheat?")

### 3.4 Database discipline
- **Exactly ONE `SQLiteAsyncConnection`**, wrapped in a `DatabaseService` registered as a **singleton in DI** (`MauiProgram`). All access goes through repository methods. Never create a second connection; never mix in the sync `SQLiteConnection`.
- Guard initialization (`CreateTableAsync` calls) with an `AsyncLazy`/`SemaphoreSlim` so it runs exactly once even under racing first accesses.
- Enable WAL: `PRAGMA journal_mode=WAL`.
- **All timestamps stored as UTC** (one convention project-wide — ISO-8601 string or ticks, pick one and never mix). Convert to local time only in ViewModels for display. (Critical for heatmap/peak-hour correctness — a 11:30 PM session must land on the right day.)

### 3.5 Gamification = derived, never stored
- Points, streaks, levels, and badges are **computed at runtime from `Session` rows** (plus `ReviewLog` for review bonuses). No points table, no per-event point rows. Single source of truth: sessions.
  - Streak = consecutive days with ≥1 completed Focus session.
  - Points = f(completed focus sessions) + small bonus for break-time flashcard reviews.
  - Levels/badges = thresholds over derived totals (sessions completed, streak length, days active).
- Only persistent profile state is a **single-row `UserProfile`** (avatar path, display name).

### 3.6 Avatar
- `MediaPicker.PickPhotoAsync()` → **copy the file into `FileSystem.AppDataDirectory`** → store that internal path in `UserProfile`. Never store the gallery content URI (its read permission expires). No shop/avatar store (cut) — custom upload is the personalization feature.

### 3.7 MVVM conventions
- CommunityToolkit.Mvvm source generators everywhere; nobody hand-writes `INotifyPropertyChanged`.
- One ViewModel per page, constructor-injected services via DI.
- Model class for tasks is named **`TaskItem`** (never `Task` — collides with `System.Threading.Tasks.Task`).

---

## 4. Confirmed Feature Set

### 4.1 Pomodoro Timer (the heart)
- Session types: Focus (default 25 min), Short Break (5 min), Long Break (15 min). Durations configurable is nice-to-have, not required.
- Start / cancel. Persisted in-progress session; resume-after-kill (see 3.1).
- Exact-alarm notification at session end (see 3.2).
- Completing a Focus session marks the row `Completed = true` and awards derived points.
- Optional link to an active `TaskItem` (nullable FK).

### 4.2 Tasks
- Simple CRUD: title, done/not done. Select one as the "active task" for the timer.
- **CUT: time estimates per task. CUT: task accuracy analytics.** Do not add an estimate field anywhere.

### 4.3 Flashcards (integrated via breaks — this is the feature's justification)
- One-sentence story: "Pomodone structures focus time; flashcards give the break a purpose — instead of leaving the app during a 5-minute break, the user does a quick review burst."
- User-created content only (offline app): Decks → Cards with **Front text and Back text only**. No images, no rich text, no import/export.
- Card entry UX: deck page "+" opens a two-field editor whose primary button is **"Save & Add Another"** (bulk entry flow). Edit/delete via tap/swipe on card list.
- **Quick Review mode:** when a break starts, offer an optional "Quick Review" button → shuffled cards from a chosen deck → tap to flip → self-grade "Got it / Missed it" → break-end alarm interrupts the review. Cards marked "Missed" are weighted to appear first next time. 
- **NEVER call it "spaced repetition"** anywhere (UI, docs, slides). It is "review mode." No SM-2, no due dates, no scheduling.
- Every graded flip writes a `ReviewLog` row → feeds a "cards reviewed during breaks this week" stat and a small point bonus (welds flashcards into gamification + analytics).
- **Pre-seed one sample deck on first launch** (e.g., "BSIT Review — Networking Basics," ~10 cards) so review mode is instantly demoable.

### 4.4 Stats & Analytics (scope is FINAL — exactly these three visuals)
1. **Weekly Trend** — line chart of focused minutes per day (LiveCharts2). Answers "Am I improving?"
2. **Peak Hour** — horizontal bar chart of focus minutes by hour of day (LiveCharts2). Answers "When am I most productive?"
3. **Monthly Heatmap** — calendar-style grid of colored squares by daily focus intensity. **NOT a chart library component**: build as plain XAML (`Grid`/`CollectionView` of `Border`s colored by intensity). Display-only; excluded from PNG export. Answers "Am I consistent?"
- **CUT: Task Accuracy chart** (depended on estimates, which are cut).
- **PNG export** applies to the two LiveCharts2 charts only: construct a headless `SKCartesianChart` in memory with the same series → `.GetImage()` → PNG bytes → save via MediaStore. No screenshot-the-view approaches.
- **MediaStore save (API 29+, zero permissions):** `ContentValues` with `DisplayName`, `MimeType = "image/png"`, `RelativePath = Pictures/Pomodone` → insert into `MediaStore.Images.Media.ExternalContentUri` → write PNG bytes to the opened output stream. (~30 lines platform code.)

### 4.5 Demo Data Seeder (mandatory — defense depends on it)
- A debug-only "Generate Demo Data" button (or seed-on-first-launch behind a DEMO flag) inserting **~6 weeks of plausible sessions**: varied days, realistic gaps, peak activity around 9 PM, a visible upward weekly trend, plus some ReviewLog rows.
- Purpose: charts and heatmap must look alive at the defense (the real app will be days old). If asked, disclose: "seeded data to demonstrate the analytics at scale."

### 4.6 Profile / Gamification display
- Avatar (custom upload), derived points, current streak, level, badges, focus-purity stat.

---

## 5. Database Schema (FROZEN)

All `*Utc` columns stored as UTC (single convention project-wide).

```
Session
  Id              INTEGER PK AUTOINCREMENT
  TaskId          INTEGER NULL        -- FK → TaskItem.Id
  StartUtc        (UTC timestamp)
  DurationMinutes INTEGER
  Type            TEXT/INT enum       -- Focus | ShortBreak | LongBreak
  Completed       BOOLEAN
  SecondsAway     INTEGER             -- lifecycle-tracked time away; stat only

TaskItem                              -- never name this class "Task"
  Id              INTEGER PK AUTOINCREMENT
  Title           TEXT
  CreatedUtc      (UTC timestamp)
  IsDone          BOOLEAN
  CompletedUtc    (UTC timestamp, NULL)

Deck
  Id              INTEGER PK AUTOINCREMENT
  Name            TEXT

Flashcard
  Id              INTEGER PK AUTOINCREMENT
  DeckId          INTEGER             -- FK → Deck.Id
  Front           TEXT
  Back            TEXT

ReviewLog
  Id              INTEGER PK AUTOINCREMENT
  FlashcardId     INTEGER             -- FK → Flashcard.Id
  ReviewedUtc     (UTC timestamp)
  WasCorrect      BOOLEAN

UserProfile                           -- exactly one row
  Id              INTEGER PK
  DisplayName     TEXT NULL
  AvatarPath      TEXT NULL           -- internal AppDataDirectory path
```

No points table. No streak table. No estimate columns. (All derived — see 3.5.)

---

## 6. Screens & Navigation (Shell)

Shell TabBar with five tabs; sub-pages via registered routes + `GoToAsync`.

```
TimerPage (home tab)
  - Current session type selector (Focus / Short / Long)
  - Big countdown display (recomputed from timestamp every second)
  - Start / Cancel
  - Active task display (optional pick from Tasks)
  - On break start: optional "Quick Review" button → ReviewPage

TasksPage
  - TaskItem CRUD list; mark done; set active task

StatsPage
  - Monthly heatmap (XAML grid)
  - Weekly Trend line chart
  - Peak Hour horizontal bar chart
  - "Export charts as PNG" button (MediaStore)
  - Debug-only "Generate Demo Data" button

DecksPage (Flashcards tab)
  - Deck list, add/rename/delete deck
  → DeckDetailPage (route)
      - Card list, swipe/tap to edit or delete
      - "+" → card editor: Front, Back, primary button "Save & Add Another"
  → ReviewPage (route; entered from break offer or from a deck)
      - Shuffled cards (missed-first weighting), tap to flip
      - "Got it" / "Missed it" buttons → writes ReviewLog
      - If entered from a break: break-end alarm interrupts

ProfilePage
  - Avatar (tap to change via MediaPicker)
  - Points, streak, level, badges, focus purity
```

---

## 7. Build Order (each day must end with a working, committed app)

A known-good baseline already exists: template app deploys to a physical Android phone from VS 2022; environment is set up and verified. Commit history starts from that baseline.

- **Day 1 — Skeleton:** Shell tabs + routes, DI registrations in `MauiProgram`, `DatabaseService` + repositories, all model classes, empty pages with empty ViewModels. App builds, launches, navigates.
- **Day 2 — Timer core:** TimerPage, timestamp persistence, resume-after-kill. (This is the product.)
- **Day 3 — Completion path:** AlarmManager notification + session completion writes. Test by force-killing the app mid-session on the real phone.
- **Day 4 — Tasks + gamification:** TaskItem CRUD, derived points/streak/level/badges, ProfilePage + avatar.
- **Day 5 — Stats:** demo-data seeder FIRST, then two LiveCharts2 charts, XAML heatmap, PNG export via MediaStore.
- **Day 6 — Flashcards:** deck/card CRUD, sample deck seed, break-time review mode, ReviewLog + review bonus.
- **Day 7 — Nothing new:** polish, real-device testing, demo rehearsal, defense prep.

**Cut ladder if behind (cut from the bottom up):** flashcards → PNG export → heatmap → avatar.
**Minimum acceptable demo:** timer that survives process death + notification + tasks + derived points/streak + one chart with seeded data.

**Working rules:**
- Deploy to the real phone daily, not just the emulator (AlarmManager/Doze/MediaStore behave differently on real OEM devices; the defense runs on a real phone).
- Pin all NuGet versions on Day 1; no mid-week upgrades.
- Understand-before-merge: every file must be explainable out loud (panel will ask "explain this method").
- Commit at every working milestone; the spike commit is the rollback point.

---

## 8. Defense Preparation Notes

- **Differentiation answer:** never claim novelty of category; claim fit of design. Offline-first, no account, no subscription, integrated timer + break review + consistency analytics for students with limited connectivity. Offline is a FEATURE (privacy by architecture: data never leaves the device), not a limitation.
- **Why MAUI:** "single C# codebase aligned with our curriculum's .NET track, native Android output, direct access to platform APIs (AlarmManager, MediaStore) when needed." Right tool for this team — do not claim superiority over Flutter/native.
- **Comparison matrix** (for slides + manuscript): rows = Pomodone, Focus To-Do, Forest, Pomofocus; columns = fully offline, free (no premium gate), no account required, break-time flashcards, on-device data only, consistency heatmap.
- **Cheating question:** prepared answer = honor system rationale (3.3) + focus-purity stat as evidence the issue was considered.
- **Demo script = objective traceability:** start timer → force-kill app → reopen (timer survived) → complete session (points/streak update) → stats page → export PNG → open gallery → start break → quick review.
- **Manuscript** is written after the app, must match the build exactly. Everything in the cut list below goes into Scope & Limitations / Future Work with one honest sentence each.
- **Terminology discipline:** "review mode" (not spaced repetition); "scheduled exact alarms" (not foreground service).
- **Cut list (Future Work section):** foreground service, task time estimates + accuracy analytics, shop/purchasable avatars, spaced-repetition scheduling, card images/rich text/import-export, heatmap in PNG export.

---

## 9. Known Pitfalls to Avoid (resolved in planning — do not regress)

1. Tick-based timers / in-memory countdown state → banned; wall clock is truth (3.1).
2. Foreground service → cut; AlarmManager replaces it (3.2).
3. Multiple SQLite connections or sync/async mixing → "database is locked" crashes; one async singleton only (3.4).
4. Local-time timestamps → wrong heatmap/peak-hour bucketing; UTC everywhere, convert in ViewModels (3.4).
5. Stored points/streak tables → drift from sessions; derive everything (3.5).
6. Screenshot-based chart export → layout/density/visibility bugs; headless SKCartesianChart instead (4.4).
7. `WRITE_EXTERNAL_STORAGE` permission for saving images → dead on API 29+; MediaStore needs none (4.4).
8. Storing gallery content URIs for the avatar → permission expires; copy into app data (3.6).
9. Xamarin.Forms APIs from AI suggestions → reject on sight (Section 2 standing instruction).
10. Model class named `Task` → use `TaskItem`.
11. Empty charts at the defense → demo-data seeder is mandatory, built before the charts (4.5).
12. Forgetting `POST_NOTIFICATIONS` runtime permission on Android 13+ → notification silently never shows on newer phones.
