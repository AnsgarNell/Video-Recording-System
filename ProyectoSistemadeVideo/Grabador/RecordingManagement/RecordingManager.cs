using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace RecordingStudio
{
    // Clase para coordinar la grabación entre el vídeo y el audio, así como las cámaras
    class RecordingManager
    {
        static string strCurrentDirectory = System.Environment.CurrentDirectory + "\\";
        static string activeDir = strCurrentDirectory + RecordingStudio.Properties.Settings.Default.VideoDir;
        public string videoFile = "";

        private AudioManager audioManager = new AudioManager();
        public VideoManager videoManager;
        
        public RecordingManager(DevicesManager deviceManager)
        {
            videoManager = new VideoManager(deviceManager);
        }     

        public void record()
        {
            // Create new video file name
            string newFileName = string.Format("Video-{0:dd-MM-yyyy_HH-mm}", DateTime.Now);
            videoFile = newFileName + ".mp4";

            // Start audio recording
            audioManager.Start();

            // Start video recording
            videoManager.Start();
        }

        internal void abort()
        {    
            int videoCount = 0;

            videoCount = videoManager.abortar();
            audioManager.abortar();
            // Calcular tiempo de postproducción
            DateTime a = DateTime.Now;
            postProduccion(videoCount);
            DateTime b = DateTime.Now;
            Logger.writeInfo("Tiempo postproducción: " + b.Subtract(a).TotalMinutes);
            //subirArchivoVideo(videoFile);
        }

        private void postProduccion(int video_count)
        {
            string str = "";
            int i = 0;
            double difference = 0.0;

	        // Prepare video concatenation
	        StringBuilder concat_files = new StringBuilder();	
	        concat_files.Append("ffmpeg -i \"concat:");
	
	        // And then we cut every video except the last one
	        for (i = 0; i < video_count; i++)
	        {
                // If this is the last video or no start time was stored for the next video 
		        // (possibly next video does not exist) don't cut the video
		        if(i == (video_count -1))
		        {
                    str = String.Format("ffmpeg -i {0}.mp4 -c copy -bsf:v h264_mp4toannexb -f mpegts -copyts -y {0}.ts", i);
                    exec_command(str);

                    // Delete original file
                    str = String.Format("{0}.mp4", i);
                    File.Delete(str);
		        }

                str = String.Format("{0}.ts", i);

		        // Check if file was created
		        if (File.Exists(str)) 
		        {
                    if (i == 0)
                    {
                        concat_files.Append(str);
                    }
                    else
                    {
                        concat_files.Append("|" + str);
                    }
		        } 
	        }
	
	        // Concat video files
            concat_files.Append("\" -c copy -copyts -start_at_zero -y videos/output_fixed.mp4");
	        exec_command(concat_files.ToString());
	
	        // Delete .ts files
            string currentDir = Environment.CurrentDirectory;
            DirectoryInfo di = new DirectoryInfo(currentDir);
            FileInfo[] files = di.GetFiles("*.ts")
                                 .Where(p => p.Extension == ".ts").ToArray();
            foreach (FileInfo file in files)
            try
            {
                file.Attributes = FileAttributes.Normal;
                File.Delete(file.FullName);
            }
            catch { }
	
	        exec_command("ffmpeg -i videos/audio.m4a -c copy -copyts -start_at_zero -y videos/audio_fixed.m4a");
	        File.Delete("videos/audio.m4a");

	        // Calculate difference between audio and video start times, and apply offset
            // NOTE: an extra positive delay of 50 ms (aprox) is mandatory, reason unknown 
	        difference = audioManager.startTime + 0.050 - videoManager.startTimes[0];
	
	        // Mix audio and video, and create final file
            str = String.Format(new System.Globalization.CultureInfo("en-US"), "ffmpeg -itsoffset {0:0.000} -i videos/audio_fixed.m4a -i videos/output_fixed.mp4 -c copy -copyts -map 0:0 -map 1:0 -y videos/output/{1}", difference, videoFile);
            //str = String.Format(new System.Globalization.CultureInfo("en-US"), "ffmpeg -itsoffset {0:0.000} -i videos/audio_fixed.m4a -i videos/output_fixed.mp4 -c copy -copyts -map 0:0 -map 1:0 -y videos/output_video.mp4", difference);
            exec_command(str);

            // Delete temporal files
            File.Delete("videos/audio_fixed.m4a");
            File.Delete("videos/output_fixed.mp4");

            /*
            // Concatenar créditos con el vídeo
            exec_command("ffmpeg -i videos/output_video.mp4 -c copy -bsf:v h264_mp4toannexb -f mpegts -y videos/output_video.ts");
            str = String.Format(new System.Globalization.CultureInfo("en-US"), "ffmpeg -f concat -i videos/mylist.txt -c copy -bsf:a aac_adtstoasc -y videos/output/{0}", videoFile);
            exec_command(str);
             */ 
        }   

        public static void exec_command(string str)
        {
            Process proc = new Process();

            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = @"/c " + str;

            // Redirect standard input & error output (as FFMPEG uses this) for the process
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;

            // UseShellExecute must be false to redirect input/output
            proc.StartInfo.UseShellExecute = false;

            // Start the process
            proc.Start();

            // Use a StringBuilder to hold the program's output
            StringBuilder procOutput = new StringBuilder();

            // Read the process's output until recording is finished
            while (!proc.StandardError.EndOfStream)
            {
                string result = proc.StandardError.ReadLine();

                // Append the chars we just read to the procOutput StringBuilder
                procOutput.Append(result);
            }

            // Now that we've sent "q" wait for the process to exit
            proc.WaitForExit();
        }
    }
}
