using DatabaseEditor.Services;
using System.Windows.Controls;
using DatabaseEditor.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Linq;

namespace DatabaseEditor.CustomComponents
{
    /// <summary>
    /// Логика взаимодействия для Editor.xaml
    /// </summary>
    public partial class TableEditor : UserControl, INotifyPropertyChanged
    {
        #region Binding
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private Visibility _renameVisibility;
        public Visibility RenameVisibility
        {
            get { return _renameVisibility; }
            set
            {
                _renameVisibility = value;
                OnPropertyChanged("RenameVisibility");
            }
        }
        private Visibility _saveVisibility;
        public Visibility SaveVisibility
        {
            get { return _saveVisibility; }
            set
            {
                _saveVisibility = value;
                OnPropertyChanged("SaveVisibility");
            }
        }
        private string _tableName;
        public string TableName
        {
            get { return _tableName; }
            set
            {
                _tableName = value;
                OnPropertyChanged("TableName");
            }
        }
        #endregion

        private SqlService sqlService { get; set; }
        private List<Field> Fields { get; set; }
        private List<SourceField> SourceFields { get; set; } = new List<SourceField>();
        private List<Field> ChangedFields { get; set; }

        public TableEditor(string tableName)
        {
            InitializeComponent();
            DataContext = this;

            TableName = tableName;
            RenameVisibility = Visibility.Hidden;
            SaveVisibility = Visibility.Hidden;

            GetFields();
        }

        private async void GetFields()
        {
            sqlService = await SqlService.GetInstance();
            Fields = await sqlService.GetTableFields(TableName);
            if (Fields == null)
            {
                return;
            }
            tableFields.ItemsSource = Fields;
            SourceFields.Clear();
            foreach (var field in Fields)
            {
                SourceFields.Add(new SourceField() { Name = field.Name, PrimaryKey = field.PrimaryKey, Type = field.Type });
            }
        }

        private void ClearParent()
        {
            var parent = (StackPanel)VisualTreeHelper.GetParent(this);
            parent.Children.Clear();
        }

        private void RenameTable()
        {
            RenameVisibility = Visibility.Visible;
            renameButton.Visibility = Visibility.Hidden;
            newNameBox.Text = TableName;
        }

        private async Task SaveName()
        {
            var result = MessageBox.Show("Вы действительно хотите сохранить изменения?", "Сохранение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                if (await sqlService.RenameTable(TableName, newNameBox.Text.Trim()))
                {
                    MainWindow.UpdateTables(TableName, newNameBox.Text.Trim());
                    TableName = newNameBox.Text;
                }
            }
            RenameVisibility = Visibility.Hidden;
            renameButton.Visibility = Visibility.Visible;
        }

        private async Task SaveFields()
        {
            var result = MessageBox.Show("Вы действительно хотите сохранить изменения?", "Сохранение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ChangedFields = (List<Field>)tableFields.Items.SourceCollection;
                if(ChangedFields.Where(x => x.PrimaryKey).Count() > 1)
                {
                    MessageBox.Show("Выбрано два или более первичных ключей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    SaveVisibility = Visibility.Hidden;
                    if (await sqlService.SaveFields(TableName, SourceFields, ChangedFields))
                    {
                        GetFields();
                    }
                }
            }
        }

        private async Task DeleteTable()
        {
            var result = MessageBox.Show("Вы действительно хотите удалить таблицу?\nДанное действие нельзя будет отменить!", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                if (await sqlService.DeleteTable(TableName))
                {
                    MainWindow.RemoveTable(TableName);
                    ClearParent();
                }
            }
        }

        private void renameButton_Click(object sender, RoutedEventArgs e)
        {
            RenameTable();
        }

        private async void saveNameButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveName();
        }

        private async void saveFieldsButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveFields();
        }

        private void tableFields_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            SaveVisibility = Visibility.Visible;
        }

        private async void deleteTableButton_Click(object sender, RoutedEventArgs e)
        {
            await DeleteTable();
        }
    }
}

//TODO выбор только одного поля для primarykey, сделать нормальный интерфейс, рефакторинг кода = залить в гит
