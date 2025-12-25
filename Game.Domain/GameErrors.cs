using System.Diagnostics;
using Game.Domain.Enums;

namespace Game.Domain;

public static class GameErrors
{
    private static Dictionary<AttributeType, Dictionary<LangType, string>> _statsRequirements = new()
    {
        {
            AttributeType.Strength, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Força insuficiente, requer {0}." },
                { LangType.EN_US, "Insufficient strength, requires {0}." }
            }
        },
        {
            AttributeType.Dexterity, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Destreza insuficiente, requer {0}." },
                {
                    LangType.EN_US, "Insufficient dexterity, requires {0}."
                }
            }
        },
        {
            AttributeType.Intelligence, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Inteligência insuficiente, requer {0}." },
                { LangType.EN_US, "Insufficient intelligence, requires {0}." }
            }
        },
        {
            AttributeType.Constitution, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Constituição insuficiente, requer {0}." },
                { LangType.EN_US, "Insufficient constitution, requires {0}." }
            }
        },
        {
            AttributeType.Spirit, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Espírito insuficiente, requer {0}." },
                { LangType.EN_US, "Insufficient spirit, requires {0}." }
            }
        }
    };

    private static Dictionary<VocationType, Dictionary<LangType, string>> _vocationRequirements = new()
    {
        {
            VocationType.None, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Vocação inadequada." },
                { LangType.EN_US, "Inadequate vocation." }
            }
        },
        {
            VocationType.Warrior, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Vocação inadequada. Requer Guerreiro." },
                { LangType.EN_US, "Inadequate vocation. Requires Warrior." }
            }
        },
        {
            VocationType.Mage, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Vocação inadequada. Requer Mago." },
                { LangType.EN_US, "Inadequate vocation. Requires Mage." }
            }
        },
        {
            VocationType.Archer, new Dictionary<LangType, string>
            {
                { LangType.PT_BR, "Vocação inadequada. Requer Arqueiro." },
                { LangType.EN_US, "Inadequate vocation. Requires Arqueiro." }
            }
        }
    };

    private static Dictionary<LangType, string> _levelRequirementMessages = new()
    {
        { LangType.PT_BR, "Nível {0} necessário para equipar este item." },
        { LangType.EN_US, "Level {0} required to equip this item." }
    };

    private static Dictionary<LangType, string> _equipmentSlotRequirementErrors = new()
    {
        { LangType.PT_BR, "O item não pode ser equipado neste slot." },
        { LangType.EN_US, "The item cannot be equipped in this slot." }
    };


    /// <summary>
    /// Obtém a mensagem de erro para um requisito de stat não atendido.
    /// </summary>
    public static string GetStatRequirementError(AttributeType attribute, LangType lang)
    {
        if (_statsRequirements.TryGetValue(attribute, out var messages) &&
            messages.TryGetValue(lang, out var message))
        {
            return message;
        }

        return "Stat requirement not met.";
    }

    /// <summary>
    /// Obtém a mensagem de erro para um requisito de vocação não atendido.
    /// </summary>
    public static string GetVocationRequirementError(VocationType vocation, LangType lang)
    {
        if (_vocationRequirements.TryGetValue(vocation, out var messages) &&
            messages.TryGetValue(lang, out var message))
        {
            return message;
        }

        return "Vocation requirement not met.";
    }

    /// <summary>
    /// Obtém a mensagem de erro para um requisito de nível não atendido.
    /// </summary>
    public static string GetLevelRequirementError(int requiredLevel, LangType lang)
    {
        return string.Format(_levelRequirementMessages.GetValueOrDefault(lang,
            "Level {0} required to equip this item."), requiredLevel);
    }

    /// <summary>
    /// Obtém a mensagem de erro para um requisito de slot de equipamento não atendido.
    /// </summary>
    public static string GetEquipmentSlotRequirementError(LangType lang)
    {
        return _equipmentSlotRequirementErrors.GetValueOrDefault(lang,
            "The item cannot be equipped in this slot.");
    }
}