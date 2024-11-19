using System.Windows.Shapes;

namespace Tanks
{
    public abstract class Item
    {
        public abstract void DrawItem(Rectangle pic);
        public int width { get; set; }
        public int height { get; set; }
    }
}

