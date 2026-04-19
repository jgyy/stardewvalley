namespace StardewBot.GameState;

public record NpcInfo(
    string Name,
    int FriendshipHearts,
    bool IsBirthday,
    bool HasPreferredGiftAvailable
);
