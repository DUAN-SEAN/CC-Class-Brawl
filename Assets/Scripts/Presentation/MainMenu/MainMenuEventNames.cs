namespace ClassBrawl.Presentation
{
    /// <summary>
    /// Constants for main menu event names, matching
    /// design/ux/main-menu.md Events Fired section.
    /// Events are logged via Debug.Log as placeholders until
    /// the event bus system is implemented.
    /// </summary>
    public static class MainMenuEventNames
    {
        /// <summary>
        /// Fired when the player selects "Start Battle".
        /// Triggers GameState transition from MainMenu to CharacterSelect.
        /// </summary>
        public const string OnMainMenuStartBattle = "OnMainMenuStartBattle";

        /// <summary>
        /// Fired when the settings popup is opened.
        /// Telemetry event.
        /// </summary>
        public const string OnMainMenuSettingsOpened = "OnMainMenuSettingsOpened";

        /// <summary>
        /// Fired when the settings popup is closed.
        /// Payload: settings_changed (bool).
        /// Telemetry event.
        /// </summary>
        public const string OnMainMenuSettingsClosed = "OnMainMenuSettingsClosed";

        /// <summary>
        /// Fired when the "How to Play" popup is opened.
        /// Telemetry event.
        /// </summary>
        public const string OnMainMenuHowToPlayOpened = "OnMainMenuHowToPlayOpened";

        /// <summary>
        /// Fired when the "How to Play" popup is closed.
        /// Telemetry event.
        /// </summary>
        public const string OnMainMenuHowToPlayClosed = "OnMainMenuHowToPlayClosed";

        /// <summary>
        /// Fired when the player opens the quit confirmation dialog.
        /// Telemetry event.
        /// </summary>
        public const string OnMainMenuQuitRequested = "OnMainMenuQuitRequested";

        /// <summary>
        /// Fired when the player confirms quitting the game.
        /// Payload: session_duration (float).
        /// This is the only event that modifies persistent state (terminates game).
        /// </summary>
        public const string OnMainMenuQuitConfirmed = "OnMainMenuQuitConfirmed";
    }
}
