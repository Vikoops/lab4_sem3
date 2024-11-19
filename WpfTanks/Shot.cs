using System;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Tanks
{
    public class Shot : Item
    {
        public int reload;
        public Tuple<int, int> coordinates { get; set; }
        private Tuple<int, int> direction;
        int speed { get; set; }
        public Shot()
        {
            reload = 0;
            speed = 6;
            width = 23;
            height = 23;
            direction = new Tuple<int, int>(0, 0);
            coordinates = new Tuple<int, int>(-50, -50);
        }
        public void Reloading()
        {
            reload++;
        }
        public bool isVisible(Tuple<int, int> coord)
        {
            if (coord.Item1 > 1140 || coord.Item2 > 740 || coord.Item1 < 1 || coord.Item2 < 0)
            {
                return false;
            }
            else
                return true;
        }
        public void Destroy()
        {
            Reloading();
            coordinates = new Tuple<int, int>(-50, -50);
            direction = new Tuple<int, int>(0, 0);
        }
        public void Movement()
        {
            int X = coordinates.Item1;
            int Y = coordinates.Item2;
            if (coordinates.Item1 > -10)
                coordinates = new Tuple<int, int>(X + direction.Item1 * speed, Y + direction.Item2 * speed);
        }
        public void Spawn(Tuple<int, int> dir, Tuple<int, int> p1)
        {
            direction = dir;
            coordinates = new Tuple<int, int>(p1.Item1, p1.Item2);
        }
        public override void DrawItem(Rectangle pic)
        {
            Canvas.SetLeft(pic, coordinates.Item1);
            Canvas.SetTop(pic, coordinates.Item2);
            /*item.DrawImage(img, new Rect(coordinates.Item1, coordinates.Item2, width, height));*/
        }
    }
}
