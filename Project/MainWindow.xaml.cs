using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Project
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> arguments = new Dictionary<string, string>();
        private static int CRnum = 20;
        private static int Minenum = (int)(CRnum * CRnum * 0.2);
        private static Cell[,] Elements;
        private static int notmines;
        public MainWindow()
        {
            string[] args = Environment.GetCommandLineArgs();
            for(int i=1; i<args.Length; i+=2)
            {
                args[i] = args[i].ToLower();
                if (args[i] == "-s" || args[i] == "--size")
                    arguments["s"] = args[i + 1];
                else if(args[i] == "-m" || args[i] == "--mines")
                    arguments["m"] = args[i + 1];
            }
            try
            {
                if (arguments.ContainsKey("s"))
                    CRnum = Math.Min(100, Convert.ToInt32(arguments["s"]));
                if (arguments.ContainsKey("m"))
                    Minenum = Math.Min((int)(CRnum * CRnum * 0.5),Math.Max((int)(CRnum * CRnum * 0.1),Convert.ToInt32(arguments["m"])));
            }
            catch (Exception e)
            {
                Console.WriteLine("Wrong argument");
                Console.WriteLine(e.Message);
            }
            Cell.setWindow(this);
            InitializeComponent();
        }
        async private void Endgame(bool win)
        {
            if(win)
            {
                this.Title = "YOU WIN!!!!!!!";
                await Task.Run(() =>
                {
                    Thread.Sleep(3000);
                });
                Close();
            }
            else
            {
                await Task.Run(() =>
                {
                    Thread.Sleep(2000);
                });
                New_Game(CRnum, Minenum);
            }
        }        
        private class Cell : Button
        {
            protected static MainWindow window;
            public static void setWindow(MainWindow _window)
            {
                window = _window;
            }
            public static int[,] coordinates = new int[,] { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 } };
            public bool is_open;
            public int state;
            protected static MainWindow w;
            protected int x, y;
            public Cell(MainWindow w,int x,int y)
            {
                if (Cell.w == null)
                    Cell.w = w;
                Focusable = false;

                this.x = x;
                this.y = y;
                is_open = false;
                state = 0;
            }
            protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
            {
                if (!is_open)
                    Open_me();
            }
            protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
            {
                if (is_open)
                    Open_near();
                else
                    Next_state();
            }
            public virtual bool Open_me()
            {
                if (is_open)
                    return true;
                if (state==1)
                    window.Title = (Convert.ToInt32(window.Title) + 1).ToString();
                is_open = true;
                state = -1;
                BorderBrush = Brushes.White;
                Style = (Style)Application.Current.Resources["DisabledButton"];
                if (--notmines == 0)
                {
                    w.Endgame(true);
                    return false;
                }
                for(int i=0;i<8;i++)
                    if (x + coordinates[i,0]>-1 && x + coordinates[i,0]<Elements.GetLength(0) &&
                        y + coordinates[i, 1] > -1 && y + coordinates[i, 1] < Elements.GetLength(1))
                        if (!Elements[x + coordinates[i, 0], y + coordinates[i, 1]].is_open && !Elements[x + coordinates[i,0],y + coordinates[i,1]].Open_me())
                            return false;
                return true;
            }
            public virtual void Open_near() { }
            public virtual void Count(ref int flags)
            {
                if (state == 1)
                    flags++;
            }
            public void Next_state()
            {
                state = (state + 1) % 3;
                switch (state)
                {
                    case 0:
                        Content = "";
                        break;
                    case 1:
                        Content = "F";
                        window.Title = (Convert.ToInt32(window.Title) - 1).ToString();
                        break;
                    case 2:
                        Content = "?";
                        window.Title = (Convert.ToInt32(window.Title) + 1).ToString();
                        break;
                }
            }
        }
        private class Mine : Cell
        {
            public Mine(MainWindow w, int x, int y) : base(w, x, y) { }
            public override bool Open_me()
            {
                Content = "X";
                w.Endgame(false);
                return false;
            }
        }
        private class Number : Cell
        {
            private int value;
            public Number(MainWindow w, int x, int y, int v) : base(w, x, y) 
            {
                value = v;
            }
            public override bool Open_me()
            {
                if (is_open)
                    return true;
                is_open = true;
                state = -1;
                BorderBrush = Brushes.White;
                Style = (Style)Application.Current.Resources["DisabledButton"];
                Content = value;
                if (--notmines == 0)
                {
                    w.Endgame(true);
                    return false;
                }
                return true;
            }
            public override void Open_near()
            {
                int flags = 0;
                for (int i = 0; i < 8; i++)
                    if (x + coordinates[i, 0] > -1 && x + coordinates[i, 0] < Elements.GetLength(0) &&
                        y + coordinates[i, 1] > -1 && y + coordinates[i, 1] < Elements.GetLength(1))
                        Elements[x + coordinates[i, 0], y + coordinates[i, 1]].Count(ref flags);
                if (flags == value)
                {
                    for (int i = 0; i < 8; i++)
                        if (x + coordinates[i, 0] > -1 && x + coordinates[i, 0] < Elements.GetLength(0) &&
                            y + coordinates[i, 1] > -1 && y + coordinates[i, 1] < Elements.GetLength(1) &&
                            Elements[x + coordinates[i, 0], y + coordinates[i, 1]].state != 1)
                            if (!Elements[x + coordinates[i, 0], y + coordinates[i, 1]].Open_me())
                                return;
                }
            }
        }
        private void MainPlate_Initialized(object sender, EventArgs e)
        {
            New_Game(CRnum, Minenum);
        }
        private void New_Game(int size=20, int mines=50)
        {
            Title = Minenum.ToString();
            notmines = size * size - mines;
            int r, x;
            MainPlate.ColumnDefinitions.Clear();
            MainPlate.RowDefinitions.Clear();
            MainPlate.Children.Clear();
            Elements = new Cell[size, size];
            int[] polygon = new int[size * size];
            Random rand = new Random();
            for (int i = 0; i < mines; i++)
                polygon[i] = -1;
            for (int i = mines; i < polygon.Length; i++)
            {
                r = rand.Next(0, i);
                x = polygon[r];
                polygon[r] = polygon[i];
                polygon[i] = x;
            }
            //
            for (int i = 0; i < size; i++)
            {
                MainPlate.RowDefinitions.Add(new RowDefinition());
                MainPlate.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    int p = j * size + i;
                    if (polygon[p] == -1)
                    {
                        for (int k = 0; k < 8; k++)
                            if (i + Cell.coordinates[k, 0] > -1 && i + Cell.coordinates[k, 0] < Elements.GetLength(0) &&
                                j + Cell.coordinates[k, 1] > -1 && j + Cell.coordinates[k, 1] < Elements.GetLength(1) &&
                                polygon[p + size * Cell.coordinates[k, 1] + Cell.coordinates[k, 0]] != -1)
                                polygon[p + size* Cell.coordinates[k, 1] + Cell.coordinates[k, 0]]++;
                    }
                }
            var starts = new List<Tuple<int,int>>();
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    if (polygon[i * size + j] == -1)
                        Elements[i, j] = new Mine(this, i, j);
                    else if (polygon[i * size + j] == 0)
                    {
                        Elements[i, j] = new Cell(this, i, j);
                        starts.Add(new Tuple<int, int>(i, j));
                    }
                    else
                        Elements[i, j] = new Number(this, i, j, polygon[i * size + j]);
                    MainPlate.Children.Add(Elements[i, j]);
                    Grid.SetColumn(Elements[i, j], i);
                    Grid.SetRow(Elements[i, j], j);
                }
            r = rand.Next(0, starts.Count());
            Elements[starts[r].Item1, starts[r].Item2].Open_me();
        }
    }
}
