using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Events;

namespace GCCC.BoardGame.Presentation.Audio
{
    internal enum AudioCue
    {
        None,
        Move,
        Battle,
        PieceDestroyed,
        Fusion,
        FusionFailed,
        GameEnded
    }

    internal static class GameEventAudioResolver
    {
        public static IReadOnlyList<AudioCue> Resolve(
            IReadOnlyList<GameEvent> events)
        {
            return (events ?? Array.Empty<GameEvent>())
                .Select(Resolve)
                .Where(cue => cue != AudioCue.None)
                .ToArray();
        }

        public static AudioCue Resolve(GameEvent gameEvent)
        {
            return gameEvent switch
            {
                PieceMoved => AudioCue.Move,
                CombatResolved => AudioCue.Battle,
                PieceDestroyed => AudioCue.PieceDestroyed,
                PiecesFused => AudioCue.Fusion,
                FusionAttemptFailed => AudioCue.FusionFailed,
                GameEnded => AudioCue.GameEnded,
                _ => AudioCue.None
            };
        }
    }
}
