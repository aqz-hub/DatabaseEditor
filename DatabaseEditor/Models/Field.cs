using System.ComponentModel.DataAnnotations;

namespace DatabaseEditor.Models
{
    internal class Field
    {
        public enum FieldType
        {
            Integer,
            Decimal,
            Tinyint,
            SmallInt,
            Numeric,
            Bigint,
            Bit,
            Money,
            Smallmoney,
            Float,
            Real,
            Date,
            Datetime2,
            Datetime,
            Datetimeoffset,
            Smalldatetime,
            Time,
            Char,
            Varchar,
            Nvarchar
        }
        [Display(Name = "Название")]
        public string Name { get; set; }
        [Display(Name = "Тип данных")]
        public FieldType Type { get; set; }
        [Display(Name = "Первичный ключ")]
        public bool PrimaryKey { get; set; }
        public static bool ParsePrimaryKey(int position)
        {
            if (position == 1)
                return true;
            else
                return false;
        }

        public static FieldType ParseType(string value)
        {
            switch(value)
            {
                case "int":
                    return FieldType.Integer;
                case "datetime":
                    return FieldType.Datetime;
                case "nvarchar":
                    return FieldType.Nvarchar;
                case "decimal":
                    return FieldType.Decimal;
                case "bit":
                    return FieldType.Bit;
                case "tinyint":
                    return FieldType.Tinyint;
                case "smallint":
                    return FieldType.SmallInt;
                case "bigint":
                    return FieldType.Bigint;
                case "numeric":
                    return FieldType.Numeric;
                case "smallmoney":
                    return FieldType.Smallmoney;
                case "money":
                    return FieldType.Money;
                case "float":
                    return FieldType.Float;
                case "real":
                    return FieldType.Real;
                case "date":
                    return FieldType.Date;
                case "datetime2":
                    return FieldType.Datetime2;
                case "datetimeoffset":
                    return FieldType.Datetimeoffset;
                case "smalldatetime":
                    return FieldType.Smalldatetime;
                case "time":
                    return FieldType.Time;
                case "char":
                    return FieldType.Char;
                case "varchar":
                    return FieldType.Varchar;
                default:
                    return FieldType.Nvarchar;
            }
        }
        public static string ParseType(FieldType type)
        {
            switch (type)
            {
                case FieldType.Integer:
                    return "int";
                case FieldType.Datetime:
                    return "datetime";
                case FieldType.Nvarchar:
                    return "nvarchar";
                case FieldType.Decimal:
                    return "decimal";
                case FieldType.Bigint:
                    return "bigint";
                case FieldType.Bit:
                    return "bit";
                case FieldType.Tinyint:
                    return "tinyint";
                case FieldType.SmallInt:
                    return "smallint";
                case FieldType.Date:
                    return "date";
                case FieldType.Time:
                    return "time";
                case FieldType.Datetimeoffset:
                    return "datetimeoffset";
                case FieldType.Smalldatetime:
                    return "smalldatetime";
                case FieldType.Datetime2:
                    return "datetime2";
                case FieldType.Money:
                    return "money";
                case FieldType.Smallmoney:
                    return "smallmoney";
                case FieldType.Char:
                    return "char";
                case FieldType.Varchar:
                    return "varchar";
                case FieldType.Numeric:
                    return "numeric";
                case FieldType.Real:
                    return "real";
                case FieldType.Float:
                    return "float";
                default:
                    return "nvarchar";
            }
        }
    }
}
