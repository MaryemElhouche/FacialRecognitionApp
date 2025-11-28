using System;
using System.Windows.Forms;

namespace FacialRecognitionApp
{
    internal static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            // Créer une instance de Form1 (notre formulaire de sélection)
            var mainForm = new Form1();
            Application.Run( mainForm );
        }
    }
}