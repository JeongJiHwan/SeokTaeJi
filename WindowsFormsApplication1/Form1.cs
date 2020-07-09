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
        float standard = 110;
        bool flip = false;
        bool startMeasure = false;
        SoundPlayer simpleSound = new SoundPlayer(@"경보음2.wav");
        int framecnt = 0;
        int alarmcnt = 0;
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
                if (framecnt > 1000000) framecnt = 0;
                framecnt++;
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
                            
                            val[i] = (float)Cv2.Mean(frame.SubMat(faces[i]));
                            if (val[i] > standard)
                            {
                                Cv2.Rectangle(frame, faces[i], Scalar.Red); // add rectangle to the image
                            }
                            else
                            {
                                Cv2.Rectangle(frame, faces[i], Scalar.Green); // add rectangle to the image
                            }
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
                        if (framecnt % 10 == 0)
                        {
                            List<Data> dList = new List<Data>();
                            for (int i = 0; i < faces.Length; i++)
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

                                if (d.warn)
                                {
                                    simpleSound.Play();
                                    alarmcnt++;
                                }
                            }
                            dMgr.DisplayData(dataGridView1, dList);
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
            if (isCameraRunning == 0)
            {
                MessageBox.Show("카메라를 켜주세요.");
            }
            else
            {
                if (button2.Text.Equals("거울모드 On"))
                {
                    button2.Text = "거울모드 Off";
                    flip = true;
                }
                else
                {
                    button2.Text = "거울모드 On";
                    flip = false;
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            simpleSound.Stop();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if(isCameraRunning == 0)
            {
                MessageBox.Show("카메라를 켜주세요.");
            }
            else
            {
                if (button4.Text.Equals("측정시작"))
                {
                    button4.Text = "측정종료";
                    startMeasure = true;
                    
                }
                else
                {
                    simpleSound.Stop();
                    button4.Text = "측정시작";
                    startMeasure = false;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Form3 f = new Form3();
            if (f.ShowDialog() == DialogResult.OK)
            {
            }
            else
            {
                Application.Exit(); 
            }
            f.Close();

            dMgr.LoadFromFile();
            dMgr.DisplayData(dataGridView1);
            textBox2.Text = DateTime.Now.ToString();
            alarmcnt = dMgr.findBadData().Count-1;
            label6.Text = string.Format("Count : 0   Alarm : 0");
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
                StreamWriter sw = File.AppendText("../../data.txt");
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
            public void DisplayData(DataGridView view)
            {
                view.Rows.Clear();
                view.Refresh();
                for (int i=0; i<dList.Count; i++)
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
                    view.FirstDisplayedScrollingRowIndex = view.Rows.Count-1;
                    view.Refresh();
                }
                
            }
            public void DisplayData(DataGridView view, List<Data> dataList, bool opt = false)
            {
                if (opt)
                {
                    view.Rows.Clear();
                    view.Refresh();
                }
                for (int i = 0; i < dataList.Count; i++)
                {
                    int lastIdx = view.Rows.Count;
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
                    view.FirstDisplayedScrollingRowIndex = view.Rows.Count - 1;
                    view.Refresh();
                }

            }
            public List<Data> findGoodData()
            {
                List<Data> list = new List<Data>();
                for(int i=0; i<dList.Count; i++)
                {
                    if (!dList[i].warn)
                    {
                        list.Add(dList[i]);
                    }
                }
                return list;                
            }
            public List<Data> findBadData()
            {
                List<Data> list = new List<Data>();
                for (int i = 0; i < dList.Count; i++)
                {
                    if (dList[i].warn)
                    {
                        list.Add(dList[i]);
                    }
                }
                return list;
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
        protected override void WndProc(ref Message m)
        {
            int WM_CLOSE = 0x0010;
            if (m.Msg == WM_CLOSE)
            {
                Form4 close = new Form4();
                close.ShowDialog();
            }
            base.WndProc(ref m);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            standard = float.Parse(textBox1.Text);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<Data> tmpList;
            switch(comboBox1.SelectedIndex){
                case 0: //전체
                    dMgr.DisplayData(dataGridView1);
                    break;
                case 1: //정상
                    tmpList = dMgr.findGoodData();
                    dMgr.DisplayData(dataGridView1, tmpList, true);
                    break;
                case 2: //경고
                    tmpList = dMgr.findBadData();
                    dMgr.DisplayData(dataGridView1, tmpList, true);
                    break;
            }
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            label6.Text = string.Format("Count : {0}   Alarm : {1}", dataGridView1.Rows.Count , alarmcnt+1);
        }

        private void 프로그램정보ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 info = new Form2();

            info.ShowDialog();
        }

        private void 로그아웃ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Restart();
        }

        private void 프로그램종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    } 
}