using System.Windows;
using System.Windows.Shapes;


namespace Tanks
{

    public class Barrier : Item
    {
        public Barrier()
        {
            width = 65;
            height = 64;
        }
        public override void DrawItem(Rectangle pic)
        {
            pic.Width = width;
            pic.Height = height;
            pic.Visibility = Visibility.Visible;
        }
    }
}
