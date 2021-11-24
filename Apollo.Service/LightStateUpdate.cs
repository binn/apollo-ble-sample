namespace Apollo.Service
{
    public class ColorStateUpdate
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int Alpha { get; set; }
    }

    public class FadeStateUpdate
    {
        public int FadeMode { get; set; }
        public int Speed { get; set; }
    }
}