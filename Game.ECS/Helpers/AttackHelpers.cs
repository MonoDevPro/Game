using Game.DTOs.Game.Player;

namespace Game.ECS.Helpers;

public class AttackHelpers
{
    // Attack range by style
    private const float MeleeRange = 2f; // TODO: Test this value
    private const float RangedRange = 8f;
    private const float MagicRange = 10f;
    
    /// <summary>
    /// Gets attack style based on vocation ID.
    /// </summary>
    public static AttackStyle GetAttackStyleFromVocation(byte vocationId)
    {
        // VocationType: 0=Unknown, 1=Warrior, 2=Archer, 3=Mage
        return vocationId switch
        {
            1 => AttackStyle.Melee,   // Warrior
            2 => AttackStyle.Ranged,  // Archer
            3 => AttackStyle.Magic,   // Mage
            _ => AttackStyle.Melee    // Default
        };
    }

    /// <summary>
    /// Gets attack range based on attack style.
    /// </summary>
    public static float GetAttackRange(AttackStyle style) => style switch
    {
        AttackStyle.Melee => MeleeRange,
        AttackStyle.Ranged => RangedRange,
        AttackStyle.Magic => MagicRange,
        _ => MeleeRange
    };
}