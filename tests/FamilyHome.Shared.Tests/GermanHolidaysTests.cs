using FamilyHome.Shared.Holidays;
using FamilyHome.Shared.Model;

namespace FamilyHome.Shared.Tests;

public class GermanHolidaysTests
{
    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    public void EasterSunday_IsCorrect(int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), GermanHolidays.EasterSunday(year));

    [Fact]
    public void Bavaria2026_HasTwelveHolidays()
    {
        // Ohne Mariä Himmelfahrt (gilt in BY nur in Gemeinden mit überwiegend katholischer Bevölkerung)
        var holidays = GermanHolidays.For(2026, FederalState.BY);
        Assert.Equal(12, holidays.Count);
        Assert.Contains(holidays, h => string.Equals(h.Key, "corpus-christi", StringComparison.Ordinal) && h.Date == new DateOnly(2026, 6, 4));
    }

    [Fact]
    public void Saxony_HasRepentanceDayOnWednesdayBeforeNov23()
    {
        var day = Assert.Single(GermanHolidays.For(2026, FederalState.SN), h => string.Equals(h.Key, "repentance-day", StringComparison.Ordinal));
        Assert.Equal(new DateOnly(2026, 11, 18), day.Date);
    }
}
