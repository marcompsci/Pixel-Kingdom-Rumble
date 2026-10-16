using System.Collections.Generic;

namespace PKR
{
    /// <summary>
    /// A link to the other players in an online Last Stand (Game Center on iPhone; an in-memory pair for tests).
    /// Polled from the game loop, so nothing arrives in the middle of a frame.
    /// </summary>
    public interface IZLTransport
    {
        /// <summary>This phone's player id.</summary>
        string LocalId { get; }
        /// <summary>The other players still connected.</summary>
        IReadOnlyList<string> PeerIds { get; }
        bool Connected { get; }
        void SendToAll(byte[] data, bool reliable);
        void SendTo(string peerId, byte[] data, bool reliable);
        bool TryReceive(out string fromId, out byte[] data);
        /// <summary>A player who dropped out since the last call (null when none).</summary>
        string TakeLeft();
        void Disconnect();
    }
}
