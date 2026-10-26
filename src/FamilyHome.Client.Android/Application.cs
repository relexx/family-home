using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using FamilyHome.Client.Data;
using FamilyHome.Client.Droid.Platform;
using FamilyHome.Client.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyHome.Client.Droid;

[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        LocalDbContext.DatabasePath = System.IO.Path.Combine(FilesDir!.AbsolutePath, "familyhome.db");
        App.ConfigurePlatformServices = services =>
            services.AddSingleton<IPlatformServices>(_ => new AndroidPlatformServices(this));

        base.OnCreate();
        NotificationChannels.EnsureCreated(this);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder).WithInterFont();
}
