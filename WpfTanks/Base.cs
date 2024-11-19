using System.Windows;
using System.Windows.Shapes;


namespace Tanks
{
    public class Base : Item
    {
        public bool isDestroyed { get; set; }
        public Base()
        {
            isDestroyed = false;
            width = 64;
            height = 59;
        }
        public override void DrawItem(Rectangle pic)
        {
            pic.Width = width;
            pic.Height = height;
            pic.Visibility = Visibility.Visible;
        }
    }
}