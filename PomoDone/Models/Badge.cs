namespace PomoDone.Models;

// A gamification badge. Like points and streaks, badges are derived at
// runtime from Session/ReviewLog rows — never stored. Every badge is built
// each time with its Earned flag set, so the UI can show earned vs. locked.
public class Badge
{
    public Badge(string name, string description, bool earned)
    {
        Name = name;
        Description = description;
        Earned = earned;
    }

    public string Name { get; }
    public string Description { get; }
    public bool Earned { get; }

    public string Icon => Earned ? "\U0001F3C5" : "\U0001F512"; // 🏅 / 🔒
    public double DisplayOpacity => Earned ? 1.0 : 0.4;
}
