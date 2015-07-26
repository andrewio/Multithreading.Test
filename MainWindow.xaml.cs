using System.IO;
using System.Windows;

namespace CRT.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonPickInputFile_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                textBoxInputFile.Text = dlg.FileName;
                textBoxOutputFile.Text = AddMarkToFileName(dlg.FileName, "_copy");
            }
        }

        public string AddMarkToFileName(string filePath, string mark)
        {
            if (filePath != string.Empty)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                return filePath.Replace(fileName, fileName + mark);
            }
            else
            {
                return string.Empty;
            }
        }

        private void buttonPickOutputFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = AddMarkToFileName(textBoxInputFile.Text, "_copy");
            dlg.DefaultExt = ".txt"; 
            dlg.Filter = "Text documents (.txt)|*.txt";
            
            bool? result = dlg.ShowDialog();
            
            if (result == true)
            {
                textBoxOutputFile.Text = dlg.FileName;
            }
        }
        
        private void textBoxBufferSize_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            //Поддерживаем только цифровой ввод
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;

            }
        }
        
    }
}
