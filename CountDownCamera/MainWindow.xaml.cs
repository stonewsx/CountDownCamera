using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
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
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using WPFMediaKit.DirectShow.Controls;

namespace CountDownCamera
{
    /// <summary>
    /// MainWindow.xaml 的交互邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
        SpeechSynthesizer synth = new SpeechSynthesizer();
        int sec = 10;

        #region Public properties

        public ObservableCollection<FilterInfo> VideoDevices { get; set; }

        public FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { _currentDevice = value; this.OnPropertyChanged("CurrentDevice"); }
        }
        private FilterInfo _currentDevice;

        #endregion

        #region Private fields

        private IVideoSource _videoSource;

        #endregion

        public MainWindow()
        {
            // Configure the audio output.
            synth.SetOutputToDefaultAudioDevice();
            synth.Rate = 2;
            synth.SpeakAsync("");
            InitializeComponent();
            System.Windows.Input.InputMethod.SetIsInputMethodEnabled(this, false);
            CameraHelper.IsDisplay = true;
            //CameraHelper.SourcePlayer = player;
            CameraHelper.UpdateCameraDevices();
            this.DataContext = this;
            GetVideoDevices();
        }
        private void GetVideoDevices()
        {
            VideoDevices = new ObservableCollection<FilterInfo>();
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                VideoDevices.Add(filterInfo);
            }
            if (VideoDevices.Any())
            {
                CurrentDevice = VideoDevices[0];
            }
            else
            {
                MessageBox.Show("No video sources found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (sec > 0)
            {
                sec--;
                timerText.Text = sec.ToString();  //timerText是介面上的一個TextBlock
            }
            else
                timer.Stop();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //選擇攝影機。
            cb.ItemsSource = MultimediaUtil.VideoInputNames;
            //設置預設第0個為鏡頭。
            if (MultimediaUtil.VideoInputNames.Length > 0)
            {
                cb.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("電腦沒有安裝任何鏡頭");
            }
        }

        private void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vce.VideoCaptureSource = (string)cb.SelectedItem;
        }
        //private void btnOpenCamera_Click(object sender, EventArgs e)
        //{
        //    btnCapture.IsEnabled = true;
        //    btnRestart.IsEnabled = true;
        //    //playerWindow.Visibility = Visibility.Visible;
        //    imgCapture.Visibility = Visibility.Collapsed;
        //    //if (CameraHelper.CameraDevices.Count > 0)
        //    //{
        //    //    CameraHelper.CloseDevice();
        //    //    CameraHelper.UpdateCameraDevices();
        //    //    //CameraHelper.SetCameraDevice(0);
        //    //    CameraHelper.SetSelectCameraDevice(CurrentDevice);
        //    //}
        //}
        private void btnCapture_Click(object sender, EventArgs e)
        {
            synth.SpeakAsyncCancelAll();
            synth.SpeakAsync("Start");
            //playerWindow.Visibility = Visibility.Visible;
            vce.Visibility = Visibility.Visible;
            timerText.Visibility = Visibility.Visible;
            imgCapture.Visibility = Visibility.Collapsed;
            btnCapture.IsEnabled = false; 
            sec = 10;
            timerText.Text = "Start";
            
            timer.Tick += new EventHandler(timer_Tick);
            timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                synth.SpeakAsyncCancelAll();
                timerText.Text = sec.ToString();
                if (sec != 0) {
                    synth.SpeakAsync(sec.ToString());
                }

                if (sec == 0) {
                    synth.SpeakAsync("Smile");
                    timer.Stop();
                    timerText.Visibility = Visibility.Collapsed;
                    imgCapture.Visibility = Visibility.Visible;
                    //playerWindow.Visibility = Visibility.Collapsed;
                    //string fullPath = CameraHelper.CaptureImage(AppDomain.CurrentDomain.BaseDirectory + @"Capture");
                    string fullPath = CaptureImageByWMK(AppDomain.CurrentDomain.BaseDirectory + @"Capture");
                    if (fullPath == null)
                        return;

                    vce.Visibility = Visibility.Collapsed;

                    BitmapImage bit = new BitmapImage();
                    bit.BeginInit();
                    bit.UriSource = new Uri(fullPath);
                    bit.EndInit();
                    imgCapture.Source = bit;
                }
                --sec;
            }, Application.Current.Dispatcher);
            timer.Start();
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            btnCapture.IsEnabled = true;
            vce.Play();
            vce.Visibility = Visibility.Visible;
            //playerWindow.Visibility = Visibility.Visible;
            timerText.Visibility = Visibility.Collapsed;
            imgCapture.Visibility = Visibility.Collapsed;
            timer.Stop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CameraHelper.CloseDevice();
        }

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion

        public string CaptureImageByWMK(string filePath, string fileName = null)
        {
            if (cb.ItemsSource == null) return null;
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            try
            {
                if (fileName == null) fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                string fullPath = System.IO.Path.Combine(filePath, fileName + "-cap.jpg");
                //抓取控制元件做成圖片
                RenderTargetBitmap bmp = new RenderTargetBitmap(
                (int)vce.ActualWidth, (int)vce.ActualHeight,
                96, 96, PixelFormats.Default);
                vce.Measure(vce.RenderSize);
                vce.Arrange(new Rect(vce.RenderSize));
                bmp.Render(vce);
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    byte[] captureData = ms.ToArray();
                    //儲存
                    File.WriteAllBytes(fullPath, captureData);
                }
                vce.Pause();
                return fullPath;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message.ToString());
                return null;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                synth.SpeakAsyncCancelAll();
                btnRestart_Click(sender, e);
                btnCapture_Click(sender, e);
            }
        }
    }
}