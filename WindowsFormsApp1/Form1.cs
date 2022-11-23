using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public enum Level
    {
        easy = 0, //20%
        normal = 1, //40%
        hard = 2 //60%
    }
    

    public partial class form1 : Form
    {
        public form1()
        {
            InitializeComponent();
        }

        Square btn = new Square();

        private void Form1_Load(object sender, EventArgs e)
        {
            panel1.Dock = DockStyle.Fill;
            btn.Build(panel1);
            btn.SetMine(Level.easy);

            foreach (var item in panel1.Controls) //обходим все элементы формы
            {
                if (item is Button) // проверяем, что это кнопка
                {
                    ((Button)item).Click += CommonBtn_Click; //приводим к типу и устанавливаем обработчик события  
                    ((Button)item).DoubleClick += CommonBtn_DoubleClick;
                }
            }
        }

        private void CommonBtn_DoubleClick(object sender, EventArgs e)
        {
            /*Square item = (Square)sender;
            if (item.flag == false)
            {
                item.flag = true;
                item.BackgroundImage = Properties.Resources.image;
                MessageBox.Show("131");
            }
            if (item.flag == true)
            {
                item.flag = false;
                MessageBox.Show("131");
            }*/
        }

        private void CommonBtn_Click(object sender, EventArgs e)
        {
            Square item = (Square)sender;
            if (Square.checkedNow == 0)
            {
                btn.Timer(timer1, 0);
            }
            if (item.bomb == true)
            {
                btn.Loose();
                MessageBox.Show(btn.Timer(timer1, 1), "Неудача");
                panel1.Enabled = false;
                return;
            }
            Square.checkedNow += 1;
            item.Text = item.NumBomb(item);
            item.BackColor = Color.DarkGray;
        }

        private void начатьЗановоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.Enabled = true;
            btn.ClearView();
            btn.Timer(timer1, 1);
        }

        private void новаяИграToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.Enabled = true;
            btn.ClearView();
            btn.ClearMine();
            btn.SetMine(Level.easy);
            btn.Timer(timer1, 1);
        }

        private void легкоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.Enabled = true;
            btn.ClearView();
            btn.ClearMine();
            btn.SetMine(Level.easy);
            btn.Timer(timer1, 1);
        }

        private void нормальноToolStripMenuItem_Click(object sender, EventArgs e)
        {

            panel1.Enabled = true;
            btn.ClearView();
            btn.ClearMine();
            btn.SetMine(Level.normal);
            btn.Timer(timer1, 1);
        }

        private void сложноToolStripMenuItem_Click(object sender, EventArgs e)
        {

            panel1.Enabled = true;
            btn.ClearView();
            btn.ClearMine();
            btn.SetMine(Level.hard);
            btn.Timer(timer1, 1);
        }

        private void завершитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Square.checkedNow == 0)
            {
                MessageBox.Show("Проверьте хотя бы одно поле", "Оповещение");
                return;
            }
            panel1.Enabled = false;

            if (Square.checkedToWin == Square.checkedNow)
            {
                MessageBox.Show(btn.Timer(timer1, 1), "Победа");
            }
            else
            {
                MessageBox.Show(btn.Timer(timer1, 1), "Неудача");
            }
            
        }
    }



    class Square : Button
    {
        DateTime dateTimeStart, dateTimeEnd;
        static int size_x = 10;
        static int size_y = 10;
        static int count = size_x * size_y;
        public bool flag = false;
        public bool bomb = false;
        static public int checkedToWin;
        static public int checkedNow = 0;
        int start_x = 0;
        int start_y = 0;
        const int ButtonWidth = 40;
        const int ButtonHeight = 40;
        

        public static Square[,] btnArr = new Square[size_x, size_y];

        public Square()
        {

        }

        public void Build(Panel panel)
        {
            int counter = 0;
            for (int x = 0; x < size_x; x++)
            {
                for (int y = 0; y < size_y; y++)
                {
                    btnArr[x, y] = new Square();
                    btnArr[x, y].Name = counter.ToString();
                    btnArr[x, y].BackColor = Color.Transparent;
                    btnArr[x, y].Top = start_x + (x * ButtonHeight);
                    btnArr[x, y].Left = start_y + (y * ButtonWidth);
                    btnArr[x, y].Width = ButtonWidth;
                    btnArr[x, y].Height = ButtonHeight;

                    panel.Controls.Add(btnArr[x, y]);
                    ++counter;
                }

            }
        }

        public void SetMine(Level level)
        {
            int tempCol, tempRow;
            double tempLvl = 0.2;
            if (level == Level.easy)
            {
                tempLvl = 0.2;
            }
            if (level == Level.normal)
            {
                tempLvl = 0.4;

            }
            if (level == Level.hard)
            {
                tempLvl = 0.6;

            }

            int limit = (int)Math.Round(count * tempLvl);
            checkedToWin = count - limit;

            Random rnd = new Random();
            for (int i = 0; i != limit;)
            {
                tempCol = rnd.Next(0, size_y);
                tempRow = rnd.Next(0, size_x);
                if (btnArr[tempRow, tempCol].bomb == false)
                {
                    btnArr[tempRow, tempCol].bomb = true;
                    ++i;
                }
            }

        }
        public string NumBomb(Square item)
        {
            int number = Convert.ToInt32(item.Name);
            int result = 0;
            int col = number % size_y - 2;
            int temp = col;
            int row = (number / size_x) - 2;
            for (int i = 0; i < 3; i++)
            {
                row += 1;

                col = temp;
                for (int j = 0; j < 3; j++)
                {
                    col += 1;
                    if (col < 0 || col > (size_y - 1) || row < 0 || row > (size_x - 1))
                    {
                        continue;
                    }

                    if (btnArr[row, col].bomb != false)
                    {
                        result += 1;
                    }

                }
            }
            return $"{result}";
        }

        public void ClearView()
        {
            for (int x = 0; x < size_x; x++)
            {
                for (int y = 0; y < size_y; y++)
                {
                    btnArr[x, y].BackColor = Color.Transparent;
                    btnArr[x, y].BackgroundImage = null;
                    btnArr[x, y].Text = null;
                    checkedNow = 0;
                }

            }
        }

        public void ClearMine()
        {
            for (int x = 0; x < size_x; x++)
            {
                for (int y = 0; y < size_y; y++)
                {
                    btnArr[x, y].bomb = false;
                }

            }
        }

        public void Loose()
        {
            for (int x = 0; x < size_x; x++)
            {
                for (int y = 0; y < size_y; y++)
                {
                    if (btnArr[x, y].bomb == true)
                    {
                        btnArr[x, y].BackgroundImage = Properties.Resources.bomb;
                        btnArr[x, y].BackgroundImageLayout = ImageLayout.Center;
                    }
                }

            }
        }

        public string Timer(Timer timer, int type)
        {
            string result = "";

            if (type == 0)
            {
                timer.Start();
                dateTimeStart = DateTime.Now;
            } 
            else if (type == 1)
            {
                dateTimeEnd = DateTime.Now;
                TimeSpan temp = dateTimeEnd - dateTimeStart;
                timer.Stop();
                result = "Время: " + temp.Minutes.ToString() + ":" + temp.Seconds.ToString() + ":" + temp.Milliseconds.ToString();
            }

            return result;
        }

    }
}
