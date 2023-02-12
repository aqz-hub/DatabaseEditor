using DatabaseEditor.Models;
using DatabaseEditor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace DatabaseEditor.CustomComponents
{
    /// <summary>
    /// Логика взаимодействия для TableCreator.xaml
    /// </summary>
    public partial class TableCreator : UserControl
    {
        private SqlService sqlService { get; set; }
        private List<Field> Fields { get; set; } = new List<Field>();
        private string TableName { get; set; }
        public TableCreator()
        {
            InitializeComponent();
            InitializeTable();
        }

        private void ClearParent()
        {
            var parent = (StackPanel)VisualTreeHelper.GetParent(this);
            parent.Children.Clear();
        }

        private async void InitializeTable()
        {
            sqlService = await SqlService.GetInstance();
            var id = new Field()
            {
                Name = "Id",
                Type = Field.FieldType.Integer,
                PrimaryKey = true
            };
            Fields.Add(id);
            tableFields.ItemsSource = Fields;
        }

        private async Task SaveTable()
        {
            TableName = newNameBox.Text.Trim();
            bool saved = await sqlService.CreateTable(TableName, Fields);
            if(saved)
            {
                MainWindow.AddTable(TableName);
                ClearParent();
            }
        }

        private async void saveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveTable();
        }
    }
}
