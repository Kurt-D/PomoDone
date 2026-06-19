namespace PomoDone.Services;

// Lets the cross-platform Timer VM observe and stop the self-played session-end
// ring without referencing platform code. The looping audio itself is owned by a
// platform-side STATIC (so the alarm BroadcastReceiver can start it with no live
// UI, even in a freshly-started process); this DI singleton just surfaces the
// stop call and a ringing-state change event for the in-app Stop button.
public interface IAlarmAudioService
{
    bool IsRinging { get; }

    void Stop();

    event EventHandler? RingingChanged;
}
