using Microsoft.Extensions.Logging;
using PomoDone.Pages;
using PomoDone.Repositories;
using PomoDone.Services;
using PomoDone.ViewModels;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;

namespace PomoDone
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()  // SkiaSharp view handlers (SKCanvasView etc.)
                .UseLiveCharts() // LiveCharts2 v2 ChartViewHandler — REQUIRED, or the
                                 // chart control throws TargetInvocationException on create
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Database: the ONE SQLiteAsyncConnection lives in this singleton.
            builder.Services.AddSingleton<DatabaseService>();

            // Exact-alarm scheduling for session-end notifications (Android).
            builder.Services.AddSingleton<ISessionAlarmService, SessionAlarmService>();

            // Active-task selection (shared Tasks <-> Timer) and derived
            // gamification computed from Session/ReviewLog rows.
            builder.Services.AddSingleton<ActiveTaskService>();
            builder.Services.AddSingleton<GamificationService>();

            // Streak-freeze startup pass: the one §3.5 exception (stored
            // consumable). Runs once on app open; all freeze writes live here.
            builder.Services.AddSingleton<StreakFreezeService>();

            // Stats: bucketing, demo-data seeder, and PNG export (MediaStore).
            builder.Services.AddSingleton<StatsService>();
            builder.Services.AddSingleton<DemoDataSeeder>();
            builder.Services.AddSingleton<IChartExportService, ChartExportService>();

            // Repositories (stateless wrappers over the singleton connection).
            builder.Services.AddSingleton<SessionRepository>();
            builder.Services.AddSingleton<TaskItemRepository>();
            builder.Services.AddSingleton<DeckRepository>();
            builder.Services.AddSingleton<FlashcardRepository>();
            builder.Services.AddSingleton<ReviewLogRepository>();
            builder.Services.AddSingleton<UserProfileRepository>();

            // ViewModels — one per page.
            builder.Services.AddTransient<TimerViewModel>();
            builder.Services.AddTransient<TasksViewModel>();
            builder.Services.AddTransient<StatsViewModel>();
            builder.Services.AddTransient<DecksViewModel>();
            builder.Services.AddTransient<DeckDetailViewModel>();
            builder.Services.AddTransient<ReviewViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();

            // Pages — resolved by Shell through DI.
            builder.Services.AddTransient<TimerPage>();
            builder.Services.AddTransient<TasksPage>();
            builder.Services.AddTransient<StatsPage>();
            builder.Services.AddTransient<DecksPage>();
            builder.Services.AddTransient<DeckDetailPage>();
            builder.Services.AddTransient<ReviewPage>();
            builder.Services.AddTransient<ProfilePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
