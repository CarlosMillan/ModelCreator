using System;
using System.Collections.Generic;
using Ding.Core;
using System.Text;
using System.Data;
using System.IO;

namespace ModelCreator
{
    public class Creator
    {
        private string _connectionstring;
        private string _filename;
        private string _namespace;
        private string _tablename;
        private Database _db;
        private DataTable _tableinfo;
        private StringBuilder _modelstring;

        public Creator(string connectionstring, string filename, string @namespace, string tablename) 
        {
            this._filename = filename;
            this._namespace = @namespace;
            this._tablename = tablename;
            this._connectionstring = connectionstring;
        }

        public bool LoadTableMetadata()
        {
            try
            {
                _db = Database.New(_connectionstring);
                _tableinfo = _db.GetTableFromQuery(@"SELECT 
                                                     COLUMN_NAME,DATA_TYPE,
                                                     COALESCE(CHARACTER_MAXIMUM_LENGTH, -1)  CHARACTER_MAXIMUM_LENGTH,
                                                     COALESCE(CHARACTER_OCTET_LENGTH, -1) CHARACTER_OCTET_LENGTH, 
                                                     COALESCE(NUMERIC_PRECISION, -1) NUMERIC_PRECISION,
                                                     COALESCE(NUMERIC_SCALE, -1) NUMERIC_SCALE
                                                     FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName", "@TableName", _tablename);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool MappingTable()
        {
            try
            {
                if (_tableinfo.Rows.Count > 0)
                {
                    string ClassName = _filename.Replace(Path.GetExtension(_filename), string.Empty);
                    _modelstring  = new StringBuilder();
                    _modelstring.AppendLine("using System;");
                    _modelstring.AppendLine("using Ding.Core;");
                    _modelstring.AppendLine();
                    _modelstring.AppendParameterizedFormat("namespace {Namespce}", "{Namespce}", _namespace);
                    _modelstring.AppendLine();
                    _modelstring.AppendLine("{");
                    _modelstring.Append("\t");
                    _modelstring.AppendParameterizedFormat(@"public class {ClassName}"
                                                           , "{ClassName}", ClassName);
                    _modelstring.AppendLine();
                    _modelstring.Append("\t{");                    
                    _modelstring.AppendLine();
                    
                    #region Content class
                    #region Fields
                    _modelstring.Append("\t\t");
                    _modelstring.AppendLine("#region Fields");

                    foreach (DataRow Row in _tableinfo.Rows)
                    {
                        _modelstring.Append("\t\t");
                        _modelstring.AppendParameterizedFormat(@"private {Type} {VariableName};"
                                                                , "{Type}", GetDotNetType(Row.S("DATA_TYPE"))
                                                                , "{VariableName}", GetFieldName(Row.S("COLUMN_NAME")));
                        _modelstring.AppendLine();
                    }

                    _modelstring.Append("\t\t");
                    _modelstring.AppendLine("#endregion");
                    #endregion

                    #region properties
                    _modelstring.AppendLine();
                    _modelstring.Append("\t\t");
                    _modelstring.AppendLine("#region Properties");

                    foreach (DataRow Row in _tableinfo.Rows)
                    {
                        _modelstring.Append("\t\t");
                        _modelstring.AppendParameterizedFormat(@"public {Type} {VariableName} { get { return {FieldName}; }}"
                                                                , "{Type}", GetDotNetType(Row.S("DATA_TYPE"))
                                                                , "{VariableName}", GetPropertyName(Row.S("COLUMN_NAME"))
                                                                , "{FieldName}", GetFieldName(Row.S("COLUMN_NAME")));
                        _modelstring.AppendLine();
                    }

                    _modelstring.Append("\t\t");
                    _modelstring.AppendLine("#endregion");
                    #endregion

                    #region Constructors
                    _modelstring.AppendLine();
                    _modelstring.Append("\t\t");
                    _modelstring.AppendParameterizedFormat("public {ClassName}(){}", "{ClassName}", ClassName);
                    _modelstring.AppendLine();
                    _modelstring.AppendLine();

                    StringBuilder ConstructorParams = new StringBuilder();

                    foreach (DataRow Row in _tableinfo.Rows)
                    {
                        ConstructorParams.AppendParameterizedFormat(@"{Type} {ParamName}, "
                                                                    , "{Type}", GetDotNetType(Row.S("DATA_TYPE"))
                                                                    , "{ParamName}", GetParamName(Row.S("COLUMN_NAME")));
                    }

                    ConstructorParams = ConstructorParams.Remove(ConstructorParams.Length - 2 , 2);

                    _modelstring.Append("\t\t");
                    _modelstring.AppendParameterizedFormat("public {ClassName}({ConstructorParams})", "{ClassName}", ClassName, "{ConstructorParams}", ConstructorParams.ToString());
                    _modelstring.AppendLine();
                    _modelstring.Append("\t\t");
                    _modelstring.AppendLine("{");
                    
                    foreach (DataRow Row in _tableinfo.Rows)
                    {
                        _modelstring.Append("\t\t\t");
                        _modelstring.AppendParameterizedFormat(@"this.{FieldName} = {ParamName};"
                                                                , "{FieldName}", GetFieldName(Row.S("COLUMN_NAME"))
                                                                , "{ParamName}", GetParamName(Row.S("COLUMN_NAME")));
                        _modelstring.AppendLine();
                    }
                    
                    _modelstring.Append("\t\t}");
                    
                    #endregion
                    #endregion

                    _modelstring.AppendLine();
                    _modelstring.Append("\t}");
                    _modelstring.AppendLine();
                    _modelstring.Append("}");                    

                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void WriteModel()
        {
            File.WriteAllText(@"C:\Models\" + _filename, _modelstring.ToString());
        }

        private object GetParamName(string p)
        {
            return p.ToLower();
        }

        private string GetPropertyName(string p)
        {
            return UppercaseFirst(p.ToLower());
        }

        private string GetFieldName(string p)
        {
            return String.Concat("_", GetParamName(p));
        }

        private string GetDotNetType(string p)
        {
            string TypeResult  = string.Empty;

            switch (p)
            { 
                case ETypes.UNIQUEIDENTIFIER:
                    TypeResult = "Guid";
                    break;

                case ETypes.NVARCHAR:
                    TypeResult = "string";
                    break;

                case ETypes.NCHAR:
                    TypeResult = "string";
                    break;

                case ETypes.DATETIME:
                    TypeResult = "DateTime";
                    break;

                case ETypes.NUMERIC:
                    TypeResult = "decimal";
                    break;

                case ETypes.IMAGE:
                    TypeResult = "byte[]";
                    break;

                case ETypes.XML:
                    TypeResult = "byte[]";
                    break;

                case ETypes.BIT:
                    TypeResult = "bool";
                    break;
                default:
                    TypeResult = "object";
                    break;
            }

            return TypeResult;
        }

        private string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
