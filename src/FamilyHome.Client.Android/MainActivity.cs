using Android.App;
using Android.Content;
using Android.Content.PM;
using Avalonia.Android;

namespace FamilyHome.Client.Droid;

[Activity(
    Label = "FamilyHome",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
// Deep-Links: familyhome://join?server=…&code=…, familyhome://event/{id} (Spezifikation 5.1 / 8)
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = "familyhome")]
public class MainActivity : AvaloniaMainActivity
{
}
