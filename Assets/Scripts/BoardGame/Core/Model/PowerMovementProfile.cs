using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class PowerMovementProfile
    {
        public const string StandardIdValue = "standard";

        public PowerMovementProfile(
            MovementProfileId id,
            IEnumerable<PowerMovementBand> bands)
        {
            Id = id.IsValid
                ? id
                : throw new ArgumentException("Movement profile ID is invalid.", nameof(id));

            PowerMovementBand[] orderedBands = (bands ??
                    throw new ArgumentNullException(nameof(bands)))
                .OrderBy(band => band?.MinCombatPower ?? int.MaxValue)
                .ToArray();
            ValidateBands(orderedBands);
            Bands = new ReadOnlyCollection<PowerMovementBand>(orderedBands);
        }

        public MovementProfileId Id { get; }

        public IReadOnlyList<PowerMovementBand> Bands { get; }

        public MoveDirections GetDirections(int combatPower)
        {
            if (combatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(combatPower));
            }

            foreach (PowerMovementBand band in Bands)
            {
                if (band.Contains(combatPower))
                {
                    return band.Directions;
                }
            }

            throw new InvalidOperationException(
                $"Movement profile '{Id}' does not cover combat power {combatPower}.");
        }

        public static MovementProfileId StandardId =>
            new MovementProfileId(StandardIdValue);

        public static PowerMovementProfile CreateStandard()
        {
            // 制限は累積する。戦闘力が上がるほど、下の段で失った方向をすべて
            // 引き継いだうえでさらに1方向を失う。
            MoveDirections power1 = MoveDirections.All;
            MoveDirections power2 = power1 & ~MoveDirections.NorthEast;
            MoveDirections power3 = power2 & ~MoveDirections.SouthEast;
            MoveDirections power4 = power3 & ~MoveDirections.NorthWest;
            MoveDirections power5 = power4 & ~MoveDirections.SouthWest;
            MoveDirections power6 = power5 & ~MoveDirections.West;
            MoveDirections power7 = power6 & ~MoveDirections.East;

            return new PowerMovementProfile(
                StandardId,
                new[]
                {
                    new PowerMovementBand(1, 1, power1),
                    new PowerMovementBand(2, 2, power2),
                    new PowerMovementBand(3, 3, power3),
                    new PowerMovementBand(4, 4, power4),
                    new PowerMovementBand(5, 5, power5),
                    new PowerMovementBand(6, 6, power6),
                    new PowerMovementBand(7, 7, power7),
                    new PowerMovementBand(8, int.MaxValue, power7)
                });
        }

        private static void ValidateBands(IReadOnlyList<PowerMovementBand> bands)
        {
            if (bands.Count == 0)
            {
                throw new ArgumentException("A movement profile must contain at least one band.");
            }

            int expectedMinimum = 1;
            for (int index = 0; index < bands.Count; index++)
            {
                PowerMovementBand band = bands[index] ??
                    throw new ArgumentException("Movement profile bands must not contain null.");
                if (band.MinCombatPower != expectedMinimum)
                {
                    throw new ArgumentException(
                        "Movement profile bands must cover every positive combat power without gaps or overlaps.");
                }

                if (band.MaxCombatPower == int.MaxValue)
                {
                    if (index != bands.Count - 1)
                    {
                        throw new ArgumentException(
                            "No movement profile band may follow an unbounded band.");
                    }

                    return;
                }

                expectedMinimum = band.MaxCombatPower + 1;
            }

            throw new ArgumentException(
                "Movement profile bands must cover all positive combat powers.");
        }
    }
}
