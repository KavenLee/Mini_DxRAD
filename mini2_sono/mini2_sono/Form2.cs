using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Graphics = System.Drawing.Graphics;
using ImageAttributes = System.Drawing.Imaging.ImageAttributes;
using ColorMatrix = System.Drawing.Imaging.ColorMatrix;
using Rectangle = System.Drawing.Rectangle;
using GraphicsUnit = System.Drawing.GraphicsUnit;
using Dicom;
using Dicom.Imaging;
using Point = System.Drawing.Point;
using InterpolationMode = System.Drawing.Drawing2D.InterpolationMode;
using System.Threading;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using Dicom.Log;

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
        private DirectoryInfo di = new DirectoryInfo("C:\\mini_jpg");
        //---------------
        //이미지 처리를 위한 변수 선언
        private Point LastPoint;

        private double ratio = 1.0F;
        private Point imgPoint;
        private Rectangle imgRect;
        private Point clickPoint, startPoint, endPoint;
        private double zoomRatio = 1.0F;


        public Form2()
        {
            InitializeComponent();
            //마우스 이벤트는 따로 선언해 줘야 작동가능

            pictureBox10.MouseWheel += new MouseEventHandler(imgZoom_MouseWheel);
            pictureBox10.Paint += new PaintEventHandler(pictureBox10_Paint);
            pictureBox10.MouseDown += new MouseEventHandler(pictureBox10_MouseDown);
            pictureBox10.MouseUp += new MouseEventHandler(pictureBox10_MouseUp);
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
                        imgPoint = new Point(pictureBox10.Width / 3, pictureBox10.Height / 3);
                        imgRect = new Rectangle(0, 0, pictureBox10.Width, pictureBox10.Height);
                        ratio = 1.0;
                        clickPoint = imgPoint;

                        pictureBox10.Visible = true;

                        break;
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

                zoomRatio = ratio;

                imgRect.Width = (int)Math.Round(pictureBox10.Width * ratio);
                imgRect.Height = (int)Math.Round(pictureBox10.Height * ratio);
                imgRect.X = (int)Math.Round(pb.Width / 2 - imgPoint.X * ratio);
                imgRect.Y = (int)Math.Round(pb.Height / 2 - imgPoint.Y * ratio);

                if (imgRect.X > 0) imgRect.X = 0;

                if (imgRect.Y > 0) imgRect.Y = 0;

                if (imgRect.X + imgRect.Width < pictureBox10.Width) imgRect.X = pictureBox10.Width - imgRect.Width;

                if (imgRect.Y + imgRect.Height < pictureBox10.Height) imgRect.Y = pictureBox10.Height - imgRect.Height;

            }

            pictureBox10.Invalidate();

        }


        //줌인 할때 이미지 보간작업
        private void pictureBox10_Paint(object sender, PaintEventArgs e)
        {
            //이미지에 선그리기 작업 시 보간작업
            if (this.Cursor == Cursors.Hand && pictureBox10.Image != null)
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.DrawLine(Pens.Red, startPoint, endPoint);
            }
            else if (pictureBox10.Image != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                pictureBox10.Focus();
            }
            e.Graphics.DrawImage(original, imgRect);
        }

        //마우스 클릭 했을 때 발생하는 이벤트

        private void pictureBox10_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.Cursor == Cursors.Hand && e.Button == MouseButtons.Left)
            {
                //원본이미지를 찾고 원본이미지와 확대된 이미지 비교 후 정확한 좌표값을 찾기위한 작업
                PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };
                for (int i = 0; i < pB.Length; i++)
                {
                    if (pictureBox10.Image == pB[i].Image)
                    {
                        double wRatio = (double)pB[i].Image.Width / pictureBox10.Width;
                        double hRatio = (double)pB[i].Image.Height / pictureBox10.Height;

                        startPoint = new Point((int)((e.X - imgRect.X) * wRatio / zoomRatio), (int)((e.Y - imgRect.Y) * hRatio / zoomRatio));
                        break;
                    }
                }

            }
            else if (e.Button == MouseButtons.Left)
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

        //마우스 클릭을 땟을 때 발생하는 이벤트
        private void pictureBox10_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.Cursor == Cursors.Hand && e.Button == MouseButtons.Left)
            {
                Pen p = new Pen(Brushes.Red, 1);
                p.DashStyle = DashStyle.Solid;
                p.EndCap = LineCap.ArrowAnchor;

                Graphics g = Graphics.FromImage(original);


                g.DrawLine(p, startPoint, endPoint);

                p.Dispose();
                g.Dispose();


                startPoint = Point.Empty;
                endPoint = Point.Empty;
            }
        }



        //마우스가 움직일때 좌표 설정이벤트
        private void pictureBox10_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.Cursor == Cursors.Hand && e.Button == MouseButtons.Left)
            {
                //원본이미지를 찾고 원본이미지와 확대된 이미지 비교 후 정확한 좌표값을 찾기위한 작업
                PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };
                for (int i = 0; i < pB.Length; i++)
                {
                    if (pictureBox10.Image == pB[i].Image)
                    {
                        double wRatio = (double)pB[i].Image.Width / pictureBox10.Width;
                        double hRatio = (double)pB[i].Image.Height / pictureBox10.Height;

                        endPoint = new Point((int)((e.X - imgRect.X) * wRatio / zoomRatio), (int)((e.Y - imgRect.Y) * hRatio / zoomRatio));
                        break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                imgRect.X = imgRect.X + (int)Math.Round((double)(e.X - clickPoint.X) / 8);
                if (imgRect.X >= 0) imgRect.X = 0;
                if (Math.Abs(imgRect.X) >= Math.Abs(imgRect.Width - pictureBox10.Width)) imgRect.X = -(imgRect.Width - pictureBox10.Width);
                imgRect.Y = imgRect.Y + (int)Math.Round((double)(e.Y - clickPoint.Y) / 8);
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
                    if (pB[i].Equals((PictureBox)sender))
                    {
                        if (pB[i].Image == null) return;
                        if (pictureBox1.Image == null) return;

                        if (pB[i].Image == pictureBox1.Image)
                        {
                            pictureBox1.Image.Dispose();
                            pictureBox1.Image = null;
                        }

                        pB[i].Image.Dispose();
                        pB[i].Image = null;


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
            pictureBox10.Image = null;
            for (int i = 0; i < pB.Length; i++)
            {

                pB[i].Image = null;
            }
            label5.Text = null;
            label6.Text = null;
            label7.Text = null;
            label8.Text = null;
            label12.Text = null;


            label5.Dispose();
            label6.Dispose();
            label7.Dispose();
            label8.Dispose();
            label12.Dispose();


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
                fileOpen();
            }
        }

        //파일 변환 및 보기를 위한 메서드
        private void fileOpen()
        {

            //리스트박스의 이름 가져오기
            String fname = (String)listBox1.SelectedItem;
            String filename = Path.GetFileNameWithoutExtension(fname) + ".jpg";


            //파일이 이미 존재 한다면 파일 생성하지않고 비어있는 picturebox에 이미지 표현하기.

            //방법 1
            try
            {

                //Dicom To Jpeg   
                using (var fileStream = new FileStream(path + fname, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {

                    DicomFile df = DicomFile.Open(fileStream);
                    dicom_Tag(fname, df);

                    DicomImage image = new DicomImage(df.Dataset);

                    string jpgPath = Path.Combine(savePath, filename);
                    Bitmap renderImage = image.RenderImage().As<Bitmap>();

                    PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };


                    foreach (FileInfo fi in di.GetFiles())
                    {

                        if (fi.Name.Equals(filename))
                        {
                            pictureBox_Open(fname, filename);
                            break;
                        }

                    }
                    for (int i = 0; i < pB.Length; i++)
                    {
                        if (pB[i].Image == null)
                        {
                            pB[i].Image = renderImage;
                            break;
                        }
                        else
                        {
                            continue;
                        }

                    }

                    if (File.Exists(jpgPath))
                    {
                        return;
                    }
                    else
                    {
                        renderImage.Save(jpgPath, ImageFormat.Jpeg);
                    }

                    renderImage.Dispose();
                    fileStream.Close();

                }

            }
            catch (DicomFileException)
            {
                MessageBox.Show("File Already Created", "File don't Created");
            }

            //방법 2
            //using (IImage image2 = image.RenderImage())
            //{
            //    original = image2.AsSharedBitmap();
            //    original.Save(jpgPath, ImageFormat.Jpeg);
            //    image2.Dispose();
            //}



            //값 초기화
            trackBar1.Value = 50;
            trackBar2.Value = 0;

        }

        //비어있는 pictureBox 에 이미지 적용하기 위한 메서드.
        private void pictureBox_Open(string file, string filename)
        {

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

                    if (pictureBox1.Image == null)
                    {
                        pictureBox1.Image = pB[i].Image;
                    }


                    using (var fileStream = new FileStream(path + file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {

                        DicomFile df = DicomFile.Open(fileStream);

                        dicom_Tag(file, df);

                    }

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


            Image images = null;

            //picturebox 에서 이미지 해제 하기 구현하기위한 배열선언
            PictureBox[] pB = new PictureBox[8] { pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6, pictureBox7, pictureBox8, pictureBox9 };

            foreach (string file in listBox1.Items)
            {

                String filename = Path.GetFileNameWithoutExtension(file) + ".jpg";

                foreach (FileInfo fi in di.GetFiles())
                {
                    if (fi.Name.Equals(filename))
                    {
                        pictureBox_Open(file, filename);
                    }
                    else
                    {
                        break;
                    }

                }
                //Dicom To Jpeg   
                var fileStream = new FileStream(path + file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                DicomFile df = DicomFile.Open(fileStream);
                DicomImage image = new DicomImage(df.Dataset);
                string jpgPath = Path.Combine(savePath, filename);
                Bitmap renderImage = image.RenderImage().As<Bitmap>();
                original = renderImage;

                if (File.Exists(jpgPath))
                {
                    return;
                }
                else
                {
                    renderImage.Save(jpgPath, ImageFormat.Jpeg);
                }

                renderImage.Dispose();
                fileStream.Close();

            }


            for (int i = 0; i < pB.Length; i++)
            {
                string file = (string)listBox1.Items[i];

                if (pB[i].Image != null)
                {
                    continue;
                }
                string filename = Path.GetFileNameWithoutExtension(file) + ".jpg";
                //사이즈 맞추기
                pB[i].SizeMode = PictureBoxSizeMode.Zoom;

                //이미지 표현하기
                images = Image.FromFile(savePath + filename);
                pB[i].Image = images;

                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                if (pictureBox1.Image == null)
                {
                    pictureBox1.Image = pB[0].Image;
                }
                pictureBox1.Invalidate();

                using (var fileStream = new FileStream(path + file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    DicomFile df = DicomFile.Open(fileStream);
                    dicom_Tag(file, df);

                }



            }

        }


        //dicom tag 표시 하기위한 메서드
        private void dicom_Tag(string file, DicomFile dicomFile)
        {

            try
            {

                dicomFile.WriteToConsole(); //태그확인용 
                dicomFile.Dataset.AddOrUpdate(DicomTag.RegionOfResidence, "Normal");
                dicomFile.Dataset.AddOrUpdate(DicomTag.PatientTelephoneNumbers, "01000000000");
                dicomFile.Save(file);


                if (dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientID) == null)
                {
                    label5.Text = "anonymous";
                }
                else
                {
                    string Id = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientID);
                    label5.Text = Id;
                }


                if (dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName) == null)
                {
                    label6.Text = "anonymous";
                }
                else
                {
                    string Name = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName);
                    label6.Text = Name;
                }


                if (dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientSex) == null)
                {
                    label7.Text = "anonymous";
                }
                else
                {
                    string Sex = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientSex);
                    label7.Text = Sex;
                }


                if (dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientBirthDate) == null)
                {
                    label8.Text = "anonymous";
                }
                else
                {
                    string Birth = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientBirthDate);
                    label8.Text = Birth;
                }

                string residence = dicomFile.Dataset.GetSingleValue<string>(DicomTag.RegionOfResidence);

                label12.Text = residence;



            }
            catch (DicomFileException)
            {
                MessageBox.Show("Image Already Opened", "Can't Open Image");
            }
            catch (DicomDataException)
            {
                label5.Text = "anonymous";
                label6.Text = "anonymous";
                label7.Text = "anonymous";
                label8.Text = "anonymous";
            }
        }



        //Save 버튼 클릭시 화면 캡쳐 후 mini_jpg 폴더에 jpg 파일로 저장
        private void button5_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = savePath;
            saveFileDialog1.DefaultExt = "jpg";
            saveFileDialog1.Filter = "JPEG File(*.jpg)|*.jpg|Bitmap File(*.bmp)|*.bmp|PNG File(*.png)|*.png";
            saveFileDialog1.ShowDialog();


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

                g.SmoothingMode = SmoothingMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighSpeed;
                g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
                return bitmap;

            }
        }

        //Draw 버튼, 클릭시 마우스 커서 모양 변경 후 그림 그리기 위한 준비.
        private void button6_Click(object sender, EventArgs e)
        {

            if (this.Cursor == Cursors.Hand)
            {
                this.Cursor = DefaultCursor;
            }
            else
            {
                this.Cursor = Cursors.Hand;
            }


        }
    }
}


