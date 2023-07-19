// CVBackup53
//
// Runs on a schedule to automate SQL server backups
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CVBackup
{
    class Program
    {
        static int VersionNumber = 53;
        static string DSN_CVMASTER = ConfigurationManager.ConnectionStrings["DSN_CVMASTER"].ConnectionString;
        static string LogFileName = Util.MergePathAndFile(Environment.CurrentDirectory, "backup.log");
        static string SQLBackupDirectory = ConfigurationManager.AppSettings["SQLBackupDirectory"].ToString();
        static string BackupDirectory = ConfigurationManager.AppSettings["BackupDirectory"].ToString();
        static string WinRar = ConfigurationManager.AppSettings["WinRar"].ToString();

        static string LastResult = "";

        static void Main(string[] args)
        {
            LogMessage("Main", "Starting backup.");

            RunBackup();
        }

        static void RunBackup()
        {
            Customer oCust = new Customer(DSN_CVMASTER);
            List<Customer> oCustomers = oCust.GetCustomers(VersionNumber);
            foreach (Customer oCustomer in oCustomers)
            {
                Console.WriteLine("Backing up customer " + oCustomer.Name);
                if(!BackupCustomer(oCustomer))
                {
                    LogMessage(oCustomer.Name, "ERROR Backing up customer " + LastResult);
                    continue;
                }
                if (!CreateArchive(oCustomer))
                {
                    LogMessage(oCustomer.Name, "ERROR creating archive " + LastResult);
                }
                Console.WriteLine("Back up complete for " + oCustomer.Name);
            }
            
        }

        static bool BackupCustomer(Customer oCustomer)
        {
            SqlConnection cn = new SqlConnection(DSN_CVMASTER);
            string BackupFilename = Path.Combine(SQLBackupDirectory, $"{oCustomer.Name}.bak");
            LogMessage(oCustomer.Name, $"Backing up Customer: {oCustomer.Name} to: {BackupFilename}");  
            try
            {
                File.Delete(BackupFilename);
                cn.Open();
                SqlCommand cmd = new SqlCommand($"BACKUP DATABASE {oCustomer.Name} TO DISK= N'{BackupFilename}' WITH NOFORMAT, INIT,  NAME = N'{oCustomer.Name}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10", cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception ex)
            {
                LastResult = ex.Message;
                return false;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return true;
        }

        static bool CreateArchive(Customer oCustomer)
        {
            Process oProcess = new Process();
            try
            {
                string ArchiveFilename = Path.Combine(BackupDirectory, $"{oCustomer.Name}_{DateTime.Today.ToString("yyyyMMdd")}.rar");
                string SQLBackupFilename = Path.Combine(SQLBackupDirectory, oCustomer.Name + ".bak");
                Config oConfig = new Config(oCustomer.DBDSN);
                string CustomerPath = oConfig.GetValue("CustomerPath");
                File.Delete(ArchiveFilename);
                ProcessStartInfo oStartInfo = new ProcessStartInfo(WinRar, "a \"" + ArchiveFilename + "\" \"" + SQLBackupFilename + "\" \"" + CustomerPath + "\"");
                oStartInfo.CreateNoWindow = true;
                oStartInfo.UseShellExecute = false;
                oStartInfo.RedirectStandardOutput = true;
                oProcess.StartInfo = oStartInfo;
                oProcess.Start();
                using (System.IO.StreamReader oStreamReader = oProcess.StandardOutput)  { 
                    while (!oStreamReader.EndOfStream) { 
                        string sOutput = oStreamReader.ReadLine();                        
                    }                
                }
                oProcess.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                LastResult = ex.Message;
                return false;
            }
        }
       

        static void LogMessage(string DBName, string Message)
        {            
            Message = DateTime.Now.ToString() + "\t" + Message.Replace("\r\n", "|") + "\r\n";
            File.AppendAllText(LogFileName, DBName + ":" + Message);
            Console.WriteLine(Message);
        }


    }
}
