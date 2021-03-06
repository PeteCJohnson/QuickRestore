﻿
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using Settings = QuickRestore.Properties.Settings;

namespace QuickRestore
{
    public class DatabaseRestore
    {
        //SMO Code http://www.mssqltips.com/sqlservertip/1849/backup-and-restore-sql-server-databases-programmatically-with-smo/

        private static readonly ManualResetEvent Sync = new ManualResetEvent(false);
        private static SqlConnectionStringBuilder _sqlConnectionStringBuilder;
        private static SqlConnection _sqlConnection;
        private const string SetDatabaseSingleUserCommandText = "ALTER DATABASE {0} SET {1} WITH ROLLBACK IMMEDIATE";
        private static Settings _settings;
        private static DateTime _startTime;

        internal static void Restore(Settings settings)
        {
            _settings = settings;

            _sqlConnectionStringBuilder = new SqlConnectionStringBuilder(string.Format("Server={0};Database=Master;Trusted_Connection=True;", _settings.Server));

            var filename = string.IsNullOrWhiteSpace(settings.RestoreFilename)
                ? _settings.GetBackupFileName()
                : settings.RestoreFilename;
            var backupPath = Path.Combine(_settings.BackupFolder, filename);

            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException(string.Format("Cannot find the backup file '{0}'", backupPath));
            }

            var restoreDb = CreateRestore(backupPath);

            var connection = SetSingleUser(true);
            var serverConnection = new ServerConnection(connection);
            var server = new Server(serverConnection);

            ProgressBar.SetupProgressBar("RESTORE " + _settings.DatabaseName);

            _startTime = DateTime.Now;
            restoreDb.SqlRestoreAsync(server);

            Sync.WaitOne();

            SetSingleUser(false);

            Cleanup(restoreDb);
        }

        private static Restore CreateRestore(string backupPath)
        {
            var restoreDb = new Restore
            {
                Database = _settings.DatabaseName,
                Action = RestoreActionType.Database
            };

            restoreDb.Devices.AddDevice(backupPath, DeviceType.File);
            restoreDb.ReplaceDatabase = true;

            restoreDb.PercentComplete += Restore_PercentComplete;
            restoreDb.Complete += Restore_Complete;

            return restoreDb;
        }

        private static void Cleanup(Restore restoreDb)
        {
            _sqlConnection.Dispose();
            restoreDb.PercentComplete -= Restore_PercentComplete;
            restoreDb.Complete -= Restore_Complete;
        }

        private static SqlConnection SetSingleUser(bool singleUser)
        {
            var commandText = string.Format(SetDatabaseSingleUserCommandText, _settings.DatabaseName, singleUser ? "SINGLE_USER" : "MULTI_USER");

            if (_sqlConnection == null)
            {
                _sqlConnection = new SqlConnection(_sqlConnectionStringBuilder.ToString());
            }

            using (var command = new SqlCommand(commandText, _sqlConnection))
            {
                if (_sqlConnection.State == ConnectionState.Closed)
                {
                    _sqlConnection.Open();
                }

                command.ExecuteNonQuery();

                if (_sqlConnection.State == ConnectionState.Open)
                {
                    _sqlConnection.Close();
                }
            }

            return _sqlConnection;
        }

        private static void Restore_Complete(object sender, ServerMessageEventArgs e)
        {
            var endTime = DateTime.Now;
            var durationSeconds = endTime.Subtract(_startTime).Seconds;
            Console.WriteLine(string.Empty);
            var message = string.Format("Restore Complete ({0} seconds)", durationSeconds);
            Console.WriteLine(message.PadBoth(ProgressBar.Bar.Length));
            Sync.Set();
        }

        private static void Restore_PercentComplete(object sender, PercentCompleteEventArgs e)
        {
            ProgressBar.IncrementProgressBar();
        }
    }
}
