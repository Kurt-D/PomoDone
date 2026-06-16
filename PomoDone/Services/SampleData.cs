using PomoDone.Models;

namespace PomoDone.Services;

// Content for the first-launch sample deck (CLAUDE.md 4.3) so review mode is
// instantly demoable. Pure data only — the actual insert is done by
// DatabaseService during initialization.
public static class SampleData
{
    public const string DeckName = "BSIT Review — Networking Basics";

    public static List<Flashcard> BuildCards(int deckId) => new()
    {
        new Flashcard { DeckId = deckId, Front = "What does TCP stand for?", Back = "Transmission Control Protocol" },
        new Flashcard { DeckId = deckId, Front = "What does IP stand for?", Back = "Internet Protocol" },
        new Flashcard { DeckId = deckId, Front = "Default port for HTTP?", Back = "80" },
        new Flashcard { DeckId = deckId, Front = "Default port for HTTPS?", Back = "443" },
        new Flashcard { DeckId = deckId, Front = "Which OSI layer is IP at?", Back = "Layer 3 (Network)" },
        new Flashcard { DeckId = deckId, Front = "What does DNS resolve?", Back = "Domain names to IP addresses" },
        new Flashcard { DeckId = deckId, Front = "How many layers are in the OSI model?", Back = "7" },
        new Flashcard { DeckId = deckId, Front = "What does DHCP do?", Back = "Automatically assigns IP addresses to hosts" },
        new Flashcard { DeckId = deckId, Front = "Which is connection-oriented, TCP or UDP?", Back = "TCP" },
        new Flashcard { DeckId = deckId, Front = "What is the IPv4 loopback address?", Back = "127.0.0.1" },
    };
}
