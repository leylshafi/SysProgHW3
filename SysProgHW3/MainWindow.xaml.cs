using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;

namespace SysProgHW3;

public partial class MainWindow : Window
{
    private CancellationTokenSource? cts;
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void btnFile_Click(object sender, RoutedEventArgs e)
    {
        FileDialog dialog = new OpenFileDialog();
        if (dialog.ShowDialog() == true)
            filePath.Text = dialog.FileName;
    }

    private void btnStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            progress.Value = 0;

            cts = new CancellationTokenSource();

            if (btnEnc.IsChecked == true)
                Encrypt(cts.Token);

            if (btnDec.IsChecked == true)
                Decrypt(cts.Token);

        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
    private void Encrypt(CancellationToken token)
    {
        var text = File.ReadAllText(filePath.Text);
        var key = Encoding.UTF8.GetBytes(password.Password);

        var bytesToWrite = EncryptString(text, key, key);
        var fs = new FileStream(filePath.Text, FileMode.Truncate);

        ThreadPool.QueueUserWorkItem(o =>
        {
            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                if (i % 32 == 0)
                {
                    if (token.IsCancellationRequested)
                    {
                        fs.Dispose();
                        Dispatcher.Invoke(() => File.WriteAllText(filePath.Text, text));
                        Dispatcher.Invoke(() => progress.Value = 0);
                        return;
                    }

                    Thread.Sleep(1000);
                    if (i != 0)
                        Dispatcher.Invoke(() => progress.Value = 100 * i / bytesToWrite.Length);
                }
                fs.WriteByte(bytesToWrite[i]);
            }

            fs.Seek(0, SeekOrigin.Begin);

            Dispatcher.Invoke(() => progress.Value = 100);
        });
    }
    private static byte[] EncryptString(string original, byte[] key, byte[] IV)
    {
        byte[] encrypted;
        using (var encryption = Aes.Create())
        {
            encryption.Key = key;
            encryption.IV = IV;


            ICryptoTransform encryptor = encryption.CreateEncryptor(encryption.Key, encryption.IV);

            using var msEncrypt = new MemoryStream();
            using var cEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            using (var swEncrypt = new StreamWriter(cEncrypt))
                swEncrypt.Write(original);

            encrypted = msEncrypt.ToArray();
        }

        return encrypted;
    }

    private void Decrypt(CancellationToken token)
    {
        var bytes = File.ReadAllBytes(filePath.Text);
        var key = Encoding.UTF8.GetBytes(password.Password);

        var text = DecryptString(bytes, key, key);
        var bytesToWrite = Encoding.UTF8.GetBytes(text);
        var fs = new FileStream(filePath.Text, FileMode.Truncate);

        ThreadPool.QueueUserWorkItem(o =>
        {
           

            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                if (i % 32 == 0)
                {
                    if (token.IsCancellationRequested)
                    {
                        fs.Dispose();
                        Dispatcher.Invoke(() => File.WriteAllBytes(filePath.Text, bytes));
                        Dispatcher.Invoke(() => progress.Value = 0);
                        return;
                    }

                    Thread.Sleep(1000);
                    if (i != 0)
                        Dispatcher.Invoke(() => progress.Value = 100 * i / bytesToWrite.Length);
                }
                fs.WriteByte(bytesToWrite[i]);
            }

            fs.Seek(0, SeekOrigin.Begin);

            Dispatcher.Invoke(() => progress.Value = 100);
        });
    }

    private static string DecryptString(byte[] encrypted, byte[] key, byte[] IV)
    {
        string plaintext = string.Empty;

        using (var encryption = Aes.Create())
        {
            encryption.Key = key;
            encryption.IV = IV;

            ICryptoTransform decryptor = encryption.CreateDecryptor(encryption.Key, encryption.IV);

            using MemoryStream msDecrypt = new MemoryStream(encrypted);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                plaintext = srDecrypt.ReadToEnd();
            }
        }
        //leylaShafiyeva27

        return plaintext;
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e) => cts?.Cancel();

}
