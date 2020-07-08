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
using System.Media;

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
        bool flip = false;
        bool startMeasure = false;
        SoundPlayer simpleSound = new SoundPlayer(@"경보음2.wav");

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
                if (flip)
                    Cv2.Flip(frame, frame, FlipMode.Y);
                if (!frame.Empty())
                {
                    faces = faceCascade.DetectMultiScale(frame);
                    if (faces.Length > 0 && startMeasure)
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
                        
                        List<Data> dList = new List<Data>();
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

                                Data d = new Data() { Date = time.ToString("yyyy/MM/dd"), Time = time.ToString("hh:mm:ss"), face = filename, measure = val[i].ToString(), stand = standard.ToString(), warn = val[i] > standard };
                                dMgr.inputData(d);
                                dList.Add(d);
                                if (float.Parse(d.measure) > float.Parse(d.stand)) simpleSound.Play();
                            }
                            catch (Exception)
                            {
                                MessageBox.Show("SubMat Error");
                            }
                        }
                        dMgr.DisplayData(dataGridView1, dList);
                        
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
                
                button1.Text = "카메라 On";
                isCameraRunning = 0;                

                if (capture.IsOpened())
                {
                    capture.Release();
                }

                pictureBox1.Image = null;
            }

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Equals("거울모드 On"))
            {                
                button2.Text = "거울모드 Off";
                flip = false;
            }
            else
            {
                button2.Text = "거울모드 On";
                flip = true;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            simpleSound.Stop();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text.Equals("측정시작"))
            {
                button4.Text = "측정종료";
                startMeasure = true;
            }
            else
            {
                button4.Text = "측정시작";
                startMeasure = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dMgr.LoadFromFile();
            dMgr.DisplayData(dataGridView1);
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
            public DataMgr()
            {
                Bitmap tmpgood = new Bitmap("../../good.png");
                Bitmap tmpbad = new Bitmap("../../bad.png");
                System.Drawing.Size resize = new System.Drawing.Size(50, 50);
                good = new Bitmap(tmpgood, resize);
                bad = new Bitmap(tmpbad, resize);
            }
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
            public int DisplayData(DataGridView view)
            {
                int cnt = 0;
                for(int i=0; i<dList.Count; i++)
                {
                    string[] row1 = { dList[i].Date, dList[i].Time};
                    view.Rows.Add(row1);
                    Image img = Image.FromFile(dList[i].face);
                    ((DataGridViewImageCell)view.Rows[i].Cells[2]).Value = img;
                    view.Rows[i].Cells[3].Value = dList[i].stand;
                    view.Rows[i].Cells[4].Value = dList[i].measure;
                    
                    if (dList[i].warn)
                    {
                        ((DataGridViewImageCell)view.Rows[i].Cells[5]).Value = bad;
                    }
                    else
                    {
                        ((DataGridViewImageCell)view.Rows[i].Cells[5]).Value = good;
                    }
                    //view.Rows.Add(
                    //list.Items.Add(dList[i]);
                    //if (dList[i].warn == true) cnt++;
                }
                return cnt;
            }
            public void DisplayData(DataGridView view, List<Data> dataList)
            {
                
                for (int i = 0; i < dataList.Count; i++)
                {
                    int lastIdx = view.Rows.Count-1;
                    string[] row1 = { dataList[i].Date, dataList[i].Time };
                    view.Rows.Add(row1);
                    Image img = Image.FromFile(dataList[i].face);
                    ((DataGridViewImageCell)view.Rows[lastIdx].Cells[2]).Value = img;
                    view.Rows[lastIdx].Cells[3].Value = dataList[i].stand;
                    view.Rows[lastIdx].Cells[4].Value = dataList[i].measure;

                    if (dataList[i].warn)
                    {
                        ((DataGridViewImageCell)view.Rows[lastIdx].Cells[5]).Value = bad;
                    }
                    else
                    {
                        ((DataGridViewImageCell)view.Rows[lastIdx].Cells[5]).Value = good;
                    }

                }
            }
            public void inputData(Data d)
            {
                dList.Add(d);
            }
            Bitmap good;
            Bitmap bad;
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