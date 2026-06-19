using Android.Content;
using Android.Media;
using Android.OS;
using PomoDone.Services;

namespace PomoDone
{
    // THE owner of the self-played session-end ring. STATIC on purpose: the alarm
    // BroadcastReceiver may run with the app UI dead (even the process freshly
    // started), so the receiver, the notification-tap intent (via MainActivity),
    // and the in-app Stop button must all reach the SAME handle — this one.
    //
    // Looping MediaPlayer at ALARM usage so it is audible with the screen off and
    // rides the alarm volume stream (replaces the old immutable notification-channel
    // sound, which the app could not stop). A partial wake-lock keeps the CPU alive
    // long enough to ring + auto-stop through Doze; a max-duration timer GUARANTEES
    // it can never loop forever. This is NOT a foreground service (§3.2) — just an
    // in-process player kept alive by active playback + a short wake-lock.
    public static class AlarmAudioPlayer
    {
        private static readonly object Gate = new();
        private static MediaPlayer? _player;
        private static PowerManager.WakeLock? _wakeLock;
        private static System.Threading.Timer? _autoStop;
        private static DateTime _ringStartedUtc;

        // Raised whenever ringing starts or stops (in-process), so the UI can
        // show/hide the Stop button. May fire off the main thread (timer / binder
        // thread) — marshal to the UI thread in the handler.
        public static event EventHandler? StateChanged;

        public static bool IsRinging
        {
            get { lock (Gate) return _player is not null; }
        }

        // Start the looping ring. Idempotent: a second call while already ringing
        // is a no-op (so a duplicate alarm fire can't stack two players).
        public static void Start(Context context)
        {
            var started = false;
            lock (Gate)
            {
                if (_player is not null)
                    return;

                var uri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm)
                          ?? RingtoneManager.GetDefaultUri(RingtoneType.Notification);
                if (uri is null)
                    return; // no tone available — receiver still posts the notification

                // Partial wake-lock with a hard timeout = the max ring window: keeps
                // the CPU alive so the ring plays and the auto-stop timer fires with
                // the screen off, and self-releases as a backstop if Stop is missed.
                var pm = (PowerManager?)context.GetSystemService(Context.PowerService);
                _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "pomodone:alarm-ring");
                if (_wakeLock is not null)
                {
                    _wakeLock.SetReferenceCounted(false);
                    _wakeLock.Acquire(AlarmRingMath.DefaultMaxRingSeconds * 1000L);
                }

                var player = new MediaPlayer();

                // ALARM usage → alarm volume stream, audible screen-off. Built in the
                // same non-fluent style the old channel sound used.
                var attrs = new AudioAttributes.Builder();
                attrs.SetUsage(AudioUsageKind.Alarm);
                attrs.SetContentType(AudioContentType.Sonification);
                player.SetAudioAttributes(attrs.Build());

                player.SetDataSource(context, uri);
                player.Looping = true;
                player.Prepare(); // local content URI → fast enough for OnReceive
                player.Start();

                _player = player;
                _ringStartedUtc = DateTime.UtcNow;

                // Hard cap: self-stop after the max ring window even if no UI ever
                // appears. The pure rule (AlarmRingMath) gates the actual stop.
                _autoStop = new System.Threading.Timer(
                    _ =>
                    {
                        if (AlarmRingMath.ShouldStop(_ringStartedUtc, DateTime.UtcNow, AlarmRingMath.DefaultMaxRingSeconds))
                            Stop();
                    },
                    null,
                    AlarmRingMath.DefaultMaxRingSeconds * 1000,
                    System.Threading.Timeout.Infinite);

                started = true;
            }

            if (started)
                StateChanged?.Invoke(null, EventArgs.Empty);
        }

        // Stop + release everything. Idempotent and safe from any thread — the
        // receiver, the notification intent, the in-app button, the auto-stop
        // timer, and a double-tap all land here without crashing.
        public static void Stop()
        {
            var changed = false;
            lock (Gate)
            {
                if (_autoStop is not null)
                {
                    _autoStop.Dispose();
                    _autoStop = null;
                }

                if (_player is not null)
                {
                    try { if (_player.IsPlaying) _player.Stop(); } catch { }
                    try { _player.Release(); } catch { }
                    _player = null;
                    changed = true;
                }

                if (_wakeLock is not null)
                {
                    try { if (_wakeLock.IsHeld) _wakeLock.Release(); } catch { }
                    _wakeLock = null;
                }
            }

            if (changed)
                StateChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
