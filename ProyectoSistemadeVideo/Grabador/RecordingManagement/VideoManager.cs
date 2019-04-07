using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Media;
using System.IO;

namespace RecordingStudio
{
    // Clase que se encarga de la grabación del vídeo
    class VideoManager
    {
        DevicesManager devicesManager;

        public event EventHandler MaxVideoFilesReached;
        
        bool grabando = false;

        bool recording1 = true;
        bool recording2 = true;
        
        int videoCount = 0;

        public double[] startTimes = new double[1024];

        Thread t;
        private Object thisLock = new Object();

        public VideoManager(DevicesManager devicesManager)
        {
            this.devicesManager = devicesManager;
        }

        protected virtual void OnMaxVideoFilesReached(EventArgs e)
        {
            EventHandler handler = MaxVideoFilesReached;
            if (handler != null)
            {
                handler.BeginInvoke(this, e, new AsyncCallback(MaxVideoFilesReachedCompleted), null);
            }
        }

        private void MaxVideoFilesReachedCompleted(IAsyncResult ar)
        {
            EventHandler handler = MaxVideoFilesReached;
            if (handler != null)
            {
                handler.EndInvoke(ar);
            }
        }

        public void Start()
        {
            grabando = true;
            t = new Thread(VideoRecorder);
            t.Start();
        }

        // Procedimiento consistente en el bucle principal de grabación de vídeos
        private void VideoRecorder(object obj)
        {
            Thread singleThread = null;
	        int timer = 0;			    // Each video random duration (in ms)
	        int i = 0;

            

            i = 0;
	        while (grabando)
	        {
                string camIdentifier = "";

                // Get next scene duration
                timer = getRandomTime();

                // Get next scene camera
                camIdentifier = devicesManager.nextCamId();

                if(i % 2 == 0)
                    recording1 = true;
                else
                    recording2 = true;

                SingleRecord singleRecord = new SingleRecord(camIdentifier, i);
                singleThread = new Thread(SingleVideoRecorder);
                singleThread.Start(singleRecord);

                // Pause and wait until recording has started
                Thread.Sleep(1500);

                // Thread is alive so recording is being done
                if(singleThread.IsAlive)
                {
                    if (i % 2 == 0)
                        recording2 = false;
                    else
                        recording1 = false;
                    Thread.Sleep(timer);
                    i++;
                    // As ffmpeg is used via cmd, we can´t have more than 1000 (more or less) video files,
                    // so we have to abort the recording if this limit is exceeded
                    if (i == 1000)
                    {
                        OnMaxVideoFilesReached(EventArgs.Empty);
                    }
                }
                // Recording failed so change again desired hub
                else
                    devicesManager.nextCamId();
            }

            recording1 = false;
            recording2 = false;
            singleThread.Join();
            videoCount = i;
        }

        // Procedimiento para grabar un vídeo por USB
        private void SingleVideoRecorder(object obj)
        {
            Process proc = new Process();

            SingleRecord singleRecord = (SingleRecord) obj;
            proc.StartInfo.FileName = "cmd.exe";      

            // Start scene recording
            string str = "";
            str = String.Format("ffmpeg -s 1920x1080 -f dshow -vcodec h264 -i video=\"{0}\" -copyinkf -copyts -vcodec copy -y {1}.mp4", singleRecord.camIdentifier, singleRecord.i);

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

                if (singleRecord.i % 2 == 0)
                {
                    if (!recording1)
                    {
                        // If recording has finished send "q" and exit this loop
                        proc.StandardInput.WriteLine("q");
                        //break;
                    }
                }
                else
                {
                    if (!recording2)
                    {
                        // If recording has finished send "q" and exit this loop
                        proc.StandardInput.WriteLine("q");
                        //break;
                    }
                }
            }

            if (procOutput.ToString().Contains("Input/output error"))
            {
                Logger.writeError(procOutput.ToString());
            }

            else
            {

                double startTime = 0.0;
                string output = procOutput.ToString();

                int found = output.IndexOf("start: ");

                if (found > 0)
                {
                    int found_dot = output.IndexOf(".", found);
                    int found_end = output.IndexOf(", ", found);
                    found = found + 7;
                    startTime = double.Parse(output.Substring(found, found_dot - found + 4), CultureInfo.InvariantCulture);

                    if (singleRecord.i > 0)
                    {
                        // Get video start time difference
                        double difference = startTime - startTimes[singleRecord.i - 1];

                        // Get it in milliseconds to make calcs easier
                        int duration = (int)(difference * 1000.0);

                        int msec = duration % 1000;
                        duration = duration / 1000;
                        int hours = duration / 3600;
                        int minutes = (duration - (hours * 3600)) / 60;
                        int seconds = (duration - (hours * 3600) - (minutes * 60));

                        str = String.Format("ffmpeg -t {0:00}:{1:00}:{2:00}.{3:000} -i {4}.mp4 -c copy -bsf:v h264_mp4toannexb -f mpegts -copyts -y {4}.ts", hours, minutes, seconds, msec, singleRecord.i - 1);
                        RecordingManager.exec_command(str);

                        // Delete original file
                        str = String.Format("{0}.mp4", singleRecord.i - 1);
                        File.Delete(str);
                    }
                }

                lock (thisLock)
                {
                    startTimes[singleRecord.i] = startTime;
                }
            }

            // Wait for the process to exit
            proc.WaitForExit();
        }

        // Procedimiento para grabar un vídeo por IP
        private void SingleVideoRecorderIP(object obj)
        {
            Process proc = new Process();

            SingleRecord singleRecord = (SingleRecord)obj;
            proc.StartInfo.FileName = "cmd.exe";

            // Start scene recording
            string str = "";
            str = String.Format("ffmpeg -i \"{0}\" -c copy -y {1}.mp4", singleRecord.camIdentifier, singleRecord.i);

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

                if (singleRecord.i % 2 == 0)
                {
                    if (!recording1)
                    {
                        // If recording has finished send "q" and exit this loop
                        proc.StandardInput.WriteLine("q");
                        //break;
                    }
                }
                else
                {
                    if (!recording2)
                    {
                        // If recording has finished send "q" and exit this loop
                        proc.StandardInput.WriteLine("q");
                        //break;
                    }
                }
            }

            if (procOutput.ToString().Contains("Input/output error"))
            {
                Logger.writeError(procOutput.ToString());
            }

            else
            {

                double startTime = 0.0;
                string output = procOutput.ToString();

                int found = output.IndexOf("start: ");

                if (found > 0)
                {
                    int found_dot = output.IndexOf(".", found);
                    int found_end = output.IndexOf(", ", found);
                    found = found + 7;
                    startTime = double.Parse(output.Substring(found, found_dot - found + 4), CultureInfo.InvariantCulture);

                    if (singleRecord.i > 0)
                    {
                        // Get video start time difference
                        double difference = startTime - startTimes[singleRecord.i - 1];

                        // Get it in milliseconds to make calcs easier
                        int duration = (int)(difference * 1000.0);

                        int msec = duration % 1000;
                        duration = duration / 1000;
                        int hours = duration / 3600;
                        int minutes = (duration - (hours * 3600)) / 60;
                        int seconds = (duration - (hours * 3600) - (minutes * 60));

                        str = String.Format("ffmpeg -t {0:00}:{1:00}:{2:00}.{3:000} -i {4}.mp4 -c copy -bsf:v h264_mp4toannexb -f mpegts -copyts -y {4}.ts", hours, minutes, seconds, msec, singleRecord.i - 1);
                        RecordingManager.exec_command(str);

                        // Delete original file
                        str = String.Format("{0}.mp4", singleRecord.i - 1);
                        File.Delete(str);
                    }
                }

                lock (thisLock)
                {
                    startTimes[singleRecord.i] = startTime;
                }
            }

            // Wait for the process to exit
            proc.WaitForExit();
        }

        private static int getRandomTime()
        {
            Random random = new Random();
            int time = random.Next(7000, 15000);
            return time;
        }

        internal int abortar()
        {
            grabando = false;
            t.Join();
            return videoCount;
        }
    }

    class SingleRecord
    {
        public string camIdentifier { get; set; }
        public int i { get; set; }

        public SingleRecord(string camIdentifier, int i)
        {
            this.camIdentifier = camIdentifier;
            this.i = i;
        }
    }
}
