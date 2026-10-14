using RobotPlanet.Core.entities;

namespace RobotPlanet.Core.events
{
    public enum RobotEventKind
    {
        Added,
        Removed,
        CrazyAction
    }

    /// <summary>
    /// Tells subscribers WHICH robot caused the event (Source) and WHAT happened (Kind).
    /// </summary>
    public class RobotEventArgs : EventArgs
    {
        public RobotEventArgs(Robot source, RobotEventKind kind)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Kind = kind;
        }

        public Robot Source { get; }
        public RobotEventKind Kind { get; }
    }

    /// <summary>
    /// One reaction log entry: who reacted (Reactor), to which event (Cause) and what they said.
    /// The ViewModel subscribes to this and appends Message to the log.
    /// </summary>
    public class ReactionEventArgs : EventArgs
    {
        public ReactionEventArgs(Robot reactor, RobotEventArgs cause, string message)
        {
            Reactor = reactor ?? throw new ArgumentNullException(nameof(reactor));
            Cause = cause ?? throw new ArgumentNullException(nameof(cause));
            Message = message ?? string.Empty;
        }

        public Robot Reactor { get; }
        public RobotEventArgs Cause { get; }
        public string Message { get; }
    }
}
