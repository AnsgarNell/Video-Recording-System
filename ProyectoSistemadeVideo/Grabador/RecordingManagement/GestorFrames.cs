using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using AForge.Video.FFMPEG;
using System.IO;
using System.Drawing.Drawing2D;
using System.Timers;
using System.Runtime.ExceptionServices;
using System.Security;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Grabador.gestorVideos
{
    class GestorFrames
    {
        VideoFileWriter writer;

        bool primer;

        public int framesTotal;
        int logoTime = 5 * 25;

        public DateTime inicio;

        static public int FPS = 25;
        static public VideoCodec codec = VideoCodec.MPEG4;
        private GestorGrabacion gestorGrabacion;

        public GestorFrames(GestorGrabacion gestorGrabacion)
        {
            this.gestorGrabacion = gestorGrabacion;
            framesTotal = 0;
            primer = true;
        }

        private void prepareVideoFile(int width, int height)
        {
            // create instance of video writer
            writer = new VideoFileWriter();    
            writer.Open
            (
                gestorGrabacion.videoFile,
                width,
                height,
                FPS,
                codec,
                3500000
            ); 
        }

        //[HandleProcessCorruptedStateExceptions]
        //[SecurityCritical]
        public void guardarFrame(Bitmap imagen)
        {
            framesTotal++;
            /*
            // Ponemos el logo durante el tiempo indicado
            if (framesTotal < logoTime)
            {
                second = Superimpose(second);
            }
             */ 
            try
            {
                if (primer)
                {
                    // We have a frame, so prepare video file
                    prepareVideoFile(imagen.Width, imagen.Height);
                    inicio = DateTime.Now;
                    writer.WriteVideoFrame(imagen);
                    primer = false;
                }
                else
                {
                    TimeSpan timeSpan = new TimeSpan();
                    timeSpan = DateTime.Now - inicio;
                    writer.WriteVideoFrame(imagen, timeSpan);
                }
            }
            catch (Exception ex)
            {
                Logger.writeError("Fallo al guardar un frame " + ex.ToString());
            }
        }

        public Bitmap Superimpose(Bitmap largeBmp)
        {
            Bitmap smallBmp = new Bitmap(Grabador.Properties.Settings.Default.LogoFile);
            Graphics g = Graphics.FromImage(largeBmp);
            g.CompositingMode = CompositingMode.SourceOver;
            smallBmp.MakeTransparent();
            int margin = 5;
            int x = largeBmp.Width - smallBmp.Width - margin;
            int y = largeBmp.Height - smallBmp.Height - margin;
            g.DrawImage(smallBmp, new Point(x, y));
            return largeBmp;
        }

        internal void abortar()
        {
            if (writer != null)
                writer.Close();
        }
    }
}
