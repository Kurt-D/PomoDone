using Android.Content;
using Android.Provider;
using PomoDone.Services;
using AndroidApp = Android.App.Application;

namespace PomoDone
{
    // MediaStore save: scoped storage, no permissions required on API 29+.
    // ContentValues -> insert into the Images collection -> write bytes to the
    // opened output stream. Lands in the device's Pictures/Pomodone folder.
    public class ChartExportService : IChartExportService
    {
        public async Task<bool> SaveImageAsync(string displayName, byte[] pngBytes)
        {
            var resolver = AndroidApp.Context.ContentResolver;
            if (resolver is null)
                return false;

            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, displayName);
            values.Put(MediaStore.IMediaColumns.MimeType, "image/png");
            values.Put(MediaStore.IMediaColumns.RelativePath, "Pictures/Pomodone");

            var collection = MediaStore.Images.Media.ExternalContentUri;
            if (collection is null)
                return false;

            var uri = resolver.Insert(collection, values);
            if (uri is null)
                return false;

            using var output = resolver.OpenOutputStream(uri);
            if (output is null)
                return false;

            await output.WriteAsync(pngBytes, 0, pngBytes.Length);
            await output.FlushAsync();
            return true;
        }
    }
}
