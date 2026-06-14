namespace PomoDone.Services;

// Saves already-rendered PNG bytes to the device gallery. Implemented in
// Platforms/Android with MediaStore (scoped storage, zero permissions on
// API 29+).
public interface IChartExportService
{
    Task<bool> SaveImageAsync(string displayName, byte[] pngBytes);
}
