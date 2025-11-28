using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FacialRecognitionApp
{
    public partial class Form1 : Form
    {
        private const string AdminDataFile = "admin_data.txt";
        private const string ClientDataFile = "client_data.txt";

        [System.Runtime.InteropServices.DllImport( "user32.dll" )]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport( "user32.dll" )]
        public static extern int SendMessage( IntPtr hWnd, int Msg, int wParam, int lParam );

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        public Form1()
        {
            InitializeComponent();
            InitializeLoginForm();
            CreateDataFilesIfNotExist();
        }

        private void Form1_Load( object sender, EventArgs e )
        {
            this.CenterToScreen();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void CreateDataFilesIfNotExist()
        {
            if (!File.Exists( AdminDataFile ))
                File.WriteAllText( AdminDataFile, "admin:admin123" ); // Default admin account

            if (!File.Exists( ClientDataFile ))
                File.Create( ClientDataFile ).Close();
        }

        private void InitializeLoginForm()
        {
            this.Text = "Système de Reconnaissance Faciale - Connexion";
            this.Size = new Size( 450, 400 );
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;  // Pour custom barre titre
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb( 240, 240, 240 );

            // Barre de titre personnalisée rouge
            var titleBar = new Panel
            {
                BackColor = Color.Red,
                Dock = DockStyle.Top,
                Height = 30
            };
            this.Controls.Add( titleBar );
            titleBar.BringToFront();

            var lblTitleBar = new Label
            {
                Text = this.Text,
                ForeColor = Color.White,
                Font = new Font( "Segoe UI", 10, FontStyle.Bold ),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            titleBar.Controls.Add( lblTitleBar );

            titleBar.MouseDown += ( s, e ) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage( this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0 );
                }
            };
            lblTitleBar.MouseDown += ( s, e ) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage( this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0 );
                }
            };

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding( 20, 40, 20, 20 )
            };
            this.Controls.Add( mainPanel );

            var lblTitle = new Label
            {
                Text = "CONNEXION",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font( "Arial", 18, FontStyle.Bold ),
                ForeColor = Color.DarkSlateBlue,
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add( lblTitle );

            int currentY = lblTitle.Bottom + 20;

            // Login
            var lblLogin = new Label
            {
                Text = "Login:",
                Location = new Point( 50, currentY ),
                Size = new Size( 100, 20 ),
                Font = new Font( "Arial", 10, FontStyle.Bold )
            };
            mainPanel.Controls.Add( lblLogin );

            var txtLogin = new TextBox
            {
                Location = new Point( 160, currentY - 3 ),
                Size = new Size( 220, 30 ),
                Font = new Font( "Arial", 10 ),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add( txtLogin );

            currentY += 40;

            // Mot de passe
            var lblPassword = new Label
            {
                Text = "Mot de passe:",
                Location = new Point( 50, currentY ),
                Size = new Size( 130, 20 ), // ← largeur corrigée
                Font = new Font( "Arial", 10, FontStyle.Bold )
            };
            mainPanel.Controls.Add( lblPassword );

            var txtPassword = new TextBox
            {
                Location = new Point( 160, currentY - 3 ),
                Size = new Size( 220, 30 ),
                PasswordChar = '*',
                Font = new Font( "Arial", 10 ),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add( txtPassword );

            currentY += 60;

            int panelWidth = mainPanel.ClientSize.Width;
            int btnWidth = 220;
            int btnX = (panelWidth - btnWidth) / 2;

            var btnLogin = new Button
            {
                Text = "Connexion",
                Location = new Point( btnX, currentY ),
                Size = new Size( btnWidth, 35 ),
                Font = new Font( "Arial", 10, FontStyle.Bold ),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += ( s, e ) => AuthenticateUser( txtLogin.Text, txtPassword.Text );
            mainPanel.Controls.Add( btnLogin );

            currentY += 50;

            int btnSmallWidth = 100;
            int spacing = 20;
            int totalWidth = btnSmallWidth * 2 + spacing;
            int startX = (panelWidth - totalWidth) / 2;

            var btnRegister = new Button
            {
                Text = "Inscription",
                Location = new Point( startX, currentY ),
                Size = new Size( btnSmallWidth, 30 ),
                Font = new Font( "Arial", 9 ),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += ( s, e ) => ShowRegistrationForm();
            mainPanel.Controls.Add( btnRegister );

            var btnQuit = new Button
            {
                Text = "Quitter",
                Location = new Point( startX + btnSmallWidth + spacing, currentY ),
                Size = new Size( btnSmallWidth, 30 ),
                Font = new Font( "Arial", 9 ),
                BackColor = Color.Firebrick,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnQuit.FlatAppearance.BorderSize = 0;
            btnQuit.Click += ( s, e ) => Application.Exit();
            mainPanel.Controls.Add( btnQuit );
        }

        private void AuthenticateUser( string login, string password )
        {
            if (string.IsNullOrWhiteSpace( login ) || string.IsNullOrWhiteSpace( password ))
            {
                MessageBox.Show( "Veuillez entrer un login et un mot de passe valides", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }

            var adminLines = File.ReadAllLines( AdminDataFile );
            foreach (var line in adminLines)
            {
                var parts = line.Split( ':' );
                if (parts.Length == 2 && parts[0] == login && parts[1] == password)
                {
                    OpenAdminInterface( login );
                    return;
                }
            }

            var clientLines = File.ReadAllLines( ClientDataFile );
            foreach (var line in clientLines)
            {
                var parts = line.Split( ':' );
                if (parts.Length == 2 && parts[0] == login && parts[1] == password)
                {
                    OpenClientInterface();
                    return;
                }
            }

            MessageBox.Show( "Login ou mot de passe incorrect", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
        }

        private void ShowRegistrationForm()
        {
            var registerForm = new Form
            {
                Text = "Inscription",
                Size = new Size( 400, 300 ),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            var lblLogin = new Label
            {
                Text = "Nouveau Login:",
                Location = new Point( 50, 50 ),
                Size = new Size( 100, 20 ),
                Font = new Font( "Arial", 10 )
            };

            var txtLogin = new TextBox
            {
                Location = new Point( 160, 50 ),
                Size = new Size( 180, 25 ),
                Font = new Font( "Arial", 10 )
            };

            var lblPassword = new Label
            {
                Text = "Nouveau Mot de passe:",
                Location = new Point( 50, 100 ),
                Size = new Size( 120, 20 ),
                Font = new Font( "Arial", 10 )
            };

            var txtPassword = new TextBox
            {
                Location = new Point( 160, 100 ),
                Size = new Size( 180, 25 ),
                PasswordChar = '*',
                Font = new Font( "Arial", 10 )
            };

            var rbClient = new RadioButton
            {
                Text = "Compte Client",
                Location = new Point( 50, 150 ),
                Checked = true,
                AutoSize = true // ← ajuste automatiquement la largeur
            };

            var rbAdmin = new RadioButton
            {
                Text = "Compte Admin",
                Location = new Point( 200, 150 ),
                AutoSize = true
            };

            var btnRegister = new Button
            {
                Text = "S'inscrire",
                Location = new Point( 150, 200 ),
                Size = new Size( 100, 30 ),
                Font = new Font( "Arial", 10 )
            };
            btnRegister.Click += ( s, e ) =>
            {
                RegisterUser( txtLogin.Text, txtPassword.Text, rbAdmin.Checked );
                registerForm.Close();
            };

            registerForm.Controls.Add( lblLogin );
            registerForm.Controls.Add( txtLogin );
            registerForm.Controls.Add( lblPassword );
            registerForm.Controls.Add( txtPassword );
            registerForm.Controls.Add( rbClient );
            registerForm.Controls.Add( rbAdmin );
            registerForm.Controls.Add( btnRegister );

            registerForm.ShowDialog();
        }

        private void RegisterUser( string login, string password, bool isAdmin )
        {
            if (string.IsNullOrWhiteSpace( login ) || string.IsNullOrWhiteSpace( password ))
            {
                MessageBox.Show( "Veuillez entrer un login et un mot de passe valides", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }

            string dataFile = isAdmin ? AdminDataFile : ClientDataFile;

            var lines = File.ReadAllLines( dataFile );
            foreach (var line in lines)
            {
                if (line.StartsWith( login + ":" ))
                {
                    MessageBox.Show( "Ce login est déjà utilisé", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }
            }

            File.AppendAllText( dataFile, $"{login}:{password}\n" );

            MessageBox.Show( "Inscription réussie!", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information );
        }

        private void OpenAdminInterface( string username )
        {
            this.Hide();
            var adminForm = new AdminForm( username );
            adminForm.FormClosed += ( s, args ) => this.Show();
            adminForm.ShowDialog();
        }

        private void OpenClientInterface()
        {
            this.Hide();
            var clientForm = new ClientForm();
            clientForm.FormClosed += ( s, args ) => this.Show();
            clientForm.ShowDialog();
        }

        protected override void OnFormClosing( FormClosingEventArgs e )
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show( "Voulez-vous vraiment quitter l'application?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question );
                e.Cancel = (result == DialogResult.No);
            }
            base.OnFormClosing( e );
        }
    }
}
