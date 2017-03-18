using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace seriaAssistant
{
    public partial class MainWindow : Window
    {
        #region 初始化核心
        public void Initcore()
        {

            LoadConfig();//加载配置信息
            InitClockTimer();//定时器初始化
            InitAutoSendDataTimer();//初始化自动发送数据定时器
            InitSerialPort();//初始化串口
            FindPorts();//查找串口

        }
       
       
        #endregion
        #region 串口操作
        /// <summary>
        ///  串口serialport对象
        /// </summary>

        private void InitSerialPort()
        {
            serialPort.DataReceived += SerialPort_DataReceived;
            InitCheckTimer();
        }

        private void FindPorts()
        {
            portsComboBox.ItemsSource = SerialPort.GetPortNames();
            if (portsComboBox.Items.Count > 0)
            {
                portsComboBox.SelectedIndex = 0;
                portsComboBox.IsEnabled = true;
                Information(string.Format("找到可以用的端口有{0}个 ", portsComboBox.Items.Count.ToString()));

            }
            else
            {
                portsComboBox.IsEnabled = false;
                Alert("没有找到串口，还没想出来纠错方法，手动换个串口插线试试");
            }
        }
        private bool OpenPort()
        {
            bool flag = false;
            ConfigurePort();

            try
            {
                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                Information(string.Format("成功打开端口{0}, 波特率{1}。", serialPort.PortName, serialPort.BaudRate.ToString()));
                flag = true;
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
            }

            return flag;
        }

        private bool ClosePort()
        {
            bool flag = false;

            try
            {
                StopAutoSendDataTimer();
                progressBar.Visibility = Visibility.Collapsed;
                serialPort.Close();
                Information(string.Format("成功关闭端口{0}。", serialPort.PortName));
                flag = true;
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
            }

            return flag;
        }

        private void ConfigurePort()
        {
            serialPort.PortName = GetSelectedPortName();
            serialPort.BaudRate = GetSelectedBaudRate();
            serialPort.Parity = GetSelectedParity();
            serialPort.DataBits = GetSelectedDataBits();
            serialPort.StopBits = GetSelectedStopBits();
            serialPort.Encoding = GetSelectedEncoding();
        }

        private string GetSelectedPortName()
        {
            return portsComboBox.Text;
        }

        private int GetSelectedBaudRate()
        {
            int baudRate = 9600;
            //string conv = baudRateComboBox.Text;
            int.TryParse(baudRateComboBox.Text, out baudRate);
            return baudRate;
        }

        private Parity GetSelectedParity()
        {
            string select = parityComboBox.Text;

            Parity p = Parity.None;
            if (select.Contains("Odd"))
            {
                p = Parity.Odd;
            }
            else if (select.Contains("Even"))
            {
                p = Parity.Even;
            }
            else if (select.Contains("Space"))
            {
                p = Parity.Space;
            }
            else if (select.Contains("Mark"))
            {
                p = Parity.Mark;
            }

            return p;
        }

        private int GetSelectedDataBits()
        {
            int dataBits = 8;
            int.TryParse(databitsComboBox.Text, out dataBits);

            return dataBits;
        }

        private StopBits GetSelectedStopBits()
        {
            StopBits stopBits = StopBits.None;
            string select = stopBitsComboBox.Text.Trim();

            if (select.Equals("1"))
            {
                stopBits = StopBits.One;
            }
            else if (select.Equals("1.5"))
            {
                stopBits = StopBits.OnePointFive;
            }
            else if (select.Equals("2"))
            {
                stopBits = StopBits.Two;
            }

            return stopBits;
        }

        private Encoding GetSelectedEncoding()
        {
            string select = encodingComboBox.Text;
            Encoding enc = Encoding.Default;

            if (select.Contains("UTF-8"))
            {
                enc = Encoding.UTF8;
            }
            else if (select.Contains("ASCII"))
            {
                enc = Encoding.ASCII;
            }
            else if (select.Contains("Unicode"))
            {
                enc = Encoding.Unicode;
            }

            return enc;
        }
        /// <summary>
        /// 将波特率列表添加进去
        /// </summary>
        /// <param name="conf"></param>
        private void AddBaudRate(Configuration conf)
        {
            conf.Add("baudRate", baudRateComboBox.Text);
        }
        #endregion

        #region  数据接收框
        private void RecvDataBoxAppend(string textData)
        {
            this.recvDataRichTextBox.AppendText(textData);
            this.recvDataRichTextBox.ScrollToEnd();
        }



        #endregion


        #region 定时器初始化
        private DispatcherTimer clockTimer = new DispatcherTimer();// 用于更新时间的定时器
        private DispatcherTimer checkTimer = new DispatcherTimer();//用于触发处理函数的定时器
        private void InitClockTimer()// 定时器初始化
        {
            clockTimer.Interval = new TimeSpan(0, 0, 1);
            clockTimer.IsEnabled = true;
            clockTimer.Start();
        }
       
        /// <summary>
        /// 超时时间为50ms
        /// </summary>
        private const int TIMEOUT = 50;
        private void InitCheckTimer()
        {
            // 如果缓冲区中有数据，并且定时时间达到前依然没有得到处理，将会自动触发处理函数。
            checkTimer.Interval = new TimeSpan(0, 0, 0, 0, TIMEOUT);
            checkTimer.IsEnabled = false;
            checkTimer.Tick += CheckTimer_Tick;
        }

        private void StartCheckTimer()
        {
            checkTimer.IsEnabled = true;
            checkTimer.Start();
        }

        private void StopCheckTimer()
        {
            checkTimer.IsEnabled = false;
            checkTimer.Stop();
        }
       

        #endregion

        #region 自动发送
        private DispatcherTimer autoSendDataTimer = new DispatcherTimer();
        private void InitAutoSendDataTimer()
        {
            autoSendDataTimer.IsEnabled = false;
            autoSendDataTimer.Tick += AutoSendDataTimer_Tick;
        }
        private void AutoSendData()
        {
            bool ret = SendData();

            if (ret == false)
            {
                return;
            }

            //启动自动发送定时器
            StartAutoSendDataTimer(GetAutoSendDataInterval());

            //提示自动发送状态
            progressBar.Visibility = Visibility.Visible;
            Information("串口数据自动发送中！！！！！");

        }
        /// <summary>
        /// 自动发送定时器
        /// </summary>
        /// <param name="自动发送定时器"></param>
        private void StartAutoSendDataTimer(int interval)
        {
            autoSendDataTimer.IsEnabled = true;
            autoSendDataTimer.Interval = TimeSpan.FromMilliseconds(interval);
            autoSendDataTimer.Start();
        }

        /// <summary>
        /// 获取自动发送时间间隔
        /// </summary>
        private int GetAutoSendDataInterval()
        {
            int interval = 1000;

            if (int.TryParse(autoSendIntervalTextBox.Text.Trim(), out interval) == true)
            {
                string select = timeUnitComboBox.Text.Trim();

                switch (select)
                {
                    case "毫秒":
                        break;
                    case "秒钟":
                        interval *= 1000;
                        break;
                    case "分钟":
                        interval = interval * 60 * 1000;
                        break;
                    default:
                        break;
                }
            }

            return interval;
        }

        /// <summary>
        /// 停止自动发送
        /// </summary>
        private void StopAutoSendDataTimer()
        {
            autoSendDataTimer.IsEnabled = false;
            autoSendDataTimer.Stop();
        }
        #endregion
        #region 加载配置信息
        private bool LoadConfig()
        {
            //new一个配置对象
            Configuration config = Configuration.Read(@"Config\default.config");

            if (config == null)
            {
                return false;
            }

            //获取波特率 
            string baudRateStr = config.GetString("baudRate");
            baudRateComboBox.Text = baudRateStr;

            //获取数据位
            int dataBitsIndex = config.GetInt("dataBits");
            databitsComboBox.SelectedIndex = dataBitsIndex;

            //获取停止位
            int stopBitsIndex = config.GetInt("stopBits");
            stopBitsComboBox.SelectedIndex = stopBitsIndex;

            //获取校验位
            int parityIndex = config.GetInt("parity");
            parityComboBox.SelectedIndex = parityIndex;

            //获取字节编码
            int encodingIndex = config.GetInt("encoding");
            encodingComboBox.SelectedIndex = encodingIndex;

            //获取发送框内容
            string sendDataText = config.GetString("sendDataTextBoxText");
            sendDataTextBox.Text = sendDataText;

            //获取发送自动发送/保存时间间隔
            string interval = config.GetString("autoSendDataInterval");
            autoSendIntervalTextBox.Text = interval;
            int timeUnitIndex = config.GetInt("timeUnit");
            timeUnitComboBox.SelectedIndex = timeUnitIndex;


            // 加载接收模式
            receiveMode = (ReceiveMode)config.GetInt("receiveMode");
            switch (receiveMode)
            {
                case ReceiveMode.Hex:
                    recvCharacterRadioButton.IsChecked = true;
                    break;
                case ReceiveMode.Character:
                    recvHexRadioButton.IsChecked = true;
                    break;
                case ReceiveMode.Decimal:
                    recvDecRadioButton.IsChecked = true;
                    break;
                default:
                    break;
            }

            showReceiveData = config.GetBool("showReceiveData");

            //加载发送模式
            sendMode = (SendMode)config.GetInt("sendmode");
            switch (sendMode)
            {
                case SendMode.Hex:
                    break;
                case SendMode.Character:
                    break;
                default:
                    break;
            }

            //追加发送换行
            appendContent = config.GetString("appendContent");

            return true;





        }
        #endregion

        private bool SendData()
        {
            string textToSend = sendDataTextBox.Text;
            if (serialPort.IsOpen == false)
            {
                Alert("串口未打开，无法发送数据。");
                return false;
            }
            else
            {

                if (string.IsNullOrEmpty(textToSend))
                {
                    Alert("发送的内容不能没有");
                }

                if (autoSendEnableCheckBox.IsChecked == true)
                {
                    recvDataRichTextBox.AppendText(textToSend);
                    recvDataRichTextBox.AppendText(appendContent);
                    recvDataRichTextBox.ScrollToEnd();
                    return SerialPortWrite(textToSend, false);
                }
                else
                {
                    recvDataRichTextBox.AppendText(textToSend);
                    recvDataRichTextBox.AppendText(appendContent);
                    recvDataRichTextBox.ScrollToEnd();
                    return SerialPortWrite(textToSend);
                }
                
            }
        }

      

        /// <summary>
        /// 警告信息提示（一直提示）
        /// </summary>
        /// <param name="message">提示信息</param>
        private void Alert(string message)
        {
            
            statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x21, 0x2A));
            statusInfoTextBlock.Text = message;
        }

        /// <summary>
        /// 普通状态信息提示
        /// </summary>
        /// <param name="message">提示信息</param>
        private void Information(string message)
        {
            if (serialPort.IsOpen)
            {
                
                statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC));
            }
            else
            {
                
                statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF , 0xDA, 0x59, 0x00));
            }
            statusInfoTextBlock.Text = message;
        }
        /// <summary>
        /// SerialPort对象
        /// </summary>
        private SerialPort serialPort = new SerialPort();
        /// <summary>
        /// 要发送的数据窗口的内容
        /// </summary>
        private bool SerialPortWrite(string textData)
        {
            SerialPortWrite(textData, false);
            return false;
        }

        private string appendContent = "\n";
        private bool SerialPortWrite(string textData, bool reportEnable)
        {
            if (serialPort == null)
            {
                return false;
            }

            if (serialPort.IsOpen == false)
            {
                Alert("串口未打开，无法发送数据。");
                return false;
            }

            try
            {
                //serialPort.DiscardOutBuffer();
                //serialPort.DiscardInBuffer();

                if (sendMode == SendMode.Character)
                {
                    serialPort.Write(textData + appendContent);
                    reportEnable = true;
                }
                else if (sendMode == SendMode.Hex)
                {
                    string[] grp = textData.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    List<byte> list = new List<byte>();

                    foreach (var item in grp)
                    {
                        list.Add(Convert.ToByte(item, 16));
                    }

                    serialPort.Write(list.ToArray(), 0, list.Count);
                    reportEnable = true;
                }

                if (reportEnable)
                {
                    // 报告发送成功的消息，提示用户。
                    Information(string.Format("成功发送：{0}。", textData));
                }
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
                return false;
            }

            return true;
        }
     
    }


}

