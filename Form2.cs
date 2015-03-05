using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Collections;
using System.Threading;

namespace Coach_Display
{
    public partial class Form2 : Form
    {

        static List<float[]> time_list = new List<float[]>();
        static float[] time_online = new float[20];
        public static bool stopUpd = false;

        public Form2()
        {
            InitializeComponent();
            //int len = time_list.Count;
            //updateTable(0);
            
        }

        public Form2(bool upd)
        {
            InitializeComponent();
            stopUpd = upd;
            //int len = time_list.Count;
            //updateTable(0);

        }

        public static bool add2table(float[] source)
        {
            if (source.Length != 20)
                return false;
            else
            {
                time_list.Add(source);
                return true;
            }
        }

        public static bool addOnline(float[] source)
        {
            if ((source.Length > 20) || (source.Length <= 0))
                return false;
            else
            {
                time_online = source;
                return true;
            }
        }

        public void updateTable(int number)
        {
            float[] data = new float[0];
            if (number == 0)
                data = time_online;
            else
            {
                data = time_list[number - 1];
            }
            try
            {
                label1.Invoke(new Action(() => label1.Text = String.Format("{0,5:0.0}", data[0])));
                label2.Invoke(new Action(() => label2.Text = String.Format("{0,5:0.0}", data[1])));
                label3.Invoke(new Action(() => label3.Text = String.Format("{0,5:0.0}", data[2])));
                label4.Invoke(new Action(() => label4.Text = String.Format("{0,5:0.0}", data[3])));
                label5.Invoke(new Action(() => label5.Text = String.Format("{0,5:0.0}", data[4])));

                label6.Invoke(new Action(() => label6.Text = String.Format("{0,5:0.0}", data[5])));
                label7.Invoke(new Action(() => label7.Text = String.Format("{0,5:0.0}", data[6])));
                label8.Invoke(new Action(() => label8.Text = String.Format("{0,5:0.0}", data[7])));
                label9.Invoke(new Action(() => label9.Text = String.Format("{0,5:0.0}", data[8])));
                label10.Invoke(new Action(() => label10.Text = String.Format("{0,5:0.0}", data[9])));

                label11.Invoke(new Action(() => label11.Text = String.Format("{0,5:0.0}", data[10])));
                label12.Invoke(new Action(() => label12.Text = String.Format("{0,5:0.0}", data[11])));
                label13.Invoke(new Action(() => label13.Text = String.Format("{0,5:0.0}", data[12])));
                label14.Invoke(new Action(() => label14.Text = String.Format("{0,5:0.0}", data[13])));
                label15.Invoke(new Action(() => label15.Text = String.Format("{0,5:0.0}", data[14])));

                label16.Invoke(new Action(() => label16.Text = String.Format("{0,5:0.0}", data[15])));
                label17.Invoke(new Action(() => label17.Text = String.Format("{0,5:0.0}", data[16])));
                label18.Invoke(new Action(() => label18.Text = String.Format("{0,5:0.0}", data[17])));
                label19.Invoke(new Action(() => label19.Text = String.Format("{0,5:0.0}", data[18])));
                label20.Invoke(new Action(() => label20.Text = String.Format("{0,5:0.0}", data[19])));

                label21.Invoke(new Action(() => label21.Text = String.Format("{0,5:0.0}", data[0] + data[1])));
                label22.Invoke(new Action(() => label22.Text = String.Format("{0,5:0.0}", data[2] + data[3])));
                label23.Invoke(new Action(() => label23.Text = String.Format("{0,5:0.0}", data[4] + data[5])));
                label24.Invoke(new Action(() => label24.Text = String.Format("{0,5:0.0}", data[6] + data[7])));
                label25.Invoke(new Action(() => label25.Text = String.Format("{0,5:0.0}", data[8] + data[9])));

                label26.Invoke(new Action(() => label26.Text = String.Format("{0,5:0.0}", data[10] + data[11])));
                label27.Invoke(new Action(() => label27.Text = String.Format("{0,5:0.0}", data[12] + data[13])));
                label28.Invoke(new Action(() => label28.Text = String.Format("{0,5:0.0}", data[14] + data[15])));
                label29.Invoke(new Action(() => label29.Text = String.Format("{0,5:0.0}", data[16] + data[17])));
                label30.Invoke(new Action(() => label30.Text = String.Format("{0,5:0.0}", data[18] + data[19])));

                label31.Invoke(new Action(() => label31.Text = String.Format("{0,5:0.0}", data[0] + data[1] + data[2] + data[3] + data[4])));
                label32.Invoke(new Action(() => label32.Text = String.Format("{0,5:0.0}", data[5] + data[6] + data[7] + data[8] + data[9])));
                label33.Invoke(new Action(() => label33.Text = String.Format("{0,5:0.0}", data[10] + data[11] + data[12] + data[13] + data[14])));
                label34.Invoke(new Action(() => label34.Text = String.Format("{0,5:0.0}", data[15] + data[16] + data[17] + data[18] + data[19])));

                label35.Invoke(new Action(() => label35.Text = String.Format("{0,5:0.0}", data[0] + data[1] + data[2] + data[3] + data[4] +
                                                          data[5] + data[6] + data[7] + data[8] + data[9])));
                label36.Invoke(new Action(() => label36.Text = String.Format("{0,5:0.0}", data[10] + data[11] + data[12] + data[13] + data[14] +
                                                          data[15] + data[16] + data[17] + data[18] + data[19])));
                label37.Invoke(new Action(() => label37.Text = String.Format("{0,5:0.0}", data.Sum())));
                this.Invoke(new Action(() => this.Update()));
            }
            catch (Exception)
            {
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //this.Invoke(new Action(() => this.Location = new Point(this.Owner.Location.X + this.Owner.Width + 5, this.Owner.Location.Y)));
            comboBox1.Items.Clear();
            for (int i = 0; i < time_list.Count; i++)
            {
                comboBox1.Items.Add((i+1)*1000);
            }
            comboBox1.Items.Add("Текущий");
            comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
            updateTable(0);
            comboBox1.Invoke(new Action(() => comboBox1.Enabled = stopUpd));
        }


        static public void clearData()
        {
            time_list.Clear();
            time_online = new float[20];
        }

        public void stopUpdate(bool source)
        {
            if (source)
                comboBox1.Invoke(new Action(() => comboBox1.Enabled = true));
            else
                comboBox1.Invoke(new Action(() => comboBox1.Enabled = false));
            comboBox1.Invoke(new Action(() =>comboBox1.Items.Clear()));
            for (int i = 0; i < time_list.Count; i++)
            {
                comboBox1.Invoke(new Action(() =>comboBox1.Items.Add((i + 1) * 1000)));
            }
            comboBox1.Invoke(new Action(() =>comboBox1.Items.Add("Текущий")));
            comboBox1.Invoke(new Action(() =>comboBox1.SelectedIndex = comboBox1.Items.Count - 1));
            

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            if (index != comboBox1.Items.Count - 1)
            {
                updateTable(index + 1);
            }
            else
                updateTable(0);
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            //this.Visible = false;
            //Form1.tableOpened = false;
            
            //Thread.Sleep(300);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Form1.recStart = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.stopUpdate(checkBox1.Checked);
            stopUpd = checkBox1.Checked;
        }

        
    }
}
