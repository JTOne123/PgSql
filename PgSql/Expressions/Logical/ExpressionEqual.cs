using System;
using System.Collections.Generic;
using System.Linq;
using doob.PgSql.ExtensionMethods;
using Newtonsoft.Json.Linq;

namespace doob.PgSql.Expressions.Logical
{
    public class ExpressionEqual : ExpressionBase
    {
        private Type valueType;

        public ExpressionEqual(string propertyName, object value) : base(propertyName)
        {
            valueType = value.GetType();

            SetValue("eq", value);
        }


        protected override void _getSqlCommand(Column column)
        {
            var props = ColumnName.Split(".".ToCharArray());
            var collName = $"\"{props[0]}\"";

            _getSqlCommandForPostgres(column);
                   
        }

        private void _getSqlCommandForPostgres(Column column)
        {

            var props = ColumnName.Split(".".ToCharArray());
            var collName = $"\"{props[0]}\"";


            var propertiesList = new List<string>();
            string searchstr = string.Empty;
            string comm = String.Empty;

            var jTokenType = column?.Properties?.DotNetType?.GetJTokenType();
            switch (jTokenType)
            {
                case JTokenType.Array when jTokenType == JTokenType.Object:

                    for (int i = 1; i < props.Length - 1; i++)
                    {
                        propertiesList.Add(props[i]);
                    }

                    if (propertiesList.Any())
                    {
                        searchstr = $"->{String.Join("->", propertiesList.Select(s => $"'{s}'"))}";
                    }

                    searchstr = $"{searchstr}->>'{props.Last()}'";


                    comm = $"exists (Select 1 from unnest({collName}) as objects where objects{searchstr} = @{GetId("eq")})";

                    OverrideValueType("eq", "text");
                    SetCommand(comm);
                    return;

                case JTokenType.Object:

                    if (props.Length == 1)
                    {
                        break;
                    }

                    for (int i = 1; i < props.Length - 1; i++)
                    {
                        propertiesList.Add(props[i]);
                    }

                    if (propertiesList.Any())
                    {
                        searchstr = $"->{String.Join("->", propertiesList.Select(s => $"'{s}'"))}";
                    }

                    searchstr = $"{searchstr}->>'{props.Last()}'";

                    comm = $"{collName}{searchstr} = @{GetId("eq")}";
                    OverrideValueType("eq", "text");
                    SetCommand(comm);
                    return;
            }

            SetCommand($"{collName} = @{GetId("eq")}");
        }
    }
}