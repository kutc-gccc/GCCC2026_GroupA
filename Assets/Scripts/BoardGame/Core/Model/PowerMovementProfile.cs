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
            return new PowerMovementProfile(
                StandardId,
                new[]
                {
                    new PowerMovementBand(1, 1, MoveDirections.All),
                    new PowerMovementBand(
                        2,
                        2,
                        MoveDirections.All & ~MoveDirections.NorthEast),
                    new PowerMovementBand(
                        3,
                        3,
                        MoveDirections.All & ~MoveDirections.SouthEast),
                    new PowerMovementBand(
                        4,
                        4,
                        MoveDirections.All & ~MoveDirections.NorthWest),
                    new PowerMovementBand(
                        5,
                        5,
                        MoveDirections.All & ~MoveDirections.SouthWest),
                    new PowerMovementBand(
                        6,
                        6,
                        MoveDirections.All & ~MoveDirections.West),
                    new PowerMovementBand(
                        7,
                        7,
                        MoveDirections.All & ~MoveDirections.East),
                    new PowerMovementBand(8, int.MaxValue, MoveDirections.All)
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
