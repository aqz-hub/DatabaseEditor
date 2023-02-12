using Microsoft.VisualBasic.FileIO;

namespace DatabaseEditor.Models
{
    internal class SourceField
    {
        public string Name { get; set; }
        public Field.FieldType Type { get; set; }
        public bool PrimaryKey { get; set; }
    }
}
