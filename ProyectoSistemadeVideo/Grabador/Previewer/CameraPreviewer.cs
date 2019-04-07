using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

using AForge.Video;
using AForge.Video.DirectShow;

namespace RecordingStudio
{
    public class CameraPreviewer : GroupBox
    {
        private PictureBox pictureBox;
        private Button previewButton;
        private Bitmap oldFrame;
        public VideoCaptureDevice captureDevice;
        private DevicesManager devicesManager;
        public int identifier { get; set; }
        public int hub { get; set; }
        public string DShowID { get; set; }
        public bool enabled;

        public CameraPreviewer()
        { }

        public CameraPreviewer(int identifier, int hub, VideoCaptureDevice captureDevice, DevicesManager devicesManager, string DShowID)
        {            
            this.Text = "Cam " + identifier;
            this.identifier = identifier;
            this.hub = hub;
            this.Font = new Font(this.Font.FontFamily, 12);
            this.Height = 321;
            this.Width = 332;
            this.ForeColor = Color.Gainsboro;
            this.BackColor = Color.Gray;

            pictureBox = new PictureBox();
            pictureBox.Height = 240;
            pictureBox.Width = 320;           

            this.Controls.Add(pictureBox);
            pictureBox.Location = new Point(6, 19);

            previewButton = new Button();
            previewButton.Text = "PREVIEW";
            previewButton.Font = new Font(previewButton.Font.FontFamily, 16);
            previewButton.Height = 50;
            previewButton.Width = 157;
            previewButton.Click += PreviewButton_Click;
            previewButton.ForeColor = Color.Gainsboro;
            previewButton.FlatStyle = FlatStyle.Flat;
            previewButton.FlatAppearance.BorderColor = Color.Black;

            this.Controls.Add(previewButton);
            previewButton.Location = new Point(88, 265);

            this.captureDevice = captureDevice;
			// TODO: If videocapabilities are null, the driver must be changed or computer reset after changing it
            this.captureDevice.VideoResolution = this.captureDevice.VideoCapabilities[5];
            this.captureDevice.NewFrame += video_FirstFrame;

            // At this point capture will stop automatically after first frame is received
            this.devicesManager = devicesManager;
            this.DShowID = DShowID;
        }

        public void Start()
        {
            captureDevice.Start();
        }

        public void Stop()
        {
            captureDevice.SignalToStop();
        }

        public void Disable()
        {
            //if (!enabled) return;
            //captureDevice.SignalToStop();
            //previewButton.Enabled = false;
            //oldFrame = (Bitmap)pictureBox.Image;
            //pictureBox.Image = MakeGrayscale3(oldFrame);
            //enabled = false;

            previewButton.Enabled = false;
        }

        public void Enable()
        {
            if (enabled) return;
            previewButton.Enabled = true;
            pictureBox.Image = oldFrame;
            enabled = true;
        }

        public string getMoniker()
        {
            return captureDevice.Source;
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            devicesManager.StartPreview(identifier);
            if (captureDevice.IsRunning)
            {
                captureDevice.SignalToStop();
            }
            else captureDevice.Start();
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap img = (Bitmap)eventArgs.Frame.Clone();
            pictureBox.Image = img;
        }

        private void video_FirstFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap img = (Bitmap)eventArgs.Frame.Clone();
            if (img == null) return;
            this.captureDevice.NewFrame -= video_FirstFrame;
            pictureBox.Image = img;
            captureDevice.SignalToStop();            
            this.captureDevice.NewFrame += video_NewFrame;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);           
        }

        private static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                    new float[] {.3f, .3f, .3f, 0, 0},
                    new float[] {.59f, .59f, .59f, 0, 0},
                    new float[] {.11f, .11f, .11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }
    }
}
