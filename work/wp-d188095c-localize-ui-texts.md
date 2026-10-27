---
status: open
branch:
assignee:
depends_on: [533c3095]   # the Security Gate must work inside the DevContainer first
security_relevant: false
---

# Localize holiday names and hard-coded UI texts (resx)

## Language
Everything written into the repo is English: code, string literals, comments, documentation, commit messages, PR texts
and the log below. The app UI supports English and German; user-facing texts go only into
`Resources/Strings*.resx` (German neutral, English), never hard-coded.

## Goal
No user-facing text is hard-coded any more; all of them come from `Strings.resx` (German) and `Strings.en.resx` (English):
- holiday names;
- the placeholder texts in the Today, Calendar and Lists views;
- the sync status in the More view;
- the date format of the Today title.

## Background and decisions
Decided by the owner:
- `FamilyHome.Shared` returns **only stable keys** for holidays (e.g. `new-year`), no names. Shared stays free of UI concerns.
- The client looks up the display name in resx.

Decided for this package:
- `Holiday` loses its `NameDe` property: `record Holiday(DateOnly Date, string Key)`.
- `GermanHolidays.AllKeys` lists all 19 keys. `GermanHolidays.ResourceName(key)` returns the resx entry name (`Holiday_` + key with `-` → `_`, e.g. `Holiday_new_year`). Client and tests use the same function.
- A test in `FamilyHome.Shared.Tests` reads both resx files (linked into the test output) and fails if a key has no name in one of the languages. Another test makes sure `For()` only returns keys from `AllKeys`. This avoids a new client test project.
- The three placeholder texts become one entry `ComingInPhase1` ("Kommt in Phase 1" / "Coming in phase 1"). What still has to be built moves into an XAML comment with the `TODO Phase 1:` marker.
- New resx entries: `TodayTitleFormat` (`dddd, d. MMMM` / `dddd, MMMM d`), `SyncNotConnected`, `SyncSucceeded`, `SyncFailed`, plus 19 `Holiday_*` entries.
- The English holiday names are fixed in the patch (e.g. `Holiday_christmas_2` = "Second Day of Christmas", `Holiday_repentance_day` = "Day of Repentance and Prayer").
- The complete change was built and tested before this package was written; it is included below as a patch:
  - `dotnet build -warnaserror` and `dotnet format` are green;
  - 10/10 tests pass;
  - removing an English holiday name makes the test fail with exactly that key.

## Out of scope
- The mode names in the calendar ComboBox (`Month`, `Week`, …). They come with the real calendar controls in phase 1; the patch adds a `TODO Phase 1` comment for this.
- Translating existing German comments. Work package bd2b179e does that.
- The federal state is still hard-coded to `BY` (existing TODO, unchanged).
- No new packages, no new projects.

## Allowed files
Exactly the 9 files in the patch, plus this work package file:
- `src/FamilyHome.Shared/Holidays/GermanHolidays.cs`
- `src/FamilyHome.Client/Resources/Strings.resx`, `src/FamilyHome.Client/Resources/Strings.en.resx`
- `src/FamilyHome.Client/ViewModels/TabViewModels.cs`
- `src/FamilyHome.Client/Views/TodayView.axaml`, `CalendarView.axaml`, `ListsView.axaml`
- `tests/FamilyHome.Shared.Tests/FamilyHome.Shared.Tests.csproj`, `tests/FamilyHome.Shared.Tests/GermanHolidaysTests.cs`

## Preconditions
- [ ] Package 533c3095 is in `work/archive/`.
- [ ] Work happens in the DevContainer, `main` is up to date.
- [ ] `dotnet test --project tests/FamilyHome.Shared.Tests` is green on `main` (6 tests).

## Claiming
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/d188095c*"` and `gh pr list --state all --head wp/d188095c-localize-ui-texts` must both be empty, and `status` must be `open`. Otherwise **do not start**.
3. `git switch -c wp/d188095c-localize-ui-texts`
4. In the front matter, set `status: in-progress`, `branch: wp/d188095c-localize-ui-texts` and `assignee`.
5. `git commit -s -m "WP d188095c: claimed" work/wp-d188095c-localize-ui-texts.md && git push -u origin HEAD`

## Steps

### Step 1: Extract and check the patch
- Do: write the patch from the section "Patch" at the end of this file into a temporary file:
```bash
awk '/^```diff$/{f=1;next} /^```$/{f=0} f' work/wp-d188095c-localize-ui-texts.md > "${TMPDIR:-/tmp}/wp-d188095c.patch"
```
- Check: `git apply --check "${TMPDIR:-/tmp}/wp-d188095c.patch" && echo OK` → `OK`.
  If it fails (the files changed on `main` since this package was written): **stop**. Do not edit the patch by hand.

### Step 2: Apply the patch
- Do: `git apply "${TMPDIR:-/tmp}/wp-d188095c.patch"`
- Check: `git status --short` lists exactly the 9 allowed files as modified (plus the untracked lock files, if they still exist).

### Step 3: Build and test
- Check: `dotnet build FamilyHome.slnx -warnaserror` → 0 warnings, 0 errors.
- Check: `dotnet test --project tests/FamilyHome.Shared.Tests` → 10 tests, all green.
- Check: `dotnet format src/FamilyHome.Shared --verify-no-changes`, the same for `src/FamilyHome.Client` and `tests/FamilyHome.Shared.Tests` → no output, exit code 0.

### Step 4: Negative test (do not commit)
- Do: `sed -i '/Holiday_assumption/d' src/FamilyHome.Client/Resources/Strings.en.resx`
- Check: `dotnet test --project tests/FamilyHome.Shared.Tests` → exactly 1 failed test whose message contains `Strings.en.resx: assumption`.
- Do: `git checkout -- src/FamilyHome.Client/Resources/Strings.en.resx`, then apply only that file's part of the patch again:
  `git apply --include='src/FamilyHome.Client/Resources/Strings.en.resx' "${TMPDIR:-/tmp}/wp-d188095c.patch"`
- Check: `dotnet test --project tests/FamilyHome.Shared.Tests` → 10/10 green again.

### Step 5: No hard-coded UI texts left
- Check: `grep -rnE 'Text="[^{]' src/FamilyHome.Client/Views` → no output.
- Check: `grep -n 'NameDe' -r src tests` → no output.
- Check: `grep -nE '= "[^"]+";|\? "[^"]+" :' src/FamilyHome.Client/ViewModels/TabViewModels.cs | grep -v '"…"'` → no output. (`"…"` is a language-neutral progress indicator and stays.)

### Step 6: Look at it (optional, if the noVNC desktop is available)
- `dotnet run --project src/FamilyHome.Client.Desktop`. The Today tab shows the date and "Kommt in Phase 1" (German UI culture) or "Coming in phase 1" (English).

## Checks (before the PR, all must pass)
- [ ] `scripts/security-gate.sh` is green.
- [ ] Steps 3–5 fulfilled.
- [ ] `git diff --name-only origin/main...HEAD` lists only allowed files.
- [ ] Every commit is signed: `for c in $(git rev-list origin/main..HEAD); do git cat-file -p "$c" | grep -q 'BEGIN SSH SIGNATURE' || echo "UNSIGNED $c"; done` → no output. After pushing, the PR shows every commit as "Verified".

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All required checks on the PR are green.
- [ ] The negative test from step 4 behaved as described (noted in the log).

## Stop conditions
**Stop immediately**, set `status: blocked`, write what you found into the log and ask the owner if:
- a commit cannot be signed. Never disable signing (`--no-gpg-sign`, `-c commit.gpgsign=false`);
- a check cannot really run in your environment (e.g. a Security Gate step fails for environmental reasons). Report it as "not run" with the reason; never report it as green;
- `git apply --check` fails;
- the build reports a warning or error, or a test fails (except during step 4);
- the negative test in step 4 does **not** fail;
- a file outside the allowed list would have to change;
- you think a translation in the patch is wrong. Note it in the log and ask; do not change it yourself.

## Completion by the worker
1. Fill in the log and set `status: in-review`.
2. `git commit -s`, push, open a PR against `main`:
   - title: `Localize holiday names and hard-coded UI texts (WP d188095c)`;
   - mention in the PR that `Holiday.NameDe` was removed;
   - if the worker is a Claude model, the PR text ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
   - fill in the PR template (`.github/pull_request_template.md`) completely; a list of commits is not a description;
3. Do not merge it yourself.

## Acceptance by the owner
On the PR branch: set `status: done`, `git mv work/wp-d188095c-localize-ui-texts.md work/archive/`, then squash-merge.

## Log (filled in by the worker)
- Date/assignee:
- PR:
- Output of the checks (shortened):
- Negative test (step 4):
- Deviations/observations:

## Patch
Tested against `main` at 559d87d. Do not edit; extract it with the command from step 1.

```diff
diff --git a/src/FamilyHome.Client/Resources/Strings.en.resx b/src/FamilyHome.Client/Resources/Strings.en.resx
index 622a5ec..b2f6866 100644
--- a/src/FamilyHome.Client/Resources/Strings.en.resx
+++ b/src/FamilyHome.Client/Resources/Strings.en.resx
@@ -12,4 +12,28 @@
   <data name="ComingInPhase2" xml:space="preserve"><value>Coming in phase 2</value></data>
   <data name="Settings" xml:space="preserve"><value>Settings</value></data>
   <data name="SyncNow" xml:space="preserve"><value>Sync now</value></data>
+  <data name="ComingInPhase1" xml:space="preserve"><value>Coming in phase 1</value></data>
+  <data name="TodayTitleFormat" xml:space="preserve"><value>dddd, MMMM d</value></data>
+  <data name="SyncNotConnected" xml:space="preserve"><value>Not connected</value></data>
+  <data name="SyncSucceeded" xml:space="preserve"><value>Synced</value></data>
+  <data name="SyncFailed" xml:space="preserve"><value>Sync failed</value></data>
+  <data name="Holiday_new_year" xml:space="preserve"><value>New Year's Day</value></data>
+  <data name="Holiday_epiphany" xml:space="preserve"><value>Epiphany</value></data>
+  <data name="Holiday_womens_day" xml:space="preserve"><value>International Women's Day</value></data>
+  <data name="Holiday_good_friday" xml:space="preserve"><value>Good Friday</value></data>
+  <data name="Holiday_easter_sunday" xml:space="preserve"><value>Easter Sunday</value></data>
+  <data name="Holiday_easter_monday" xml:space="preserve"><value>Easter Monday</value></data>
+  <data name="Holiday_labour_day" xml:space="preserve"><value>Labour Day</value></data>
+  <data name="Holiday_ascension" xml:space="preserve"><value>Ascension Day</value></data>
+  <data name="Holiday_whit_sunday" xml:space="preserve"><value>Whit Sunday</value></data>
+  <data name="Holiday_whit_monday" xml:space="preserve"><value>Whit Monday</value></data>
+  <data name="Holiday_corpus_christi" xml:space="preserve"><value>Corpus Christi</value></data>
+  <data name="Holiday_assumption" xml:space="preserve"><value>Assumption Day</value></data>
+  <data name="Holiday_childrens_day" xml:space="preserve"><value>World Children's Day</value></data>
+  <data name="Holiday_unity_day" xml:space="preserve"><value>German Unity Day</value></data>
+  <data name="Holiday_reformation_day" xml:space="preserve"><value>Reformation Day</value></data>
+  <data name="Holiday_all_saints" xml:space="preserve"><value>All Saints' Day</value></data>
+  <data name="Holiday_repentance_day" xml:space="preserve"><value>Day of Repentance and Prayer</value></data>
+  <data name="Holiday_christmas_1" xml:space="preserve"><value>Christmas Day</value></data>
+  <data name="Holiday_christmas_2" xml:space="preserve"><value>Second Day of Christmas</value></data>
 </root>
diff --git a/src/FamilyHome.Client/Resources/Strings.resx b/src/FamilyHome.Client/Resources/Strings.resx
index 113da99..aa7462b 100644
--- a/src/FamilyHome.Client/Resources/Strings.resx
+++ b/src/FamilyHome.Client/Resources/Strings.resx
@@ -12,4 +12,28 @@
   <data name="ComingInPhase2" xml:space="preserve"><value>Kommt in Phase 2</value></data>
   <data name="Settings" xml:space="preserve"><value>Einstellungen</value></data>
   <data name="SyncNow" xml:space="preserve"><value>Jetzt synchronisieren</value></data>
+  <data name="ComingInPhase1" xml:space="preserve"><value>Kommt in Phase 1</value></data>
+  <data name="TodayTitleFormat" xml:space="preserve"><value>dddd, d. MMMM</value></data>
+  <data name="SyncNotConnected" xml:space="preserve"><value>Nicht verbunden</value></data>
+  <data name="SyncSucceeded" xml:space="preserve"><value>Synchronisiert</value></data>
+  <data name="SyncFailed" xml:space="preserve"><value>Sync fehlgeschlagen</value></data>
+  <data name="Holiday_new_year" xml:space="preserve"><value>Neujahr</value></data>
+  <data name="Holiday_epiphany" xml:space="preserve"><value>Heilige Drei Könige</value></data>
+  <data name="Holiday_womens_day" xml:space="preserve"><value>Internationaler Frauentag</value></data>
+  <data name="Holiday_good_friday" xml:space="preserve"><value>Karfreitag</value></data>
+  <data name="Holiday_easter_sunday" xml:space="preserve"><value>Ostersonntag</value></data>
+  <data name="Holiday_easter_monday" xml:space="preserve"><value>Ostermontag</value></data>
+  <data name="Holiday_labour_day" xml:space="preserve"><value>Tag der Arbeit</value></data>
+  <data name="Holiday_ascension" xml:space="preserve"><value>Christi Himmelfahrt</value></data>
+  <data name="Holiday_whit_sunday" xml:space="preserve"><value>Pfingstsonntag</value></data>
+  <data name="Holiday_whit_monday" xml:space="preserve"><value>Pfingstmontag</value></data>
+  <data name="Holiday_corpus_christi" xml:space="preserve"><value>Fronleichnam</value></data>
+  <data name="Holiday_assumption" xml:space="preserve"><value>Mariä Himmelfahrt</value></data>
+  <data name="Holiday_childrens_day" xml:space="preserve"><value>Weltkindertag</value></data>
+  <data name="Holiday_unity_day" xml:space="preserve"><value>Tag der Deutschen Einheit</value></data>
+  <data name="Holiday_reformation_day" xml:space="preserve"><value>Reformationstag</value></data>
+  <data name="Holiday_all_saints" xml:space="preserve"><value>Allerheiligen</value></data>
+  <data name="Holiday_repentance_day" xml:space="preserve"><value>Buß- und Bettag</value></data>
+  <data name="Holiday_christmas_1" xml:space="preserve"><value>1. Weihnachtsfeiertag</value></data>
+  <data name="Holiday_christmas_2" xml:space="preserve"><value>2. Weihnachtsfeiertag</value></data>
 </root>
diff --git a/src/FamilyHome.Client/ViewModels/TabViewModels.cs b/src/FamilyHome.Client/ViewModels/TabViewModels.cs
index 448d8b3..2e488f1 100644
--- a/src/FamilyHome.Client/ViewModels/TabViewModels.cs
+++ b/src/FamilyHome.Client/ViewModels/TabViewModels.cs
@@ -1,6 +1,7 @@
 using System.Globalization;
 using CommunityToolkit.Mvvm.ComponentModel;
 using CommunityToolkit.Mvvm.Input;
+using FamilyHome.Client.Resources;
 using FamilyHome.Client.Sync;
 using FamilyHome.Shared.Holidays;
 using FamilyHome.Shared.Model;
@@ -12,9 +13,12 @@ public partial class TodayViewModel : ViewModelBase
     public TodayViewModel()
     {
         var today = DateOnly.FromDateTime(DateTime.Today);
-        Title = today.ToString("dddd, d. MMMM", CultureInfo.CurrentUICulture);
+        Title = today.ToString(Strings.TodayTitleFormat, CultureInfo.CurrentUICulture);
         // TODO: Bundesland aus den Familieneinstellungen
-        HolidayText = GermanHolidays.For(today.Year, FederalState.BY).FirstOrDefault(h => h.Date == today)?.NameDe;
+        var holiday = GermanHolidays.For(today.Year, FederalState.BY).FirstOrDefault(h => h.Date == today);
+        HolidayText = holiday is null
+            ? null
+            : Strings.ResourceManager.GetString(GermanHolidays.ResourceName(holiday.Key), CultureInfo.CurrentUICulture);
     }
 
     public string Title { get; }
@@ -38,12 +42,12 @@ public partial class ListsViewModel : ViewModelBase
 public partial class MoreViewModel(SyncEngine sync) : ViewModelBase
 {
     [ObservableProperty]
-    public partial string SyncStatus { get; set; } = "Nicht verbunden";
+    public partial string SyncStatus { get; set; } = Strings.SyncNotConnected;
 
     [RelayCommand]
     private async Task SyncNowAsync()
     {
         SyncStatus = "…";
-        SyncStatus = await sync.SyncNowAsync() ? "Synchronisiert" : "Sync fehlgeschlagen";
+        SyncStatus = await sync.SyncNowAsync() ? Strings.SyncSucceeded : Strings.SyncFailed;
     }
 }
diff --git a/src/FamilyHome.Client/Views/CalendarView.axaml b/src/FamilyHome.Client/Views/CalendarView.axaml
index c0413f0..74b0d52 100644
--- a/src/FamilyHome.Client/Views/CalendarView.axaml
+++ b/src/FamilyHome.Client/Views/CalendarView.axaml
@@ -1,11 +1,13 @@
 <UserControl xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:vm="using:FamilyHome.Client.ViewModels"
+             xmlns:res="using:FamilyHome.Client.Resources"
              x:Class="FamilyHome.Client.Views.CalendarView"
              x:DataType="vm:CalendarViewModel">
   <DockPanel Margin="16">
     <ComboBox DockPanel.Dock="Top" ItemsSource="{Binding Modes}" SelectedItem="{Binding Mode}" />
-    <TextBlock Text="TODO: Monats-/Wochen-/Tages-/Agenda-Control (eigene Controls, Phase 1)"
+    <!-- TODO Phase 1: month/week/day/agenda controls (own controls); localize the mode names in the ComboBox -->
+    <TextBlock Text="{x:Static res:Strings.ComingInPhase1}"
                TextWrapping="Wrap" Opacity="0.6" Margin="0,12,0,0" />
   </DockPanel>
 </UserControl>
diff --git a/src/FamilyHome.Client/Views/ListsView.axaml b/src/FamilyHome.Client/Views/ListsView.axaml
index 16ca9f5..4d1dab7 100644
--- a/src/FamilyHome.Client/Views/ListsView.axaml
+++ b/src/FamilyHome.Client/Views/ListsView.axaml
@@ -1,8 +1,10 @@
 <UserControl xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:vm="using:FamilyHome.Client.ViewModels"
+             xmlns:res="using:FamilyHome.Client.Resources"
              x:Class="FamilyHome.Client.Views.ListsView"
              x:DataType="vm:ListsViewModel">
-  <TextBlock Margin="16" Text="TODO: Listenübersicht nach Typ, Einkaufsliste nach Kategorie (Phase 1)"
+  <!-- TODO Phase 1: list overview by type, shopping list by category -->
+  <TextBlock Margin="16" Text="{x:Static res:Strings.ComingInPhase1}"
              TextWrapping="Wrap" Opacity="0.6" />
 </UserControl>
diff --git a/src/FamilyHome.Client/Views/TodayView.axaml b/src/FamilyHome.Client/Views/TodayView.axaml
index fac1ac9..8d78a67 100644
--- a/src/FamilyHome.Client/Views/TodayView.axaml
+++ b/src/FamilyHome.Client/Views/TodayView.axaml
@@ -1,12 +1,14 @@
 <UserControl xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:vm="using:FamilyHome.Client.ViewModels"
+             xmlns:res="using:FamilyHome.Client.Resources"
              x:Class="FamilyHome.Client.Views.TodayView"
              x:DataType="vm:TodayViewModel">
   <StackPanel Margin="16" Spacing="12">
     <TextBlock Text="{Binding Title}" FontSize="22" FontWeight="SemiBold" />
     <TextBlock Text="{Binding HolidayText}" Foreground="{DynamicResource MdOnSurfaceVariant}"
                IsVisible="{Binding HolidayText, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
-    <TextBlock Text="TODO: Termine heute/morgen und fällige Todos (Phase 1)" TextWrapping="Wrap" Opacity="0.6" />
+    <!-- TODO Phase 1: appointments today/tomorrow and due todos -->
+    <TextBlock Text="{x:Static res:Strings.ComingInPhase1}" TextWrapping="Wrap" Opacity="0.6" />
   </StackPanel>
 </UserControl>
diff --git a/src/FamilyHome.Shared/Holidays/GermanHolidays.cs b/src/FamilyHome.Shared/Holidays/GermanHolidays.cs
index 8b16b71..f81f7a5 100644
--- a/src/FamilyHome.Shared/Holidays/GermanHolidays.cs
+++ b/src/FamilyHome.Shared/Holidays/GermanHolidays.cs
@@ -2,7 +2,7 @@ using FamilyHome.Shared.Model;
 
 namespace FamilyHome.Shared.Holidays;
 
-public sealed record Holiday(DateOnly Date, string Key, string NameDe);
+public sealed record Holiday(DateOnly Date, string Key);
 
 /// <summary>
 /// Gesetzliche Feiertage in Deutschland pro Bundesland, lokal berechnet (keine externe Abhängigkeit).
@@ -16,42 +16,53 @@ public static class GermanHolidays
         var easter = EasterSunday(year);
         var list = new List<Holiday>
         {
-            new(new DateOnly(year, 1, 1), "new-year", "Neujahr"),
-            new(easter.AddDays(-2), "good-friday", "Karfreitag"),
-            new(easter.AddDays(1), "easter-monday", "Ostermontag"),
-            new(new DateOnly(year, 5, 1), "labour-day", "Tag der Arbeit"),
-            new(easter.AddDays(39), "ascension", "Christi Himmelfahrt"),
-            new(easter.AddDays(50), "whit-monday", "Pfingstmontag"),
-            new(new DateOnly(year, 10, 3), "unity-day", "Tag der Deutschen Einheit"),
-            new(new DateOnly(year, 12, 25), "christmas-1", "1. Weihnachtsfeiertag"),
-            new(new DateOnly(year, 12, 26), "christmas-2", "2. Weihnachtsfeiertag"),
+            new(new DateOnly(year, 1, 1), "new-year"),
+            new(easter.AddDays(-2), "good-friday"),
+            new(easter.AddDays(1), "easter-monday"),
+            new(new DateOnly(year, 5, 1), "labour-day"),
+            new(easter.AddDays(39), "ascension"),
+            new(easter.AddDays(50), "whit-monday"),
+            new(new DateOnly(year, 10, 3), "unity-day"),
+            new(new DateOnly(year, 12, 25), "christmas-1"),
+            new(new DateOnly(year, 12, 26), "christmas-2"),
         };
 
-        void AddIf(bool condition, DateOnly date, string key, string name)
+        void AddIf(bool condition, DateOnly date, string key)
         {
-            if (condition) list.Add(new Holiday(date, key, name));
+            if (condition) list.Add(new Holiday(date, key));
         }
 
         AddIf(state is FederalState.BW or FederalState.BY or FederalState.ST,
-            new DateOnly(year, 1, 6), "epiphany", "Heilige Drei Könige");
+            new DateOnly(year, 1, 6), "epiphany");
         AddIf((state == FederalState.BE && year >= 2019) || (state == FederalState.MV && year >= 2023),
-            new DateOnly(year, 3, 8), "womens-day", "Internationaler Frauentag");
-        AddIf(state == FederalState.BB, easter, "easter-sunday", "Ostersonntag");
-        AddIf(state == FederalState.BB, easter.AddDays(49), "whit-sunday", "Pfingstsonntag");
+            new DateOnly(year, 3, 8), "womens-day");
+        AddIf(state == FederalState.BB, easter, "easter-sunday");
+        AddIf(state == FederalState.BB, easter.AddDays(49), "whit-sunday");
         AddIf(state is FederalState.BW or FederalState.BY or FederalState.HE or FederalState.NW or FederalState.RP or FederalState.SL,
-            easter.AddDays(60), "corpus-christi", "Fronleichnam");
-        AddIf(state == FederalState.SL, new DateOnly(year, 8, 15), "assumption", "Mariä Himmelfahrt");
-        AddIf(state == FederalState.TH && year >= 2019, new DateOnly(year, 9, 20), "childrens-day", "Weltkindertag");
+            easter.AddDays(60), "corpus-christi");
+        AddIf(state == FederalState.SL, new DateOnly(year, 8, 15), "assumption");
+        AddIf(state == FederalState.TH && year >= 2019, new DateOnly(year, 9, 20), "childrens-day");
         AddIf(IsReformationDayState(state, year) || year == 2017,
-            new DateOnly(year, 10, 31), "reformation-day", "Reformationstag");
+            new DateOnly(year, 10, 31), "reformation-day");
         AddIf(state is FederalState.BW or FederalState.BY or FederalState.NW or FederalState.RP or FederalState.SL,
-            new DateOnly(year, 11, 1), "all-saints", "Allerheiligen");
-        AddIf(state == FederalState.SN, RepentanceDay(year), "repentance-day", "Buß- und Bettag");
+            new DateOnly(year, 11, 1), "all-saints");
+        AddIf(state == FederalState.SN, RepentanceDay(year), "repentance-day");
 
         list.Sort((a, b) => a.Date.CompareTo(b.Date));
         return list;
     }
 
+    /// <summary>All keys that <see cref="For"/> can return. The client shows a localized name per key (resx).</summary>
+    public static IReadOnlyList<string> AllKeys { get; } =
+    [
+        "new-year", "epiphany", "womens-day", "good-friday", "easter-sunday", "easter-monday", "labour-day",
+        "ascension", "whit-sunday", "whit-monday", "corpus-christi", "assumption", "childrens-day", "unity-day",
+        "reformation-day", "all-saints", "repentance-day", "christmas-1", "christmas-2",
+    ];
+
+    /// <summary>Name of the resx entry that holds the display name for a holiday key, e.g. "Holiday_new_year".</summary>
+    public static string ResourceName(string key) => "Holiday_" + key.Replace('-', '_');
+
     /// <summary>Ostersonntag nach der anonymen gregorianischen Osterformel (Meeus/Jones/Butcher).</summary>
     public static DateOnly EasterSunday(int year)
     {
diff --git a/tests/FamilyHome.Shared.Tests/FamilyHome.Shared.Tests.csproj b/tests/FamilyHome.Shared.Tests/FamilyHome.Shared.Tests.csproj
index 40bf8c1..55d03fe 100644
--- a/tests/FamilyHome.Shared.Tests/FamilyHome.Shared.Tests.csproj
+++ b/tests/FamilyHome.Shared.Tests/FamilyHome.Shared.Tests.csproj
@@ -15,6 +15,11 @@
     <Using Include="Xunit" />
   </ItemGroup>
 
+  <!-- Client resource files: GermanHolidaysTests checks that every holiday key has a name in both languages -->
+  <ItemGroup>
+    <None Include="..\..\src\FamilyHome.Client\Resources\Strings*.resx" LinkBase="ClientResources" CopyToOutputDirectory="PreserveNewest" />
+  </ItemGroup>
+
   <ItemGroup>
     <ProjectReference Include="..\..\src\FamilyHome.Shared\FamilyHome.Shared.csproj" />
   </ItemGroup>
diff --git a/tests/FamilyHome.Shared.Tests/GermanHolidaysTests.cs b/tests/FamilyHome.Shared.Tests/GermanHolidaysTests.cs
index 2d7e214..447d02e 100644
--- a/tests/FamilyHome.Shared.Tests/GermanHolidaysTests.cs
+++ b/tests/FamilyHome.Shared.Tests/GermanHolidaysTests.cs
@@ -1,3 +1,4 @@
+using System.Xml.Linq;
 using FamilyHome.Shared.Holidays;
 using FamilyHome.Shared.Model;
 
@@ -28,4 +29,35 @@ public class GermanHolidaysTests
         var day = Assert.Single(GermanHolidays.For(2026, FederalState.SN), h => string.Equals(h.Key, "repentance-day", StringComparison.Ordinal));
         Assert.Equal(new DateOnly(2026, 11, 18), day.Date);
     }
+
+    [Fact]
+    public void For_ReturnsOnlyKnownKeys()
+    {
+        for (var year = 2015; year <= 2035; year++)
+        {
+            foreach (var state in Enum.GetValues<FederalState>())
+            {
+                Assert.All(GermanHolidays.For(year, state), h => Assert.Contains(h.Key, GermanHolidays.AllKeys, StringComparer.Ordinal));
+            }
+        }
+    }
+
+    [Fact]
+    public void ResourceName_ReplacesHyphens()
+    {
+        Assert.Equal("Holiday_new_year", GermanHolidays.ResourceName("new-year"));
+    }
+
+    [Theory]
+    [InlineData("Strings.resx")]
+    [InlineData("Strings.en.resx")]
+    public void EveryHolidayKey_HasNameInClientResources(string file)
+    {
+        var values = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ClientResources", file))
+            .Root!.Elements("data")
+            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value"), StringComparer.Ordinal);
+
+        Assert.All(GermanHolidays.AllKeys, key =>
+            Assert.False(string.IsNullOrWhiteSpace(values.GetValueOrDefault(GermanHolidays.ResourceName(key))), $"{file}: {key}"));
+    }
 }
```
