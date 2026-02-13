using System.Collections.Generic;
using TurnBasedGame.Unit;
using TurnBasedGame.Skills;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Validation logic cho deck trước khi vào trận.
    /// Tách riêng để dễ unit test và tái sử dụng.
    /// </summary>
    public static class DeckValidator
    {
        public const int MinUnits = 1;
        public const int MaxUnits = PlayerDataSO.MaxDeckSize;
        public const int MaxSpells = PlayerDataSO.MaxSpellSlots;

        public struct ValidationResult
        {
            public bool IsValid;
            public string Message;

            public static ValidationResult Valid() =>
                new() { IsValid = true, Message = "" };

            public static ValidationResult Invalid(string msg) =>
                new() { IsValid = false, Message = msg };
        }

        /// <summary>Validate toàn bộ deck (units + spells).</summary>
        public static ValidationResult Validate(
            IReadOnlyList<UnitData> units,
            IReadOnlyList<SkillBase> spells)
        {
            if (units == null || units.Count < MinUnits)
                return ValidationResult.Invalid($"Cần ít nhất {MinUnits} binh lính!");

            if (units.Count > MaxUnits)
                return ValidationResult.Invalid($"Tối đa {MaxUnits} binh lính!");

            if (spells != null && spells.Count > MaxSpells)
                return ValidationResult.Invalid($"Tối đa {MaxSpells} phép thuật!");

            if (HasDuplicates(units))
                return ValidationResult.Invalid("Đội hình có binh lính trùng lặp!");

            if (spells != null && HasDuplicates(spells))
                return ValidationResult.Invalid("Phép thuật bị trùng lặp!");

            return ValidationResult.Valid();
        }

        /// <summary>Tính tổng spawn cost của deck.</summary>
        public static int CalculateTotalCost(IReadOnlyList<UnitData> units)
        {
            if (units == null) return 0;

            int total = 0;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null) total += units[i].spawnCost;
            }
            return total;
        }

        private static bool HasDuplicates<T>(IReadOnlyList<T> items)
        {
            var seen = new HashSet<T>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && !seen.Add(items[i]))
                    return true;
            }
            return false;
        }
    }
}
