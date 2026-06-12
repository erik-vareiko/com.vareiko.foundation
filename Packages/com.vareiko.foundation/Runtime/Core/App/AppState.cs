using System;

namespace Vareiko.Foundation.App
{
    /// <summary>
    /// Application state identifier. Replaces the former fixed enum: the well-known states keep
    /// their names (call sites compile unchanged), and hosts add their own via
    /// <see cref="AppState(string)"/> — e.g. <c>new AppState("MetaShop")</c>.
    /// </summary>
    public readonly struct AppState : IEquatable<AppState>
    {
        private readonly string _id;

        public AppState(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("AppState id must be non-empty. Use AppState.None for the unset state.", nameof(id));
            }

            _id = id.Trim();
        }

        public string Id => _id ?? string.Empty;

        /// <summary>True for the default/unset state (the pre-boot state).</summary>
        public bool IsNone => string.IsNullOrEmpty(_id);

        public static AppState None => default;
        public static AppState Boot { get; } = new AppState("Boot");
        public static AppState MainMenu { get; } = new AppState("MainMenu");
        public static AppState Loading { get; } = new AppState("Loading");
        public static AppState Gameplay { get; } = new AppState("Gameplay");
        public static AppState Pause { get; } = new AppState("Pause");
        public static AppState Results { get; } = new AppState("Results");
        public static AppState Error { get; } = new AppState("Error");
        public static AppState Shutdown { get; } = new AppState("Shutdown");

        public bool Equals(AppState other)
        {
            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is AppState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(AppState left, AppState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AppState left, AppState right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return IsNone ? "None" : Id;
        }
    }
}
