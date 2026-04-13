public enum Team 
{ 
    Neutral = 0,
    Player = 1, 
    Enemy = 2, 
}

public static class TeamExtensions
{
    public static bool CanDamage(this Team attacker, Team target)
    {
        if (target == Team.Neutral) return true;
        return attacker != target;
    }
}