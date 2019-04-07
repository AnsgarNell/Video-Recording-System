using System;
using System.IO;

namespace RecordingStudio
{
    // Clase para registrar la información y errores de la aplicación, y mostrar ventanas con el mismo fin
    static class Logger
    {
        static string strCurrentDirectory = System.Environment.CurrentDirectory + "\\";
        public static string debugFile = Path.Combine(strCurrentDirectory + RecordingStudio.Properties.Settings.Default.DebugDir, string.Format("Debug-{0:dd-MM-yyyy_HH-mm_ss_fff}.log", DateTime.Now));

        public static void writeInfo(string text)
        {
            write("INFO: " + text);
        }

        public static void writeError(string text)
        {
            write("ERROR: " + text);
        }

        private static void write(string text)
        {
            // create a writer and open the file in append mode
            TextWriter tw = new StreamWriter(debugFile, true);

            // write a line of text to the file
            string fileLine = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + text;
            tw.WriteLine(fileLine);

            // close the stream
            tw.Close();
        }
    }
}
