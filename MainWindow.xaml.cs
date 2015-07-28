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
        //Сколько осталось обработать байтов для завершения копирования
        int numBytesToProcess = 0;
        //Происходит ли копирование 
        bool isCopying = false;
        bool isCanReading = true;
        bool isCanWriting = true;

        string inputFilePath;
        string outputFilePath;
        int fileSize;

        List<UIElement> mustLockOnCopy;

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
            isCopying = true;

            inputFilePath = textBoxInputFile.Text;
            outputFilePath = textBoxOutputFile.Text;

            //сколько байтов осталось прочитать до конца файла
            FileInfo fileInfo = new FileInfo(inputFilePath);
            fileSize = (int)fileInfo.Length;
            numBytesToProcess = fileSize;
            
            progressBarFileCopying.Minimum = 0;
            progressBarFileCopying.Maximum = fileSize;

            progressBarBufferState.Minimum = 0;
            progressBarBufferState.Maximum = bufferSize;

            numBytesForCopy = 0;
            isBufferFull = false;

            if (File.Exists(inputFilePath)) //Исходный файл существует
            {
                SetEnabledStateForCriticalUI(false);

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
                
                while (numBytesToProcess > 0)
                {
                    lock (_locker)
                    {
                        if (!isBufferFull && isCanReading)
                        {
                            //Читаем блок данных в буфер
                            numBytesForCopy = fileStream.Read(buffer, 0, bufferSize);
                            isBufferFull = true;

                            //уменьшаем число байтов, нужных для прочтения всего файла 
                            numBytesToProcess -= numBytesForCopy; 
                        }
                    }

                    ShowBufferState();

                }//файл прочтен

            }
        }

        void WritingHandler()
        {
            using (FileStream destinationStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                //Пока есть байты для записи
                while (isCopying)
                {
                    lock (_locker)
                    {
                        if (isBufferFull && isCanWriting)
                        {
                            destinationStream.Write(buffer, 0, numBytesForCopy);
                            isBufferFull = false;
                            if (numBytesToProcess == 0)
                                isCopying = false;
                        }
                    }
                    ShowCopyingProgress();

                }
            }

            SetEnabledStateForCriticalUI(true);

        }

        //Заблокировать UI для предотвращения редактирования во время копирования
        private void SetEnabledStateForCriticalUI(bool isUnlocked)
        {
            Dispatcher.Invoke(() =>
            {
                mustLockOnCopy.ForEach(t => t.IsEnabled = isUnlocked);
            });

        }

        void ShowCopyingProgress()
        {
            Dispatcher.Invoke(() =>
            {
                progressBarFileCopying.Value = fileSize - numBytesToProcess;
            });
        }

        void ShowBufferState()
        {
            Dispatcher.Invoke(() =>
            {
                progressBarBufferState.Value = numBytesForCopy;
            });
        }

        private void Window_Initialized(object sender, System.EventArgs e)
        {

#if DEBUG
            textBoxInputFile.Text = @"C:\Users\andrewio\Desktop\TESTs.txt";
            textBoxOutputFile.Text = AddMarkToFileName(textBoxInputFile.Text, "_copy");
#endif
            mustLockOnCopy = new List<UIElement>()
            {
                textBoxBufferSize,
                textBoxInputFile,
                textBoxOutputFile,
                buttonCopyFile,
                buttonPickInputFile,
                buttonPickOutputFile
            };

        }
    }
}
