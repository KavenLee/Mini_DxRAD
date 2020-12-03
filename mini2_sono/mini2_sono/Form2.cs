using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using Aspose.Imaging.FileFormats.Dicom;
using Aspose.Imaging.ImageOptions;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Graphics = System.Drawing.Graphics;
using ImageAttributes = System.Drawing.Imaging.ImageAttributes;
using ColorMatrix = System.Drawing.Imaging.ColorMatrix;
using Rectangle = System.Drawing.Rectangle;
using GraphicsUnit = System.Drawing.GraphicsUnit;
using Dicom;
using Point = System.Drawing.Point;
using InterpolationMode = System.Drawing.Drawing2D.InterpolationMode;
using System.Threading;

namespace mini2_sono
{
    public partial class Form2 : Form
    {
        //DICOM 파일 불러오는 폴더 주소(==pacs)
        private String path = "C:\\pacs\\";
        //DICOM 파일을 PictureBox에 띄우기 위해 jpg파일로 변환 해주기 위한 폴더
        private String savePath = "C:\\mini_jpg\\";
        //이미지 밝기 조절을 위한 비트맵 변수 생성
        private Bitmap original = null;

        //---------------
        //이미지 처리를 위한 변수 선언
        private Point LastPoint;

        private double ratio = 1.0F;
        private Point imgPoint;
        private Rectangle imgRect;
        private Point clickPoint;




        public Form2()
        {
            InitializeComponent();
            //마우스 이벤트는 따로 선언해 줘야 작동가능

            pictureBox10.MouseWheel += new MouseEventHandler(imgZoom_MouseWheel);
            pictureBox10.Paint += new PaintEventHandler(pictureBox10_Paint);
            pictureBox10.MouseDown += new MouseEventHandler(pictureBox10_MouseDown);
            pictureBox10.MouseMove += new MouseEventHandler(pictureBox10_MouseMove);


            //for문을 돌리기 위해 선언한 pictureBox 배열
            PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };
            for (int i = 0; i < 8; i++)
            {
                pB[i].MouseDoubleClick += new MouseEventHandler(imgZoom);
                pB[i].MouseDown += new MouseEventHandler(imgDelete);
                pB[i].MouseWheel += new MouseEventHandler(imgZoom_MouseWheel);

            }

        }
        private void Form2_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.WindowState = FormWindowState.Maximized;

        }

        //마우스휠로 zoomin 하는 이벤트 
        private void imgZoom_MouseWheel(object sender, MouseEventArgs e)
        {
            if (pictureBox10.Image == null)
            {
                PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };
                for (int i = 0; i < 8; i++)
                {
                    if (pB[i] == (PictureBox)sender)
                    {

                        if (pB[i].Image == null)
                        {
                            return;
                        }

                        pictureBox10.Refresh();
                        pictureBox10.Image = pB[i].Image;
                        original = (Bitmap)pictureBox10.Image;

                        //마우스 이벤트를 위한 좌표와 확대/축소를 위한 사각형,확대/축소 비율선언
                        imgPoint = new Point(pictureBox10.Width / 2, pictureBox10.Height / 2);
                        imgRect = new Rectangle(0, 0, pictureBox10.Width, pictureBox10.Height);
                        ratio = 1.0;
                        clickPoint = imgPoint;

                        pictureBox10.Visible = true;
                    }

                }

            }
            else
            {
                pictureBox10.Refresh();
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

                imgRect.Width = (int)Math.Round(pictureBox10.Width * ratio);
                imgRect.Height = (int)Math.Round(pictureBox10.Height * ratio);
                imgRect.X = (int)Math.Round(pb.Width / 2 - imgPoint.X * ratio);
                imgRect.Y = (int)Math.Round(pb.Height / 2 - imgPoint.Y * ratio);

                if (imgRect.X > 0) imgRect.X = 0;

                if (imgRect.Y > 0) imgRect.Y = 0;

                if (imgRect.X + imgRect.Width < pictureBox10.Width) imgRect.X = pictureBox10.Width - imgRect.Width;

                if (imgRect.Y + imgRect.Height < pictureBox10.Height) imgRect.Y = pictureBox10.Height - imgRect.Height;

                pictureBox10.Invalidate();
            }
        }
        //줌인 할때 이미지 보간작업
        private void pictureBox10_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox10.Image != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                e.Graphics.DrawImage(pictureBox10.Image, imgRect);
                pictureBox10.Focus();
            }
        }

        //마우스 클릭 했을 때 발생하는 이벤트

        private void pictureBox10_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                clickPoint = new Point(e.X, e.Y);
            }
            else if (e.Button == MouseButtons.Right)
            {
                pictureBox10.Visible = false;
                pictureBox10.Image = null;
                pictureBox10.Refresh();
            }
            else
            {
                trackBar1.Value = 50;
                trackBar2.Value = 0;
                pictureBox10.Image = AdjustBrightnessContrast(original, trackBar1.Value, trackBar2.Value);
            }
            pictureBox10.Invalidate();
        }

        //마우스가 움직일때 좌표 설정이벤트
        private void pictureBox10_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                imgRect.X = imgRect.X + (int)Math.Round((double)(e.X - clickPoint.X) / 5);
                if (imgRect.X >= 0) imgRect.X = 0;
                if (Math.Abs(imgRect.X) >= Math.Abs(imgRect.Width - pictureBox10.Width)) imgRect.X = -(imgRect.Width - pictureBox10.Width);
                imgRect.Y = imgRect.Y + (int)Math.Round((double)(e.Y - clickPoint.Y) / 5);
                if (imgRect.Y >= 0) imgRect.Y = 0;
                if (Math.Abs(imgRect.Y) >= Math.Abs(imgRect.Height - pictureBox10.Height)) imgRect.Y = -(imgRect.Height - pictureBox10.Height);
            }
            else
            {
                LastPoint = e.Location;
                imgPoint = new Point(e.X, e.Y);
            }

            pictureBox10.Invalidate();
        }

        //더블클릭 시 오른쪽에 해당하는 이미지 크게보기.
        private void imgZoom(object sender, MouseEventArgs e)
        {
            PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };
            for (int i = 0; i < 8; i++)
            {
                if (pB[i] == (PictureBox)sender)
                {
                    //사이즈 맞추기
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox1.Image = pB[i].Image;


                    //이미지 표현하기
                    original = (Bitmap)pictureBox1.Image;

                    //값 초기화
                    trackBar1.Value = 50;
                    trackBar2.Value = 0;



                    pictureBox1.Invalidate();
                }
            }
        }

        //오른클릭시 해당하는 이미지 단일 삭제.
        private void imgDelete(object sender, MouseEventArgs e)
        {
            PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };
            if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < 8; i++)
                {
                    //picturbox 여러 개 중 해당하는 picturebox 이미지 삭제
                    if (pB[i] == (PictureBox)sender)
                    {
                        if (pB[i].Image == null) return;
                        if (pictureBox1.Image == null) return;

                        pB[i].Image.Dispose();
                        pB[i].Image = null;
                        pictureBox1.Image.Dispose();
                        pictureBox1.Image = null;
                    }
                }
            }
        }


        //프로세스 연결 해제를 위한 작업.    
        private void Image_null()
        {
            original = null;
            //for문을 돌리기 위해 선언한 pictureBox 배열
            PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };
            pictureBox1.Image = null;
            for (int i = 0; i < 8; i++)
            {
                pB[i].Image = null;
                pB[i].Invalidate();
            }
            label5.Text = null;
            label6.Text = null;
            label7.Text = null;
            label8.Text = null;

            pictureBox1.Invalidate();
            pictureBox10.Invalidate();
            trackBar1.Value = 50;
            trackBar2.Value = 0;
        }

        //Import 버튼 ,DICOM 파일을 불러오기 위한 이벤트
        private void button1_Click(object sender, EventArgs e)
        {

            openFileDialog1.Filter = "DICOM File (*.dcm)|*.dcm*|All file|*.*";
            openFileDialog1.InitialDirectory = path;
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

        //Reset버튼,DICOM 파일을 사용 후 목록 전체 지우기와 변환한 jpg 파일 전체 삭제를 위한 이벤트
        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();

            //이미지뷰의 작업 모두 삭제 후 빈 화면 만들기
            Image_null();

            //DICOM 파일 변환 한 jpg파일이 모여있는 폴더 삭제를 위한 작업
            DirectoryInfo di = new DirectoryInfo("C:\\mini_jpg");

            if (di.GetFiles() != null)
            {
                foreach (FileInfo fi in di.GetFiles())
                {
                    //파일 삭제 시 프로세스 반환이 안되는 문제를 해결하기 위해
                    //GC 수동 작업.
                    original = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    fi.Delete();

                }
            }
            else
            {
                return;
            }
        }

        //Open 버튼,리스트에 불러온 DICOM 파일을 jpg 파일로 변환 후 이미지로 보기 위한 이벤트
        private void button2_Click(object sender, EventArgs e)
        {
            //리스트에 선택된 파일이 없다면 아무기능 하지않기
            if (listBox1.SelectedItem == null)
            {
                return;
            }
            else
            {
                pictureBox1.Image = null;
                fileOpen();
            }
        }

        //파일 변환 및 보기를 위한 메서드
        private void fileOpen()
        {
            //이미지 밝기조절용 비트맵을 초기화.
            original = null;


            //리스트박스의 이름 가져오기
            String fname = (String)listBox1.SelectedItem;
            String filename = Path.GetFileNameWithoutExtension(fname) + ".jpg";


            //파일이 이미 존재 한다면 파일 생성하지않고 비어있는 picturebox에 이미지 표현하기.
            DirectoryInfo di = new DirectoryInfo("C:\\mini_jpg");
            if (di.GetFiles() != null)
            {
                foreach (FileInfo fi in di.GetFiles())
                {
                    if (fi.Name.Equals(filename))
                    {
                        pictureBox_Open();
                    }
                }
            }



            using (var fileStream = new FileStream(path + fname, FileMode.Open, FileAccess.Read))
            using (DicomImage image = new DicomImage(fileStream))
            {
                foreach (FileInfo fi in di.GetFiles())
                {
                    if (fi.Name.Equals(filename))
                    {
                        return;
                    }
                }
                // Save as JPEG
                image.Save(savePath + filename, new JpegOptions());
            }


            pictureBox_Open();


            //값 초기화
            trackBar1.Value = 50;
            trackBar2.Value = 0;



        }

        //비어있는 pictureBox 에 이미지 적용하기 위한 메서드.
        private void pictureBox_Open()
        {
            String fname = (String)listBox1.SelectedItem;
            String filename = Path.GetFileNameWithoutExtension(fname) + ".jpg";
            Image images = null;

            //for문 사용해서 picturebox 2~9까지 적용하기.
            //if문으로 picturebox 2~9까지 이미지 열려있는지 확인하기.
            //확인 후 해당하는 picturebox 에 이미지 열기.

            //for문을 돌리기 위해 선언한 pictureBox 배열
            PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };

            for (int i = 0; i < 8; i++)
            {


                if (pB[i].Image == null)
                {
                    //사이즈 맞추기
                    pB[i].SizeMode = PictureBoxSizeMode.Zoom;

                    //이미지 표현하기
                    images = Image.FromFile(savePath + filename);
                    pB[i].Image = images;
                    original = (Bitmap)images;


                    DicomFile dicomFile = new DicomFile();
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


                    break;
                }
                else
                {
                    continue;
                }

            }
        }


        //All 버튼,listbox에 있는 목록의 전체 아이템들을 한번에 오픈하기 위한 메서드
        private void button3_Click(object sender, EventArgs e)
        {

            if (listBox1.SelectedItem == null)
            {
                return;
            }

            DirectoryInfo di = new DirectoryInfo("C:\\mini_jpg");
            Image images = null;

            //picturebox 에서 이미지 해제 하기 구현하기위한 배열선언
            PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };

            foreach (string file in listBox1.Items)
            {

                String filename = Path.GetFileNameWithoutExtension(file) + ".jpg";

                using (var fileStream = new FileStream(path + file, FileMode.Open, FileAccess.Read))
                using (DicomImage image = new DicomImage(fileStream))
                {
                    foreach (FileInfo fi in di.GetFiles())
                    {
                        if (fi.Name.Equals(filename))
                        {
                            return;
                        }
                    }
                    // Save as JPEG
                    image.Save(savePath + filename, new JpegOptions());
                }
            }


            for (int i = 0; i < 8; i++)
            {

                if (pB[i].Image != null)
                {
                    continue;
                }
                string file=(string)listBox1.Items[i];
                string filename = Path.GetFileNameWithoutExtension(file) + ".jpg";
                //사이즈 맞추기
                pB[i].SizeMode = PictureBoxSizeMode.Zoom;

                //이미지 표현하기
                images = Image.FromFile(savePath + filename);
                pB[i].Image = images;
                original = (Bitmap)images;

                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = pB[0].Image;


                DicomFile dicomFile = new DicomFile();
                dicomFile = DicomFile.Open(path + file, FileReadOption.ReadAll);
                //dicomFile.WriteToConsole(); 태그확인용 


                string Id = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientID);
                string Name = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName);
                string Sex = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientSex);
                string Birth = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientBirthDate);
                label5.Text = Id;
                label6.Text = Name;
                label7.Text = Sex;
                label8.Text = Birth;




            }




        }

        //Save 버튼 클릭시 화면 캡쳐 후 mini_jpg 폴더에 jpg 파일로 저장
        private void button5_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = savePath;
            saveFileDialog1.DefaultExt = "jpg";
            saveFileDialog1.Filter = "JPEG File(*.jpg)|*.jpg|Bitmap File(*.bmp)|*.bmp|PNG File(*.png)|*.png";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

            }
            saveFileDialog1.Dispose();

            //화면캡쳐시 savefiledialog 가 같이 찍히는 현상을 막기 위한 Thread 주기
            Thread.Sleep(1000);

            Rectangle bounds = Screen.PrimaryScreen.WorkingArea;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                bitmap.Save(saveFileDialog1.FileName, ImageFormat.Jpeg);
            }

        }



        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            trackBar_Scroll(sender, e);
        }
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            trackBar_Scroll(sender, e);
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            if (pictureBox10.Image == null) return;
            pictureBox10.Image = AdjustBrightnessContrast(original, trackBar1.Value, trackBar2.Value);
        }


        private Bitmap AdjustBrightnessContrast(Image image, int contrastValue, int brightnessValue)
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


    }
}


