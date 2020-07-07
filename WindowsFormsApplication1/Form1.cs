using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Threading;
using System.Drawing.Imaging;
using System.IO;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        VideoCapture capture;
        Mat frame;
        Bitmap image;
        private Thread camera;
        int isCameraRunning = 0;
        Font arialFont = new Font("Arial", 20);
        Rect[] faces;
        float[] val = new float[10];
        DataMgr dMgr = new DataMgr();
        float standard = 70;
        private void CaptureCamera()
        {

            camera = new Thread(new ThreadStart(CaptureCameraCallback));
            camera.Start();
        }

        private void CaptureCameraCallback()
        {
            frame = new Mat();
            capture = new VideoCapture();
            capture.FrameWidth = 680;
            capture.FrameHeight = 480;
            capture.Open(0);
            String filenameFaceCascade = "haarcascade_frontalface_alt.xml";
            CascadeClassifier faceCascade = new CascadeClassifier();

            if (!faceCascade.Load(filenameFaceCascade))
            {
                MessageBox.Show("error");
                return;
            }
            while (isCameraRunning == 1)
            {
                capture.Read(frame);
                if (!frame.Empty())
                {
                    faces = faceCascade.DetectMultiScale(frame);
                    if (faces.Length > 0)
                    {
                        for (int i = 0; i < faces.Length; i++)
                        {
                            Cv2.Rectangle(frame, faces[i], Scalar.Red); // add rectangle to the image
                            val[i] = (float)Cv2.Mean(frame.SubMat(faces[i]));
                            //textBox1.Text = textBox1.Text + "\tfaces : " + faces[i];
                        }

                        image = BitmapConverter.ToBitmap(frame);

                        using (Graphics graphics = Graphics.FromImage(image))
                        {
                            for (int i = 0; i < faces.Length; i++)
                            {
                                PointF p = new PointF(faces[i].X + faces[i].Width / 2 - 10, faces[i].Y - 15);
                                if (val[i] > standard)
                                {
                                    graphics.DrawString(val[i].ToString(), arialFont, Brushes.Red, p);
                                }
                                else
                                {
                                    graphics.DrawString(val[i].ToString(), arialFont, Brushes.Green, p);
                                }
                            }

                        }
                    }
                    else
                    {
                        image = BitmapConverter.ToBitmap(frame);
                    }
                    pictureBox1.Image = image;
                }
                image = null;
            }

        }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text.Equals("카메라 On"))
            {
                CaptureCamera();
                button1.Text = "카메라 Off";
                isCameraRunning = 1;
            }
            else
            {
                List<Data> dList = new List<Data>();
                button1.Text = "카메라 On";
                isCameraRunning = 0;

                for (int i = 0; i < faces.Length; i++)
                {                    
                    try
                    {
                        Mat dst = frame.SubMat(faces[i]);
                        DateTime time = DateTime.Now;
                        string str = time.ToString("yyyyMMddhhmmss");

                        string filename = string.Format("../../faces/{0}.jpg", str);

                        //Cv2.ImShow(filename, dst);
                        Cv2.ImWrite(filename, dst);

                        Data d = new Data() { Date = time.ToString("yyyy/MM/dd"), Time = time.ToString("hh:mm:ss"), face = filename, measure = val[i].ToString(), stand = standard.ToString(), warn = val[i] > float.Parse("75") };
                        dMgr.inputData(d);
                        dList.Add(d);
                        Thread.Sleep(1000);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("SubMat Error");
                    }
                }

                if (capture.IsOpened())
                {
                    capture.Release();
                }

                dMgr.DisplayData(listBox1, dList);
            }

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Equals("거울모드 On"))
            {
                CaptureCamera();
                button2.Text = "거울모드 Off";
                isCameraRunning = 1;
            }
            else
            {
                if (capture.IsOpened())
                {
                    capture.Release();
                }

                button2.Text = "거울모드 On";
                isCameraRunning = 0;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text.Equals("알람 Off"))
            {
                CaptureCamera();
                button3.Text = "알람 On";
                isCameraRunning = 1;
            }
            else
            {
                if (capture.IsOpened())
                {
                    capture.Release();
                }

                button3.Text = "알람 Off";
                isCameraRunning = 0;
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text.Equals("측정시작"))
            {
                CaptureCamera();
                button4.Text = "카메라 Off";
                isCameraRunning = 1;
            }
            else
            {
                if (capture.IsOpened())
                {
                    capture.Release();
                }

                button4.Text = "측정시작";
                isCameraRunning = 0;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dMgr.LoadFromFile();
            dMgr.DisplayData(listBox1);
            textBox2.Text = DateTime.Now.ToString();
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            textBox2.Text = DateTime.Now.ToString();
        }

        struct Data
        {
            public override string ToString()
            {
                return string.Format("{0}\t{1}\t{2}\t{3}\t{4}", Date, Time, face, measure, stand, warn);
            }
            public void SaveToFile(StreamWriter sw)
            {
                sw.WriteLine(Date);
                sw.WriteLine(Time);
                sw.WriteLine(face);
                sw.WriteLine(measure);
                sw.WriteLine(stand);
                sw.WriteLine(warn==true?"경고":"정상");
            }
            public void LoadFromFile(StreamReader sr)
            {
                Date = sr.ReadLine();
                Time = sr.ReadLine();
                face = sr.ReadLine();
                measure = sr.ReadLine();
                stand = sr.ReadLine();
                warn = sr.ReadLine() == "정상" ? false : true;
            }
            public string Date;
            public string Time;
            public string face;
            public string measure;
            public string stand;
            public bool warn;
        };
        class DataMgr
        {
            public void SaveToFile()
            {
                StreamWriter sw = new StreamWriter("../../data.txt");
                sw.WriteLine("{0}", dList.Count);
                foreach (Data m in dList)
                {
                    m.SaveToFile(sw);
                }
                sw.Close();
                sw.Dispose();
            }
            public void LoadFromFile()
            {
                StreamReader sr = new StreamReader("../../data.txt");
                int iCount = int.Parse(sr.ReadLine());
                for (int i = 0; i < iCount; i++)
                {
                    Data m = new Data();
                    m.LoadFromFile(sr);
                    dList.Add(m);
                }
                sr.Close();
                sr.Dispose();
            }
            public int DisplayData(ListBox list)
            {
                int cnt = 0;
                list.Items.Clear();
                for(int i=0; i<dList.Count; i++)
                {
                    list.Items.Add(dList[i]);
                    if (dList[i].warn == true) cnt++;
                }
                return cnt;
            }
            public void DisplayData(ListBox list, List<Data> dataList)
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    list.Items.Add(dataList[i]);
                }
            }
            public void inputData(Data d)
            {
                dList.Add(d);
            }
            List<Data> dList = new List<Data>();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            dMgr.SaveToFile();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            standard = float.Parse(textBox1.Text);
        }
    }
    
    
}