using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Collections;
using ZedGraph;


namespace Coach_Display
{
    public partial class Form1 : Form
    {
        //System.Drawing.Graphics graph; // используется для рисования
        string build_version = "0.3 Master"; 

        Form2 table = new Form2();
        public static volatile bool recStart = false; // индикатор заполнение таблицы (заполняется/не заполняется)
        public static volatile bool tableOpened = false; // состояние таблицы (открыта/закрыта)

        public static double zero_set = 1; // граница для определения начала/конца гребка
        public static int time_set = 100; // минимальное время гребка в мс
        public static double true_zero_set = 0; // искусственный нуль, для отображения на графике и рассчета

        // There be commentary!
        static volatile byte THREAD_INDEX = 0;
        SerialPort[] active_com = new SerialPort[2];
        public enum portState {None, First, Second, Both}; // режимы работы: без датчика, первый датчик, второй датчик, два датчика
        Queue<float>[] rawAcceleration = new Queue<float>[2];  // очереди ускорений 
        //static portState PORT_MODE = portState.First; // выбор режима работы с датчиками
        static string[] comNames = new string[2];
        Thread[] firstStage = new Thread[2]; // чтение с СОМ порта, отображение первичных данных и пополнение очередей ускорений
        Thread[] secondStage = new Thread[2]; // чтение очередей ускорений, расчет и отображение дополнительных параметров

        // Controls
        TextBox[] speedBox = new TextBox[2];
        TextBox[] tempBox = new TextBox[2];
        TextBox[] speedIncrBox1 = new TextBox[2];
        TextBox[] speedIncrBox2 = new TextBox[2];
        TextBox[] baseTimeBox = new TextBox[2];
        TextBox[] baseMovementBox = new TextBox[2];


        # region zedgraph variables

        // созание массивов, которые будут содержать списки точек
        PointPairList[] Stroke_Graph = new PointPairList[5];

        //RollingPointPairList[] Stroke_Dyn_Graph = new RollingPointPairList[3];

        // создание массивов, которые будут содежать кривые (собственно линии на графике)
        LineItem[] StaticCurve = new LineItem[5];
        //LineItem[] DynamicCurve = new LineItem[3];

        #endregion

        public Form1()
        {
            InitializeComponent();
            this.Text += " v" + build_version;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tempBox[0] = textBox1; tempBox[1] = textBox2;
            speedBox[0] = textBox3; speedBox[1] = textBox4;
            speedIncrBox1[0] = textBox5; speedIncrBox1[1] = textBox6;
            speedIncrBox2[0] = textBox7; speedIncrBox2[1] = textBox8;
            baseTimeBox[0] = textBox9; baseTimeBox[1] = textBox10;
            baseMovementBox[0] = textBox11; baseMovementBox[1] = textBox12;
            Form3.keyPoint = Coach_Display.Properties.Settings.Default.KeyPoint;
            Form3.timePast = Coach_Display.Properties.Settings.Default.timePast;
            Form3.timeForward = Coach_Display.Properties.Settings.Default.timeForward;
            Form3.initTime = Coach_Display.Properties.Settings.Default.initTime;
            switch (Coach_Display.Properties.Settings.Default.mode)
            {
                case 0:
                    Form3.PORT_MODE = portState.None;
                    break;
                case 1:
                    Form3.PORT_MODE = portState.First;
                    break;
                case 2:
                    Form3.PORT_MODE = portState.Second;
                    break;
                case 3:
                    Form3.PORT_MODE = portState.Both;
                    break;
                default:
                    Form3.PORT_MODE = portState.None;
                    break;
            }
            zedGraph_init(); // инициализация модуля графиков
            // Важно инициализировать графики до запуска потоков взаимодействующих с ними
            com_search(); // найти и автоматически выбрать СОМ порты
            Connect(); // подключиться к выбранным СОМ портам и запустить потоки считывания и отображения
            
        }

        private void stageOneThread(object com)
        {
            int index = THREAD_INDEX;
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch tableTimer = new System.Diagnostics.Stopwatch();
            timer.Start();
            int read_buffer = 59; // длина буффера для пакетов с дисплея
            float speedNow = 0; // текущая скорость (значение из последнего принятого пакета) в м/с
            float[] recMass = new float[20];
            int recCount = 0;
            

            int len = 0;
            byte[] buffer = new byte[read_buffer];
            byte mini_buffer = 0;
            byte[] temp = new byte[2];

            float recDistance = 0;
            
            timer.Start();
            float[] acceleration = new float[19];
            
            try
            {
                while (true)
                {
                    if (active_com[index].BytesToRead > 0)
                    {
                        mini_buffer = (byte)active_com[index].ReadByte();
                        if (mini_buffer == 10)
                        {
                            mini_buffer = (byte)active_com[index].ReadByte();
                            if (mini_buffer == 13)
                            {
                                while (active_com[index].BytesToRead < 60)
                                {
                                }
                                buffer = new byte[read_buffer];
                                len = active_com[index].Read(buffer, 0, 59);

                                temp[0] = buffer[10]; temp[1] = buffer[11];
                                speedNow = (float)BitConverter.ToInt16(temp, 0); // скорость в км/ч

                                acceleration = new float[19];
                                for (int i = 0; i < 38; i += 2)
                                {
                                    temp[0] = buffer[19 + i]; temp[1] = buffer[20 + i];
                                    acceleration[i / 2] = (float)BitConverter.ToInt16(temp, 0) / (1000) * 9.8F; //ускорения
                                    rawAcceleration[index].Enqueue(acceleration[i / 2]); //ускорения
                                }
                                // Искуственное введение скорости для тестирования без GPS
                                //buffer[0] = (byte)DateTime.Now.Second;
                                //label10.Invoke(new Action(() => label10.Text = (buffer[0] / 10F).ToString()));
                                if ((!pauseButton.Checked))
                                {
                                    speedBox[index].Invoke(new Action(() => speedBox[index].Text =
                                        String.Format("{0,5:0.0}", speedNow * 3.6F / 10F))); // скорость в км/ч
                                } // if (!pauseButton.Checked)

                                # region table block
                                if (!recStart)
                                {
                                    toolStrip1.Invoke(new Action(() => tableParameter.Enabled = true)); // Для начала отсчета времени
                                    // требуется найти первый гребок. До этого момента вызмать таблицу времени нельзя, так как ее невозможно заполнять
                                    recStart = true;
                                    recMass = new float[20];
                                    recCount = 0;
                                    Form2.clearData();
                                    if (tableParameter.Checked && tableOpened)
                                    {
                                        table.updateTable(0); // update with "time online" flag
                                    }
                                    if (tableTimer.IsRunning)
                                        tableTimer.Restart(); // shouldnt happen, just in case
                                    else
                                        tableTimer.Start();
                                }
                                if ((recStart))//&&(!checkBox1.Checked))
                                {
                                    //recDistance += (((recCount%2)*5F)) + DateTime.Now.Second/15F; 
                                    recDistance += speedNow * 0.2F; // speed*time (5 Hz GPS update rate)
                                    if ((recDistance >= 47) && (recDistance <= 53))
                                    {
                                        recMass[recCount++] = tableTimer.ElapsedMilliseconds / 1000F;
                                        recDistance = 0;
                                        tableTimer.Restart();
                                    }
                                    Form2.addOnline(recMass);
                                    if (tableOpened && (tableParameter.Checked) && (!Form2.stopUpd) && (!table.IsDisposed)) // overkill, rebalance later
                                        table.updateTable(0);
                                    if (recCount >= 20)
                                    {
                                        recCount = 0;
                                        Form2.add2table(recMass);
                                        recMass = new float[20];
                                    }
                                }
                                # endregion
                                
                            } // if (mini_buffer == 13)
                        } // if (mini_buffer == 10)
                        timer.Restart();
                    } // if (active_com[index].BytesToRead > 0)
                    if (timer.ElapsedMilliseconds >= 3000)
                    {
                        richTextBox1.Invoke(new Action(() => richTextBox1.ForeColor = Color.Black));
                        Disconnect();
                    }
                } // while (true)
            } // try
            catch (Exception sd)
            {
                if (!(sd is ThreadAbortException))
                {
                    MessageBox.Show("Произошла ошибка при передачи данных (Stage one)\n" + sd.Message + "\n" +
                        rawAcceleration[index].Count);
                    Disconnect();
                    Thread.Sleep(50);
                    return;
                }
            }
            //timer.Stop();
        } // private void stageOneThread(object com)

       

        private void form2th(object none)
        {
            //table = new Form2(checkBox1.Checked);
            table = new Form2();
            //table.Owner = this;
            tableOpened = true; // probably not needed (doubled)
            table.ShowDialog();
            tableOpened = false;
            toolStrip1.Invoke(new Action(() => tableParameter.Checked = false));
            //checkBox2.Invoke(new Action(() => checkBox2.Checked = false));
            
            //checkBox1.Invoke(new Action(() => checkBox1.Enabled = false));
        }

        private void stageTwoThread(object com)
        {
            int index = THREAD_INDEX;
            List<float> miniAcc = new List<float>(); // используется для хранения +-2 значений относительно текущего
            List<float> oldAcc = new List<float>(); // используется для хранения -20 сглаженных значений
            int oldLen = Form3.timePast; // количество точек хранимых в памяти - количество точек влево от ключевой точки
            int newLen = Form3.timeForward; // количество точек отображаемых на графике - количество точек вправо от ключевой точки
            float calcAcc = 0; // текущее значение сглаженного ускорения для расчетов
            float dispAcc = 0; // текущее значение ускорения для отображения
            float zeroSignal = 0; // выставленный нуль
            float tempZeroSignal = 0; // используется для расчета нулевого сигнала
            float keyPoint = Form3.keyPoint; // ключевая точка относительно выставленного нуля
            int accTimer = newLen + 1; // использутся для подсчета количества точек между событиями
            float time = 0; // ось Х на графике
            bool baseCounting = false; // индикатор подсчета времени опорной фазы
            int baseTimer = 9001; // счетчик времени опорной фазы
            float speedIncr = 0; // прирост скорости на текущем гребке
            List<float> intSpeed = new List<float>(); // прирост скорости на последних 5 гребках
            int updateRate = 25; // период обновления графика (период 4 = частота 100/4 = частота 25 Гц) 


            bool start_found = false;
            int line_flag = 0;

            try
            {
                while (true)
                {
                    if (rawAcceleration[index].Count > 0)
                    {
                        if ((miniAcc.Count > 4))
                            miniAcc.RemoveAt(0);
                        dispAcc = rawAcceleration[index].Dequeue() - zeroSignal;
                        if (dispAcc == zeroSignal)
                            dispAcc = miniAcc[miniAcc.Count - 1];
                        miniAcc.Add(dispAcc);
                        if (baseTimer > 9000)
                        {
                            tempZeroSignal += dispAcc / (Form3.initTime * 100F);
                            baseTimer++;
                            Add_to_static_graph(time, dispAcc, index);
                            time += 0.01F;
                            if ((!pauseButton.Checked)&&(baseTimer % updateRate == 0))
                            {
                                redraw_graph();
                            }
                            //if (baseTimer % 3000 == 0)
                            //{
                            //    StaticCurve[index].Clear();
                            //    time = 0;
                            //}
                            if (baseTimer >= 9000 + Form3.initTime * 100F)
                            {
                                zedGraph.GraphPane.XAxis.Scale.Max = 2;
                                StaticCurve[index].Clear();
                                baseTimer = 0;
                                time = 0;
                                zeroSignal = tempZeroSignal;
                                System.Media.SystemSounds.Asterisk.Play();
                            }
                        } // if (baseTimer > 9000)
                        else
                        {
                            calcAcc = 0;
                            for (int i = 0; i < 5; i++) // slide average 5
                                calcAcc += miniAcc[i] / 5;
                            dispAcc = 0;
                            for (int i = 1; i < 4; i++) // slide avegare 3
                                dispAcc += miniAcc[i] / 3;

                            #region concept 
                            //accTimer++;
                            //if ((calcAcc <= keyPoint) && (oldAcc[oldAcc.Count - 1] >= keyPoint) && (accTimer >= newLen))
                            //{
                            //    StaticCurve[index].Clear();
                            //    time = 0;

                            //    if (intSpeed.Count > 4) // 5 to be exact
                            //        intSpeed.RemoveAt(0);
                            //    intSpeed.Add(speedIncr);
                            //    baseCounting = true;

                            //    if (!pauseButton.Checked)
                            //    {
                            //        tempBox[index].Invoke(new Action(() => tempBox[index].Text =
                            //            String.Format("{0,5:0.0}", 60F / (float)((accTimer + oldLen) / 100F)))); // темп
                            //        //baseTimeBox[index].Invoke(new Action(() => baseTimeBox[index].Text =
                            //        //    String.Format("{0,5:0.00}", (baseTimer) / 100F))); // время опорной фазы (только положительные ускорения)
                            //        //baseMovementBox[index].Invoke(new Action(() => baseMovementBox[index].Text =
                            //        //    String.Format("{0,5:0.00}", speedIncr * ((baseTimer) / 100F)))); // прокат
                            //        //speedIncrBox1[index].Invoke(new Action(() => speedIncrBox1[index].Text =
                            //        //    String.Format("{0,5:0.0}", speedIncr))); // прирост скорости на текущем гребке

                            //        //if (intSpeed.Count > 4) // 5 to be exact
                            //        //{
                            //        //    speedIncr = 0;
                            //        //    for (int r = 0; r < 5; r++)
                            //        //        speedIncr += intSpeed[r] / 5;
                            //        //    speedIncrBox2[index].Invoke(new Action(() => speedIncrBox2[index].Text =
                            //        //        String.Format("{0,5:0.0}", speedIncr))); // средний прирост скорости
                            //        //}
                            //    } // if (!pauseButton.Checked)
                            //    accTimer = 0;
                            //    baseTimer = 0;
                            //    speedIncr = 0;

                            //    for (int j = 0; j < oldLen; j++)
                            //    {
                            //            Add_to_static_graph(time, oldAcc[j], index);
                            //            time += 0.01F;
                            //        if (oldAcc[j] > 0)
                            //        {
                            //            speedIncr += oldAcc[j] * 0.01F;
                            //            baseTimer++;
                            //        }
                            //    } // for (int j = 0; j < oldLen; j++)
                            //    //if ((!pauseButton.Checked))
                            //    //    redraw_graph();
                            //} // if ((calcAcc >= keyPoint) && (oldAcc[oldAcc.Count - 1] <= keyPoint) && (accTimer >= newLen))
                            //if (accTimer < newLen)
                            //{
                            //    Add_to_static_graph(time, dispAcc, index);
                            //    time += 0.01F;
                            //    if ((baseCounting) && (calcAcc > 0))
                            //    {
                            //        baseTimer++;
                            //        speedIncr += calcAcc * 0.01F;
                            //    }
                            //    if ((calcAcc <= keyPoint) && (oldAcc[oldAcc.Count - 1] >= keyPoint))
                            //        baseCounting = false;
                            //} // if (accTimer < newLen)
                            //accTimer++;

                            //if (oldAcc.Count > oldLen - 1)
                            //    oldAcc.RemoveAt(0);
                            //oldAcc.Add(calcAcc);
                            //if ((!pauseButton.Checked) && (accTimer % updateRate == 0))
                            //    redraw_graph();
                            ////Redraw_one_area(time, dispAcc);
                            ////
                            #endregion

                            start_found = false;

                            if ((calcAcc <= -3) && (oldAcc[oldAcc.Count - 1] >= -3) && (accTimer >= newLen))
                            {
                                start_found = true;
                                accTimer = 0;
                                //stop_point = i;
                                line_flag++;
                                if (line_flag > 1)
                                    line_flag = 0;
                                StaticCurve[line_flag].Clear();
                                time = 0;
                                Change_color(line_flag);
                            }
                            accTimer++;
                            Add_to_static_graph(time, dispAcc, line_flag);
                            time += 0.01F;
                            if (oldAcc.Count > oldLen - 1)
                                oldAcc.RemoveAt(0);
                            oldAcc.Add(calcAcc);
                           
                            if ((!pauseButton.Checked)&&(accTimer % updateRate == 0))
                                redraw_graph();
                                //Redraw_one_area(time, dispAcc);
                                //
                        } // if (baseTimer > 9000) else
                    } // (rawAcceleration[index].Count > 0)
                } // while(true)
            } // try

            catch (Exception sd)
            {
                if (!(sd is ThreadAbortException))
                {
                    MessageBox.Show("Произошла ошибка при обработке данных (Stage two)\n" + sd.Message + "\nQueue length " +
                        rawAcceleration[index].Count + "\nCurrent acceleration " + dispAcc);
                    Disconnect();
                    Thread.Sleep(50);

                    return;
                }
            }
        } // private void stageTwoThread(object com)



        

        private void com_search()
        {
            string[] ports = SerialPort.GetPortNames();
            ToolStripMenuItem menuItem;
            foreach (string port in ports)
            {
                menuItem = new ToolStripMenuItem(port); // создает элемент меню с именем этого СОМ порта
                menuItem.Click += new System.EventHandler(this.menuItem1_Click); // добавляет событие по клику
                port1Parameter.DropDownItems.Add(menuItem); // добавляет этот элемент в меню

                menuItem = new ToolStripMenuItem(port);
                menuItem.Click += new System.EventHandler(this.menuItem2_Click);
                port2Parameter.DropDownItems.Add(menuItem);
            }


            // условие (PORT_MODE != portState.Second) должно работать, но может не пройти в случае portState.None
            if ((ports.Length != 0) && ((Form3.PORT_MODE == portState.First) || (Form3.PORT_MODE == portState.Both)))
                comNames[0] = ports[0];
            if ((ports.Length > 1) && ((Form3.PORT_MODE == portState.Second) || (Form3.PORT_MODE == portState.Both)))
                comNames[1] = ports[1];
            if ((Form3.PORT_MODE == portState.Second) && (ports.Length == 1))
                comNames[1] = ports[0];
            // Отмечает выбранные СОМ порты в меню
            for (int i = 0; i < port1Parameter.DropDownItems.Count; i++)
            {
                if (port1Parameter.DropDownItems[i].Text == comNames[0]) ((ToolStripMenuItem)port1Parameter.DropDownItems[i]).Checked = true;
                else ((ToolStripMenuItem)port1Parameter.DropDownItems[i]).Checked = false;
                if (port2Parameter.DropDownItems[i].Text == comNames[1]) ((ToolStripMenuItem)port2Parameter.DropDownItems[i]).Checked = true;
                else ((ToolStripMenuItem)port2Parameter.DropDownItems[i]).Checked = false;
            }
        }

        private void menuItem1_Click(object sender, System.EventArgs e)
        {
            comNames[0] = sender.ToString();
            foreach (ToolStripMenuItem COM_now in port1Parameter.DropDownItems)
            {
                if (COM_now.Text == comNames[0]) COM_now.Checked = true;
                else COM_now.Checked = false;
            }
            Disconnect();
            Connect();
        }

        private void menuItem2_Click(object sender, System.EventArgs e)
        {
            comNames[1] = sender.ToString();
            foreach (ToolStripMenuItem COM_now in port2Parameter.DropDownItems)
            {
                if (COM_now.Text == comNames[1]) COM_now.Checked = true;
                else COM_now.Checked = false;
            }
            Disconnect();
            Connect();
        }

        private bool Connect()
        {
            active_com[0] = new SerialPort();
            active_com[1] = new SerialPort();
            
            if ((port1Parameter.DropDownItems.Count != 0) && ((Form3.PORT_MODE == portState.First) || (Form3.PORT_MODE == portState.Both)))
            {
                active_com[0] = new SerialPort(comNames[0], 115200, 0, 8, StopBits.One);
                active_com[0].WriteBufferSize = 512;
                active_com[0].ReadBufferSize = 8192;
            }
            if ((port2Parameter.DropDownItems.Count != 0) && ((Form3.PORT_MODE == portState.Second) || (Form3.PORT_MODE == portState.Both)))
            {
                active_com[1] = new SerialPort(comNames[1], 115200, 0, 8, StopBits.One);
                active_com[1].WriteBufferSize = 512;
                active_com[1].ReadBufferSize = 8192;
            }
            if (Form3.PORT_MODE != portState.None)
                try
                {
                    if ((Form3.PORT_MODE == portState.First) || (Form3.PORT_MODE == portState.Both))
                        active_com[0].Open();
                    if ((Form3.PORT_MODE == portState.Second) || (Form3.PORT_MODE == portState.Both))
                        active_com[1].Open();
                }
                catch (Exception)
                {
                    MessageBox.Show("Не удалось открыть СОМ порт\n" + comNames[0] + " - " +
                        active_com[0].IsOpen + "\n" + comNames[1] + " - " + active_com[1].IsOpen);
                    return false;
                }
            else
            {
                richTextBox1.ForeColor = Color.Black;
                return false;
            }
            if ((Form3.PORT_MODE == portState.First) || (Form3.PORT_MODE == portState.Both))
            {
                THREAD_INDEX = 0;
                richTextBox1.ForeColor = Color.Green;

                rawAcceleration[0] = new Queue<float>();

                firstStage[0] = new Thread(stageOneThread);
                firstStage[0].Start();

                secondStage[0] = new Thread(stageTwoThread);
                secondStage[0].Start();
            }
            Thread.Sleep(50);
            if ((Form3.PORT_MODE == portState.Second) || (Form3.PORT_MODE == portState.Both))
            {
                THREAD_INDEX = 1;
                richTextBox1.ForeColor = Color.Green;

                rawAcceleration[1] = new Queue<float>();

                firstStage[1] = new Thread(stageOneThread);
                firstStage[1].Start();

                secondStage[1] = new Thread(stageTwoThread);
                secondStage[1].Start();
            }
            return true;
        } // private bool Connect()

        private void Disconnect()
        {
            if ((Form3.PORT_MODE == portState.First) || (Form3.PORT_MODE == portState.Both))
            {
                if (secondStage[0].IsAlive)
                    secondStage[0].Abort();
                if (firstStage[0].IsAlive)
                    firstStage[0].Abort();
                if (active_com[0].IsOpen)
                    active_com[0].Close();
            }
            if ((Form3.PORT_MODE == portState.Second) || (Form3.PORT_MODE == portState.Both))
            {
                if (secondStage[1].IsAlive)
                    secondStage[1].Abort();
                if (firstStage[1].IsAlive)
                    firstStage[1].Abort();
                if (active_com[1].IsOpen)
                    active_com[1].Close();
            }
            Thread.Sleep(50);     
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Disconnect();
                if (tableParameter.Checked)
                {
                    toolStrip1.Invoke(new Action(() => tableParameter.Checked = false));
                    tableParameter_CheckStateChanged(new object(), new EventArgs());
                }
            }
            catch (Exception c)
            {
                MessageBox.Show("Ошибка при выходе из программы (Form closing)\n" + c.Message);
            }
            Application.Exit();
        }


        /// <summary>
        /// Тут все функции для работы с графиками
        /// </summary>
        # region zedgraph methods 

        private void zedGraph_init()
        {
            zedGraph.GraphPane.Title.Text = "График последнего гребка";
            zedGraph.GraphPane.XAxis.Title.Text = "Время, сек";
            zedGraph.GraphPane.YAxis.Title.Text = "Ускорение (м/сек^2)";
            zedGraph.GraphPane.XAxis.Type = AxisType.Linear;
            zedGraph.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(zedGraph_ContextMenuBuilder);
            
            

            zedGraph.GraphPane.YAxis.Title.FontSpec.Size = 22;
            zedGraph.GraphPane.XAxis.Title.FontSpec.Size = 22;
            zedGraph.GraphPane.Title.FontSpec.Size = 22;

            zedGraph.GraphPane.XAxis.Scale.Min = 0;
            zedGraph.GraphPane.XAxis.Scale.Max = 2.5;
            zedGraph.GraphPane.YAxis.Scale.Min = -10;
            zedGraph.GraphPane.YAxis.Scale.Max = 10;

            // отобразить сетку по осям
            zedGraph.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zedGraph.GraphPane.YAxis.MajorGrid.IsVisible = true;

            // lists init
            Stroke_Graph[0] = new PointPairList();
            Stroke_Graph[1] = new PointPairList();
            Stroke_Graph[2] = new PointPairList();
            Stroke_Graph[3] = new PointPairList();
            Stroke_Graph[4] = new PointPairList();

            //Stroke_Dyn_Graph[0] = new RollingPointPairList(500);
            //Stroke_Dyn_Graph[1] = new RollingPointPairList(500);
            //Stroke_Dyn_Graph[2] = new RollingPointPairList(500);

            // graph init
            StaticCurve[0] = zedGraph.GraphPane.AddCurve("Параметр X", Stroke_Graph[0], Color.Blue, SymbolType.None);
            StaticCurve[0].IsVisible = true;    // сделать кривую видимой
            StaticCurve[0].Label.IsVisible = false;  // сделать видимым заглавие
            StaticCurve[0].Line.Width = 3;
            StaticCurve[1] = zedGraph.GraphPane.AddCurve("Параметр Y", Stroke_Graph[1], Color.Red, SymbolType.None);
            StaticCurve[1].IsVisible = true;
            StaticCurve[1].Label.IsVisible = false;
            StaticCurve[1].Line.Width = 3;
            StaticCurve[2] = zedGraph.GraphPane.AddCurve("Параметр Z", Stroke_Graph[2], Color.Blue, SymbolType.None);
            StaticCurve[2].IsVisible = false;
            StaticCurve[2].Label.IsVisible = false;
            StaticCurve[3] = zedGraph.GraphPane.AddCurve("Параметр Y", Stroke_Graph[3], Color.Red, SymbolType.None);
            StaticCurve[3].IsVisible = true;
            StaticCurve[3].Label.IsVisible = false;
            StaticCurve[4] = zedGraph.GraphPane.AddCurve("Параметр Z", Stroke_Graph[4], Color.Red, SymbolType.None);
            StaticCurve[4].IsVisible = false;
            StaticCurve[4].Label.IsVisible = false;

            //DynamicCurve[0] = zedGraph.GraphPane.AddCurve("Ускорение X", Stroke_Dyn_Graph[0], Color.Blue, SymbolType.None);
            //DynamicCurve[0].IsVisible = true;
            //DynamicCurve[0].Label.IsVisible = true;
            //DynamicCurve[1] = zedGraph.GraphPane.AddCurve("Ускорение Y", Stroke_Dyn_Graph[1], Color.Green, SymbolType.None);
            //DynamicCurve[1].IsVisible = false;
            //DynamicCurve[1].Label.IsVisible = false;
            //DynamicCurve[2] = zedGraph.GraphPane.AddCurve("Ускорение Z", Stroke_Dyn_Graph[2], Color.Red, SymbolType.None);
            //DynamicCurve[2].IsVisible = false;
            //DynamicCurve[2].Label.IsVisible = false;

            zedGraph.AxisChange();
            zedGraph.Invalidate();

        }

        private void zedGraph_ContextMenuBuilder(ZedGraphControl sender,
            ContextMenuStrip menuStrip,
            Point mousePt,
            ZedGraphControl.ContextMenuObjectState objState)
        {
            menuStrip.Items.RemoveAt(7);
        }

        private void redraw_graph(double max = 0)
        {
            zedGraph.AxisChange();
            zedGraph.Invalidate();
            if (max != 0)
                zedGraph.GraphPane.XAxis.Scale.Max = max;
        }

        public void Add_to_static_graph(double time, float x, int id = 0)
        {
            Stroke_Graph[id].Add(time, x);
            //Stroke_Graph[1].Add(time, true_zero_set); // may god have mercy on my soul
        }

        public void Change_color(int id)
        {
            if (id == 0)
            {
                StaticCurve[0].Color = Color.Red;
                StaticCurve[1].Color = Color.Blue;
                StaticCurve[0].Line.Width = 3;
                StaticCurve[1].Line.Width = 2;
                
            }
            else if (id == 1)
            {
                StaticCurve[0].Color = Color.Blue;
                StaticCurve[1].Color = Color.Red;
                StaticCurve[0].Line.Width = 2;
                StaticCurve[1].Line.Width = 3;
            }
        }

        //public void Add_pack_dynamic_graph(double time, double x, double y, double z)
        //{
        //    Stroke_Dyn_Graph[0].Add(time, x);
        //    Stroke_Dyn_Graph[1].Add(time, y);
        //    Stroke_Dyn_Graph[2].Add(time, z);
        //}

        public void Redraw_one_area(double index, double Y)
        {
            RectangleF to_redraw = new RectangleF((float)index - 0.2F, 10, 0.5F, 20);
            Rectangle redraw = new Rectangle((int)index, (int)zedGraph.GraphPane.YAxis.Scale.Max, 1, (int)(zedGraph.GraphPane.YAxis.Scale.Max - zedGraph.GraphPane.YAxis.Scale.Min));
            Region zone = new Region(to_redraw);
            zedGraph.Invoke(new Action(() => zedGraph.Invalidate(redraw)));
            zedGraph.Invoke(new Action(() => zedGraph.Update()));
        }

        #endregion

        private void Form1_Resize(object sender, EventArgs e)
        {
            // panel loc 571; 6  size 186; 456
            // graph loc 2 6
            panel1.Location = new Point(this.Width - 285, 55);
            //zedGraph.Location = new Point(2, 46);
            toolStrip1.Location = new Point(this.Width - 285, 2);
            zedGraph.Width = this.Width - 290;
            zedGraph.Height = this.Height - 40;
        }


        private void tableParameter_CheckStateChanged(object sender, EventArgs e)
        {
            Thread timeTableTh; // используется для запуска потопа таблицы и контролирования момента его завершения

            if (tableParameter.Checked)
            {
                timeTableTh = new Thread(form2th);

                timeTableTh.Start();
            }
            else
            {
                if (tableOpened)
                {
                    table.Invoke(new Action(() => table.Close()));
                    tableOpened = false;
                }
            }
            Thread.Sleep(50);
        }

        private void exitParameter_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void optionsParameter_Click(object sender, EventArgs e)
        {
            Form3 optionsChange = new Form3(zero_set, time_set / 100, true_zero_set);
            optionsChange.ShowDialog();
        }






        

    }

    // Дополнительные классы, которые когда-то использовались
    // Служат для описания формата пакетов радиоканала
    // Не удалять, может пригодиться

    //public struct packet
    //{
    //    public byte time_h;     // 0
    //    public byte time_min;   // 1
    //    public byte time_sec;   // 2
    //    public byte status;     // 3
    //    public short temp;      // 4..5
    //    public short prok;      // 6..7
    //    public short n;         // 8..9
    //    public short vel;       // 10..11
    //    public byte Km;         // 12
    //    public ushort meter;    // 13..14
    //    public byte char_pulse; // 15
    //    public byte check_sum;  // 16
    //}
    //public struct new_acc_pack
    //{
    //    public byte frame1; // 17 - 16 0x10
    //    public byte frame3; // 18 - 19 0x13
    //    public short[] a;   // 19..56 
    //    public byte frame2; // 57 - 16 0x10
    //    public byte frame4; // 58 - 03 0x03
    //}

    //public struct acc_packet
    //{
    //    public byte frame1;   // 0 0x10 16
    //    public byte type;     // 1 0x34 52
    //    public byte vel_H;    // 2
    //    public short[] a;   // 3...102 (50 elements)
    //    public byte crc;      // 103
    //    public byte frame2;   // 104 0x10 16
    //    public byte frame3;   // 105 0x03 3
    //}  /* 106 bytes */

   
}
