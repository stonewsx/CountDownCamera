using System;
using AForge.Video.DirectShow;
using AForge.Controls;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;

namespace CountDownCamera
{
    public static class CameraHelper
    {
        private static FilterInfoCollection _cameraDevices;
        private static VideoCaptureDevice div = null;
        private static VideoSourcePlayer sourcePlayer = new VideoSourcePlayer();
        private static bool _isDisplay = false;
        //指示_isDisplay設置爲true後，是否設置了其他的sourcePlayer，若未設置則_isDisplay重設爲false
        private static bool isSet = false;

        /// <summary>
        /// 獲取或設置攝像頭設備，無設備爲null
        /// </summary>
        public static FilterInfoCollection CameraDevices
        {
            get
            {
                return _cameraDevices;
            }
            set
            {
                _cameraDevices = value;
            }
        }
        /// <summary>
        /// 指示是否顯示攝像頭視頻畫面
        /// 默認false
        /// </summary>
        public static bool IsDisplay
        {
            get { return _isDisplay; }
            set { _isDisplay = value; }
        }
        /// <summary>
        /// 獲取或設置VideoSourcePlayer控件，
        /// 只有當IsDisplay設置爲true時，該屬性纔可以設置成功
        /// </summary>
        public static VideoSourcePlayer SourcePlayer
        {
            get { return sourcePlayer; }
            set
            {
                if (_isDisplay)
                {
                    sourcePlayer = value;
                    isSet = true;
                }

            }
        }
        /// <summary>
        /// 更新攝像頭設備信息
        /// </summary>
        public static void UpdateCameraDevices()
        {
            _cameraDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        }
        /// <summary>
        /// 設置使用的攝像頭設備
        /// </summary>
        /// <param name="index">設備在CameraDevices中的索引</param>
        /// <returns><see cref="bool"/></returns>
        public static bool SetCameraDevice(int index)
        {
            if (!isSet) _isDisplay = false;
            //無設備，返回false
            if (_cameraDevices.Count <= 0 || index < 0) return false;
            if (index > _cameraDevices.Count - 1) return false;
            // 設定初始視頻設備
            div = new VideoCaptureDevice(_cameraDevices[index].MonikerString);
            sourcePlayer.VideoSource = div;
            div.Start();
            sourcePlayer.Start();
            return true;
        }
        public static bool SetSelectCameraDevice(FilterInfo CurrentDevice)
        {
            if (CurrentDevice != null)
            {
                div = new VideoCaptureDevice(CurrentDevice.MonikerString);
                sourcePlayer.VideoSource = div;
                sourcePlayer.Start();
                return true;
            }
            return false;
        }
        /// <summary>
        /// 截取一幀圖像並保存
        /// </summary>
        /// <param name="filePath">圖像保存路徑</param>
        /// <param name="fileName">保存的圖像文件名</param>
        public static string CaptureImage(string filePath, string fileName = null)
        {
            if (sourcePlayer.VideoSource == null) return null;
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            try
            {
                System.Drawing.Image bitmap = sourcePlayer.GetCurrentVideoFrame();
                if (fileName == null) fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                string fullPath = Path.Combine(filePath, fileName + "-cap.jpg");
                bitmap.Save(fullPath, ImageFormat.Jpeg);
                bitmap.Dispose();
                return fullPath;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message.ToString());
                return null;
            }
        }
        /// <summary>
        /// 關閉攝像頭設備
        /// </summary>
        public static void CloseDevice()
        {
            if (div != null && div.IsRunning)
            {
                sourcePlayer.Stop();
                div.SignalToStop();
                div = null;
                _cameraDevices = null;
            }
        }
    }
}