namespace FamilyHome.Client.ViewModels;

public class MainViewModel(TodayViewModel today, CalendarViewModel calendar, ListsViewModel lists, MoreViewModel more)
    : ViewModelBase
{
    public TodayViewModel Today { get; } = today;
    public CalendarViewModel Calendar { get; } = calendar;
    public ListsViewModel Lists { get; } = lists;
    public MoreViewModel More { get; } = more;
}
