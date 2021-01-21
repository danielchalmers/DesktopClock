namespace DesktopClock
{
    public readonly struct Theme
    {
        public string Name { get; }
        public string PrimaryColor { get; }
        public string SecondaryColor { get; }

        public Theme(string name, string primaryColor, string secondaryColor)
        {
            Name = name;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
        }
    }
}