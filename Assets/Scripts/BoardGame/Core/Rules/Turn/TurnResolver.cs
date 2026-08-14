using System;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Turn
{
    public sealed class TurnResolver
    {
        public TurnResolution ResolveAfterAction(
            PlayerId playerWhoActed,
            Func<PlayerId, bool> hasLegalAction)
        {
            PlayerId otherPlayer = playerWhoActed.Other();
            if (hasLegalAction(otherPlayer))
            {
                return new TurnResolution(otherPlayer, false, false);
            }

            if (hasLegalAction(playerWhoActed))
            {
                return new TurnResolution(playerWhoActed, true, false);
            }

            return new TurnResolution(playerWhoActed, false, true);
        }

        public TurnResolution ResolveInitialTurn(
            PlayerId firstPlayer,
            Func<PlayerId, bool> hasLegalAction)
        {
            if (hasLegalAction(firstPlayer))
            {
                return new TurnResolution(firstPlayer, false, false);
            }

            PlayerId otherPlayer = firstPlayer.Other();
            if (hasLegalAction(otherPlayer))
            {
                return new TurnResolution(otherPlayer, true, false);
            }

            return new TurnResolution(firstPlayer, false, true);
        }
    }
}
