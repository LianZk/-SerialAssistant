using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;


namespace seriaAssistant
{
    public enum ReceiveMode
    {
        Character,  //字符显示
        Hex,        //十六进制
        Decimal,    //十进制

    }

    public enum SendMode
    {
        Character,  //字符
        Hex         //十六进制
    }

    public partial class MainWindow : Window
    {

        #region Global
        // 接收并显示的方式
        private ReceiveMode receiveMode = ReceiveMode.Hex;

        // 发送的方式
        private SendMode sendMode = SendMode.Hex;

        #endregion


        #region Event handler for buttons and so on.
        private void openClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                if (ClosePort())
                {
                    openClosePortButton.Content = "打开";

                }
            }
            else
            {
                if (OpenPort())
                {
                    openClosePortButton.Content = "关闭";
                }
            }
        }

        private void findPortButton_Click(object sender, RoutedEventArgs e)
        {
            FindPorts();
        }
        #endregion

        private void autoSendEnableCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (autoSendEnableCheckBox.IsChecked == true)
            {
                Information(string.Format("使能串口自动发送功能，发送间隔：{0} {1}。", autoSendIntervalTextBox.Text, timeUnitComboBox.Text.Trim()));
            }
            else
            {
                Information("禁用串口自动发送功能。");
                StopAutoSendDataTimer();
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void sendDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (autoSendEnableCheckBox.IsChecked == true)
            {
                AutoSendData();
            }
            else
            {
                SendData();
            }
        }


        private void clearRecvDataBoxButton_Click(object sender, RoutedEventArgs e)
        {
            recvDataRichTextBox.Document.Blocks.Clear();
            recvDataRichTextBoxForXYZ.Document.Blocks.Clear();
            Information("清空成功");
        }

        private void recvModeButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (recvDataRichTextBox == null)
            {
                return;
            }

            if (rb != null)
            {

                recvDataRichTextBox.Document.Blocks.Clear();

                switch (rb.Tag.ToString())
                {
                    case "char":
                        receiveMode = ReceiveMode.Character;
                        Information("提示：字符显示模式。");
                        break;
                    case "hex":
                        receiveMode = ReceiveMode.Hex;
                        Information("提示：十六进制显示模式。");
                        break;
                    case "dec":
                        receiveMode = ReceiveMode.Decimal;
                        Information("提示：十进制显示模式。");
                        break;

                    default:
                        break;
                }
            }
        }

        private bool showReceiveData = true;


        private void sendDataModeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb != null)
            {
                switch (rb.Tag.ToString())
                {
                    case "char":
                        sendMode = SendMode.Character;
                        Information("提示：发送字符串。");
                        // 将文本框中内容转换成char
                        sendDataTextBox.Text = Utilities.ToSpecifiedText(sendDataTextBox.Text, SendMode.Character, serialPort.Encoding);
                        break;
                    case "hex":
                        // 将文本框中的内容转换成hex
                        sendMode = SendMode.Hex;
                        Information("提示：发送十六进制。输入十六进制数据之间用空格隔开，如：1A 2B 3C。");
                        sendDataTextBox.Text = Utilities.ToSpecifiedText(sendDataTextBox.Text, SendMode.Hex, serialPort.Encoding);
                        break;
                    default:
                        break;
                }
            }
        }

        private void AutoSendDataTimer_Tick(object sender, EventArgs e)
        {
            bool ret = false;
            ret = SendData();

            if (ret == false)
            {
                StopAutoSendDataTimer();
            }
        }

        private void readCoordinate_Click(object sender, RoutedEventArgs e)
        {
            if (readCoordinate.IsChecked == true)
            {

                Information("坐标转换正常功能开启");

            }
            else
            {
                Information("坐标转换功能关闭");
            }


        }

        /// <summary>
        /// 窗口关闭前拦截
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 释放没有关闭的端口资源
            if (serialPort.IsOpen)
            {
                ClosePort();
            }

        }

        /// <summary>
        /// 捕获窗口按键。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool AreCharacter()
        {
            if (sendMode == SendMode.Character)
            {
                Alert("飙车要切换到十六进制输出模式");
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {



            // Enter发送数据
            if (e.Key == Key.Enter)
            {
                SendData();

            }

            if (e.Key == Key.NumPad0)
            {
                if (AreCharacter())
                {
                    string stop = Convert.ToString(0); //前进
                    SerialPortWrite(stop);
                    Information("停车");
                }
            }
            if (e.Key == Key.Up)
            {
                if (AreCharacter())
                {
                    string up = Convert.ToString(1); //前进
                    SerialPortWrite(up);
                    Information("前进");
                }
            }
            if (e.Key == Key.Down)
            {
                if (AreCharacter())
                {
                    string down = Convert.ToString(2);//后退
                    SerialPortWrite(down);
                    Information("后退");
                }
            }
            if (e.Key == Key.Left)
            {
                if (AreCharacter())
                {
                    string left = Convert.ToString(3);//原地左转
                    SerialPortWrite(left);
                    Information("原地左转");
                }
            }
            if (e.Key == Key.Right)
            {
                if (AreCharacter())
                {
                    string right = Convert.ToString(4);//原地右转
                    SerialPortWrite(right);
                    Information("原地右转");
                }
            }


            if (Keyboard.IsKeyDown(Key.A))
            {
                if (AreCharacter())
                {
                    string upandleft = Convert.ToString(5);  //左转
                    SerialPortWrite(upandleft);
                    Information("左转");
                }
            }
            if (Keyboard.IsKeyDown(Key.D))
            {
                if (AreCharacter())
                {
                    string upandright = Convert.ToString(6);//右转
                    SerialPortWrite(upandright);
                    Information("右转");
                }

            }
            if (Keyboard.IsKeyDown(Key.Z))
            {
                if (AreCharacter())
                {
                    string downandleft = Convert.ToString(7);//左后倒车
                    SerialPortWrite(downandleft);
                    Information("左后倒车");
                }
            }
            if (Keyboard.IsKeyDown(Key.X))
            {
                if (AreCharacter())
                {
                    string downandright = Convert.ToString(8);//右后倒车
                    SerialPortWrite(downandright);
                    Information("右后倒车");
                }
            }
            if (e.Key == Key.Back)
            {
                sendDataTextBox.Text = sendDataTextBox.Text.Substring(0, Judegetext_lenght());
                sendDataTextBox.SelectionStart = sendDataTextBox.Text.Length;
            }


        }
        #region EventHandler for serialPort

        // 数据接收缓冲区
        private List<byte> receiveBuffer = new List<byte>();

        // 一个阈值，当接收的字节数大于这么多字节数之后，就将当前的buffer内容交由数据处理的线程
        // 分析。这里存在一个问题，假如最后一次传输之后，缓冲区并没有达到阈值字节数，那么可能就
        // 没法启动数据处理的线程将最后一次传输的数据处理了。这里应当设定某种策略来保证数据能够
        // 在尽可能短的时间内得到处理。
        private const int THRESH_VALUE = 128;

        private bool shouldClear = true;

        /// <summary>
        /// 更新：采用一个缓冲区，当有数据到达时，把字节读取出来暂存到缓冲区中，缓冲区到达定值
        /// 时，在显示区显示数据即可。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = sender as System.IO.Ports.SerialPort;

            if (sp != null)
            {
                // 临时缓冲区将保存串口缓冲区的所有数据
                int bytesToRead = sp.BytesToRead;
                byte[] tempBuffer = new byte[bytesToRead];

                // 将缓冲区所有字节读取出来
                sp.Read(tempBuffer, 0, bytesToRead);

                // 检查是否需要清空全局缓冲区先
                if (shouldClear)
                {
                    receiveBuffer.Clear();
                    shouldClear = false;
                }

                // 暂存缓冲区字节到全局缓冲区中等待处理
                receiveBuffer.AddRange(tempBuffer);

                if (receiveBuffer.Count >= THRESH_VALUE)
                {
                    //Dispatcher.Invoke(new Action(() =>
                    //{
                    //    recvDataRichTextBox.AppendText("Process data.\n");
                    //}));
                    // 进行数据处理，采用新的线程进行处理。
                    Thread dataHandler = new Thread(new ParameterizedThreadStart(ReceivedDataHandler));
                    dataHandler.Start(receiveBuffer);
                }

                // 启动定时器，防止因为一直没有到达缓冲区字节阈值，而导致接收到的数据一直留存在缓冲区中无法处理。
                StartCheckTimer();

                this.Dispatcher.Invoke(new Action(() =>
                {
                    dataRecvStatusBarItem.Visibility = Visibility.Visible;
                }));
            }
        }

        #endregion

        #region 数据处理

        private void CheckTimer_Tick(object sender, EventArgs e)
        {
            // 触发了就把定时器关掉，防止重复触发。
            StopCheckTimer();

            // 只有没有到达阈值的情况下才会强制其启动新的线程处理缓冲区数据。
            if (receiveBuffer.Count < THRESH_VALUE)
            {
                //recvDataRichTextBox.AppendText("Timeout!\n");
                // 进行数据处理，采用新的线程进行处理。
                Thread dataHandler = new Thread(new ParameterizedThreadStart(ReceivedDataHandler));
                dataHandler.Start(receiveBuffer);
            }
        }


        private void ReceivedDataHandler(object obj)
        {
            List<byte> recvBuffer = new List<byte>();
            recvBuffer.AddRange((List<byte>)obj);

            if (recvBuffer.Count == 0)
            {
                return;
            }

            // 必须应当保证全局缓冲区的数据能够被完整地备份出来，这样才能进行进一步的处理。
            shouldClear = true;

            this.Dispatcher.Invoke(new Action(() =>
            {

                if (showReceiveData)
                {
                    // 根据显示模式显示接收到的字节. 
                    string result = Utilities.BytesToText(recvBuffer, receiveMode, serialPort.Encoding) + "\n";
                    recvDataRichTextBox.AppendText(result);
                    recvDataRichTextBox.ScrollToEnd();
                    ShowXYZ(recvBuffer);

                }

                dataRecvStatusBarItem.Visibility = Visibility.Collapsed;
            }));

            // TO-DO：
            // 处理数据，比如解析指令等等
        }






        #endregion

        #region 十六进制自动追加空格
        private void sendDataTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sendMode == SendMode.Hex)
            {
                if ((sendDataTextBox.Text.Length + 1) % 3 == 0)//每多出2个字符的时候自动在最后加一个空格
                {
                    sendDataTextBox.Text += " ";//追加空格
                    sendDataTextBox.SelectionStart = sendDataTextBox.Text.Length;
                }

            }
        }
        private int Judegetext_lenght()
        {
            if (sendDataTextBox.Text.Length - 2 < 0)
            {
                return 0;
            }
            else
            {
                return sendDataTextBox.Text.Length - 2;
            }
        }

        static int index = 0;
        // 转换出X,Y,Z坐标再richtextbox里输出
        public void ShowXYZ(List<byte> bytesBuffer)
        {
            try
            {
                if (readCoordinate.IsChecked == true)
                {


                    if (bytesBuffer.Count > 13)
                    {
                        int XH = bytesBuffer[9];
                        int XL = bytesBuffer[8];
                        int YH = bytesBuffer[11];
                        int YL = bytesBuffer[10];
                        //  int ZH = bytesBuffer[13];
                        //  int ZL = bytesBuffer[12];


                        int X = (XH << 8) | XL;
                        int Y = (YH << 8) | YL;
                        //  int Z = (ZH << 8) | ZL;


                        recvDataRichTextBoxForXYZ.AppendText("{");
                        recvDataRichTextBoxForXYZ.AppendText(Convert.ToString(X) + " ");
                        recvDataRichTextBoxForXYZ.AppendText(",");
                        recvDataRichTextBoxForXYZ.AppendText(Convert.ToString(Y) + " ");
                        recvDataRichTextBoxForXYZ.AppendText("}");
                        recvDataRichTextBoxForXYZ.AppendText(",");
                        //   recvDataRichTextBoxForXYZ.AppendText(Convert.ToString(Z) + " ");

                        recvDataRichTextBoxForXYZ.AppendText("\n");
                        recvDataRichTextBoxForXYZ.ScrollToEnd();

                        index++;
                        if (index == 50)
                        {
                            index = 0;
                            recvDataRichTextBoxForXYZ.AppendText("\n");
                            recvDataRichTextBoxForXYZ.AppendText("\n");
                            //    SaveXYZ();

                        }
                    }

                }
            }
            catch (Exception)
            {

               
            }
           

        }

        private void SaveXYZButton_Click(object sender, EventArgs e)
        {
            
            TextRange textRange = new TextRange( recvDataRichTextBoxForXYZ.Document.ContentStart,recvDataRichTextBoxForXYZ.Document.ContentEnd );
            
            using (System.IO.StreamWriter file =
                           new System.IO.StreamWriter(@"D:\C#编译出来的玩意\测试\1.txt", true))
            {
                file.WriteLine(textRange.Text);
            }
            Information("保存成功");
        }

        private void SaveXYZ()
        {
            TextRange textRange = new TextRange(recvDataRichTextBoxForXYZ.Document.ContentStart, recvDataRichTextBoxForXYZ.Document.ContentEnd);

            using (System.IO.StreamWriter file =
                           new System.IO.StreamWriter(@"D:\C#编译出来的玩意\测试\1.txt", true))
            {
                file.WriteLine(textRange.Text);
            }
            Information("保存成功");
        }
        #endregion







    }
}


