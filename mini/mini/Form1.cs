using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using Aspose.Imaging.FileFormats.Dicom;
using Aspose.Imaging.ImageOptions;
using Aspose.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Graphics = System.Drawing.Graphics;
using ImageAttributes = System.Drawing.Imaging.ImageAttributes;
using ColorMatrix = System.Drawing.Imaging.ColorMatrix;
using Rectangle = System.Drawing.Rectangle;
using GraphicsUnit = System.Drawing.GraphicsUnit;
using Dicom.Log;
using Dicom;
using Point = System.Drawing.Point;
using InterpolationMode = System.Drawing.Drawing2D.InterpolationMode;

namespace mini
{
    public partial class Form1 : Form
    {
        //DICOM 파일 불러오는 폴더 주소(==pacs)
        private String path = "C:\\Users\\20191120\\Desktop\\TEST 영상\\NOR\\";
        //DICOM 파일을 PictureBox에 띄우기 위해 jpg파일로 변환 해주기 위한 폴더
        private String savePath = "C:\\Users\\20191120\\Desktop\\mini_jpg\\";
        //이미지 밝기 조절을 위한 비트맵 변수 생성
        private Bitmap original = null;


        //---------------
        private Point LastPoint;

        private double ratio = 1.0F;
        private Point imgPoint;
        private Rectangle imgRect;
        private Point clickPoint;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.MouseWheel += new MouseEventHandler(imgZoom_MouseWheel);
            pictureBox1.Paint += new PaintEventHandler(pictureBox1_Paint);
            pictureBox1.MouseDown += new MouseEventHandler(pictureBox1_MouseDown);
            pictureBox1.MouseMove += new MouseEventHandler(pictureBox1_MouseMove);
            

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
        }

        //마우스휠로 zoomin 하는 이벤트 
        private void imgZoom_MouseWheel(object sender, MouseEventArgs e)
        {
           

            int lines = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            PictureBox pb = (PictureBox)sender;

            if (lines > 0)
            {
                ratio *= 1.1F;
                if (ratio > 100.0) ratio = 100.0;
            }
            else if (lines < 0)
            {
                ratio *= 0.9F;
                if (ratio < 1) ratio = 1;
            }

            imgRect.Width = (int)Math.Round(pictureBox1.Width * ratio);
            imgRect.Height = (int)Math.Round(pictureBox1.Height * ratio);
            imgRect.X = (int)Math.Round(pb.Width / 2 - imgPoint.X * ratio);
            imgRect.Y = (int)Math.Round(pb.Height / 2 - imgPoint.Y * ratio);

            if (imgRect.X > 0) imgRect.X = 0;

            if (imgRect.Y > 0) imgRect.Y = 0;

            if (imgRect.X + imgRect.Width < pictureBox1.Width) imgRect.X = pictureBox1.Width - imgRect.Width;

            if (imgRect.Y + imgRect.Height < pictureBox1.Height) imgRect.Y = pictureBox1.Height - imgRect.Height;

            pictureBox1.Invalidate();

        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                e.Graphics.DrawImage(pictureBox1.Image, imgRect);
                pictureBox1.Focus();
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                clickPoint = new Point(e.X, e.Y);
            }
            pictureBox1.Invalidate();
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                imgRect.X = imgRect.X + (int)Math.Round((double)(e.X - clickPoint.X) / 5);
                if (imgRect.X >= 0) imgRect.X = 0;
                if (Math.Abs(imgRect.X) >= Math.Abs(imgRect.Width - pictureBox1.Width)) imgRect.X = -(imgRect.Width - pictureBox1.Width);
                imgRect.Y = imgRect.Y + (int)Math.Round((double)(e.Y - clickPoint.Y) / 5);
                if (imgRect.Y >= 0) imgRect.Y = 0;
                if (Math.Abs(imgRect.Y) >= Math.Abs(imgRect.Height - pictureBox1.Height)) imgRect.Y = -(imgRect.Height - pictureBox1.Height);
            }
            else
            {
                LastPoint = e.Location;
                imgPoint = new Point(e.X, e.Y);
            }

            pictureBox1.Invalidate();
        }




        //DICOM 파일을 불러오기 위한 이벤트
        private void button1_Click(object sender, EventArgs e)
        {

            openFileDialog1.Filter = "DICOM File (*.dcm)|*.dcm*|All file|*.*";
            openFileDialog1.InitialDirectory = "C:\\Users\\20191120\\Desktop\\TEST 영상\\NOR\\";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {

                foreach (string FileName in openFileDialog1.SafeFileNames)
                {


                    //listbox에 똑같은 파일이름이 있으면 열리지 않게 하기
                    foreach (String list in listBox1.Items)
                    {
                        if (FileName.Equals(list))
                        {
                            return;
                        }
                    }

                    listBox1.Items.Add(FileName);
                    openFileDialog1.Dispose();
                }

            listBox1.SetSelected(0, true);
            }


        }

        //DICOM 파일을 사용 후 목록 전체 지우기와 변환한 jpg 파일 전체 삭제를 위한 이벤트
        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();

            //이미지뷰의 작업 모두 삭제 후 빈 화면 만들기
            pictureBox1.Dispose();
            pictureBox1.Image = null;
            label5.Text = null;
            label6.Text = null;
            label7.Text = null;
            label8.Text = null;

            trackBar1.Value = 50;
            trackBar2.Value = 0;

            //DICOM 파일 변환 한 jpg파일이 모여있는 폴더 삭제를 위한 작업
            DirectoryInfo di = new DirectoryInfo("C:\\Users\\20191120\\Desktop\\mini_jpg");

            if (di.GetFiles() != null)
            {
                foreach (FileInfo fi in di.GetFiles())
                {
                    //파일 삭제 시 프로세스 반환이 안되는 문제를 해결하기 위해
                    //GC 수동 작업.
                    original = null;
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    fi.Delete();

                }
            }
            else
            {
                return;
            }
        }

        //리스트에 불러온 DICOM 파일을 jpg 파일로 변환 후 이미지로 보기 위한 이벤트
        private void button2_Click(object sender, EventArgs e)
        {
            //리스트에 선택된 파일이 없다면 아무기능 하지않기
            if (listBox1.SelectedItem == null)
            {
                return;
            }
            else
            {
                //이미지가 띄어져 있다면
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image = null;
                    fileOpen();
                }
                else
                {
                    fileOpen();
                }
            }
        }

        //파일 변환 및 보기를 위한 메서드
        private void fileOpen()
        {
            //이미지 밝기조절용 비트맵을 초기화.
            original = null;
            //리스트박스의 이름 가져오기
            String fname = (String)listBox1.SelectedItem;
            String filename = Path.GetFileNameWithoutExtension(fname);
            System.Drawing.Image images = null;

            using (var fileStream = new FileStream(path + fname, FileMode.Open, FileAccess.Read))
            using (DicomImage image = new DicomImage(fileStream))
            {
                // Save as JPEG
                image.Save(savePath + filename + ".jpg", new JpegOptions());

            }

            Dicom.DicomFile dicomFile = new DicomFile();
            dicomFile = DicomFile.Open(path + fname, FileReadOption.ReadAll);
            //dicomFile.WriteToConsole(); 태그확인용 


            string Id = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientID);
            string Name = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName);
            string Sex = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientSex);
            string Birth = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientBirthDate);
            label5.Text = Id;
            label6.Text = Name;
            label7.Text = Sex;
            label8.Text = Birth;


            //사이즈 맞추기
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;


            //이미지 표현하기
            images = System.Drawing.Image.FromFile(savePath + filename + ".jpg");
            pictureBox1.Image = images;
            pictureBox1.Show();

            trackBar1.Value = 50;
            trackBar2.Value = 0;

            imgPoint = new Point(pictureBox1.Width / 2, pictureBox1.Height / 2);
            imgRect = new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);
            ratio = 1.0;
            clickPoint = imgPoint;

            pictureBox1.Invalidate();
        }



        //DICOM파일 사용 후 목록에 남아있는 이름과 jpg 파일을 하나씩 지우기 위한 이벤트
        private void button3_Click(object sender, EventArgs e)
        {
            int cnt = listBox1.Items.Count;

            for (int i = 0; i < cnt; i++)
            {
                //리스트에 남아있는 DICOM 파일 이름 
                String EtName = (String)listBox1.SelectedItem;

                //변환된 jpg 파일을 삭제하기 위해 DICOM 확장자를 jpg 로 변환
                String FileName = Path.GetFileNameWithoutExtension(EtName) + ".jpg";

                //jpg파일이 저장된곳에서 리스트에서 선택된 파일과 똑같은 이름을
                //가진 파일 선택 해서 삭제 하기 위한 변수 선언.
                FileInfo file = new FileInfo(savePath + FileName);

                listBox1.Items.Remove(listBox1.SelectedItem);
                //프로세스 연결 해제를 위한 작업.    
                original = null;
                pictureBox1.Image = null;
                label5.Text = null;
                label6.Text = null;
                label7.Text = null;
                label8.Text = null;

                trackBar1.Value = 50;
                trackBar2.Value = 0;

                if (file.Exists)
                {
                    //bitmap 으로 변환된 파일을 picturebox 에서 해제먼저 시키기
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    File.Delete(savePath + FileName);
                }
            }
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null) return;
            original = (Bitmap)pictureBox1.Image;
            pictureBox1.Image = AdjustBrightnessContrast(original, trackBar1.Value, trackBar2.Value);
        }
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null) return;
            original = (Bitmap)pictureBox1.Image;
            pictureBox1.Image = AdjustBrightnessContrast(original, trackBar1.Value, trackBar2.Value);
        }


        private Bitmap AdjustBrightnessContrast(System.Drawing.Image image, int contrastValue, int brightnessValue)
        {

            float brightness = -(brightnessValue / 100.0f);
            float contrast = contrastValue / 100.0f;
            var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            using (var attributes = new ImageAttributes())
            {
                float[][] matrix = {
            new float[] { contrast, 0, 0, 0, 0},
            new float[] {0, contrast, 0, 0, 0},
            new float[] {0, 0, contrast, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {brightness, brightness, brightness, 1, 1}

                };

                ColorMatrix colorMatrix = new ColorMatrix(matrix);
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
                return bitmap;

            }
        }












        /*
        class DICOMtoPACS
        {
            public static void SendToPACS(string DICOMFile, string SourceAET, string TargetIP, int TargetPort, string TargetAET)
            {
                var m_pDicomFile = DicomFile.Open(DICOMFile);

                Dicom.Network.DicomClient pClient = new Dicom.Network.DicomClient();
                pClient.NegotiateAsyncOps();
                pClient.AddRequest(new DicomCStoreRequest(m_pDicomFile, DicomPriority.Medium)); 
                pClient.Send(TargetIP, TargetPort, false, SourceAET, TargetAET);
            }
        }
        private void simpleButton2_Click(object sender, EventArgs e)
        {
            DICOMtoPACS.SendToPACS(@"D:\dicomfile.dcm", "Source_AETITLE", "192.168.19.216", 104, "TargetPACS_AETITLE");
        }
        */
    }
}


