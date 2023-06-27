﻿using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Practice
{
    public partial class MainForm : Form
    {
        private readonly string JsonFilePath = $"{Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))}Users\\{Environment.UserName}\\data.json";
        private List<Dictionary<string, string>> jsonData;
        private readonly Stack<string> backStack = new Stack<string>();
        private readonly Stack<string> forwardStack = new Stack<string>();
        private string currentDirectory = "home";

        private readonly string encryptionKey = "YourEncryptionKey"; // Replace with your own encryption key

        public MainForm()
        {
            InitializeComponent();
            LoadData();
                
        }

        private void LoadData()
        {
            listView.Items.Clear();
            addressBar.Text = "Home";
            toolStripStatusLabelFullPath.Text = "";

            try
            {
                if (File.Exists(JsonFilePath))
                {
                    string encryptedContent = File.ReadAllText(JsonFilePath);
                    string decryptedContent = DecryptData(encryptedContent, encryptionKey);

                    jsonData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(decryptedContent);

                    foreach (Dictionary<string, string> path in jsonData)
                    {
                        if (Directory.Exists(path["FullPath"]))
                        {
                            ListViewItem item = listView.Items.Add(new DirectoryInfo(path["FullPath"]).Name);
                            item.SubItems.Add(path["Type"]);
                            item.SubItems.Add("");
                            item.ImageIndex = 1;
                            item.Tag = path["FullPath"];
                            item.ToolTipText = $"Size : N/A\n" +
                                                $"Status : Hidden";
                        }
                        else if (File.Exists(path["FullPath"]))
                        {
                            ListViewItem item = listView.Items.Add(new FileInfo(path["FullPath"]).Name);
                            item.SubItems.Add(path["Type"]);
                            item.SubItems.Add(new FileInfo(path["FullPath"]).Length.ToString());
                            item.ImageIndex = 0;
                            item.Tag = path["FullPath"];

                            item.ToolTipText = $"Size : {new FileInfo(path["FullPath"]).Length}\n" +
                                                $"Status : Hidden";
                        }
                    }
                }
                else
                {
                    jsonData = new List<Dictionary<string, string>>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveData()
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                string encryptedContent = EncryptData(jsonContent, encryptionKey);
                File.WriteAllText(JsonFilePath, encryptedContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddItemToJsonData(string fullPath, string type)
        {
            try
            {
                Dictionary<string, string> newItem = new Dictionary<string, string>
                {
                    { "FullPath", fullPath },
                    { "Type", type }
                };

                jsonData.Add(newItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding an item to JSON data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveItemFromJsonData(string fullPath)
        {
            try
            {
                jsonData.RemoveAll(item => item["FullPath"] == fullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing an item from JSON data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Encrypt data using AES algorithm
        private static string EncryptData(string data, string key)
        {
            byte[] encryptedBytes;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.GenerateIV();

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                        csEncrypt.Write(dataBytes, 0, dataBytes.Length);
                    }
                    encryptedBytes = msEncrypt.ToArray();
                }
            }
            return Convert.ToBase64String(encryptedBytes);
        }

        // Decrypt data using AES algorithm
        private static string DecryptData(string encryptedData, string key)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
            byte[] iv = new byte[16];
            Array.Copy(encryptedBytes, iv, iv.Length);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        private void RemoveSelectedItems()
        {
            try
            {
                foreach (ListViewItem selectedItem in listView.SelectedItems)
                {
                    string itemPath = selectedItem.Tag.ToString();

                    // Remove system and hidden attributes
                    FileAttributes currentAttributes = File.GetAttributes(itemPath);
                    FileAttributes newAttributes = currentAttributes & ~(FileAttributes.System | FileAttributes.Hidden);
                    File.SetAttributes(itemPath, newAttributes);

                    selectedItem.Remove();

                    RemoveItemFromJsonData(itemPath);
                }

                SaveData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddFilesOrFolders(bool onlyFolder)
        {
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog
            {
                Multiselect = true,
                IsFolderPicker = onlyFolder
            };

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        SetSystemAndHiddenAttributes(filePath);

                        FileSystemInfo fileInfo = onlyFolder ? (FileSystemInfo)new DirectoryInfo(filePath) : new FileInfo(filePath);

                        ListViewItem item = new ListViewItem(fileInfo.Name);
                        item.SubItems.Add(onlyFolder ? "Folder" : "File");
                        item.SubItems.Add(onlyFolder ? "" : ((FileInfo)fileInfo).Length.ToString());
                        item.SubItems.Add("Hidden");
                        item.ImageIndex = onlyFolder ? 1 : 0;
                        item.Tag = fileInfo.FullName;
                        listView.Items.Add(item);

                        AddItemToJsonData(fileInfo.FullName, onlyFolder ? "Folder" : "File");
                    }

                    SaveData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while adding files or folders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SetSystemAndHiddenAttributes(string filePath)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                attributes |= FileAttributes.System | FileAttributes.Hidden;
                File.SetAttributes(filePath, attributes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while setting system and hidden attributes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (backStack.Count > 0)
                {
                    forwardStack.Push(currentDirectory);
                    addressBar.Text = currentDirectory = backStack.Pop();
                    if (currentDirectory == "home")
                        LoadData();
                    else
                        PopulateListView(currentDirectory);
                    forwardButton.Enabled = true;
                }

                backButton.Enabled = backStack.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while navigating back: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (forwardStack.Count > 0)
                {
                    backStack.Push(currentDirectory);
                    addressBar.Text = currentDirectory = forwardStack.Pop();
                    PopulateListView(currentDirectory);
                    backButton.Enabled = true;
                }

                forwardButton.Enabled = forwardStack.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while navigating forward: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NavigateToDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(currentDirectory))
                {
                    backStack.Push(currentDirectory);
                    backButton.Enabled = true;
                }

                addressBar.Text = currentDirectory = path;
                forwardStack.Clear();
                forwardButton.Enabled = false;
                PopulateListView(currentDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while navigating to the directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        public void PopulateListView(string path)
        {
            listView.BeginUpdate();
            listView.Items.Clear();
            toolStripStatusLabelFullPath.Text = "";

            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                DirectoryInfo[] directories = directoryInfo.GetDirectories();
                FileInfo[] files = directoryInfo.GetFiles();

                foreach (DirectoryInfo directory in directories)
                {
                    ListViewItem item = listView.Items.Add(directory.Name);

                    item.Tag = directory.FullName;
                    item.ImageIndex = 1;
                    item.SubItems.AddRange(new[]
                    {
                        "Folder",
                        string.Empty,
                        directory.LastWriteTime.ToString()
                    });

                    FileAttributes attributes = File.GetAttributes(directory.FullName);
                    bool hasHiddenAndSystemAttributes = (attributes & (FileAttributes.Hidden | FileAttributes.System)) ==
                                                        (FileAttributes.Hidden | FileAttributes.System);

                    item.ToolTipText = $"Size: N/A\nStatus: {(hasHiddenAndSystemAttributes ? "Hidden" : "Visible")}";
                }

                foreach (FileInfo file in files)
                {
                    ListViewItem item = listView.Items.Add(file.Name);

                    item.Tag = file.FullName;
                    item.ImageIndex = 0;
                    item.SubItems.AddRange(new[]
                    {
                        file.Extension,
                        file.Length.ToString(),
                        file.LastWriteTime.ToString()
                    });

                    FileAttributes attributes = File.GetAttributes(file.FullName);
                    bool hasHiddenAndSystemAttributes = (attributes & (FileAttributes.Hidden | FileAttributes.System)) ==
                                                        (FileAttributes.Hidden | FileAttributes.System);

                    item.ToolTipText = $"Size: {file.Length}\nStatus: {(hasHiddenAndSystemAttributes ? "Hidden" : "Visible")}";
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            listView.EndUpdate();
        }

        private void AddFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddFilesOrFolders(false);
        }

        private void AddFoldersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddFilesOrFolders(true);
        }

        private void RemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveSelectedItems();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (currentDirectory == "home")
                LoadData();
            else
                PopulateListView(currentDirectory);
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                ListView listView = sender as ListView;
                if (listView.SelectedItems.Count == 1)
                {
                    ListViewItem selectedItem = listView.SelectedItems[0];
                    toolStripStatusLabelFullPath.Text = $"Path: {selectedItem.Tag}";
                }
                else
                {
                    toolStripStatusLabelFullPath.Text = "";
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error in Select Index Changed + {ex.Message}", "Error");
            }
        }

        private void ListView_ItemActivate(object sender, EventArgs e)
        {
            try
            {
                if (listView.SelectedItems[0].Tag is string path)
                {

                    if (File.Exists(path))
                    {
                        Process.Start(path);
                    }
                    else
                    {
                        NavigateToDirectory(path);
                        addressBar.Text = path;

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while opening the file or directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}