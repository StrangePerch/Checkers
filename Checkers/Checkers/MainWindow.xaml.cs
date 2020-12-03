using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Controls.Primitives;

namespace Checkers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public enum State
    {
        Empty,
        White,
        Black,
        MegaWhite,
        MegaBlack
    };

    public struct Cords
    {
        public int X, Y;

        public Cords(int x, int y)
        {
            X = x;
            Y = y;
        }
    };

    public static class Colors
    {
        public static Brush Color1 = Brushes.DimGray;
        public static Brush Color2 = Brushes.LightGray;
    }

    public class Move : ICloneable
    {
        public int FromX, FromY;
        public int x, y;
        public List<Cords> eat = new List<Cords>();
        private static Label PrevFrom;
        private static Label PrevTo;
        private static List<Label> PrevEat = new List<Label>();

        public Move(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public void Add(Cords toAdd)
        {
            eat.Add(toAdd);
        }

        

        public void Show(UniformGrid MainGrid)
        {
            foreach (var cord in eat)
            {
                (MainGrid.Children[cord.X * 10 + cord.Y] as Label).Background = Brushes.Red;
            }
            
            (MainGrid.Children[x * 10 + y] as Label).Background = Brushes.Green;
            
        }

        public void Hide(UniformGrid MainGrid)
        {
            foreach (var cord in eat)
            {
                if((cord.X + cord.Y) % 2 == 1)
                    (MainGrid.Children[cord.X * 10 + cord.Y] as Label).Background = Colors.Color1;
                else
                    (MainGrid.Children[cord.X * 10 + cord.Y] as Label).Background = Colors.Color2;

            }

            if ((x + y) % 2 == 1)
                (MainGrid.Children[x * 10 + y] as Label).Background = Colors.Color1;
            else
                (MainGrid.Children[x * 10 + y] as Label).Background = Colors.Color2;

        }

        public void Eat(State[,] Map, UniformGrid MainGrid)
        {
            foreach (var t in PrevEat)
            {
                t.BorderThickness = new Thickness(0);
            }
            PrevEat.Clear();
            foreach (var cord in eat)
            {
                Map[cord.X, cord.Y] = State.Empty;
                    var label = (MainGrid.Children[cord.X * 10 + cord.Y] as Label);
                label.Content = null;
                label.BorderBrush = Brushes.Red;
                label.BorderThickness = new Thickness(3);
                PrevEat.Add(label);
            }
        }

        public void Turn(State[,] Map, UniformGrid MainGrid)
        {
            Map[x, y] =  Map[FromX, FromY];
            Map[FromX, FromY] = State.Empty;
            var from = (MainGrid.Children[FromX * 10 + FromY] as Label);
            var to = (MainGrid.Children[x * 10 + y] as Label);
            to.Content = from.Content;
            from.Content = null;

            if (Move.PrevFrom != null)
                PrevFrom.BorderThickness = new Thickness(0);

            if (PrevTo != null)
                PrevTo.BorderThickness = new Thickness(0);

            from.BorderBrush = Brushes.Red;
            from.BorderThickness = new Thickness(3);
            to.BorderBrush = Brushes.Red;
            to.BorderThickness = new Thickness(3);
            PrevFrom = from;
            PrevTo = to;
        }

        public object Clone()
        {
            Move clone = new Move(x,y);
            clone.eat = new List<Cords>(eat);
            return clone;
        }
    }

    public partial class MainWindow : Window
    {
        private State[,] Map = new State[10,10];
        private Image Drag;

        private State turn;
        private State Turn
        {
            set
            {
                turn = value;
                if (Turn == State.White)
                    TurnLabel.Content = Strings.turn_white;
                else
                    TurnLabel.Content = Strings.turn_black;
            }
            get => turn;
        }

        private int FromX;
        private int FromY;
        private List<Move> Moves = new List<Move>();

        void SetLanguage()
        {
            if(DifficultyLabel != null)
                DifficultyLabel.Content = Strings.difficulty;
            if (TurnLabel != null)
                TurnLabel.Content = Strings.turn_white;
        }

        public MainWindow()
        {
            InitializeComponent();
            
            Turn = State.White;

            
            ChooseLanguage.Text = Thread.CurrentThread.CurrentUICulture.Name.ToUpper();
            SetLanguage();

            for (var i = 0; i < 10; i++)
            for (var j = 0; j < 10; j++)
            {
                var temp = new Label();
                Grid.SetColumn(temp, i);
                Grid.SetRow(temp, j);
                var img = new Image();
                temp.AllowDrop = true;
                temp.MouseDown += DragStart;
                temp.Drop += TargetDrop;
                temp.MouseEnter += ShowMoves;
                temp.MouseLeave += HideMoves;

                Map[i,j] = State.Empty;
               
                    if ((i + j) % 2 == 1)
                    {
                        temp.Background = Colors.Color1;
                        if (i <= 3)
                        {
                            img.Source = new BitmapImage(
                                new Uri(AppDomain.CurrentDomain.BaseDirectory +
                                        "black.png", UriKind.Absolute));
                            temp.Content = img;
                            Map[i, j] = State.Black;
                        }
                        else if (i >= 6)
                        {
                            img.Source = new BitmapImage(
                                new Uri(AppDomain.CurrentDomain.BaseDirectory +
                                        "white.png", UriKind.Absolute));
                            temp.Content = img;
                            Map[i, j] = State.White;
                        }
                    }
                    else
                    {
                        temp.Background = Colors.Color2;
                    }
                
                    MainGrid.Children.Add(temp);
            }
        }


        private void DragStart(object sender, MouseButtonEventArgs e)
        {
            var obj = sender as Label;
            if (obj?.Content == null) return;
            int col, row;
            col = Grid.GetColumn(obj);
            row = Grid.GetRow(obj);
            if (Turn == State.White && (Map[col, row] == State.White || Map[col, row] == State.MegaWhite))
            {
                FromX = col;
                FromY = row;
                Drag = obj.Content as Image;
                DragDrop.DoDragDrop(obj, obj.Content, DragDropEffects.Copy);
                obj.Content = Drag;
                if(turn == State.Black)
                    PCTurn();
            }
        }

        private void TargetDrop(object sender, DragEventArgs e)
        {
            var obj = sender as Label;
            if (obj?.Content != null) return;
            var col = Grid.GetColumn(obj);
            var row = Grid.GetRow(obj);
            foreach (var move in GetMoves(new Move(FromX, FromY), Map))
            {
                if (move.x == col && move.y == row)
                {
                    if (Map[FromX, FromY] == State.White)
                        Turn = State.Black;
                    else
                        Turn = State.White;
                    Map[col, row] = Map[FromX, FromY];
                    Map[FromX, FromY] = State.Empty;
                    obj.Content = Drag;
                    Drag = null;

                    move.Eat(Map,MainGrid);
                    Mega_Check();
                    return;
                }
            }
        }

        private void Mega_Check()
        {
            for (int i = 0; i < 10; i++)
            {
                if (Map[0, i] == State.White)
                {
                    Map[0, i] = State.MegaWhite;

                    Image img = new Image();
                    img.Source = new BitmapImage(
                        new Uri(AppDomain.CurrentDomain.BaseDirectory +
                                "white_mega.png", UriKind.Absolute));
                    (MainGrid.Children[i] as Label).Content = img;
                }

                if (Map[9, i] == State.Black)
                {
                    Map[9, i] = State.MegaBlack;

                    Image img = new Image();
                    img.Source = new BitmapImage(
                        new Uri(AppDomain.CurrentDomain.BaseDirectory +
                                "black_mega.png", UriKind.Absolute));
                    (MainGrid.Children[90 + i] as Label).Content = img;
                }
            }
        }

        private void PCTurn()
        {

            var moves = GetAllMovesOfSide(Map, State.Black);
            if (moves.Count > 0)
            {
                var move = GetBestMove(moves);

                move.Turn(Map, MainGrid);
                move.Eat(Map, MainGrid);
                
            }

            turn = State.White;
            Mega_Check();

        }

        private Move GetBestMove(List<Move> moves)
        {
            if (moves.Count == 0) return null;
            Move move = moves[0];
            double score = GetScore(move, Convert.ToInt32(DifficultySlider.Value), Map);

            foreach (var tempMove in moves)
            {
                double temp_score = GetScore(tempMove, Convert.ToInt32(DifficultySlider.Value), Map);
                if (temp_score > score)
                {
                    move = tempMove;
                    score = temp_score;
                }
            }

            Score.Content = score;

            return move;
        }

        private double GetScore(Move Move, int depth, State[,] map)
        {
            State[,] MapClone = map.Clone() as State[,];
            State you = MapClone[Move.FromX, Move.FromY];
            MapClone[Move.x, Move.y] = MapClone[Move.FromX, Move.FromY];
            MapClone[Move.FromX, Move.FromY] = State.Empty;

            foreach (var move in Move.eat)
            {
                MapClone[move.X, move.Y] = State.Empty;
            }

            double score = Move.eat.Count;

            if (depth == 0)
                return score;

            State enemy;
            if (you == State.White) enemy = State.Black;
            else enemy = State.White;

            double enemy_score = -100;
            var enemy_moves = GetAllMovesOfSide(MapClone, enemy);

            foreach (Move enemy_move in enemy_moves)
            {
                var temp_score = GetScore(enemy_move, depth - 1, MapClone);
                if (temp_score > enemy_score) enemy_score = temp_score;
            }

            score -= enemy_score;


            return score;
        }

        private List<Move> GetAllMovesOfSide(State[,] map, State side)
        {
            List<Move> list = new List<Move>();

            State mega;
            if (side == State.Black) mega = State.MegaBlack;
            else mega = State.MegaWhite;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (map[i, j] == side || map[i, j] == mega)
                    {
                        list.AddRange(GetMoves(new Move(i, j), map));
                    }

                }
            }

            return list;
        }

        private void ShowMoves(object sender, MouseEventArgs e)
        {
            var obj = sender as Label;
            if (obj?.Content == null) return;
            int col, row;
            col = Grid.GetColumn(obj);
            row = Grid.GetRow(obj);
            Moves = GetMoves(new Move(col,row),Map);
            foreach (var cord in Moves)
            {
                cord.Show(MainGrid);
                //(MainGrid.Children[cord.x * 10 + cord.y] as Label).Content =
                //    GetScore(cord, Convert.ToInt32(DifficultySlider.Value), Map);
            }
        }

        private void HideMoves(object sender, MouseEventArgs e)
        {
            for (var i = 0; i < 10; i++)
            for (var j = 0; j < 10; j++)
            {
               //// if ((MainGrid.Children[i * 10 + j] as Label).Content is double)
               //// {
               ////     (MainGrid.Children[i * 10 + j] as Label).Content = null;
               //// }
                if ((i + j) % 2 == 1)
                    (MainGrid.Children[i * 10 + j] as Label).Background = Colors.Color1;
                else
                    (MainGrid.Children[i * 10 + j] as Label).Background = Colors.Color2;
            }

        }

        private List<Move> GetWalkMoves(Move cords, State[,] map)
        {
            List<Move> moves = new List<Move>();
            int col = cords.x;
            int row = cords.y;

            State you = map[col, row];
            if (you == State.Empty) return moves;
            State enemy;
            if (you == State.White) enemy = State.Black;
            else enemy = State.White;

            if (you == State.White)
            {
                if (col > 0)
                {
                    if (row < 9 && map[col - 1, row + 1] == State.Empty)
                        moves.Add(new Move(col - 1, row + 1));
                    if (row > 0 && map[col - 1, row - 1] == State.Empty)
                        moves.Add(new Move(col - 1, row - 1));
                }
            }
            else if (you == State.Black)
            {
                if (col < 9)
                {
                    if (row < 9 && map[col + 1, row + 1] == State.Empty)
                        moves.Add(new Move(col + 1, row + 1));
                    if (row > 0 && map[col + 1, row - 1] == State.Empty)
                        moves.Add(new Move(col + 1, row - 1));
                }
            }
            else if (you == State.MegaWhite || you == State.MegaBlack)
            {
                int tempx = col, tempy = row;
                while (true)
                {
                    tempx++;
                    tempy++;
                    if (tempx > 9 || tempy > 9 || map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx,tempy));
                }

                tempx = col;
                tempy = row;
                while (true)
                {
                    tempx--;
                    tempy--;
                    if (tempx < 0 || tempy < 0 || map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx, tempy));
                }

                tempx = col;
                tempy = row;
                while (true)
                {
                    tempx--;
                    tempy++;
                    if (tempx < 0 || tempy > 9 || map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx, tempy));
                }

                tempx = col;
                tempy = row;
                while (true)
                {
                    tempx++;
                    tempy--;
                    if (tempx > 9 || tempy < 0 || map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx, tempy));
                }
            }

            foreach (var move in moves)
            {
                move.FromX = col;
                move.FromY = row;
            }

            return moves;
        }

        private void AddEatMoves(State[,] map, int x, int y, int x2, int y2, Move move, List<Move> moves, State enemy)
        {
            State enemy2;

            if (enemy == State.White) enemy2 = State.MegaWhite;
            else if (enemy == State.Black) enemy2 = State.MegaBlack;
            else if (enemy == State.MegaWhite) enemy2 = State.White;
            else enemy2 = State.Black;

            if (map[x, y] == State.Empty && (map[x2, y2] == enemy || map[x2, y2] == enemy2))
            {
                move.x = x;
                move.y = y;
                move.Add(new Cords(x2, y2));
                var temp = GetEatMoves(move.Clone() as Move, map, enemy);
                if (temp.Count == 0)
                {
                    moves.Add(move.Clone() as Move);
                }
                else
                    moves.AddRange(temp);
            }
        }

        private List<Move> GetEatMoves(Move cords, State[,] map, State _enemy = State.Empty)
        {
            List<Move> moves = new List<Move>();
            int col = cords.x;
            int row = cords.y;
            State enemy;
            if (_enemy == State.Empty)
            {
                State you = map[col, row];
                if (you == State.Empty) return moves;
                if (you == State.White || you == State.MegaWhite) enemy = State.Black;
                else enemy = State.White;
            }
            else
                enemy = _enemy;

            if (map[col, row] == State.White || map[col, row] == State.Black || _enemy != State.Empty)
            {
                int x = col + 2, y = row + 2, x2 = col + 1, y2 = row + 1;
                if (x < 10 && y < 10)
                    if (cords.eat.Count == 0 || !cords.eat.Contains(new Cords(x2, y2)))
                        AddEatMoves(map, x, y, x2, y2, cords.Clone() as Move, moves, enemy);

                x = col - 2;
                y = row + 2;
                x2 = col - 1;
                y2 = row + 1;
                if (x >= 0 && y < 10)
                    if (cords.eat.Count == 0 || !cords.eat.Contains(new Cords(x2, y2)))
                        AddEatMoves(map, x, y, x2, y2, cords.Clone() as Move, moves, enemy);

                x = col + 2;
                y = row - 2;
                x2 = col + 1;
                y2 = row - 1;
                if (x < 10 && y >= 0)
                    if (cords.eat.Count == 0 || !cords.eat.Contains(new Cords(x2, y2)))
                        AddEatMoves(map, x, y, x2, y2, cords.Clone() as Move, moves, enemy);

                x = col - 2;
                y = row - 2;
                x2 = col - 1;
                y2 = row - 1;
                if (x >= 0 && y >= 0)
                    if (cords.eat.Count == 0 || !cords.eat.Contains(new Cords(x2, y2)))
                        AddEatMoves(map, x, y, x2, y2, cords.Clone() as Move, moves, enemy);
            }
            else if (map[col, row] == State.MegaWhite || map[col, row] == State.MegaBlack)
            {
                State enemy1;
                State enemy2;
                if (map[col, row] == State.MegaWhite)
                {
                    enemy1 = State.Black;
                    enemy2 = State.MegaBlack;
                }
                else
                {
                    enemy1 = State.White;
                    enemy2 = State.MegaWhite;
                }
                int tempx = col, tempy = row;
                while (true)
                {
                    tempx++;
                    tempy++;
                    if (tempx > 8 || tempy > 8) break;
                    if (map[tempx, tempy] == enemy1 || map[tempx, tempy] == enemy2)
                    {
                        AddEatMoves(map, tempx + 1, tempy + 1, tempx, tempy, cords.Clone() as Move, moves, enemy);
                    }
                    if (map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx, tempy));
                }

                tempx = col;
                tempy = row;
                while (true)
                {
                    tempx--;
                    tempy--;
                    if (tempx < 1 || tempy < 1) break;
                    if (map[tempx, tempy] == enemy1 || map[tempx, tempy] == enemy2)
                    {
                        AddEatMoves(map, tempx - 1, tempy - 1, tempx, tempy, cords.Clone() as Move, moves, enemy);
                    }
                    if (map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx, tempy));
                }

                tempx = col;
                tempy = row;
                while (true)
                {
                    tempx--;
                    tempy++;
                    if (tempx < 1 || tempy > 8) break;
                    if (map[tempx, tempy] == enemy1 || map[tempx, tempy] == enemy2)
                    {
                        AddEatMoves(map, tempx - 1, tempy + 1, tempx, tempy, cords.Clone() as Move, moves, enemy);
                    }
                    if (map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx, tempy));
                }

                tempx = col;
                tempy = row;
                while (true)
                {
                    tempx++;
                    tempy--;
                    if (tempx > 8 || tempy < 1) break;
                    if (map[tempx, tempy] == enemy1 || map[tempx, tempy] == enemy2)
                    {
                        AddEatMoves(map, tempx + 1, tempy - 1, tempx, tempy, cords.Clone() as Move, moves, enemy);
                    }
                    if (map[tempx, tempy] != State.Empty) break;
                    moves.Add(new Move(tempx, tempy));
                }
            }

            foreach (var move in moves)
            {
                move.FromX = col;
                move.FromY = row;
            }

            return moves;
        }

        private List<Move> GetMoves(Move cords, State[,] map)
        {
            List<Move> moves = new List<Move>();

            State you = map[cords.x, cords.y];
            State you2;
            if (you == State.White) you2 = State.MegaWhite;
            else if (you == State.Black) you2 = State.MegaBlack;
            else if (you == State.MegaWhite) you2 = State.White;
            else you2 = State.Black;

            if (you == State.Empty) return moves;

            bool walk = true;

            for (int i = 0; i < 10 && walk; i++)
            {
                for (int j = 0; j < 10 && walk; j++)
                {
                    if(map[i,j] == you || map[i, j] == you2)
                        if (GetEatMoves(new Move(i,j), map).Capacity > 0)
                            walk = false;
                }
            }

            moves = GetEatMoves(cords, map);

            if(walk)
                moves = GetWalkMoves(cords, map);

            return moves;
        }

        private void Resize(object sender, SizeChangedEventArgs e)
        {
            var size = Math.Min(Height, Width);
            MainGrid.Width = size - 50 - 40;
            MainGrid.Height = size - 50 - 40;
        }

        private void ChangeLanguage(object sender, SelectionChangedEventArgs e)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo((ChooseLanguage.SelectedValue as ComboBoxItem).Content.ToString().ToLower());
            SetLanguage();
        }

        private void MainWindow_OnStateChanged(object sender, EventArgs e)
        {
            Resize(null,null);
        }
    }
}