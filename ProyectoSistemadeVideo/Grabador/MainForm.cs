using System;
using System.Windows.Forms;

namespace RecordingStudio
{
    // Clase principal de la aplicación, contiene la ventana inicial
    public partial class MainForm : Form
    {
        bool grabando = false;        
        string strCurrentDirectory = System.Environment.CurrentDirectory + "\\";

        private LogoForm logoForm;
        private RecordingManager recorder;
        private DevicesManager devicesManager;

        public MainForm()
        {
            logoForm = new LogoForm();
            logoForm.Show();
            InitializeComponent();
            FileManager.checkFolders();
            Logger.writeInfo("Application STARTED");
            devicesManager = new DevicesManager(this);
            recorder = new RecordingManager(devicesManager);
            recorder.videoManager.MaxVideoFilesReached += btnRec_Click;
        }

        public void AddPreviewer(CameraPreviewer cameraPreviewer)
        {
            previewersPanel.Controls.Add(cameraPreviewer);
        }

        public void enableRec()
        {
            btnRec.Enabled = true;
        }

        private void btnRec_Click(object sender, EventArgs e)
        {
            if (grabando)
            {
                grabando = false;
                stopRecording();
                btnRec.Text = "REC";
            }
            else
            {
                grabando = true;
                Logger.writeInfo("REC button pushed");
                btnRec.Text = "STOP";
                devicesManager.StopPreview();
                recorder.record();
            }                     
        }

        private void stopRecording()
        {
            btnRec.Enabled = false;
            recorder.abort();
            btnRec.Enabled = true;
            Logger.writeInfo("Pulsado botón STOP");  
        }

        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            devicesManager.StopPreview();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            logoForm.Close();
        }
    }
}
