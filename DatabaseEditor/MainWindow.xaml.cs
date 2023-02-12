using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DatabaseEditor.CustomComponents;
using DatabaseEditor.Services;
using static System.Net.Mime.MediaTypeNames;

namespace DatabaseEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ObservableCollection<string> DatabaseTables { get; set; } = new ObservableCollection<string>();
        public static ObservableCollection<string> Log { get; set; } = new ObservableCollection<string>();
        private static SqlService sqlService { get; set; }
        private static ObservableCollection<string> Databases { get; set; } = new ObservableCollection<string>();
        private bool CanChangeDatabase { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            InitDataBase();
        }

        private async void InitDataBase()
        {
            sqlService = await SqlService.GetInstance();
            Databases = await sqlService.GetDatabases();
            RemoveSystemDatabases();
            databaseBox.ItemsSource = Databases;
            if (Databases.Count > 0)
            {
                sqlService.CurrentDatabase = Databases.FirstOrDefault();
                databaseBox.SelectedItem = sqlService.CurrentDatabase;
                await sqlService.OpenConnection();
                DatabaseTables = await sqlService.GetTables();
                tablesList.ItemsSource = DatabaseTables;
                logList.ItemsSource = Log;
            }
            CanChangeDatabase = true;
        }

        public static void AddTable(string tableName)
        {
            DatabaseTables.Add(tableName);
        }
        public static void UpdateTables(string oldName, string newName)
        {
            int i = DatabaseTables.IndexOf(oldName);
            DatabaseTables[i] = newName;
        }
        public static void RemoveTable(string tableName)
        {
            DatabaseTables.Remove(tableName);
        }
        private static void RemoveSystemDatabases()
        {
            if(Databases.Count > 0)
            {
                Databases.Remove("master");
                Databases.Remove("tempdb");
                Databases.Remove("msdb");
                Databases.Remove("model");
            }
        }

        private async Task ChangeDatabase(SelectionChangedEventArgs e)
        {
            tableEditorPanel.Children.Clear();

            await sqlService.CloseConnection();
            sqlService.CurrentDatabase = e.AddedItems[0].ToString();
            await sqlService.OpenConnection();

            DatabaseTables.Clear();
            Log.Clear();
            DatabaseTables = await sqlService.GetTables();
            tablesList.ItemsSource = DatabaseTables;
            logList.ItemsSource = Log;
        }

        private void tablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var editor = new TableEditor(e.AddedItems[0].ToString());
                editor.Margin = new Thickness(5, 0, 0, 0);
                tableEditorPanel.Children.Clear();
                tableEditorPanel.Children.Add(editor);
            }
        }

        private void createTableButton_Click(object sender, RoutedEventArgs e)
        {
            var creator = new TableCreator();
            creator.Margin = new Thickness(5, 0, 0, 0);
            tableEditorPanel.Children.Clear();
            tableEditorPanel.Children.Add(creator);
        }

        private async void databaseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CanChangeDatabase)
                await ChangeDatabase(e);
        }
    }
}
