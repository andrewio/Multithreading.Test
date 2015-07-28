using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

namespace CRT.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Работа с буфером и с флагами в монопольном режиме
        static readonly object _locker = new object();

        //Буфер для копирования порции данных из исходного файла в дубликат
        byte[] buffer;
        //Занят ли буфер
        bool isBufferFull = false;
        //Размер буфера
        int bufferSize;
        //Кол-во байтов, прочитанных за одну итерацию в потоку чтения  
        //и готовых для записи в потоке записи
        int numBytesForCopy = 0;

        string inputFilePath;
        string outputFilePath;
        int fileSize;

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

        private void buttonCopyFile_Click(object sender, RoutedEventArgs e)
        {
            //Начать копирование
            //--------------------------------
            inputFilePath = textBoxInputFile.Text;
            outputFilePath = textBoxOutputFile.Text;
            if (File.Exists(inputFilePath)) //Исходный файл существует
            {
                BlockUIOnCopy();

                //Задать размер буфера 
                bufferSize = int.Parse(textBoxBufferSize.Text);
                buffer = new byte[bufferSize];

                //поток 1 : чтение из файла и запись в буфер
                Thread reader = new Thread(ReadingHandler);
                reader.Start();

                //поток 2 : чтение из буфера и запись в файл
                Thread writer = new Thread(WritingHandler);
                writer.Start();
            }
            else
            {
                MessageBox.Show("Неверный путь к файлу!");
            }
            
        }

        void ReadingHandler()
        {
            using (FileStream fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                //сколько байтов осталось прочитать до конца файла
                fileSize = (int)fileStream.Length;
                int numBytesToRead = fileSize;
                
                while (numBytesToRead > 0)
                {
                    lock (_locker)
                    {
                        if (!isBufferFull)
                        {
                            //Читаем блок данных в буфер
                            numBytesForCopy = fileStream.Read(buffer, 0, bufferSize);
                            isBufferFull = true;
                        }
                    }

                    //уменьшаем число байтов, нужных для прочтения всего файла 
                    numBytesToRead -= numBytesForCopy;

                }//файл прочтен
                
            }

            MessageBox.Show("Копирование завершено.");

        }

        void WritingHandler()
        {
            using (FileStream destinationStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                lock (_locker)
                {
                    if (isBufferFull)
                    {
                        destinationStream.Write(buffer, 0, numBytesForCopy);
                        isBufferFull = false;
                    }
                }
            }

        }

        //Заблокировать UI для предотвращения редактирования во время копирования
        private void BlockUIOnCopy()
        {
            List<UIElement> mustLock =  new List<UIElement>()
            {
                textBoxBufferSize,
                textBoxInputFile,
                textBoxOutputFile,
                buttonCopyFile,
                buttonPickInputFile,
                buttonPickOutputFile
            };

            mustLock.ForEach(t => t.IsEnabled = false);
        }
        
   

        private void Window_Initialized(object sender, System.EventArgs e)
        {

#if DEBUG
            textBoxInputFile.Text = @"C:\Users\andrewio\Desktop\TESTs.txt";
            textBoxOutputFile.Text = AddMarkToFileName(textBoxInputFile.Text, "_copy");
#endif

        }
    }
}
