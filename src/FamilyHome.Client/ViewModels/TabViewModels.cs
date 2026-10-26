using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyHome.Client.Sync;
using FamilyHome.Shared.Holidays;
using FamilyHome.Shared.Model;

namespace FamilyHome.Client.ViewModels;

public partial class TodayViewModel : ViewModelBase
{
    public TodayViewModel()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Title = today.ToString("dddd, d. MMMM", CultureInfo.CurrentUICulture);
        // TODO: Bundesland aus den Familieneinstellungen
        HolidayText = GermanHolidays.For(today.Year, FederalState.BY).FirstOrDefault(h => h.Date == today)?.NameDe;
    }

    public string Title { get; }
    public string? HolidayText { get; }
}

public enum CalendarMode { Month, Week, ThreeDays, Day, Agenda }

public partial class CalendarViewModel : ViewModelBase
{
    public IReadOnlyList<CalendarMode> Modes { get; } = Enum.GetValues<CalendarMode>();

    [ObservableProperty]
    public partial CalendarMode Mode { get; set; } = CalendarMode.Month;
}

public partial class ListsViewModel : ViewModelBase
{
}

public partial class MoreViewModel(SyncEngine sync) : ViewModelBase
{
    [ObservableProperty]
    public partial string SyncStatus { get; set; } = "Nicht verbunden";

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        SyncStatus = "…";
        SyncStatus = await sync.SyncNowAsync() ? "Synchronisiert" : "Sync fehlgeschlagen";
    }
}
