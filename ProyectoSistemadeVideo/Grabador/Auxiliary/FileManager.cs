using System;
using System.IO;

namespace RecordingStudio
{
    // Clase que se encarga de mover los archivos de log de cada ejecución, una vez que no hace falta,
    // a la carpeta que le corresponda según año, mes y día
    class FileManager
    {
        static string strCurrentDirectory = System.Environment.CurrentDirectory + "\\";
        static string debugDir = strCurrentDirectory + RecordingStudio.Properties.Settings.Default.DebugDir;

        public static void checkFolders()
        {
            string strDirectory = strCurrentDirectory + RecordingStudio.Properties.Settings.Default.LogsDir;
            createDirectory(strDirectory);
            strDirectory = strCurrentDirectory + RecordingStudio.Properties.Settings.Default.DebugDir;
            createDirectory(strDirectory);
            strDirectory = strCurrentDirectory + RecordingStudio.Properties.Settings.Default.VideoDir;
            createDirectory(strDirectory);
            strDirectory = strCurrentDirectory + RecordingStudio.Properties.Settings.Default.VideoDir + "\\output";
            createDirectory(strDirectory);
            move();
        }

        private static void createDirectory(string strDirectory)
        {
            if (!Directory.Exists(strDirectory))
            {
                Directory.CreateDirectory(strDirectory);
            }
        }

        // Mueve todos los archivos que no hayan sido movidos ya a la carpeta correspondiente
        internal static void move()
        {
            string[] filePaths = Directory.GetFiles(debugDir);

            foreach (string filePath in filePaths)
            {
                FileManager.moveFile(filePath, debugDir);
            }
        }

        // Mueve un archivo
        internal static void moveFile(string filePath, string activeDir)
        {
            string[] completeDate = filePath.Split('.');

            string[] dateDay = completeDate[0].Split('_');

            string[] date = dateDay[0].Split('-');

            // Crear carpeta del año si no existe ya
            string newPath = Path.Combine(activeDir, date[3]);

            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }

            // Crear carpeta del mes si no existe ya
            newPath = Path.Combine(newPath, date[2]);

            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }

            // Crear nombre del archivo si no existe ya
            newPath = Path.Combine(newPath, Path.GetFileName(filePath));

            try
            {
                File.Move(filePath, newPath);
            }
            catch (Exception e)
            {
                Logger.writeError(e.ToString());
            }
        }
    }
}
