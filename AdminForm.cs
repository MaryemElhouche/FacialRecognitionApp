using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Face;
using OpenCvSharp.Dnn;
using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;

namespace FacialRecognitionApp
{
    public partial class AdminForm : Form
    {
        private CascadeClassifier? faceCascade;
        private FaceRecognizer? lbphRecognizer;
        private readonly List<string> personNames = new();
        private List<Mat> trainingImages = new();
        private List<int> trainingLabels = new();
        private readonly Dictionary<int, string> labelToName = new();

        // Interface utilisateur
        private PictureBox? pictureBoxReference;
        private Button? btnLoadReference;
        private Button? btnTrain;
        private ListBox? listBoxPersons;
        private Button? btnAddPerson;
        private TextBox? textBoxPersonName;
        private Button? btnDeletePerson;
        private Button? btnBack;

        public AdminForm( string username )
        {
            // Vérification des droits admin
            var adminLines = File.ReadAllLines( "admin_data.txt" );
            if (!adminLines.Any( line => line.StartsWith( username + ":" ) ))
            {
                MessageBox.Show( "Accès refusé - Droits administrateur requis" );
                this.Close();
                return;
            }

            InitializeAdminForm();
            InitializeOpenCV();
        }

        private void InitializeAdminForm()
        {
            this.Size = new System.Drawing.Size( 900, 600 );
            this.Text = "Mode Administrateur - Gestion des Visages";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font( "Segoe UI", 9 );

            // PictureBox pour l'image de référence
            pictureBoxReference = new PictureBox
            {
                Location = new System.Drawing.Point( 50, 20 ),
                Size = new System.Drawing.Size( 300, 300 ),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add( pictureBoxReference );

            // Bouton Charger Image
            btnLoadReference = new Button
            {
                Text = "Charger",
                Location = new System.Drawing.Point( 50, 340 ),
                Size = new System.Drawing.Size( 80, 30 ),
                BackColor = Color.LightSkyBlue,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnLoadReference.FlatAppearance.BorderSize = 1;
            btnLoadReference.Click += BtnLoadReference_Click;
            this.Controls.Add( btnLoadReference );

            // Gestion des personnes - Label
            var labelPersons = new Label
            {
                Text = "Personnes enregistrées:",
                Location = new System.Drawing.Point( 400, 20 ),
                Size = new System.Drawing.Size( 180, 20 )
            };
            this.Controls.Add( labelPersons );

            // Liste des personnes
            listBoxPersons = new ListBox
            {
                Location = new System.Drawing.Point( 400, 50 ),
                Size = new System.Drawing.Size( 200, 200 )
            };
            this.Controls.Add( listBoxPersons );

            // Champ pour le nom
            textBoxPersonName = new TextBox
            {
                Location = new System.Drawing.Point( 400, 270 ),
                Size = new System.Drawing.Size( 120, 25 ),
                PlaceholderText = "Nom de la personne"
            };
            this.Controls.Add( textBoxPersonName );

            // Bouton Ajouter
            btnAddPerson = new Button
            {
                Text = "Ajouter",
                Location = new System.Drawing.Point( 530, 270 ),
                Size = new System.Drawing.Size( 80, 30 ),
                BackColor = Color.LightGreen,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnAddPerson.FlatAppearance.BorderSize = 1;
            btnAddPerson.Click += BtnAddPerson_Click;
            this.Controls.Add( btnAddPerson );

            // Bouton Entraîner
            btnTrain = new Button
            {
                Text = "Entraîner",
                Location = new System.Drawing.Point( 400, 320 ),
                Size = new System.Drawing.Size( 80, 30 ),
                BackColor = Color.Khaki,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnTrain.FlatAppearance.BorderSize = 1;
            btnTrain.Click += BtnTrain_Click;
            this.Controls.Add( btnTrain );

            // Bouton Supprimer
            btnDeletePerson = new Button
            {
                Text = "Supprimer",
                Location = new System.Drawing.Point( 530, 320 ),
                Size = new System.Drawing.Size( 80, 30 ),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDeletePerson.FlatAppearance.BorderSize = 1;
            btnDeletePerson.Click += BtnDeletePerson_Click;
            this.Controls.Add( btnDeletePerson );

            // Bouton Retour
            btnBack = new Button
            {
                Text = "Retour",
                Location = new System.Drawing.Point( 400, 500 ),
                Size = new System.Drawing.Size( 80, 30 ),
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

            // Chargement des données existantes
            LoadExistingPersons();
        }


        private void LoadExistingPersons()
        {
            try
            {
                string namesPath = Path.Combine( Application.StartupPath, "TrainingData", "names.txt" );
                if (File.Exists( namesPath ))
                {
                    var lines = File.ReadAllLines( namesPath );
                    foreach (var line in lines)
                    {
                        var parts = line.Split( ':' );
                        if (parts.Length == 2)
                        {
                            personNames.Add( parts[1] );
                            listBoxPersons.Items.Add( parts[1] );
                            if (int.TryParse( parts[0], out int label ))
                            {
                                labelToName[label] = parts[1];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Erreur lors du chargement des personnes: {ex.Message}" );
            }
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

                lbphRecognizer = LBPHFaceRecognizer.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Erreur lors de l'initialisation d'OpenCV: {ex.Message}",
                              "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void BtnLoadReference_Click( object sender, EventArgs e )
        {
            using OpenFileDialog openFileDialog = new()
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Sélectionner une image de référence",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using Mat image = Cv2.ImRead( openFileDialog.FileName, ImreadModes.Color );
                if (image.Empty())
                {
                    MessageBox.Show( "Impossible de charger l'image. Format non supporté ou fichier corrompu.",
                                 "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }

                pictureBoxReference.Image = BitmapConverter.ToBitmap( image.Clone() );

                if (string.IsNullOrWhiteSpace( textBoxPersonName.Text ))
                {
                    MessageBox.Show( "Veuillez d'abord entrer un nom pour la personne.",
                                  "Information", MessageBoxButtons.OK, MessageBoxIcon.Information );
                    return;
                }

                AddTrainingImage( image, textBoxPersonName.Text.Trim() );
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Erreur critique : {ex.Message}\n\nVeuillez essayer avec une autre image.",
                              "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void AddTrainingImage( Mat image, string personName )
        {
            try
            {
                if (image.Empty() || image.Channels() < 3)
                    throw new ArgumentException( "Image vide ou format non supporté" );

                if (string.IsNullOrWhiteSpace( personName ))
                    throw new ArgumentException( "Le nom ne peut pas être vide" );

                personName = personName.Trim();

                using Mat grayImage = new Mat();
                Cv2.CvtColor( image, grayImage, ColorConversionCodes.BGR2GRAY );
                Cv2.EqualizeHist( grayImage, grayImage );

                using Mat processedImage = new Mat();
                Cv2.GaussianBlur( grayImage, processedImage, new Size( 3, 3 ), 0 );

                var faces = faceCascade.DetectMultiScale(
                    processedImage,
                    scaleFactor: 1.05,
                    minNeighbors: 4,
                    flags: HaarDetectionTypes.DoRoughSearch | HaarDetectionTypes.ScaleImage,
                    minSize: new Size( 80, 80 ) );

                if (faces.Length == 0)
                {
                    MessageBox.Show( "Aucun visage détecté dans l'image.",
                                  "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                    return;
                }

                var mainFace = faces.OrderByDescending( f => f.Width * f.Height ).First();
                using Mat faceROI = new Mat( processedImage, mainFace );
                Cv2.Resize( faceROI, faceROI, new Size( 150, 150 ) );

                using Mat laplacian = new Mat();
                Cv2.Laplacian( faceROI, laplacian, MatType.CV_64F );
                double sharpness = Cv2.Mean( laplacian.Mul( laplacian ) ).Val0;

                if (sharpness < 100)
                {
                    MessageBox.Show( $"Image trop floue (qualité : {sharpness:F2}/500)\nVeuillez utiliser une image plus nette.",
                                  "Qualité insuffisante", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                    return;
                }

                int label = personNames.IndexOf( personName );
                bool isNewPerson = (label == -1);

                if (isNewPerson)
                {
                    label = personNames.Count;
                    personNames.Add( personName );
                    listBoxPersons.Items.Add( personName );
                }

                trainingImages.Add( faceROI.Clone() );
                trainingLabels.Add( label );
                labelToName[label] = personName;

                string trainingDir = Path.Combine( Application.StartupPath, "TrainingData" );
                Directory.CreateDirectory( trainingDir );

                string savePath = Path.Combine( trainingDir, $"{personName}_{label}_{trainingLabels.Count( l => l == label )}.jpg" );
                Cv2.ImWrite( savePath, faceROI );

                MessageBox.Show( $"Image ajoutée avec succès pour {personName}",
                              "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            catch (Exception ex)
            {
                string errorLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name} : {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText( "error_log.txt", errorLog );

                MessageBox.Show( $"Erreur technique :\n{ex.Message}",
                               "Erreur critique", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void BtnAddPerson_Click( object sender, EventArgs e )
        {
            if (string.IsNullOrEmpty( textBoxPersonName.Text ))
            {
                MessageBox.Show( "Veuillez entrer un nom pour la personne.", "Attention",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning );
                return;
            }

            string personName = textBoxPersonName.Text.Trim();

            if (personNames.Contains( personName ))
            {
                MessageBox.Show( "Cette personne est déjà enregistrée.", "Attention",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning );
                return;
            }

            if (pictureBoxReference.Image == null)
            {
                MessageBox.Show( "Veuillez d'abord charger une image de référence.", "Attention",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning );
                return;
            }

            personNames.Add( personName );
            listBoxPersons.Items.Add( personName );
            textBoxPersonName.Clear();
        }

        private void BtnTrain_Click( object sender, EventArgs e )
        {
            try
            {
                if (lbphRecognizer == null)
                {
                    throw new InvalidOperationException( "Le modèle LBPH n'est pas initialisé" );
                }

                if (trainingImages.Count == 0)
                {
                    MessageBox.Show( "Aucune image disponible pour l'entraînement.",
                                  "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                    return;
                }

                if (trainingImages.Count != trainingLabels.Count)
                {
                    MessageBox.Show( "Incohérence entre le nombre d'images et de labels.",
                                  "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }

                var watch = System.Diagnostics.Stopwatch.StartNew();
                lbphRecognizer.Train( trainingImages, trainingLabels );
                watch.Stop();

                string trainingDir = Path.Combine( Application.StartupPath, "TrainingData" );
                Directory.CreateDirectory( trainingDir );

                string modelPath = Path.Combine( trainingDir, "lbph_model.xml" );
                lbphRecognizer.Save( modelPath );

                string namesPath = Path.Combine( trainingDir, "names.txt" );
                using (var writer = new StreamWriter( namesPath ))
                {
                    foreach (var kvp in labelToName)
                    {
                        writer.WriteLine( $"{kvp.Key}:{kvp.Value}" );
                    }
                }

                MessageBox.Show( $"Modèle entraîné avec succès en {watch.Elapsed.TotalSeconds:F2} secondes.\n" +
                               $"{trainingImages.Count} images utilisées.",
                               "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            catch (Exception ex)
            {
                string errorMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n" +
                                 $"ERREUR: {ex.Message}\n" +
                                 $"Stack Trace: {ex.StackTrace}\n\n";

                File.AppendAllText( "training_errors.log", errorMsg );

                MessageBox.Show( $"Échec de l'entraînement:\n{ex.Message}",
                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void BtnDeletePerson_Click( object sender, EventArgs e )
        {
            try
            {
                if (listBoxPersons.SelectedIndex == -1)
                {
                    MessageBox.Show( "Veuillez sélectionner une personne à supprimer.",
                                  "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                    return;
                }

                var result = MessageBox.Show( $"Êtes-vous sûr de vouloir supprimer '{listBoxPersons.SelectedItem}' ?",
                                           "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question );

                if (result != DialogResult.Yes) return;

                int selectedIndex = listBoxPersons.SelectedIndex;
                string personName = listBoxPersons.SelectedItem.ToString();

                personNames.RemoveAt( selectedIndex );

                List<int> indexesToRemove = new List<int>();
                for (int i = 0; i < trainingLabels.Count; i++)
                {
                    if (trainingLabels[i] == selectedIndex)
                    {
                        indexesToRemove.Add( i );
                    }
                    else if (trainingLabels[i] > selectedIndex)
                    {
                        trainingLabels[i]--;
                    }
                }

                foreach (int i in indexesToRemove.OrderByDescending( x => x ))
                {
                    trainingImages[i].Dispose();
                    trainingImages.RemoveAt( i );
                    trainingLabels.RemoveAt( i );
                }

                labelToName.Clear();
                for (int i = 0; i < personNames.Count; i++)
                {
                    labelToName[i] = personNames[i];
                }

                listBoxPersons.Items.RemoveAt( selectedIndex );
                DeletePersonFiles( personName );

                MessageBox.Show( $"Personne '{personName}' supprimée avec succès.",
                               "Information", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Erreur lors de la suppression : {ex.Message}",
                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void DeletePersonFiles( string personName )
        {
            try
            {
                string trainingDir = Path.Combine( Application.StartupPath, "TrainingData" );
                if (Directory.Exists( trainingDir ))
                {
                    foreach (var file in Directory.GetFiles( trainingDir, $"{personName}_*" ))
                    {
                        try { File.Delete( file ); }
                        catch { }
                    }
                }

                // Après suppression, mettre à jour le fichier names.txt
                string namesPath = Path.Combine( trainingDir, "names.txt" );
                if (File.Exists( namesPath ))
                {
                    using (var writer = new StreamWriter( namesPath ))
                    {
                        for (int i = 0; i < personNames.Count; i++)
                        {
                            writer.WriteLine( $"{i}:{personNames[i]}" );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText( "deletion_errors.log", $"[{DateTime.Now}] Erreur suppression fichiers: {ex}\n" );
            }
        }

        private void AdminForm_Load( object sender, EventArgs e )
        {

        }
    }
}