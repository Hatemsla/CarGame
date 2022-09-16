namespace DBScripts
{
    public class JsonFormat
    {
        public int DetailId { get; set; }
        public string DetailColor { get; set; }
        public float DetailSmothness { get; set; }
        
        public JsonFormat(int id, string color, float smothness)
        {
            DetailId = id;
            DetailColor = color;
            DetailSmothness = smothness;
        }
    }
}