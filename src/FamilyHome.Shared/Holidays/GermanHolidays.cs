using FamilyHome.Shared.Model;

namespace FamilyHome.Shared.Holidays;

public sealed record Holiday(DateOnly Date, string Key, string NameDe);

/// <summary>
/// Gesetzliche Feiertage in Deutschland pro Bundesland, lokal berechnet (keine externe Abhängigkeit).
/// Feiertage, die nur in Teilen eines Landes gelten (Fronleichnam in Teilen von SN/TH,
/// Mariä Himmelfahrt in Teilen von BY, Augsburger Friedensfest), sind bewusst nicht enthalten.
/// </summary>
public static class GermanHolidays
{
    public static IReadOnlyList<Holiday> For(int year, FederalState state)
    {
        var easter = EasterSunday(year);
        var list = new List<Holiday>
        {
            new(new DateOnly(year, 1, 1), "new-year", "Neujahr"),
            new(easter.AddDays(-2), "good-friday", "Karfreitag"),
            new(easter.AddDays(1), "easter-monday", "Ostermontag"),
            new(new DateOnly(year, 5, 1), "labour-day", "Tag der Arbeit"),
            new(easter.AddDays(39), "ascension", "Christi Himmelfahrt"),
            new(easter.AddDays(50), "whit-monday", "Pfingstmontag"),
            new(new DateOnly(year, 10, 3), "unity-day", "Tag der Deutschen Einheit"),
            new(new DateOnly(year, 12, 25), "christmas-1", "1. Weihnachtsfeiertag"),
            new(new DateOnly(year, 12, 26), "christmas-2", "2. Weihnachtsfeiertag"),
        };

        void AddIf(bool condition, DateOnly date, string key, string name)
        {
            if (condition) list.Add(new Holiday(date, key, name));
        }

        AddIf(state is FederalState.BW or FederalState.BY or FederalState.ST,
            new DateOnly(year, 1, 6), "epiphany", "Heilige Drei Könige");
        AddIf((state == FederalState.BE && year >= 2019) || (state == FederalState.MV && year >= 2023),
            new DateOnly(year, 3, 8), "womens-day", "Internationaler Frauentag");
        AddIf(state == FederalState.BB, easter, "easter-sunday", "Ostersonntag");
        AddIf(state == FederalState.BB, easter.AddDays(49), "whit-sunday", "Pfingstsonntag");
        AddIf(state is FederalState.BW or FederalState.BY or FederalState.HE or FederalState.NW or FederalState.RP or FederalState.SL,
            easter.AddDays(60), "corpus-christi", "Fronleichnam");
        AddIf(state == FederalState.SL, new DateOnly(year, 8, 15), "assumption", "Mariä Himmelfahrt");
        AddIf(state == FederalState.TH && year >= 2019, new DateOnly(year, 9, 20), "childrens-day", "Weltkindertag");
        AddIf(IsReformationDayState(state, year) || year == 2017,
            new DateOnly(year, 10, 31), "reformation-day", "Reformationstag");
        AddIf(state is FederalState.BW or FederalState.BY or FederalState.NW or FederalState.RP or FederalState.SL,
            new DateOnly(year, 11, 1), "all-saints", "Allerheiligen");
        AddIf(state == FederalState.SN, RepentanceDay(year), "repentance-day", "Buß- und Bettag");

        list.Sort((a, b) => a.Date.CompareTo(b.Date));
        return list;
    }

    /// <summary>Ostersonntag nach der anonymen gregorianischen Osterformel (Meeus/Jones/Butcher).</summary>
    public static DateOnly EasterSunday(int year)
    {
        int a = year % 19, b = year / 100, c = year % 100;
        int d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }

    /// <summary>Mittwoch vor dem 23. November.</summary>
    private static DateOnly RepentanceDay(int year)
    {
        var date = new DateOnly(year, 11, 22);
        while (date.DayOfWeek != DayOfWeek.Wednesday) date = date.AddDays(-1);
        return date;
    }

    private static bool IsReformationDayState(FederalState state, int year) => state switch
    {
        FederalState.BB or FederalState.MV or FederalState.SN or FederalState.ST or FederalState.TH => true,
        FederalState.HB or FederalState.HH or FederalState.NI or FederalState.SH => year >= 2018,
        _ => false,
    };
}
