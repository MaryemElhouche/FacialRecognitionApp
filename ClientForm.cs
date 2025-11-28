using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Face;
using OpenCvSharp.Dnn;
using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;
using System.Diagnostics;

namespace FacialRecognitionApp
{
    public partial class ClientForm : Form
    {
        private VideoCapture? capture;
        private Mat? frame;
        private CascadeClassifier? faceCascade;
        private FaceRecognizer? lbphRecognizer;
        private Net? dnnNet;
        private bool isCapturing = false;
        private readonly Dictionary<int, string> labelToName = new();
        private System.Windows.Forms.Timer? frameTimer;

        // Interface utilisateur
        private PictureBox? pictureBoxCamera;
        private Button? btnStartCamera;
        private Button? btnStopCamera;
        private ComboBox? comboBoxMethod;
        private Label? labelResult;
        private Label? labelConfidence;
        private Button? btnBack;

        public ClientForm()
        {
            InitializeClientForm();
            InitializeOpenCV();
            LoadTrainedModel();
        }

        private void InitializeClientForm()
        {
            this.Size = new System.Drawing.Size( 900, 600 );
            this.Text = "Mode Client - Reconnaissance Faciale";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font( "Segoe UI", 9 );

            // PictureBox pour la caméra
            pictureBoxCamera = new PictureBox
            {
                Location = new System.Drawing.Point( 50, 20 ),
                Size = new System.Drawing.Size( 480, 360 ),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add( pictureBoxCamera );

            // Label "Méthode"
            var labelMethod = new Label
            {
                Text = "Méthode:",
                Location = new System.Drawing.Point( 50, 400 ),
                Size = new System.Drawing.Size( 60, 20 )
            };
            this.Controls.Add( labelMethod );

            // ComboBox Méthode
            comboBoxMethod = new ComboBox
            {
                Location = new System.Drawing.Point( 120, 400 ),
                Size = new System.Drawing.Size( 80, 30 ),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxMethod.Items.AddRange( new string[] { "LBPH", "DNN" } );
            comboBoxMethod.SelectedIndex = 0;
            this.Controls.Add( comboBoxMethod );

            // Bouton Démarrer
            btnStartCamera = new Button
            {
                Text = "Démarrer",
                Location = new System.Drawing.Point( 220, 400 ),
                Size = new System.Drawing.Size( 90, 30 ),
                BackColor = Color.LightGreen,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnStartCamera.FlatAppearance.BorderSize = 1;
            btnStartCamera.Click += BtnStartCamera_Click;
            this.Controls.Add( btnStartCamera );

            // Bouton Arrêter
            btnStopCamera = new Button
            {
                Text = "Arrêter",
                Location = new System.Drawing.Point( 320, 400 ),
                Size = new System.Drawing.Size( 90, 30 ),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnStopCamera.FlatAppearance.BorderSize = 1;
            btnStopCamera.Click += BtnStopCamera_Click;
            this.Controls.Add( btnStopCamera );

            // Zone des résultats (Panel)
            var resultsPanel = new Panel
            {
                Location = new System.Drawing.Point( 550, 20 ),
                Size = new System.Drawing.Size( 300, 150 ),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add( resultsPanel );

            // Résultat affiché
            labelResult = new Label
            {
                Text = "Résultat: Aucun",
                Location = new System.Drawing.Point( 10, 20 ),
                Size = new System.Drawing.Size( 280, 60 ),
                Font = new Font( "Arial", 12, FontStyle.Bold ),
                ForeColor = Color.Blue
            };
            resultsPanel.Controls.Add( labelResult );

            // Confiance affichée
            labelConfidence = new Label
            {
                Text = "Confiance: 0%",
                Location = new System.Drawing.Point( 10, 90 ),
                Size = new System.Drawing.Size( 280, 40 ),
                Font = new Font( "Arial", 10 )
            };
            resultsPanel.Controls.Add( labelConfidence );

            // Bouton Retour
            btnBack = new Button
            {
                Text = "Retour",
                Location = new System.Drawing.Point( 550, 400 ),
                Size = new System.Drawing.Size( 90, 30 ),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font( "Arial", 10 ),
                FlatStyle = FlatStyle.Flat
            };
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.Click += ( s, e ) =>
            {
                this.Hide();
                var form1 = new Form1();
                form1.ShowDialog();
                this.Close();
            };
            this.Controls.Add( btnBack );

            // Timer pour la capture vidéo
            frameTimer = new System.Windows.Forms.Timer
            {
                Interval = 33 // ~30 FPS
            };
            frameTimer.Tick += FrameTimer_Tick;
        }


        private void InitializeOpenCV()
        {
            try
            {
                string appDirectory = Path.GetDirectoryName( Application.ExecutablePath ) ?? Environment.CurrentDirectory;
                string cascadePath = Path.Combine( appDirectory, "haarcascade_frontalface_alt.xml" );

                if (!File.Exists( cascadePath ))
                {
                    MessageBox.Show( $"Fichier haarcascade_frontalface_alt.xml non trouvé dans :\n{cascadePath}",
                                  "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }

                faceCascade = new CascadeClassifier( cascadePath );

                if (faceCascade.Empty())
                {
                    MessageBox.Show( "ERREUR : Le classificateur Haar n'a pas pu être chargé.",
                                   "Erreur Critique", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }

                // Initialisation du réseau DNN
                InitializeDNN();
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Erreur lors de l'initialisation d'OpenCV: {ex.Message}",
                              "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void InitializeDNN()
        {
            try
            {
                string appDirectory = Path.GetDirectoryName( Application.ExecutablePath ) ?? Environment.CurrentDirectory;
                string prototxtPath = Path.Combine( appDirectory, "deploy.prototxt" );
                string modelPath = Path.Combine( appDirectory, "res10_300x300_ssd_iter_140000.caffemodel" );

                if (File.Exists( prototxtPath ) && File.Exists( modelPath ))
                {
                    dnnNet = CvDnn.ReadNetFromCaffe( prototxtPath, modelPath );

                    if (dnnNet.Empty())
                    {
                        MessageBox.Show( "Impossible de charger le réseau DNN. Les fichiers pourraient être corrompus.",
                                      "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                        dnnNet = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Erreur lors de l'initialisation du DNN: {ex.Message}",
                              "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void LoadTrainedModel()
        {
            try
            {
                string trainingDir = Path.Combine( Application.StartupPath, "TrainingData" );
                string modelPath = Path.Combine( trainingDir, "lbph_model.xml" );

                if (File.Exists( modelPath ))
                {
                    lbphRecognizer = LBPHFaceRecognizer.Create();
                    lbphRecognizer.Read( modelPath );

                    // Charger les noms associés aux labels
                    string namesPath = Path.Combine( trainingDir, "names.txt" );
                    if (File.Exists( namesPath ))
                    {
                        var lines = File.ReadAllLines( namesPath );
                        foreach (var line in lines)
                        {
                            var parts = line.Split( ':' );
                            if (parts.Length == 2 && int.TryParse( parts[0], out int label ))
                            {
                                labelToName[label] = parts[1];
                            }
                        }
                    }
                    // Charger toutes les images d'entraînement sauvegardées
                    foreach (var file in Directory.GetFiles( trainingDir, "*.jpg" ))
                    {
                        try
                        {
                            using (Mat img = Cv2.ImRead( file, ImreadModes.Grayscale ))
                            {
                                if (!img.Empty())
                                {
                                    // Extraire le label du nom de fichier (format: Nom_label_numero.jpg)
                                    var fileName = Path.GetFileNameWithoutExtension( file );
                                    var parts = fileName.Split( '_' );
                                    if (parts.Length >= 2 && int.TryParse( parts[1], out int label ))
                                    {
                                        Cv2.Resize( img, img, new Size( 100, 100 ) );
                                        lbphRecognizer.Update( new List<Mat> { img }, new List<int> { label } );
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine( $"Erreur lors du chargement de l'image {file}: {ex.Message}" );
                        }
                    }
                }
                else
                {
                    MessageBox.Show( "Aucun modèle entraîné trouvé. Veuillez d'abord entraîner un modèle en mode administrateur.",
                                  "Information", MessageBoxButtons.OK, MessageBoxIcon.Information );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Erreur lors du chargement du modèle entraîné: {ex.Message}",
                              "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }





        private void BtnStartCamera_Click( object? sender, EventArgs e )
        {
            try
            {
                // Vérifier que la méthode de reconnaissance est sélectionnée
                if (comboBoxMethod.SelectedItem == null)
                {
                    MessageBox.Show( "Veuillez sélectionner une méthode de reconnaissance",
                                  "Configuration manquante",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Warning );
                    return;
                }

                // Vérifier que le modèle est chargé pour LBPH
                if (comboBoxMethod.SelectedItem.ToString() == "LBPH" && lbphRecognizer == null)
                {
                    MessageBox.Show( "Le modèle LBPH n'est pas chargé. Veuillez entraîner un modèle en mode admin d'abord.",
                                  "Modèle non chargé",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error );
                    return;
                }

                // Vérifier que le réseau DNN est chargé si sélectionné
                if (comboBoxMethod.SelectedItem.ToString() == "DNN" && dnnNet == null)
                {
                    MessageBox.Show( "Le modèle DNN n'est pas disponible. Vérifiez les fichiers deploy.prototxt et .caffemodel",
                                  "Modèle DNN manquant",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error );
                    return;
                }

                // Initialiser la capture vidéo
                capture = new VideoCapture( 0 );

                // Vérifier que la caméra est ouverte
                if (!capture.IsOpened())
                {
                    MessageBox.Show( "Impossible d'ouvrir la caméra. Vérifiez qu'une caméra est connectée.",
                                  "Erreur caméra",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error );
                    capture.Dispose();
                    return;
                }

                // Configurer les propriétés de la caméra
                capture.Set( VideoCaptureProperties.FrameWidth, 640 );
                capture.Set( VideoCaptureProperties.FrameHeight, 480 );
                capture.Set( VideoCaptureProperties.Fps, 30 );

                // Démarrer la capture
                isCapturing = true;
                frameTimer?.Start();

                // Mettre à jour l'interface
                btnStartCamera.Enabled = false;
                btnStopCamera.Enabled = true;
                labelResult.Text = "Caméra démarrée...";
                labelConfidence.Text = "Prêt pour reconnaissance";

                // Journaliser le démarrage
                Debug.WriteLine( $"Capture démarrée avec la méthode: {comboBoxMethod.SelectedItem}" );
            }
            catch (Exception ex)
            {
                // Gestion complète des erreurs
                string errorMessage = $"Erreur lors du démarrage de la caméra:\n{ex.Message}";

                MessageBox.Show( errorMessage,
                              "Erreur critique",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error );

                // Nettoyage des ressources en cas d'erreur
                if (capture != null && capture.IsOpened())
                {
                    capture.Release();
                    capture.Dispose();
                }

                // Réactiver l'interface
                btnStartCamera.Enabled = true;
                btnStopCamera.Enabled = false;
                labelResult.Text = "Erreur de démarrage";
                labelConfidence.Text = "Veuillez réessayer";

                // Journaliser l'erreur
                Debug.WriteLine( $"Erreur BtnStartCamera_Click: {ex}" );
            }
        }


        private void BtnStopCamera_Click( object? sender, EventArgs e )
        {
            StopCamera();
        }


        private void StopCamera()
        {
            try
            {
                isCapturing = false;

                if (frameTimer != null)
                {
                    frameTimer.Stop();
                }

                if (capture != null && !capture.IsDisposed)
                {
                    capture.Release();
                }

                if (pictureBoxCamera != null && !pictureBoxCamera.IsDisposed)
                {
                    pictureBoxCamera.Image?.Dispose();
                    pictureBoxCamera.Image = null;
                }

                btnStartCamera.Enabled = true;
                btnStopCamera.Enabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine( $"Erreur lors de l'arrêt de la caméra: {ex.Message}" );
            }
        }





        private void FrameTimer_Tick( object? sender, EventArgs e )
        {
            if (!isCapturing || capture == null) return;

            try
            {
                frame = new Mat();
                capture.Read( frame );

                if (frame.Empty()) return;

                ProcessFaceRecognition( frame );

                // Afficher l'image
                pictureBoxCamera.Image = BitmapConverter.ToBitmap( frame );
            }
            catch (Exception ex)
            {
                labelResult.Text = $"Erreur: {ex.Message}";
            }
        }



        private void ProcessFaceRecognition( Mat image )
        {
            Mat grayImage = new Mat();
            Cv2.CvtColor( image, grayImage, ColorConversionCodes.BGR2GRAY );

            if (comboBoxMethod.SelectedItem.ToString() == "LBPH" && lbphRecognizer != null)
            {
                ProcessLBPH( image, grayImage );
            }
            else if (comboBoxMethod.SelectedItem.ToString() == "DNN" && dnnNet != null)
            {
                ProcessDNN( image );
            }
        }

        private void ProcessLBPH( Mat colorImage, Mat grayImage )
        {
            try
            {
                var faces = faceCascade.DetectMultiScale(
                    grayImage,
                    1.1,
                    3,
                    HaarDetectionTypes.ScaleImage,
                    new Size( 30, 30 ) );

                foreach (var face in faces)
                {
                    Cv2.Rectangle( colorImage, face, Scalar.Green, 2 );

                    using Mat faceROI = new Mat( grayImage, face );
                    Cv2.Resize( faceROI, faceROI, new Size( 100, 100 ) );

                    lbphRecognizer.Predict( faceROI, out int label, out double confidence );

                    string name = labelToName.ContainsKey( label ) && confidence < 80
                        ? labelToName[label]
                        : "Inconnu";

                    Cv2.PutText(
                        colorImage,
                        $"{name} ({100 - confidence:F1}%)",
                        new Point( face.X, face.Y - 10 ),
                        HersheyFonts.HersheySimplex,
                        0.9,
                        Scalar.Green,
                        2 );

                    // Mise à jour de l'UI
                    BeginInvoke( (Action)(() =>
                    {
                        labelResult.Text = $"Résultat: {name}";
                        labelConfidence.Text = $"Confiance: {100 - confidence:F1}%";
                    }) );
                }
            }
            catch (Exception ex)
            {
                BeginInvoke( (Action)(() =>
                    labelResult.Text = $"Erreur LBPH: {ex.Message}") );
            }
        }




        private void ProcessDNN( Mat image )
        {
            try
            {
                int imageHeight = image.Height;
                int imageWidth = image.Width;

                // Créer un blob à partir de l'image
                Mat blob = CvDnn.BlobFromImage( image, 1.0, new Size( 300, 300 ), new Scalar( 104, 117, 123 ) );
                dnnNet.SetInput( blob );

                // Faire la prédiction
                Mat detection = dnnNet.Forward();

                // Traiter les détections
                Mat detectionMat = Mat.FromPixelData( detection.Size( 2 ), detection.Size( 3 ), MatType.CV_32F, detection.Ptr( 0 ) );

                for (int i = 0; i < detectionMat.Rows; i++)
                {
                    float confidence = detectionMat.At<float>( i, 2 );

                    if (confidence > 0.5) // Seuil de confiance
                    {
                        int x1 = (int)(detectionMat.At<float>( i, 3 ) * imageWidth);
                        int y1 = (int)(detectionMat.At<float>( i, 4 ) * imageHeight);
                        int x2 = (int)(detectionMat.At<float>( i, 5 ) * imageWidth);
                        int y2 = (int)(detectionMat.At<float>( i, 6 ) * imageHeight);

                        Rect faceRect = new Rect( x1, y1, x2 - x1, y2 - y1 );
                        Cv2.Rectangle( image, faceRect, Scalar.Green, 2 );

                        Cv2.PutText( image, "Visage detecté", new Point( x1, y1 - 10 ),
                                   HersheyFonts.HersheySimplex, 0.9, Scalar.Green, 2 );

                        BeginInvoke( (Action)(() =>
                        {
                            labelResult.Text = "Résultat: Visage détecté (DNN)";
                            labelConfidence.Text = $"Confiance: {confidence * 100:F1}%";
                        }) );
                    }
                }
            }
            catch (Exception ex)
            {
                BeginInvoke( (Action)(() =>
                    labelResult.Text = $"Erreur DNN: {ex.Message}") );
            }
        }



        protected override void OnFormClosed( FormClosedEventArgs e )
        {
            try
            {
                // 1. Arrêt sécurisé de la caméra
                if (isCapturing)
                {
                    StopCamera();
                }

                // 2. Libération des ressources avec vérification de null et de l'état disposed
                if (frameTimer != null)
                {
                    frameTimer.Stop();
                    frameTimer.Dispose();
                    frameTimer = null;
                }

                if (capture != null && !capture.IsDisposed)
                {
                    try
                    {
                        capture.Release();
                        capture.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignorer si déjà libéré
                    }
                    capture = null;
                }

                if (faceCascade != null && !faceCascade.IsDisposed)
                {
                    faceCascade.Dispose();
                    faceCascade = null;
                }

                if (lbphRecognizer != null && !lbphRecognizer.IsDisposed)
                {
                    lbphRecognizer.Dispose();
                    lbphRecognizer = null;
                }

                if (dnnNet != null && !dnnNet.IsDisposed)
                {
                    dnnNet.Dispose();
                    dnnNet = null;
                }

                // 3. Nettoyage des images
                if (pictureBoxCamera != null && !pictureBoxCamera.IsDisposed)
                {
                    pictureBoxCamera.Image?.Dispose();
                    pictureBoxCamera.Image = null;
                }

                if (frame != null && !frame.IsDisposed)
                {
                    frame.Dispose();
                    frame = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine( $"Erreur lors du nettoyage des ressources: {ex.Message}" );
            }
            finally
            {
                base.OnFormClosed( e );
            }
        }




        private void ClientForm_Load( object sender, EventArgs e )
        {

        }
    }
}