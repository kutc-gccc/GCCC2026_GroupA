using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class PieceState
    {
        public PieceState(
            PieceId id,
            PlayerId owner,
            GridPosition position,
            int combatPower,
            MovementProfileId movementProfileId,
            IEnumerable<string> appliedPermanentEffectIds = null,
            IEnumerable<ActiveCellEffectState> activeCellEffects = null)
        {
            if (combatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(combatPower),
                    "Combat power must be greater than zero.");
            }

            if (!movementProfileId.IsValid)
            {
                throw new ArgumentException(
                    "Movement profile ID is invalid.", nameof(movementProfileId));
            }

            string[] permanentEffects = (appliedPermanentEffectIds ??
                    Array.Empty<string>())
                .ToArray();
            ActiveCellEffectState[] activeEffects = (activeCellEffects ??
                    Array.Empty<ActiveCellEffectState>())
                .Select(effect => new ActiveCellEffectState(
                    effect.EffectId, effect.TemporaryCombatPower))
                .ToArray();

            if (permanentEffects.Any(string.IsNullOrWhiteSpace) ||
                permanentEffects.Distinct(StringComparer.Ordinal).Count() !=
                permanentEffects.Length ||
                activeEffects.Select(effect => effect.EffectId)
                    .Distinct(StringComparer.Ordinal).Count() != activeEffects.Length)
            {
                throw new ArgumentException("Cell effect IDs must be valid and unique.");
            }

            Id = id;
            Owner = owner;
            Position = position;
            CombatPower = combatPower;
            MovementProfileId = movementProfileId;
            AppliedPermanentEffectIds =
                new ReadOnlyCollection<string>(permanentEffects);
            ActiveCellEffects =
                new ReadOnlyCollection<ActiveCellEffectState>(activeEffects);
        }

        public PieceId Id { get; }

        public PlayerId Owner { get; }

        public GridPosition Position { get; }

        public int CombatPower { get; }

        public int TemporaryCombatPower =>
            ActiveCellEffects.Sum(effect => effect.TemporaryCombatPower);

        public int EffectiveCombatPower => CombatPower + TemporaryCombatPower;

        public MovementProfileId MovementProfileId { get; }

        public IReadOnlyList<string> AppliedPermanentEffectIds { get; }

        public IReadOnlyList<ActiveCellEffectState> ActiveCellEffects { get; }

        public bool HasAppliedPermanentEffect(string effectId)
        {
            return AppliedPermanentEffectIds.Contains(effectId, StringComparer.Ordinal);
        }

        public bool HasActiveEffect(string effectId)
        {
            return ActiveCellEffects.Any(effect =>
                string.Equals(effect.EffectId, effectId, StringComparison.Ordinal));
        }

        public PieceState WithPosition(GridPosition position)
        {
            return Copy(position: position);
        }

        public PieceState WithCombatPower(int combatPower)
        {
            return Copy(combatPower: combatPower);
        }

        public PieceState WithMovementProfile(MovementProfileId movementProfileId)
        {
            return Copy(movementProfileId: movementProfileId);
        }

        public PieceState WithAttributes(
            int combatPower,
            MovementProfileId movementProfileId)
        {
            return Copy(combatPower: combatPower, movementProfileId: movementProfileId);
        }

        public PieceState WithPermanentEffectApplied(string effectId)
        {
            if (HasAppliedPermanentEffect(effectId))
            {
                return this;
            }

            return Copy(appliedPermanentEffectIds:
                AppliedPermanentEffectIds.Concat(new[] { effectId }));
        }

        public PieceState WithActiveEffect(string effectId, int temporaryCombatPower = 0)
        {
            ActiveCellEffectState replacement =
                new ActiveCellEffectState(effectId, temporaryCombatPower);
            ActiveCellEffectState[] effects = ActiveCellEffects
                .Where(effect => !string.Equals(
                    effect.EffectId, effectId, StringComparison.Ordinal))
                .Concat(new[] { replacement })
                .ToArray();
            return Copy(activeCellEffects: effects);
        }

        public PieceState WithoutActiveEffects()
        {
            return ActiveCellEffects.Count == 0
                ? this
                : Copy(activeCellEffects: Array.Empty<ActiveCellEffectState>());
        }

        public PieceState ApplyDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            if (damage == 0)
            {
                return this;
            }

            int remainingDamage = damage;
            List<ActiveCellEffectState> activeEffects =
                new List<ActiveCellEffectState>(ActiveCellEffects.Count);
            foreach (ActiveCellEffectState effect in ActiveCellEffects)
            {
                int absorbed = Math.Min(effect.TemporaryCombatPower, remainingDamage);
                activeEffects.Add(effect.WithTemporaryCombatPower(
                    effect.TemporaryCombatPower - absorbed));
                remainingDamage -= absorbed;
            }

            int remainingCombatPower = CombatPower - remainingDamage;
            return remainingCombatPower > 0
                ? Copy(
                    combatPower: remainingCombatPower,
                    activeCellEffects: activeEffects)
                : null;
        }

        public PieceState MergeWith(PieceState second, int bonus)
        {
            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (Owner != second.Owner)
            {
                throw new ArgumentException(
                    "Only pieces owned by the same player can be merged.",
                    nameof(second));
            }

            IEnumerable<string> permanentEffects = AppliedPermanentEffectIds
                .Concat(second.AppliedPermanentEffectIds)
                .Distinct(StringComparer.Ordinal);
            return new PieceState(
                Id,
                Owner,
                Position,
                CombatPower + second.CombatPower + bonus,
                MovementProfileId,
                permanentEffects,
                ActiveCellEffects);
        }

        private PieceState Copy(
            GridPosition? position = null,
            int? combatPower = null,
            MovementProfileId? movementProfileId = null,
            IEnumerable<string> appliedPermanentEffectIds = null,
            IEnumerable<ActiveCellEffectState> activeCellEffects = null)
        {
            return new PieceState(
                Id,
                Owner,
                position ?? Position,
                combatPower ?? CombatPower,
                movementProfileId ?? MovementProfileId,
                appliedPermanentEffectIds ?? AppliedPermanentEffectIds,
                activeCellEffects ?? ActiveCellEffects);
        }
    }
}
