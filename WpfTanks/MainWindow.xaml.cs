using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Tanks
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       /* string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string folderPath = Path.Combine(basePath, "Resources");*/
        private DispatcherTimer timer1;
        private List<Point> startPos = new List<Point>();
        private int animationMode = 3;
        private bool isPressedKey = false;
        private List<Rectangle> tanks = new List<Rectangle>();
        private List<Rectangle> bricks = new List<Rectangle>();
        private List<Rectangle> walls = new List<Rectangle>();
        private List<Rectangle> bases = new List<Rectangle>();
        private Barrier brick = new Barrier();
        private Base Base = new Base();
        private PlayerTank player = new PlayerTank();
        private List<Tank> all = new List<Tank>();
        private string TanksControls = @"..\\..\\Resources\\TanksControls.json";
        private string BricksContorls = @"..\\..\\Resources\\BricksControls.json";
        private string WallsControls = @"..\\..\\Resources\\WallsControls.json";
        private string BaseControl = @"..\\..\\Resources\\BaseControl.json";
        private string PlayerTankInfo = @"..\\..\\Resources\\PlayerTankInfo.json";
        private string EnemyTanksInfo = @"..\\..\\Resources\\EnemyTankInfo.json";
        List<Rectangle> bullets = new List<Rectangle>();

        public MainWindow() // инициализация всех компонентов формы WPF
        {
            InitializeComponent();

            foreach (var item in GameField.Children)
            {
                if (item is Rectangle)
                {
                    if (((Rectangle)item).Width == 65)
                    {
                        ImageBrush imageControl = new ImageBrush();
                        BitmapImage img = new BitmapImage(new Uri(@"..\\..\\Resources\\brick.png", UriKind.Relative));
                        imageControl.ImageSource = img;
                        imageControl.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                        ((Rectangle)item).Fill = imageControl;
                    }
                    else if (((Rectangle)item).Width == 64 && ((Rectangle)item).Height != 59)
                    {
                        ImageBrush imageControl = new ImageBrush();
                        BitmapImage img = new BitmapImage(new Uri(@"..\\..\\Resources\\wall.png", UriKind.Relative));
                        imageControl.ImageSource = img;
                        imageControl.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                        ((Rectangle)item).Fill = imageControl;
                    }
                    else if (((Rectangle)item).Height == 59)
                    {
                        ImageBrush imageControl = new ImageBrush();
                        BitmapImage img = new BitmapImage(new Uri(@"..\\..\\Resources\\base.png", UriKind.Relative));
                        imageControl.ImageSource = img;
                        imageControl.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                        ((Rectangle)item).Fill = imageControl;
                    }
                    else if (((Rectangle)item) == player1)
                    {
                        ImageBrush imageControl = new ImageBrush();
                        BitmapImage img = new BitmapImage(new Uri(@"..\\..\\Resources\\playerTankUp.png", UriKind.Relative));
                        imageControl.ImageSource = img;
                        imageControl.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
                        ((Rectangle)item).Fill = imageControl;
                    }
                    else
                    {
                        ImageBrush imageControl = new ImageBrush();
                        BitmapImage img = new BitmapImage(new Uri(@"..\\..\\Resources\\CompTankDown.png", UriKind.Relative));
                        imageControl.ImageSource = img;
                        imageControl.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
                        ((Rectangle)item).Fill = imageControl;
                    }
                }
            }

            startPos.Add(new Point(Canvas.GetLeft(player1), Canvas.GetTop(player1)));
            startPos.Add(new Point(Canvas.GetLeft(computer1), Canvas.GetTop(computer1)));
            startPos.Add(new Point(Canvas.GetLeft(computer2), Canvas.GetTop(computer2)));
            startPos.Add(new Point(Canvas.GetLeft(computer3), Canvas.GetTop(computer3)));

            if (File.ReadAllText(TanksControls).Length > 0)
                ReDraw();
            else
            {
                DrawMap();
                player = new PlayerTank();
                all.Add(player);
                ComputerTank enemy;
                enemy = new ComputerTank();
                enemy.Mode = 1;
                all.Add(enemy);
                enemy = new ComputerTank();
                enemy.Mode = 1;
                all.Add(enemy);
                enemy = new ComputerTank();
                all.Add(enemy);
                tanks.Add(player1);
                tanks.Add(computer1);
                tanks.Add(computer2);
                tanks.Add(computer3);
                for (int i = 0; i < all.Count; i++)
                {
                    BitmapImage texture = new BitmapImage(new Uri(@"..\\..\\Resources\\Bullet.png", UriKind.Relative));
                    ImageBrush textureImage = new ImageBrush();
                    textureImage.ImageSource = texture;
                    bullets.Add(new Rectangle());
                    bullets[i].Width = 23;
                    bullets[i].Height = 23;
                    bullets[i].Fill = textureImage;
                    GameField.Children.Add(bullets[i]);
                }
            }

            this.Closing += new System.ComponentModel.CancelEventHandler(Form_FormClosing);
            this.KeyDown += new KeyEventHandler(keyboard);
            this.KeyUp += new KeyEventHandler(freeKey);

            timer1 = new DispatcherTimer();
            timer1.Interval = TimeSpan.FromMilliseconds(15);
            timer1.Tick += new EventHandler(update);
            new Thread(() =>
            {
                timer1.Start();
            }).Start();

            this.Loaded += (object sender, RoutedEventArgs e) => // для быстрой отрисовки объектов
            {
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
                RenderOptions.SetEdgeMode(this, EdgeMode.Unspecified);
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            };

            PlayerTankAnimation();
            EnemyTankAnimation();
        }
        private Point GetEmptySpace(List<Rectangle> tanks) // алгоритм для корректного возраждения вражеского танка
        {
            Point tup = new Point(1, 1);
            bool flag = true;
            bool conflict;
            while (flag)
            {
                conflict = false;
                foreach (Rectangle tank in tanks)
                {
                    Rect collision = new Rect(Canvas.GetLeft(tank), Canvas.GetTop(tank), tank.Width, tank.Height);
                    if (collision.IntersectsWith(new Rect(tup, new Size(60, 60))))
                    {
                        tup = new Point(tup.X + 60, tup.Y);
                        conflict = true;
                    }
                }
                if (!conflict)
                    flag = false;
            }
            return tup;
        }
        private void keyboard(object sender, KeyEventArgs e) // логика для клавиш
        {
            switch (e.Key)
            {
                case Key.D:
                    animationMode = 1;
                    isPressedKey = true;
                    break;
                case Key.A:
                    animationMode = 2;
                    isPressedKey = true;
                    break;
                case Key.W:
                    animationMode = 3;
                    isPressedKey = true;
                    break;
                case Key.S:
                    animationMode = 4;
                    isPressedKey = true;
                    break;
                case Key.Space:
                    if (player.bullet.coordinates.Item1 < 0)
                        player.bullet.Spawn(player.GetDirection(), player.GetFirePosition(player1));
                    break;
            }
        }
        private void freeKey(object sender, KeyEventArgs e) // функция, которая отслеживает, нажата ли клавиша
        {
            isPressedKey = false;
        }
        public void EnemyTankAnimation() // отрисовка вражеских танков
        {
            ((ComputerTank)all[1]).DrawItem(computer1, bullets[1]);
            ((ComputerTank)all[2]).DrawItem(computer2, bullets[2]);
            ((ComputerTank)all[3]).DrawItem(computer3, bullets[3]);
        }
        public void PlayerTankAnimation() // отрисовка игрока
        {
            player.DrawItem(player1);
        }
        private void EnemyTankMovement() // алгоритм движения вражеских танков
        {
            if (((ComputerTank)all[1]).isStoped && !((ComputerTank)all[1]).isDead)
                ((ComputerTank)all[1]).Movement(computer1, GameField.Children);
            else if (!all[1].isDead)
                ((ComputerTank)all[1]).Movement(computer1);

            if (((ComputerTank)all[2]).isStoped && !((ComputerTank)all[2]).isDead)
                ((ComputerTank)all[2]).Movement(computer2, GameField.Children);
            else if (!all[2].isDead)
                ((ComputerTank)all[2]).Movement(computer2);

            if (((ComputerTank)all[3]).isStoped && !((ComputerTank)all[3]).isDead)
                ((ComputerTank)all[3]).Movement(computer3, GameField.Children);
            else if (!all[3].isDead)
                ((ComputerTank)all[3]).Movement(computer3);
        }
        private void Form_FormClosing(object sender, System.ComponentModel.CancelEventArgs e) // сохранение игры при закрытии формы
        {
            List<Point> adder = new List<Point>();
            foreach (Rectangle pic in tanks)
            {
                adder.Add(new Point((int)Canvas.GetLeft(pic), (int)Canvas.GetTop(pic)));
            }
            string json1 = JsonSerializer.Serialize<List<Point>>(adder);
            adder = new List<Point>();
            foreach (Rectangle pic in bricks)
            {
                adder.Add(new Point((int)Canvas.GetLeft(pic), (int)Canvas.GetTop(pic)));
            }
            string json2 = JsonSerializer.Serialize<List<Point>>(adder);
            adder = new List<Point>();
            foreach (Rectangle pic in walls)
            {
                adder.Add(new Point(Canvas.GetLeft(pic), Canvas.GetTop(pic)));
            }
            string json3 = JsonSerializer.Serialize<List<Point>>(adder);
            adder = new List<Point>();
            foreach (Rectangle pic in bases)
            {
                adder.Add(new Point(Canvas.GetLeft(pic), Canvas.GetTop(pic)));
            }
            string json4 = JsonSerializer.Serialize<List<Point>>(adder);
            adder = new List<Point>();
            player = (PlayerTank)all[0];
            string json5 = JsonSerializer.Serialize<PlayerTank>(player);
            List<ComputerTank> comp = new List<ComputerTank>();
            comp.Add((ComputerTank)all[1]);
            comp.Add((ComputerTank)all[2]);
            comp.Add((ComputerTank)all[3]);
            string json6 = JsonSerializer.Serialize<List<ComputerTank>>(comp);
            File.WriteAllText(TanksControls, json1);
            File.WriteAllText(BricksContorls, json2);
            File.WriteAllText(WallsControls, json3);
            File.WriteAllText(BaseControl, json4);
            File.WriteAllText(PlayerTankInfo, json5);
            File.WriteAllText(EnemyTanksInfo, json6);
        }
        public void DrawMap() // отрисовка карты
        {
            brick = new Barrier();
            //wall = new Wall();
            foreach (var item in GameField.Children)
                if (item is Rectangle)
                {
                    if (!((Rectangle)item).IsVisible)
                        ((Rectangle)item).Visibility = Visibility.Visible;
                    if (((Rectangle)item).Width == 65)
                    {
                        brick.DrawItem((Rectangle)item);
                        bricks.Add((Rectangle)item);
                    }
                    if (((Rectangle)item).Width == 64 && ((Rectangle)item).Height != 59)
                    {
                        
                        walls.Add((Rectangle)item);
                    }
                    if (((Rectangle)item).Height == 59)
                    {
                        Base.DrawItem((Rectangle)item);
                        bases.Add((Rectangle)item);
                    }
                }
        }
        private void ReDraw() // зарузка последней сохраненной игры
        {
            string json1 = File.ReadAllText(TanksControls);
            string json2 = File.ReadAllText(BricksContorls);
            string json3 = File.ReadAllText(WallsControls);
            string json4 = File.ReadAllText(BaseControl);
            string json5 = File.ReadAllText(PlayerTankInfo);
            string json6 = File.ReadAllText(EnemyTanksInfo);

            List<Point> tankscon = new List<Point>();
            List<Point> brickscon = new List<Point>();
            List<Point> wallscon = new List<Point>();
            List<Point> basecon = new List<Point>();

            if (json1.Length > 0)
                tankscon = JsonSerializer.Deserialize<List<Point>>(json1);
            if (json2.Length > 0)
                brickscon = JsonSerializer.Deserialize<List<Point>>(json2);
            if (json3.Length > 0)
                wallscon = JsonSerializer.Deserialize<List<Point>>(json3);
            if (json4.Length > 0)
                basecon = JsonSerializer.Deserialize<List<Point>>(json4);
            if (json5.Length > 0)
                all.Add(JsonSerializer.Deserialize<PlayerTank>(json5));
            if (json6.Length > 0)
            {
                all.AddRange(JsonSerializer.Deserialize<List<ComputerTank>>(json6));
            }

            Canvas.SetLeft(player1, tankscon[0].X);
            Canvas.SetTop(player1, tankscon[0].Y);
            Canvas.SetLeft(computer1, tankscon[1].X);
            Canvas.SetTop(computer1, tankscon[1].Y);
            Canvas.SetLeft(computer2, tankscon[2].X);
            Canvas.SetTop(computer2, tankscon[2].Y);
            Canvas.SetLeft(computer3, tankscon[3].X);
            Canvas.SetTop(computer3, tankscon[3].Y);

            tanks.Add(player1);
            tanks.Add(computer1);
            tanks.Add(computer2);
            tanks.Add(computer3);
            player = (PlayerTank)all[0];
            player.bullet = new Shot();
            all[1].bullet = new Shot();
            all[2].bullet = new Shot();
            all[3].bullet = new Shot();

            foreach (var item in GameField.Children)
            {
                if (item is Rectangle)
                {
                    if (((Rectangle)item).Width == 65 && brickscon.Contains(new Point(Canvas.GetLeft(((Rectangle)item)), Canvas.GetTop(((Rectangle)item)))))
                    {
                        brick.DrawItem((Rectangle)item);
                        bricks.Add((Rectangle)item);
                    }
                    else if (((Rectangle)item).Width == 64 && ((Rectangle)item).Height != 59 && wallscon.Contains(new Point(Canvas.GetLeft(((Rectangle)item)), Canvas.GetTop(((Rectangle)item)))))
                    {
                        
                        walls.Add((Rectangle)item);
                    }
                    else if (((Rectangle)item).Height == 59 && basecon.Contains(new Point(Canvas.GetLeft(((Rectangle)item)), Canvas.GetTop(((Rectangle)item)))))
                    {
                        Base.DrawItem((Rectangle)item);
                        bases.Add((Rectangle)item);
                    }
                    else if (((Rectangle)item).Height != 60)
                        ((Rectangle)item).Visibility = Visibility.Hidden;
                }
            }
            for (int i = 0; i < all.Count; i++)
            {
                BitmapImage texture = new BitmapImage(new Uri(@"..\\..\\Resources\\Bullet.png", UriKind.Relative));
                ImageBrush textureImage = new ImageBrush();
                textureImage.ImageSource = texture;
                bullets.Add(new Rectangle());
                bullets[i].Width = 23;
                bullets[i].Height = 23;
                bullets[i].Fill = textureImage;
                GameField.Children.Add(bullets[i]);
            }
            tankscon = null;
            brickscon = null;
            wallscon = null;
            basecon = null;
        }
        private void Start() // функция для перезапуска при поражении
        {
            Base.isDestroyed = false;
            Canvas.SetLeft(player1, startPos[0].X);
            Canvas.SetTop(player1, startPos[0].Y);
            Canvas.SetLeft(computer1, startPos[1].X);
            Canvas.SetTop(computer1, startPos[1].Y);
            Canvas.SetLeft(computer2, startPos[2].X);
            Canvas.SetTop(computer2, startPos[2].Y);
            Canvas.SetLeft(computer3, startPos[3].X);
            Canvas.SetTop(computer3, startPos[3].Y);

            tanks = new List<Rectangle>();
            bricks = new List<Rectangle>();
            walls = new List<Rectangle>();
            bases = new List<Rectangle>();
            all = new List<Tank>();
            DrawMap();
            Canvas.SetLeft(bullets[0], -60);
            Canvas.SetTop(bullets[0], -60);
            player = new PlayerTank();
            all.Add(player);
            ComputerTank enemy;
            enemy = new ComputerTank();
            enemy.Mode = 1;
            all.Add(enemy);
            enemy = new ComputerTank();
            enemy.Mode = 1;
            all.Add(enemy);
            enemy = new ComputerTank();
            all.Add(enemy);
            tanks.Add(player1);
            tanks.Add(computer1);
            tanks.Add(computer2);
            tanks.Add(computer3);
            PlayerTankAnimation();
            EnemyTankAnimation();
        }
        private void update(object sender, EventArgs e) // обновление формы (анимация объектов)
        {
            InvalidateVisual();

            if (Base.isDestroyed || player.isDead) // проверяем, можно ли продолжать игру
            {
                Start();
                return;
            }
            player.Mode = animationMode;
            if (isPressedKey && player.isValidMove(player.coordinates)) // фиксируем нажатую пользователем кнопку для движения танка
            {
                switch (animationMode)
                {
                    case 1:
                        if (player.isNoBarrier(all))
                        {
                            Canvas.SetLeft(player1, Canvas.GetLeft(player1) + player.Speed);
                            Canvas.SetTop(player1, Canvas.GetTop(player1));
                        }
                        break;
                    case 2:
                        if (player.isNoBarrier(all))
                        {
                            Canvas.SetLeft(player1, Canvas.GetLeft(player1) - player.Speed);
                            Canvas.SetTop(player1, Canvas.GetTop(player1));
                        }
                        break;
                    case 3:
                        if (player.isNoBarrier(all))
                        {
                            Canvas.SetLeft(player1, Canvas.GetLeft(player1));
                            Canvas.SetTop(player1, Canvas.GetTop(player1) - player.Speed);
                        }
                        break;
                    case 4:
                        if (player.isNoBarrier(all))
                        {
                            Canvas.SetLeft(player1, Canvas.GetLeft(player1));
                            Canvas.SetTop(player1, Canvas.GetTop(player1) + player.Speed);
                        }
                        break;
                }
            }
            for (int i = 0; i < tanks.Count; i++) // проверка столкновений танков с внутриигровыми объектами
            {
                Rect tankCollision = new Rect(Canvas.GetLeft(tanks[i]), Canvas.GetTop(tanks[i]), tanks[i].Width, tanks[i].Height);
                int mode = 0;
                if (tanks[i] == computer1)
                    mode = all[1].Mode;
                else if (tanks[i] == computer2)
                    mode = all[2].Mode;
                else if (tanks[i] == computer3)
                    mode = all[3].Mode;
                else
                    mode = player.Mode;
                foreach (Rectangle br in bricks)
                {
                    Rect collision = new Rect(Canvas.GetLeft(br), Canvas.GetTop(br), br.Width, br.Height);
                    if (collision.IntersectsWith(tankCollision))
                    {
                        if (mode == 1)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]) - 2);
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]));
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                        if (mode == 2)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]) + 2);
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]));
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                        if (mode == 3)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]));
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]) + 2);
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                        if (mode == 4)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]));
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]) - 2);
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                    }
                }
                foreach (Rectangle wl in walls)
                {
                    Rect collision = new Rect(Canvas.GetLeft(wl), Canvas.GetTop(wl), wl.Width, wl.Height);
                    if (collision.IntersectsWith(tankCollision))
                    {
                        if (mode == 1)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]) - 2);
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]));
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                        if (mode == 2)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]) + 2);
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]));
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                        if (mode == 3)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]));
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]) + 2);
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                        if (mode == 4)
                        {
                            Canvas.SetLeft(tanks[i], Canvas.GetLeft(tanks[i]));
                            Canvas.SetTop(tanks[i], Canvas.GetTop(tanks[i]) - 2);
                            if (all[i] is ComputerTank)
                                ((ComputerTank)all[i]).isStoped = true;
                            break;
                        }
                    }
                }
            }

            foreach (Tank t in all) // если пришло время для возрождения, алгоритм находит нужное место для появления танка
            {
                if (t is ComputerTank && t.isDead)
                    if (((ComputerTank)t).restoreTime > 399)
                        ((ComputerTank)t).restoreCoord = GetEmptySpace(tanks);
            }

            for (int i = 0; i < tanks.Count; i++) // регистрация столкновения между танками
            {
                Rect comparison = new Rect();
                for (int j = 0; j < tanks.Count; j++)
                {
                    if (all[j].isDead || all[i].isDead)
                        break;
                    if (all[j] is ComputerTank)
                        comparison = all[j].NextRadius;
                    else
                        comparison = all[j].Radius;
                    if (tanks[i] == tanks[j])
                        continue;
                    else if (!all[i].NextRadius.IntersectsWith(comparison))
                        continue;
                    else if (all[i] is ComputerTank)
                    {
                        ((ComputerTank)all[i]).isStoped = true;
                        if (all[j] is ComputerTank)
                        {
                            ((ComputerTank)all[j]).isStoped = true;
                        }
                    }
                    else
                    {
                        ((ComputerTank)all[j]).isStoped = true;
                    }
                    break;
                }
            }
            foreach (Tank item in all) // регистрация попадания снаряда 
            {
                if (item.bullet.coordinates != null && item.bullet.coordinates.Item1 > 0)
                {
                    if (item is PlayerTank)
                    {
                        item.bullet.DrawItem(bullets[0]);
                        item.bullet.Movement();
                    }
                    foreach (Rectangle brick in bricks)
                    {
                        Rect collision = new Rect(Canvas.GetLeft(brick), Canvas.GetTop(brick), brick.Width, brick.Height);
                        if (collision.IntersectsWith(new Rect(new Point(item.bullet.coordinates.Item1, item.bullet.coordinates.Item2), new Size(23, 23))))
                        {
                            item.bullet.Destroy();
                            brick.Visibility = Visibility.Hidden;
                            bricks.Remove(brick);
                            break;
                        }
                    }
                    foreach (Rectangle wall in walls)
                    {
                        Rect collision = new Rect(Canvas.GetLeft(wall), Canvas.GetTop(wall), wall.Width, wall.Height);
                        if (collision.IntersectsWith(new Rect(new Point(item.bullet.coordinates.Item1, item.bullet.coordinates.Item2), new Size(23, 23))))
                        {
                            item.bullet.Destroy();
                            break;
                        }
                    }
                    foreach (Rectangle b in bases)
                    {
                        Rect collision = new Rect(Canvas.GetLeft(b), Canvas.GetTop(b), b.Width, b.Height);
                        if (collision.IntersectsWith(new Rect(new Point(item.bullet.coordinates.Item1, item.bullet.coordinates.Item2), new Size(23, 23))))
                        {
                            item.bullet.Destroy();
                            b.Visibility = Visibility.Hidden;
                            bases.Remove(b);
                            Base.isDestroyed = true;
                            break;
                        }
                    }
                    foreach (Rectangle en in tanks)
                    {
                        Rect collision = new Rect(Canvas.GetLeft(en), Canvas.GetTop(en), en.Width, en.Height);
                        if (collision.IntersectsWith(new Rect(new Point(item.bullet.coordinates.Item1, item.bullet.coordinates.Item2), new Size(23, 23))))
                        {
                            if ((en == computer1 || en == computer2 || en == computer3) && item is ComputerTank)
                            {
                                item.bullet.Destroy();
                                break;
                            }
                            item.bullet.Destroy();
                            Canvas.SetLeft(bullets[0], -60);
                            Canvas.SetTop(bullets[0], -60);
                            Canvas.SetLeft(en, -60);
                            Canvas.SetTop(en, -60);
                            break;
                        }
                    }
                    if (!item.bullet.isVisible(item.bullet.coordinates))
                    {
                        item.bullet.Destroy();
                        if (item is PlayerTank)
                        {
                            Canvas.SetLeft(bullets[0], -60);
                            Canvas.SetTop(bullets[0], -60);
                        }
                    }
                }
            }
            EnemyTankAnimation();
            EnemyTankMovement();
            PlayerTankAnimation();
        }
    }
}