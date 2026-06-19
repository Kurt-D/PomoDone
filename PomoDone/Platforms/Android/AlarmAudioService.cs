using PomoDone.Services;

namespace PomoDone
{
    // DI-facing adapter over the AlarmAudioPlayer static, so the cross-platform
    // Timer VM can observe ringing state and stop the ring without touching
    // platform code. Registered as a singleton; it forwards the static's
    // StateChanged for its whole lifetime, so there is nothing to leak.
    public class AlarmAudioService : IAlarmAudioService
    {
        public AlarmAudioService()
        {
            AlarmAudioPlayer.StateChanged += OnStateChanged;
        }

        public bool IsRinging => AlarmAudioPlayer.IsRinging;

        public void Stop() => AlarmAudioPlayer.Stop();

        public event EventHandler? RingingChanged;

        private void OnStateChanged(object? sender, EventArgs e)
            => RingingChanged?.Invoke(this, EventArgs.Empty);
    }
}
