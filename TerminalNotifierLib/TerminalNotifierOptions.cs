namespace TerminalNotifierLib
{
    public class TerminalNotifierOptions
    {
        public string Message { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public NotificationSound Sound { get; set; }
        public string ContentImage { get; set; }
    }

    public enum NotificationSound
    {
        Default, Basso, Blow, Bottle, Frog, Funk, Glass, Hero, Morse, Ping, Pop, Purr, Sosumi, Submarine, Tink
    }
}