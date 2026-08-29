using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Movement
{
    public sealed class ProfileMoveDirectionResolver : IMoveDirectionResolver
    {
        private readonly IReadOnlyDictionary<MovementProfileId, PowerMovementProfile> profiles;

        public ProfileMoveDirectionResolver(IEnumerable<PowerMovementProfile> profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            try
            {
                this.profiles = profiles.ToDictionary(profile => profile.Id);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    "Movement profile IDs must be unique.", nameof(profiles), exception);
            }

            if (this.profiles.Count == 0)
            {
                throw new ArgumentException(
                    "At least one movement profile is required.", nameof(profiles));
            }
        }

        public MoveDirections Resolve(PieceState piece)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            if (!profiles.TryGetValue(piece.MovementProfileId, out PowerMovementProfile profile))
            {
                throw new InvalidOperationException(
                    $"Movement profile '{piece.MovementProfileId}' is not registered.");
            }

            return profile.GetDirections(piece.EffectiveCombatPower);
        }
    }
}
