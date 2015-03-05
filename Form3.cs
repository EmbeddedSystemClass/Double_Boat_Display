using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Coach_Display
{
    public partial class Form3 : Form
    {

        public static float keyPoint = 3;
        public static int timePast = 20;
        public static int timeForward = 80;
        //public static double zeroSignal = 0;
        public static float initTime = 5; // время осреднения ускорения для расчета нулевого значения
        public static Form1.portState PORT_MODE = Form1.portState.First;

        public Form3()
        {
            InitializeComponent();
        }

        public Form3(double edge, int time, double zero)
        {
            InitializeComponent();
            textBox1.Text = edge.ToString();
            textBox2.Text = time.ToString();
           // textBox3.Text = String.Format("{0,7:0.0000}", zero);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                keyPoint = Convert.ToSingle(textBox1.Text.Replace('.', ','));
                timePast = Convert.ToInt32(textBox2.Text.Replace('.', ','));
                initTime = Convert.ToSingle(textBox4.Text.Replace('.', ','));
                timePast = Convert.ToInt32(textBox5.Text.Replace('.', ','));
                PORT_MODE = (Form1.portState) comboBox1.SelectedItem;
                Coach_Display.Properties.Settings.Default.KeyPoint = keyPoint;
                Coach_Display.Properties.Settings.Default.timePast = timePast;
                Coach_Display.Properties.Settings.Default.timeForward = timeForward;
                Coach_Display.Properties.Settings.Default.initTime = initTime;
                switch (PORT_MODE)
                {
                    case Form1.portState.None:
                        Coach_Display.Properties.Settings.Default.mode = 0;
                        break;
                    case Form1.portState.First:
                        Coach_Display.Properties.Settings.Default.mode = 1;
                        break;
                    case Form1.portState.Second:
                        Coach_Display.Properties.Settings.Default.mode = 2;
                        break;
                    case Form1.portState.Both:
                        Coach_Display.Properties.Settings.Default.mode = 3;
                        break;
                    default:
                        Coach_Display.Properties.Settings.Default.mode = 0;
                        break;
                }
                Coach_Display.Properties.Settings.Default.Save();
                //Form1.true_zero_set = Convert.ToDouble(textBox3.Text.Replace('.', ','));
            }
            catch (Exception)
            {
            }
            this.Close();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            textBox1.Text = keyPoint.ToString();
            textBox2.Text = timePast.ToString();
            textBox4.Text = initTime.ToString();
            textBox5.Text = timeForward.ToString();
            comboBox1.Items.Add(Form1.portState.None);
            comboBox1.Items.Add(Form1.portState.First);
            comboBox1.Items.Add(Form1.portState.Second);
            comboBox1.Items.Add(Form1.portState.Both);
            for (int i = 0; i < 4; i++)
                if ((Form1.portState)comboBox1.Items[i] == PORT_MODE)
                    comboBox1.SelectedIndex = i;
        }
    }
}
